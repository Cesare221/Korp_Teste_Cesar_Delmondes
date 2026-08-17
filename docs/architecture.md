# Arquitetura

## Componentes

O sistema é composto por:

- `frontend`: aplicação Angular standalone.
- `services/inventory-service`: ASP.NET Core Web API do dominio de estoque.
- `services/billing-service`: ASP.NET Core Web API do dominio de faturamento.
- `inventory_db`: PostgreSQL exclusivo do Inventory.
- `billing_db`: PostgreSQL exclusivo do Billing.

```text
Angular
   |
   |---- REST ---- Inventory Service ---- inventory_db
   |
   `---- REST ---- Billing Service ------ billing_db
                         |
                         `---- REST ---- Inventory Service
```

Cada microsserviço possui pastas internas de `Domain`, `Application`, `Contracts`, `Controllers` e `Infrastructure`. As camadas ficam no mesmo projeto para manter o desafio objetivo.

## Isolamento dos bancos

O Inventory Service usa somente `inventory_db`.

O Billing Service usa somente `billing_db`.

Não há foreign keys entre bancos diferentes. A comunicação entre domínios usa HTTP/REST/JSON.

## Inventory Service

Responsabilidades:

- cadastrar, listar e consultar produtos;
- validar código, descrição e saldo;
- garantir código único;
- manter saldo de estoque;
- executar baixa transacional em lote;
- impedir saldo negativo;
- registrar idempotência em `stock_operations`;
- expor health check e Swagger em desenvolvimento;
- simular falhas funcionais controladas em `Development`.

Entidades principais:

- `Product`
- `StockOperation`

Endpoints principais:

- `POST /api/products`
- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products/lookup`
- `POST /api/stock/debit`
- `POST /debug/fail-next-stock-debit` somente em `Development`

## Billing Service

Responsabilidades:

- criar, listar e consultar notas fiscais;
- gerar numeração sequencial;
- validar produtos via Inventory;
- persistir snapshot de código e descrição dos produtos;
- fechar notas após baixa confirmada;
- manter notas abertas quando o Inventory falha;
- expor health check e Swagger em desenvolvimento.

Entidades principais:

- `Invoice`
- `InvoiceItem`
- `InvoiceStatus`

Endpoints principais:

- `POST /api/invoices`
- `GET /api/invoices`
- `GET /api/invoices/{id}`
- `POST /api/invoices/{id}/print`

## Fluxo de criação de nota

```text
Angular
-> Billing POST /api/invoices
-> Billing valida ProductIds no Inventory
-> Billing obtem Code/Description
-> Billing cria snapshot em InvoiceItem
-> Billing salva Invoice + InvoiceItems no billing_db
-> Angular navega para detalhe da nota
```

Criar nota não reserva e não debita estoque.

## Snapshot dos produtos

Os itens da nota armazenam:

- `ProductId`
- `ProductCode`
- `ProductDescription`
- `Quantity`

`ProductCode` e `ProductDescription` são obtidos do Inventory no momento da criação da nota. Alterações futuras no cadastro do produto não alteram notas já emitidas.

## Fluxo de impressão

```text
Angular
-> Billing POST /api/invoices/{id}/print
-> Billing envia operationId = Invoice.Id ao Inventory
-> Inventory POST /api/stock/debit
-> Inventory baixa estoque em transação local
-> Billing fecha Invoice no billing_db
-> Angular recebe Invoice Closed
-> Angular chama window.print()
```

O frontend chama `window.print()` somente após confirmacao do backend.

## Baixa transacional de estoque

A baixa acontece dentro do Inventory Service em uma transação PostgreSQL.

Cada item usa SQL atômico:

```sql
UPDATE products
SET balance = balance - @quantity,
    updated_at = @now
WHERE id = @productId
AND balance >= @quantity;
```

Se qualquer item falhar:

- o Inventory verifica se o produto existe;
- retorna `404` para produto inexistente ou `409` para saldo insuficiente;
- faz rollback da transação inteira.

Não existe baixa parcial.

## Concorrência

O `WHERE balance >= @quantity` no `UPDATE` faz a decisão de saldo no banco, no mesmo comando que altera o saldo.

Quando duas requisicoes competem pelo mesmo saldo:

- uma atualização pode afetar `1` linha e prosseguir;
- a outra pode afetar `0` linhas e retornar conflito;
- o saldo nunca fica negativo.

Os itens são ordenados por `ProductId` antes da baixa para reduzir risco de deadlock em operações com múltiplos produtos.

## Idempotência

`stock_operations.operation_id` é a chave de idempotência.

Na impressão de nota:

```text
operationId = Invoice.Id
```

Fluxos:

- primeira baixa valida: `Processed`;
- repetição do mesmo `operationId`: `AlreadyProcessed`;
- falha por saldo/produto inexistente: rollback tambem remove o registro da operação, permitindo retry depois que a causa for corrigida.

## Falha antes da baixa

```text
Angular
-> Billing
-> Inventory
-> simulação BeforeProcessing
-> Inventory 503
-> Billing mantém Invoice Open
-> Angular exibe erro

Retry:
Angular
-> Billing
-> Inventory
-> baixa transacional
-> Billing fecha Invoice
-> Angular chama window.print()
```

Nesse caso não há alteração no `inventory_db` na primeira tentativa.

## Falha após commit

```text
Angular
-> Billing
-> Inventory
-> COMMIT estoque + StockOperation
-> resposta simuladamente perdida
-> Inventory 503
-> Billing mantém Invoice Open

Retry:
Angular
-> Billing
-> Inventory
-> operationId já processada
-> AlreadyProcessed
-> Billing fecha Invoice
-> Angular chama window.print()
```

Esse é o caso mais importante para consistência: o estoque foi debitado, mas o Billing ainda não fechou a nota. O retry manual resolve sem duplicar baixa.

## Consistencia entre microsserviços

Não há transação distribuída entre `billing_db` e `inventory_db`.

A consistência é obtida por:

- transação local no Inventory;
- idempotência por `operationId`;
- retry manual seguro.

## Tratamento de erros

Os serviços usam `ValidationProblemDetails` e `ProblemDetails`.

Status:

- `400`: request invalido.
- `404`: recurso inexistente.
- `409`: conflito de negócio.
- `422`: produto invalido na criação da nota.
- `503`: Inventory indisponível ou falha transitoria simulada.
- `500`: erro inesperado tratado pelo middleware global.

As respostas incluem `traceId` técnico e não expõem stack trace.

## Observabilidade

Logs estruturados cobrem:

- criação de produto;
- criação de nota;
- inicio de impressão;
- baixa de estoque;
- saldo insuficiente;
- replay idempotente;
- simulação de falha;
- Inventory indisponível;
- nota fechada.

## Trade-offs

- REST foi escolhido em vez de mensageria para manter simplicidade.
- Retry manual foi escolhido para que a falha seja visível na demonstração.
- `window.print()` foi usado em vez de PDF backend.
- Sequence PostgreSQL garante Números crescentes e únicos, mas pode gerar gaps.
- EF Core cobre CRUD e migrations; SQL explícito cobre o ponto crítico de concorrência.
- O simulador de falha e em memória e restrito a `Development`.


