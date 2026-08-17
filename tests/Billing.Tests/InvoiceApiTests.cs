using System.Net;
using System.Net.Http.Json;
using Billing.Api.Application.Inventory;
using Billing.Api.Application.Invoices;
using Billing.Api.Contracts;
using Billing.Api.Domain;
using Billing.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Billing.Tests;

public sealed class InvoiceApiTests : IDisposable
{
    private readonly FakeInventoryClient _inventoryClient = new();
    private readonly IncrementingInvoiceNumberGenerator _numberGenerator = new();
    private readonly WebApplicationFactory<Program> _factory;

    public InvoiceApiTests()
    {
        var databaseName = $"billing-invoices-{Guid.NewGuid()}";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<BillingDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<BillingDbContext>>();
                    services.RemoveAll<IInventoryClient>();
                    services.RemoveAll<IInvoiceNumberGenerator>();
                    services.AddDbContext<BillingDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                    services.AddSingleton<IInventoryClient>(_inventoryClient);
                    services.AddSingleton<IInvoiceNumberGenerator>(_numberGenerator);
                });
            });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task PostInvoices_creates_open_invoice_with_snapshot_for_one_product()
    {
        using var client = _factory.CreateClient();
        var product = AddInventoryProduct("PROD-BIL-001", "Produto faturado", 10);

        var response = await client.PostAsJsonAsync("/api/invoices", new CreateInvoiceRequest(
            [new CreateInvoiceItemRequest(product.Id, 2)]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);
        Assert.Equal(1, invoice.Number);
        Assert.Equal("Open", invoice.Status);
        Assert.Null(invoice.ClosedAt);

        var item = Assert.Single(invoice.Items);
        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal("PROD-BIL-001", item.ProductCode);
        Assert.Equal("Produto faturado", item.ProductDescription);
        Assert.Equal(2, item.Quantity);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task PostInvoices_accepts_multiple_products()
    {
        using var client = _factory.CreateClient();
        var first = AddInventoryProduct("PROD-BIL-002", "Produto A", 3);
        var second = AddInventoryProduct("PROD-BIL-003", "Produto B", 4);

        var response = await client.PostAsJsonAsync("/api/invoices", new CreateInvoiceRequest(
            [
                new CreateInvoiceItemRequest(first.Id, 2),
                new CreateInvoiceItemRequest(second.Id, 3)
            ]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);
        Assert.Equal(2, invoice.Items.Count);
        Assert.Contains(invoice.Items, item => item.ProductId == first.Id && item.Quantity == 2);
        Assert.Contains(invoice.Items, item => item.ProductId == second.Id && item.Quantity == 3);
    }

    [Fact]
    public async Task PostInvoices_assigns_different_increasing_numbers()
    {
        using var client = _factory.CreateClient();
        var product = AddInventoryProduct("PROD-BIL-004", "Produto numerado", 8);

        var first = await CreateInvoiceAsync(client, product.Id);
        var second = await CreateInvoiceAsync(client, product.Id);

        Assert.NotEqual(first.Number, second.Number);
        Assert.True(second.Number > first.Number);
    }

    [Fact]
    public async Task PostInvoices_rejects_request_without_items()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/invoices", new CreateInvoiceRequest([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PostInvoices_rejects_non_positive_quantity(int quantity)
    {
        using var client = _factory.CreateClient();
        var product = AddInventoryProduct("PROD-BIL-005", "Produto quantidade", 5);

        var response = await client.PostAsJsonAsync("/api/invoices", new CreateInvoiceRequest(
            [new CreateInvoiceItemRequest(product.Id, quantity)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostInvoices_rejects_duplicate_products()
    {
        using var client = _factory.CreateClient();
        var product = AddInventoryProduct("PROD-BIL-006", "Produto duplicado", 5);

        var response = await client.PostAsJsonAsync("/api/invoices", new CreateInvoiceRequest(
            [
                new CreateInvoiceItemRequest(product.Id, 1),
                new CreateInvoiceItemRequest(product.Id, 2)
            ]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostInvoices_returns_unprocessable_entity_for_missing_products_without_persisting()
    {
        using var client = _factory.CreateClient();
        var missingProductId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/api/invoices", new CreateInvoiceRequest(
            [new CreateInvoiceItemRequest(missingProductId, 1)]));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Invalid invoice items", problem.Title);

        Assert.Equal(0, await CountInvoicesAsync());
    }

    [Fact]
    public async Task PostInvoices_returns_service_unavailable_when_inventory_is_unavailable_without_persisting()
    {
        using var client = _factory.CreateClient();
        _inventoryClient.IsUnavailable = true;

        var response = await client.PostAsJsonAsync("/api/invoices", new CreateInvoiceRequest(
            [new CreateInvoiceItemRequest(Guid.NewGuid(), 1)]));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, await CountInvoicesAsync());
    }

    [Fact]
    public async Task GetInvoices_returns_empty_list_when_no_invoices_exist()
    {
        using var client = _factory.CreateClient();

        var invoices = await client.GetFromJsonAsync<InvoiceListItemResponse[]>("/api/invoices");

        Assert.NotNull(invoices);
        Assert.Empty(invoices);
    }

    [Fact]
    public async Task GetInvoices_returns_created_invoices_ordered_by_number_desc()
    {
        using var client = _factory.CreateClient();
        var product = AddInventoryProduct("PROD-BIL-007", "Produto lista", 5);

        var first = await CreateInvoiceAsync(client, product.Id);
        var second = await CreateInvoiceAsync(client, product.Id);

        var invoices = await client.GetFromJsonAsync<InvoiceListItemResponse[]>("/api/invoices");

        Assert.NotNull(invoices);
        Assert.True(invoices.Length >= 2);
        Assert.Equal(second.Id, invoices[0].Id);
        Assert.Equal(first.Id, invoices[1].Id);
        Assert.All(invoices.Take(2), invoice =>
        {
            Assert.Equal("Open", invoice.Status);
            Assert.Equal(1, invoice.ItemCount);
        });
    }

    [Fact]
    public async Task GetInvoice_returns_full_invoice_details()
    {
        using var client = _factory.CreateClient();
        var product = AddInventoryProduct("PROD-BIL-008", "Produto detalhe", 5);
        var created = await CreateInvoiceAsync(client, product.Id);

        var invoice = await client.GetFromJsonAsync<InvoiceResponse>($"/api/invoices/{created.Id}");

        Assert.NotNull(invoice);
        Assert.Equal(created.Id, invoice.Id);
        Assert.Equal(created.Number, invoice.Number);
        Assert.Single(invoice.Items);
    }

    [Fact]
    public async Task GetInvoice_returns_not_found_for_unknown_id()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/invoices/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PrintInvoice_closes_open_invoice_after_inventory_debit_succeeds()
    {
        using var client = _factory.CreateClient();
        var product = AddInventoryProduct("PROD-PRN-001", "Produto impressao", 5);
        var created = await CreateInvoiceAsync(client, product.Id, quantity: 2);

        var response = await client.PostAsync($"/api/invoices/{created.Id}/print", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var printed = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(printed);
        Assert.Equal("Closed", printed.Status);
        Assert.NotNull(printed.ClosedAt);
        var debitCall = Assert.Single(_inventoryClient.DebitCalls);
        Assert.Equal(created.Id, debitCall.OperationId);
        Assert.Contains(debitCall.Items, item => item.ProductId == product.Id && item.Quantity == 2);
    }

    [Fact]
    public async Task PrintInvoice_closes_invoice_when_inventory_reports_already_processed()
    {
        using var client = _factory.CreateClient();
        var product = AddInventoryProduct("PROD-PRN-002", "Produto ja processado", 5);
        var created = await CreateInvoiceAsync(client, product.Id);
        _inventoryClient.NextDebitResult = InventoryDebitResult.AlreadyProcessed();

        var response = await client.PostAsync($"/api/invoices/{created.Id}/print", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var printed = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(printed);
        Assert.Equal("Closed", printed.Status);
        Assert.NotNull(printed.ClosedAt);
        Assert.Equal(created.Id, Assert.Single(_inventoryClient.DebitCalls).OperationId);
    }

    [Fact]
    public async Task PrintInvoice_keeps_invoice_open_when_inventory_reports_insufficient_stock()
    {
        using var client = _factory.CreateClient();
        var product = AddInventoryProduct("PROD-PRN-003", "Produto sem saldo", 0);
        var created = await CreateInvoiceAsync(client, product.Id);
        _inventoryClient.NextDebitResult = InventoryDebitResult.InsufficientStock();

        var response = await client.PostAsync($"/api/invoices/{created.Id}/print", content: null);
        var persisted = await GetInvoiceEntityAsync(created.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(InvoiceStatus.Open, persisted.Status);
        Assert.Null(persisted.ClosedAt);
    }

    [Fact]
    public async Task PrintInvoice_keeps_invoice_open_when_inventory_is_unavailable()
    {
        using var client = _factory.CreateClient();
        var product = AddInventoryProduct("PROD-PRN-004", "Produto indisponivel", 5);
        var created = await CreateInvoiceAsync(client, product.Id);
        _inventoryClient.NextDebitResult = InventoryDebitResult.Unavailable();

        var response = await client.PostAsync($"/api/invoices/{created.Id}/print", content: null);
        var persisted = await GetInvoiceEntityAsync(created.Id);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(InvoiceStatus.Open, persisted.Status);
        Assert.Null(persisted.ClosedAt);
    }

    [Fact]
    public async Task PrintInvoice_returns_not_found_for_unknown_invoice()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/invoices/{Guid.NewGuid()}/print", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PrintInvoice_rejects_closed_invoice_without_calling_inventory()
    {
        using var client = _factory.CreateClient();
        var product = AddInventoryProduct("PROD-PRN-005", "Produto fechado", 5);
        var created = await CreateInvoiceAsync(client, product.Id);
        await MarkInvoiceClosedAsync(created.Id);

        var response = await client.PostAsync($"/api/invoices/{created.Id}/print", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(_inventoryClient.DebitCalls);
    }

    private InventoryProduct AddInventoryProduct(string code, string description, int balance)
    {
        var product = new InventoryProduct(Guid.NewGuid(), code, description, balance);
        _inventoryClient.Products[product.Id] = product;
        return product;
    }

    private static async Task<InvoiceResponse> CreateInvoiceAsync(
        HttpClient client,
        Guid productId,
        int quantity = 1)
    {
        var response = await client.PostAsJsonAsync("/api/invoices", new CreateInvoiceRequest(
            [new CreateInvoiceItemRequest(productId, quantity)]));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InvoiceResponse>())!;
    }

    private async Task<Invoice> GetInvoiceEntityAsync(Guid invoiceId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        return await dbContext.Invoices
            .AsNoTracking()
            .SingleAsync(invoice => invoice.Id == invoiceId);
    }

    private async Task MarkInvoiceClosedAsync(Guid invoiceId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var invoice = await dbContext.Invoices.SingleAsync(invoice => invoice.Id == invoiceId);
        invoice.Status = InvoiceStatus.Closed;
        invoice.ClosedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private async Task<int> CountInvoicesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        return await dbContext.Invoices.CountAsync();
    }

    private sealed class FakeInventoryClient : IInventoryClient
    {
        public Dictionary<Guid, InventoryProduct> Products { get; } = [];

        public List<InventoryDebitCall> DebitCalls { get; } = [];

        public bool IsUnavailable { get; set; }

        public InventoryDebitResult? NextDebitResult { get; set; }

        public Task<InventoryLookupResult> LookupProductsAsync(
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken)
        {
            if (IsUnavailable)
            {
                return Task.FromResult(InventoryLookupResult.Unavailable());
            }

            var products = productIds
                .Distinct()
                .Where(Products.ContainsKey)
                .Select(productId => Products[productId])
                .ToList();

            return Task.FromResult(InventoryLookupResult.Success(products));
        }

        public Task<InventoryDebitResult> DebitStockAsync(
            Guid operationId,
            IReadOnlyCollection<InventoryDebitItem> items,
            CancellationToken cancellationToken)
        {
            DebitCalls.Add(new InventoryDebitCall(operationId, items.ToArray()));

            var result = NextDebitResult ?? InventoryDebitResult.Processed();
            NextDebitResult = null;

            return Task.FromResult(result);
        }
    }

    private sealed record InventoryDebitCall(
        Guid OperationId,
        IReadOnlyList<InventoryDebitItem> Items);

    private sealed class IncrementingInvoiceNumberGenerator : IInvoiceNumberGenerator
    {
        private long _current;

        public Task<long> GenerateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Interlocked.Increment(ref _current));
        }
    }
}
