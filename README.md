# Sistema de Emissão de Notas Fiscais

Sistema web para cadastro de produtos, criação de notas fiscais, finalização com impressão e baixa transacional de estoque.

O projeto foi desenvolvido como uma solução full stack com frontend Angular, dois microsserviços ASP.NET Core em C# e bancos PostgreSQL isolados por domínio.

## Visão geral

A aplicação permite:

- cadastrar e listar produtos;
- criar notas fiscais com um ou mais produtos;
- manter snapshot de código e descrição dos produtos dentro da nota;
- finalizar uma nota fiscal;
- imprimir a nota pelo navegador;
- baixar estoque de forma transacional;
- impedir saldo negativo mesmo em cenários concorrentes;
- recuperar falhas entre microsserviços com retry manual seguro.

## Início rápido

### Pré-requisitos

- Docker Desktop
- .NET SDK 10
- Node.js 24 LTS ou versão compatível com Angular 21
- npm

### Subir bancos e backends

Na raiz do projeto:

```powershell
docker compose up --build -d
```

### Subir o frontend

Em outro terminal:

```powershell
cd frontend
npm ci
npm start
```

### Acessos principais

| Recurso | URL |
| --- | --- |
| Frontend Angular | `http://localhost:4200` |
| Inventory Service | `http://localhost:5001` |
| Billing Service | `http://localhost:5002` |
| Inventory Swagger | `http://localhost:5001/swagger` |
| Billing Swagger | `http://localhost:5002/swagger` |
| Inventory Health | `http://localhost:5001/health` |
| Billing Health | `http://localhost:5002/health` |

## Como visualizar o sistema

Com os serviços ativos, abra:

```text
http://localhost:4200
```

Rotas úteis:

- Produtos: `http://localhost:4200/products`
- Notas fiscais: `http://localhost:4200/invoices`
- Nova nota fiscal: `http://localhost:4200/invoices/new`

## Funcionalidades

### Produtos

- Cadastro de produtos com código, descrição e saldo inicial.
- Validação de campos obrigatórios.
- Validação de saldo não negativo.
- Código de produto único.
- Listagem de produtos cadastrados.
- Atualização manual da listagem pelo botão `Atualizar lista`.

### Notas fiscais

- Cadastro de notas fiscais com múltiplos itens.
- Seleção de produtos cadastrados.
- Quantidade por produto.
- Bloqueio de produto duplicado dentro da mesma nota.
- Snapshot de código e descrição do produto no item da nota.
- Numeração sequencial com PostgreSQL sequence.
- Status `Open` e `Closed`.
- Tela de detalhe da nota.
- Finalização pelo botão `Finalizar e imprimir`.
- Impressão via `window.print()` após confirmação do fechamento.
- Bloqueio de nova finalização para notas já fechadas.

### Estoque

- Baixa transacional em lote.
- Proteção contra saldo negativo.
- Tratamento de concorrência no banco.
- Idempotência por operação.
- Rollback quando algum item não pode ser debitado.

### Resiliência

- Tratamento de indisponibilidade do Inventory Service.
- Nota permanece aberta quando a baixa de estoque falha.
- Mensagem amigável no frontend.
- Retry manual seguro.
- Simulação controlada de falhas em ambiente `Development`.

## Arquitetura

```text
Angular
   |
   |---- REST ---- Inventory Service
   |                  |
   |             inventory_db
   |
   `---- REST ---- Billing Service
                         |
                    billing_db
                         |
                         `---- REST ---- Inventory Service
```

## Responsabilidades

| Camada | Responsabilidade |
| --- | --- |
| `frontend` | Interface Angular para produtos, notas fiscais e impressão. |
| `services/inventory-service` | Produtos, saldos, baixa de estoque, idempotência e simulação de falhas. |
| `services/billing-service` | Notas fiscais, numeração, snapshot dos produtos e fechamento. |
| `inventory_db` | Banco exclusivo do Inventory Service. |
| `billing_db` | Banco exclusivo do Billing Service. |

O Billing Service não acessa diretamente o banco do Inventory Service. A integração entre os domínios ocorre por REST.

## Tecnologias

### Backend

- C#
- ASP.NET Core Web API
- Entity Framework Core
- Npgsql
- PostgreSQL
- Swagger / OpenAPI
- Health Checks
- xUnit

### Frontend

