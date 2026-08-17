# Pontos técnicos para o video

## Arquitetura

- Frontend Angular consome dois microsserviços REST.
- Inventory Service gerencia produtos e estoque.
- Billing Service gerencia notas fiscais.
- Cada microsserviço possui banco PostgreSQL proprio.
- Billing chama Inventory por HTTP e não acessa `inventory_db`.

## Produtos

- Cadastro exige código, descrição e saldo inicial não negativo.
- código duplicado retorna `409 Conflict`.
- Listagem ordena por código.

## Notas

- Nota nasce `Open`.
- Itens guardam snapshot de código e descrição do produto.
- numeração usa sequence PostgreSQL.
- Criar nota não baixa estoque.

## impressão

- `POST /api/invoices/{id}/print` inicia o processamento.
- Billing chama `POST /api/stock/debit`.
- Nota fecha somente após confirmacao do Inventory.
- Angular chama `window.print()` apenas depois de receber nota `Closed`.

## Estoque

- Baixa em lote ocorre em transação local no Inventory.
- Falha em qualquer item faz rollback de todos os itens.
- SQL atômico impede saldo negativo.

## Concorrência

- O banco decide a baixa com `WHERE balance >= quantity`.
- Requisições concorrentes contra saldo insuficiente resultam em uma baixa e um conflito.
- Itens são processados ordenados por `ProductId`.

## Idempotência

- `operationId = InvoiceId`.
- `stock_operations` registra operações processadas.
- repetição retorna `AlreadyProcessed`.
- No `AfterCommit`, o retry fecha a nota sem novo debito.

## Falha

- `BeforeProcessing`: falha antes de alterar banco.
- `AfterCommit`: falha depois do commit.
- Container parado: Billing retorna `503` e mantém a nota aberta.
- recuperação e feita por retry manual seguro.

## Angular Lifecycle

- `ngOnInit` carrega dados iniciais em telas de produtos e notas.
- `takeUntilDestroyed` gerencia encerramento das subscriptions.

## RxJS

- `catchError` para mensagens amigaveis.
- `finalize` para desligar loading.
- `switchMap` para fluxo de rota e recarregamento.
- `filter` para ignorar dialog cancelado.

## Angular Material

- Toolbar, buttons, icons, cards, tables, dialog, form fields, select, snack bar, spinner e chips.
- formulários usam validações client-side.

## C#

- ASP.NET Core Web API.
- Dependency Injection para services e clients.
- EF Core/Npgsql para persistencia.
- HttpClient tipado para Billing -> Inventory.
- ProblemDetails para erros.
- Health checks e Swagger.

## LINQ

- `Where`, `Select`, `Any`, `OrderBy`, `OrderByDescending`, `GroupBy`, `Distinct` e `ToDictionary` aparecem em fluxos reais.

## Tratamento de erros

- `400`, `404`, `409`, `422`, `503` e `500` são usados conforme o tipo de falha.
- `traceId` aparece em respostas de erro.
- Stack trace não é exposto ao usuário.

## Testes

- xUnit cobre entidades, APIs, idempotência, concorrência e falhas.
- Vitest cobre services e componentes Angular.
- Smoke tests reais validam Docker, PostgreSQL, fluxo completo e cenários de falha.


