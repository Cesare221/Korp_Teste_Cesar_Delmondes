# Graph Report - Korp_Teste_Cesar_Delmondes  (2026-08-16)

## Corpus Check
- 99 files · ~18,613 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 680 nodes · 756 edges · 79 communities (59 shown, 20 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9315ed9d`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- [[_COMMUNITY_Community 0|Community 0]]
- [[_COMMUNITY_Community 1|Community 1]]
- [[_COMMUNITY_Community 2|Community 2]]
- [[_COMMUNITY_Community 3|Community 3]]
- [[_COMMUNITY_Community 4|Community 4]]
- [[_COMMUNITY_Community 5|Community 5]]
- [[_COMMUNITY_Community 6|Community 6]]
- [[_COMMUNITY_Community 7|Community 7]]
- [[_COMMUNITY_Community 8|Community 8]]
- [[_COMMUNITY_Community 9|Community 9]]
- [[_COMMUNITY_Community 10|Community 10]]
- [[_COMMUNITY_Community 11|Community 11]]
- [[_COMMUNITY_Community 12|Community 12]]
- [[_COMMUNITY_Community 13|Community 13]]
- [[_COMMUNITY_Community 14|Community 14]]
- [[_COMMUNITY_Community 15|Community 15]]
- [[_COMMUNITY_Community 16|Community 16]]
- [[_COMMUNITY_Community 17|Community 17]]
- [[_COMMUNITY_Community 18|Community 18]]
- [[_COMMUNITY_Community 19|Community 19]]
- [[_COMMUNITY_Community 20|Community 20]]
- [[_COMMUNITY_Community 21|Community 21]]
- [[_COMMUNITY_Community 22|Community 22]]
- [[_COMMUNITY_Community 23|Community 23]]
- [[_COMMUNITY_Community 24|Community 24]]
- [[_COMMUNITY_Community 25|Community 25]]
- [[_COMMUNITY_Community 26|Community 26]]
- [[_COMMUNITY_Community 27|Community 27]]
- [[_COMMUNITY_Community 28|Community 28]]
- [[_COMMUNITY_Community 29|Community 29]]
- [[_COMMUNITY_Community 30|Community 30]]
- [[_COMMUNITY_Community 31|Community 31]]
- [[_COMMUNITY_Community 32|Community 32]]
- [[_COMMUNITY_Community 33|Community 33]]
- [[_COMMUNITY_Community 34|Community 34]]
- [[_COMMUNITY_Community 35|Community 35]]
- [[_COMMUNITY_Community 36|Community 36]]
- [[_COMMUNITY_Community 37|Community 37]]
- [[_COMMUNITY_Community 38|Community 38]]
- [[_COMMUNITY_Community 40|Community 40]]
- [[_COMMUNITY_Community 41|Community 41]]
- [[_COMMUNITY_Community 42|Community 42]]
- [[_COMMUNITY_Community 43|Community 43]]
- [[_COMMUNITY_Community 44|Community 44]]
- [[_COMMUNITY_Community 45|Community 45]]
- [[_COMMUNITY_Community 46|Community 46]]
- [[_COMMUNITY_Community 47|Community 47]]
- [[_COMMUNITY_Community 48|Community 48]]
- [[_COMMUNITY_Community 49|Community 49]]
- [[_COMMUNITY_Community 50|Community 50]]
- [[_COMMUNITY_Community 69|Community 69]]
- [[_COMMUNITY_Community 70|Community 70]]
- [[_COMMUNITY_Community 71|Community 71]]
- [[_COMMUNITY_Community 72|Community 72]]
- [[_COMMUNITY_Community 73|Community 73]]
- [[_COMMUNITY_Community 74|Community 74]]
- [[_COMMUNITY_Community 75|Community 75]]
- [[_COMMUNITY_Community 76|Community 76]]
- [[_COMMUNITY_Community 77|Community 77]]
- [[_COMMUNITY_Community 78|Community 78]]

## God Nodes (most connected - your core abstractions)
1. `InvoiceApiTests` - 29 edges
2. `Sistema de Emissão de Notas Fiscais` - 28 edges
3. `StockApiTests` - 25 edges
4. `Arquitetura` - 17 edges
5. `Pontos técnicos para o video` - 16 edges
6. `compilerOptions` - 15 edges
7. `ProductApiTests` - 15 edges
8. `InvoiceNewPageComponent` - 14 edges
9. `InvoiceService` - 12 edges
10. `Notas técnicas` - 11 edges

## Surprising Connections (you probably didn't know these)
- `FakeInventoryClient` --inherits--> `IInventoryClient`  [EXTRACTED]
  tests/Billing.Tests/InvoiceApiTests.cs → services/billing-service/Application/Inventory/IInventoryClient.cs
- `IncrementingInvoiceNumberGenerator` --inherits--> `IInvoiceNumberGenerator`  [EXTRACTED]
  tests/Billing.Tests/InvoiceApiTests.cs → services/billing-service/Application/Invoices/IInvoiceNumberGenerator.cs
- `InventoryClient` --inherits--> `IInventoryClient`  [EXTRACTED]
  services/billing-service/Application/Inventory/InventoryClient.cs → services/billing-service/Application/Inventory/IInventoryClient.cs
- `PostgresInvoiceNumberGenerator` --inherits--> `IInvoiceNumberGenerator`  [EXTRACTED]
  services/billing-service/Application/Invoices/PostgresInvoiceNumberGenerator.cs → services/billing-service/Application/Invoices/IInvoiceNumberGenerator.cs
- `InvoiceService` --inherits--> `IInvoiceService`  [EXTRACTED]
  services/billing-service/Application/Invoices/InvoiceService.cs → services/billing-service/Application/Invoices/IInvoiceService.cs

## Communities (79 total, 20 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.07
Nodes (31): build, serve, test, builder, configurations, defaultConfiguration, options, development (+23 more)

### Community 1 - "Community 1"
Cohesion: 0.07
Nodes (20): InvoiceDetailPageComponent, closedInvoice, firstFailure, openInvoice, pending, printSpy, text, InvoiceService (+12 more)

### Community 2 - "Community 2"
Cohesion: 0.12
Nodes (16): Angular e RxJS, Angular, lifecycle e RxJS, C# e LINQ, code:sql (UPDATE products), Concorrência, Decisoes técnicas, Decisões técnicas e trade-offs, Fora do escopo (+8 more)

### Community 3 - "Community 3"
Cohesion: 0.10
Nodes (12): apiEndpoints, InvoiceItemForm, CreateProductRequest, Product, ProductFormDialogComponent, ProductService, expected, payload (+4 more)

### Community 4 - "Community 4"
Cohesion: 0.07
Nodes (28): dependencies, @angular/animations, @angular/cdk, @angular/common, @angular/compiler, @angular/core, @angular/forms, @angular/material (+20 more)

### Community 5 - "Community 5"
Cohesion: 0.14
Nodes (4): InvoiceApiTests, FakeInventoryClient, IncrementingInvoiceNumberGenerator, WebApplicationFactory

### Community 6 - "Community 6"
Cohesion: 0.08
Nodes (24): Arquitetura, Baixa transacional de estoque, Billing Service, code:text (Angular), code:text (Angular), code:text (Angular), code:sql (UPDATE products), code:text (operationId = Invoice.Id) (+16 more)

### Community 7 - "Community 7"
Cohesion: 0.17
Nodes (4): IAsyncLifetime, StockApiTests, string, WebApplicationFactory

### Community 8 - "Community 8"
Cohesion: 0.09
Nodes (21): angularCompilerOptions, enableI18nLegacyMessageIdFormat, strictInjectionParameters, strictInputAccessModifiers, strictTemplates, compileOnSave, compilerOptions, experimentalDecorators (+13 more)

### Community 9 - "Community 9"
Cohesion: 0.10
Nodes (7): FakeInventoryClient, IncrementingInvoiceNumberGenerator, IInventoryClient, InventoryClient, IInvoiceNumberGenerator, PostgresInvoiceNumberGenerator, long

### Community 10 - "Community 10"
Cohesion: 0.14
Nodes (4): ControllerBase, InvoicesController, ProductsController, StockController

### Community 11 - "Community 11"
Cohesion: 0.12
Nodes (16): Angular Lifecycle, Angular Material, Arquitetura, C#, Concorrência, Estoque, Falha, Idempotência (+8 more)

### Community 12 - "Community 12"
Cohesion: 0.14
Nodes (3): IDisposable, ProductApiTests, WebApplicationFactory

### Community 13 - "Community 13"
Cohesion: 0.15
Nodes (3): IProductService, ProductService, string

### Community 14 - "Community 14"
Cohesion: 0.13
Nodes (14): code:powershell (docker compose up --build -d), code:powershell (cd frontend), code:text (http://localhost:4200), Como visualizar, Modal de Novo Produto, Navegação, Redefinição visual aplicada, Relatório de funcionamento do site (+6 more)

### Community 15 - "Community 15"
Cohesion: 0.14
Nodes (13): Angular Lifecycle, Angular Material, ASP.NET Core, code:sql (UPDATE products), code:text (operationId = InvoiceId), Concorrência, Entity Framework Core e PostgreSQL, Idempotência (+5 more)

### Community 17 - "Community 17"
Cohesion: 0.14
Nodes (13): Arquitetura, code:text (Angular), code:sql (UPDATE products), code:yaml (learning:), Destaques técnicos, Foco atual, Funcionalidades, GitHub Stats (+5 more)

### Community 18 - "Community 18"
Cohesion: 0.14
Nodes (5): Migration, InitialInventoryCreate, InitialBillingCreate, AddStockOperations, Inventory.Api.Infrastructure.Migrations

### Community 20 - "Community 20"
Cohesion: 0.15
Nodes (12): AllowedHosts, ConnectionStrings, BillingDb, Cors, AllowedOrigins, BaseUrl, Logging, LogLevel (+4 more)

### Community 21 - "Community 21"
Cohesion: 0.20
Nodes (9): AllowedHosts, ConnectionStrings, InventoryDb, Cors, AllowedOrigins, Logging, LogLevel, Default (+1 more)

### Community 22 - "Community 22"
Cohesion: 0.22
Nodes (3): FailureSimulationService, IFailureSimulationService, int

### Community 23 - "Community 23"
Cohesion: 0.33
Nodes (4): AppComponent, appConfig, routes, apiErrorInterceptor()

### Community 24 - "Community 24"
Cohesion: 0.25
Nodes (7): commands, rollForward, version, isRoot, tools, dotnet-ef, version

### Community 25 - "Community 25"
Cohesion: 0.25
Nodes (4): BillingDbContextModelSnapshot, Inventory.Api.Infrastructure.Migrations, InventoryDbContextModelSnapshot, ModelSnapshot

### Community 26 - "Community 26"
Cohesion: 0.29
Nodes (6): compilerOptions, outDir, types, extends, files, include

### Community 27 - "Community 27"
Cohesion: 0.29
Nodes (3): DbContext, BillingDbContext, InventoryDbContext

### Community 29 - "Community 29"
Cohesion: 0.29
Nodes (3): IHealthCheck, DbContextHealthCheck, DbContextHealthCheck

### Community 33 - "Community 33"
Cohesion: 0.33
Nodes (5): compilerOptions, outDir, types, extends, include

### Community 36 - "Community 36"
Cohesion: 0.40
Nodes (4): Logging, LogLevel, Default, Microsoft.AspNetCore

### Community 37 - "Community 37"
Cohesion: 0.40
Nodes (4): Logging, LogLevel, Default, Microsoft.AspNetCore

### Community 69 - "Community 69"
Cohesion: 0.13
Nodes (14): cli, analytics, prefix, projectType, root, schematics, sourceRoot, newProjectRoot (+6 more)

### Community 70 - "Community 70"
Cohesion: 0.29
Nodes (6): Conclusão, Conformidade com o PDF de referência, Matriz de conformidade, Requisitos extraídos do PDF, Resumo executivo, Validação local executada

### Community 71 - "Community 71"
Cohesion: 0.24
Nodes (10): code:powershell (powershell -ExecutionPolicy Bypass -File scripts/demo-failur), code:text (operationId = Invoice.Id), code:text (Status = Open), code:http (POST /debug/fail-next-stock-debit), code:powershell (powershell -ExecutionPolicy Bypass -File scripts/demo-failur), Falha antes do processamento, Falha após o commit, Idempotência (+2 more)

### Community 72 - "Community 72"
Cohesion: 0.25
Nodes (9): Backend, code:powershell (powershell -ExecutionPolicy Bypass -File scripts/demo-failur), code:powershell (docker compose stop inventory-service), code:powershell (dotnet test), code:powershell (cd frontend), Falha real com container parado, Frontend, Health do Inventory (+1 more)

### Community 73 - "Community 73"
Cohesion: 0.36
Nodes (8): Acessos principais, code:powershell (docker compose up --build -d), code:powershell (cd frontend), Inicio rapido, Início rápido, Pré-requisitos, Subir bancos e backends, Subir o frontend

### Community 74 - "Community 74"
Cohesion: 0.29
Nodes (7): Arquitetura, Banco de dados, Billing, code:text (http://localhost:4200), code:text (Angular), Como visualizar o sistema, Inventory

### Community 75 - "Community 75"
Cohesion: 0.40
Nodes (5): Estoque, Funcionalidades, Notas fiscais, Produtos, Resiliência

### Community 76 - "Community 76"
Cohesion: 0.50
Nodes (4): Backend, Frontend, Infraestrutura, Tecnologias

### Community 77 - "Community 77"
Cohesion: 0.67
Nodes (3): APIs, Billing, Inventory

### Community 78 - "Community 78"
Cohesion: 0.67
Nodes (3): Build, code:powershell (dotnet build), code:powershell (cd frontend)

## Knowledge Gaps
- **241 isolated node(s):** `version`, `isRoot`, `version`, `commands`, `rollForward` (+236 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **20 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `InvoiceApiTests` connect `Community 5` to `Community 9`, `Community 12`?**
  _High betweenness centrality (0.012) - this node is a cross-community bridge._
- **Why does `Sistema de Emissão de Notas Fiscais` connect `Community 2` to `Community 71`, `Community 72`, `Community 73`, `Community 74`, `Community 75`, `Community 76`, `Community 77`, `Community 78`?**
  _High betweenness centrality (0.008) - this node is a cross-community bridge._
- **Why does `StockApiTests` connect `Community 7` to `Community 12`?**
  _High betweenness centrality (0.008) - this node is a cross-community bridge._
- **What connects `version`, `isRoot`, `version` to the rest of the system?**
  _241 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.07096774193548387 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.07439024390243902 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.11764705882352941 - nodes in this community are weakly interconnected._