- Angular standalone
- Angular Material
- Reactive Forms
- RxJS
- Vitest

### Infraestrutura

- Docker Compose
- PostgreSQL em containers separados por domínio

## Serviços e portas

| Recurso | Porta / URL |
| --- | --- |
| Angular | `http://localhost:4200` |
| Inventory Service | `http://localhost:5001` |
| Billing Service | `http://localhost:5002` |
| Inventory PostgreSQL | `localhost:5433` |
| Billing PostgreSQL | `localhost:5434` |
| Inventory Health | `http://localhost:5001/health` |
| Billing Health | `http://localhost:5002/health` |
| Inventory Swagger | `http://localhost:5001/swagger` |
| Billing Swagger | `http://localhost:5002/swagger` |

## Banco de dados

Em `Development`, os serviços aplicam migrations automaticamente ao iniciar.

### Inventory

- `products`
- `stock_operations`

### Billing

- `billing.invoices`
- `billing.invoice_items`
- `billing.invoice_number_seq`

As variáveis necessárias para execução local estão documentadas em `.env.example`.

## APIs

### Inventory

| Método | Rota | Descrição |
| --- | --- | --- |
| `POST` | `/api/products` | Cria produto. |
| `GET` | `/api/products` | Lista produtos. |
| `GET` | `/api/products/{id}` | Busca produto por ID. |
| `POST` | `/api/products/lookup` | Busca produtos por lista de IDs. |
| `POST` | `/api/stock/debit` | Executa baixa transacional de estoque. |
| `POST` | `/debug/fail-next-stock-debit` | Arma falha controlada somente em `Development`. |

### Billing

| Método | Rota | Descrição |
| --- | --- | --- |
| `POST` | `/api/invoices` | Cria nota fiscal. |
| `GET` | `/api/invoices` | Lista notas fiscais. |
| `GET` | `/api/invoices/{id}` | Busca nota fiscal por ID. |
| `POST` | `/api/invoices/{id}/print` | Finaliza a nota, baixa estoque e libera impressão. |

## Concorrência

A baixa de estoque usa SQL atômico no Inventory Service:

```sql
UPDATE products
SET balance = balance - @quantity,
    updated_at = @now
WHERE id = @productId
  AND balance >= @quantity;
```

Se `rows affected = 0`, o Inventory Service diferencia produto inexistente de saldo insuficiente e realiza rollback da transação.

Os itens são ordenados por `ProductId` antes da baixa para reduzir risco de deadlock em operações concorrentes.

Esse desenho cobre o cenário em que duas notas tentam consumir simultaneamente o último item disponível: somente uma operação conclui e o saldo nunca fica negativo.

## Idempotência

`stock_operations.operation_id` é a chave de idempotência.

Durante a finalização da nota, o Billing Service envia:

```text
operationId = Invoice.Id
```

Resultados possíveis:

- `Processed`: estoque debitado e operação registrada.
- `AlreadyProcessed`: operação já havia sido processada e o estoque não é debitado novamente.

Esse mecanismo permite recuperar com segurança o cenário em que o Inventory confirma a baixa no banco, mas a resposta HTTP se perde antes de o Billing fechar a nota.

## Tratamento de falhas

Quando o Inventory Service está indisponível, o Billing Service retorna `503 Service Unavailable` e mantém a nota com:

```text
Status = Open
ClosedAt = null
```

No frontend:

- o loading é encerrado;
- uma mensagem amigável é exibida;
- `window.print()` não é chamado em caso de falha;
- a nota permanece disponível para nova tentativa.

A recuperação é manual: o usuário clica em `Finalizar e imprimir` novamente.

A idempotência garante que essa repetição seja segura.

## Simulação de falhas

O endpoint de simulação existe somente em `Development`:

```http
POST /debug/fail-next-stock-debit
```

### Falha antes do processamento

```powershell
powershell -ExecutionPolicy Bypass -File scripts/demo-failure.ps1 -Mode BeforeProcessing
```

Comportamento esperado:

- a primeira tentativa retorna `503`;
- o saldo permanece intacto;
- nenhuma `StockOperation` é persistida;
- a nota permanece aberta;
- uma nova tentativa processa o estoque e fecha a nota.

### Falha após o commit

```powershell
powershell -ExecutionPolicy Bypass -File scripts/demo-failure.ps1 -Mode AfterCommit
```

