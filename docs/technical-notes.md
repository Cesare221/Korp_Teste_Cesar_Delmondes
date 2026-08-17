# Notas técnicas

Este documento resume pontos reais do código para apoiar a apresentacao técnica.

## Angular Lifecycle

Hooks utilizados:

- `ngOnInit` em `ProductsPageComponent`: carrega produtos ao abrir `/products`.
- `ngOnInit` em `InvoicesPageComponent`: carrega notas ao abrir `/invoices`.
- `ngOnInit` em `InvoiceNewPageComponent`: carrega produtos para montar a nota.
- `ngOnInit` em `InvoiceDetailPageComponent`: carrega a nota pelo id da rota.

Não há `ngAfterViewInit` nem `ngOnDestroy` implementados diretamente. O ciclo de vida de subscriptions usa `takeUntilDestroyed`.

## RxJS

Operadores e usos:

- `Observable`: retorno dos services Angular baseados em `HttpClient`.
- `catchError`: transforma falhas HTTP em mensagens amigaveis e evita deixar loading preso.
- `finalize`: encerra estados `isLoading`, `isSaving` e `isPrinting`.
- `switchMap`: recarrega listas após dialog e carrega nota com base no id da rota.
- `filter`: ignora fechamento de dialog sem cadastro de produto.
- `takeUntilDestroyed`: encerra subscriptions quando o componente e destruido.
- `of` e `EMPTY`: encerram fluxos depois de erro tratado.

## Angular Material

Componentes utilizados:

- `MatToolbar`: barra superior.
- `MatButton` e `MatIconButton`: ações principais e remoção de item.
- `MatIcon`: ícones de Navegação é estado.
- `MatCard`: agrupamento visual das telas.
- `MatTable`: listagens de produtos, notas e itens.
- `MatDialog`: cadastro de produto.
- `MatFormField`, `MatInput`, `MatSelect`: formulários.
- `MatSnackBar`: feedback de sucesso.
- `MatProgressSpinner`: loading.
- `MatChip`: status da nota.

## ASP.NET Core

- Controllers expõem APIs REST.
- Dependency Injection registra services de aplicação, `DbContext`, `HttpClient` tipado e simulador de falha.
- `ProblemDetails` e `ValidationProblemDetails` padronizam respostas de erro.
- Swagger/OpenAPI fica disponível em `Development`.
- Health checks validam a conexao de cada serviço com seu banco.
- `HttpClientFactory` configura o client tipado do Billing para chamar o Inventory com timeout de 5 segundos.

## Entity Framework Core e PostgreSQL

- EF Core mapeia entidades, constraints, índices e migrations.
- Npgsql conecta os serviços ao PostgreSQL.
- Inventory possui `products` e `stock_operations`.
- Billing possui `invoices`, `invoice_items` e `billing.invoice_number_seq`.
- Testes simples de Billing usam EF InMemory; comportamentos dependentes de PostgreSQL são cobertos por testes reais no Inventory e por smoke tests com Docker.

## LINQ

Exemplos reais:

- `ProductService.ListAsync`: `OrderBy(product => product.Code)` organiza produtos.
- `ProductService.LookupAsync`: `Where(product => productIds.Contains(product.Id))` retorna somente produtos solicitados.
- `InvoiceService.CreateAsync`: `Distinct()` remove ids repetidos antes da consulta ao Inventory.
- `InvoiceService.CreateAsync`: `ToDictionary(product => product.Id)` acelera montagem do snapshot.
- `StockService.Validate`: `GroupBy(item => item.ProductId)` detecta produtos duplicados na baixa.
- `InvoiceService.ListAsync`: `OrderByDescending(invoice => invoice.Number)` mostra notas mais recentes primeiro.

## Tratamento de erros

- `400 Bad Request`: campos obrigatórios, quantidades inválidas ou modo de debug inválido.
- `404 Not Found`: produto ou nota inexistente.
- `409 Conflict`: código duplicado, estoque insuficiente ou tentativa de imprimir nota fechada.
- `422 Unprocessable Entity`: produto informado na nota não existe no Inventory.
- `503 Service Unavailable`: Inventory indisponível, timeout ou falha simulada.
- `500 Internal Server Error`: exceção inesperada tratada pelo middleware global.

As respostas incluem `traceId` para diagnóstico e não retornam stack trace.

## Concorrência

O Inventory debita estoque com:

```sql
UPDATE products
SET balance = balance - @quantity,
    updated_at = @now
WHERE id = @productId
AND balance >= @quantity;
```

`rows affected = 0` significa que o produto não existe ou não tem saldo suficiente. O serviço diferencia os casos e faz rollback da transação inteira.

Os itens são ordenados por `ProductId` antes das atualizações para reduzir risco de deadlock em baixas com múltiplos produtos.

## Idempotência

Na impressão:

```text
operationId = InvoiceId
```

O Inventory registra `StockOperation` na mesma transação da baixa.

- `Processed`: primeira execucao.
- `AlreadyProcessed`: repetição segura sem novo debito.

Esse desenho resolve o caso `AfterCommit`: o Inventory comita, a resposta falha, o Billing mantém a nota aberta e o retry fecha a nota sem baixar estoque de novo.

## Trade-offs

- REST em vez de mensageria.
- Retry manual seguro em vez de retry automático.
- `window.print()` em vez de PDF no backend.
- Sequence PostgreSQL aceita gaps em troca de segurança em concorrência.
- Sem transação distribuída.
- EF Core para CRUD e SQL explícito para a atualização crítica de estoque.