Comportamento esperado:

- a primeira tentativa retorna `503` depois do commit;
- o saldo já fica debitado;
- a nota permanece `Open`;
- a nova tentativa recebe `AlreadyProcessed`;
- a nota fecha sem débito duplicado.

### Health do Inventory

```powershell
powershell -ExecutionPolicy Bypass -File scripts/demo-failure.ps1 -Mode Health
```

### Falha real com container parado

```powershell
docker compose stop inventory-service
docker compose start inventory-service
```

Com o Inventory parado, a finalização retorna `503` e a nota permanece aberta. Após o restart e a recuperação do health check, uma nova tentativa processa a nota normalmente.

## Angular, lifecycle e RxJS

O frontend utiliza:

- componentes standalone;
- rotas lazy com `loadComponent`;
- Reactive Forms;
- `FormArray` para itens dinâmicos da nota;
- Angular Material;
- RxJS para chamadas HTTP e controle de estados assíncronos.

Nos componentes que precisam de carregamento inicial, é utilizado `ngOnInit`.

Para subscriptions manuais que acompanham o ciclo de vida do componente, o projeto utiliza `takeUntilDestroyed`, evitando gerenciamento manual no `ngOnDestroy`.

Operadores RxJS utilizados:

- `catchError`
- `finalize`
- `switchMap`
- `filter`
- `takeUntilDestroyed`

## C# e LINQ

O backend utiliza LINQ em consultas, validações e projeções de dados.

Exemplos de operadores usados:

- `Where`
- `Select`
- `Any`
- `OrderByDescending`

Para CRUD, leitura e projeção, o Entity Framework Core é utilizado normalmente.

No ponto crítico de concorrência da baixa de estoque, foi usado SQL explícito e atômico para garantir que a validação de saldo e o débito ocorram de forma segura na mesma operação.

## Tratamento de erros e exceções

Os serviços retornam `ValidationProblemDetails` e `ProblemDetails` para erros esperados.

Status utilizados:

- `400 Bad Request`: requisição ou validação inválida.
- `404 Not Found`: recurso inexistente.
- `409 Conflict`: conflito de negócio, como estoque insuficiente ou nota já fechada.
- `422 Unprocessable Entity`: produto inexistente na criação da nota.
- `503 Service Unavailable`: Inventory Service indisponível.
- `500 Internal Server Error`: erro inesperado tratado pelo middleware global.

As respostas de erro incluem `traceId` técnico e não expõem stack trace ao cliente.

## Testes

### Backend

```powershell
dotnet test
```

Os testes em .NET utilizam xUnit.

### Frontend

```powershell
cd frontend
npm test
```

Os testes unitários do frontend utilizam Vitest.

Principais fluxos validados:

- cadastro de produtos;
- criação de notas com múltiplos itens;
- finalização e impressão;
- atualização de estoque;
- saldo insuficiente;
- concorrência;
- idempotência;
- falhas entre microsserviços;
- recuperação após falhas.

## Build

Backend:

```powershell
dotnet build
```

Frontend:

```powershell
cd frontend
npm run build
```

## Decisões técnicas e trade-offs

- REST em vez de mensageria para manter a solução proporcional ao escopo do desafio.
- Bancos separados para preservar isolamento entre microsserviços.
- Entity Framework Core para CRUD, consultas e migrations.
- SQL explícito no ponto crítico de concorrência do estoque.
- PostgreSQL sequence para numeração sequencial das notas, aceitando possíveis gaps.
- Retry manual seguro em vez de retry automático, facilitando a demonstração e evitando repetições ocultas.
- `window.print()` no navegador em vez de geração de PDF no backend.
- Sem transação distribuída; consistência obtida com transações locais, idempotência e retry seguro.

## Fora do escopo

Para manter o projeto alinhado ao desafio proposto, não foram adicionados recursos que não eram necessários para a avaliação principal, como:

- autenticação;
- cadastro de clientes;
- preços;
- impostos;
- geração de PDF no backend;
- mensageria;
- transação distribuída.

## Observações

- O frontend não faz parte do Docker Compose e deve ser iniciado separadamente.
- O simulador de falhas utiliza estado em memória e existe apenas em ambiente de desenvolvimento/demonstração.
- Notas fiscais antigas preservam snapshot dos produtos usados no momento da criação.
