using System.Net;
using System.Net.Http.Json;
using Inventory.Api.Contracts;
using Inventory.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Inventory.Tests;

public sealed class StockApiTests : IDisposable, IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=inventory_db;Username=korp;Password=korp_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public StockApiTests()
    {
        _factory = CreateFactory("Test");
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task DebitStock_debits_one_product()
    {
        using var client = _factory.CreateClient();
        var product = await CreateProductAsync(client, "STK-ONE", 10);
        var operationId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/api/stock/debit", new StockDebitRequest(
            operationId,
            [new StockDebitItemRequest(product.Id, 2)]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<StockDebitResponse>();
        Assert.NotNull(result);
        Assert.Equal(operationId, result.OperationId);
        Assert.Equal("Processed", result.Status);
        Assert.Equal(8, await GetBalanceAsync(product.Id));
    }

    [Fact]
    public async Task DebitStock_debits_multiple_products_atomically()
    {
        using var client = _factory.CreateClient();
        var first = await CreateProductAsync(client, "STK-MULTI-A", 10);
        var second = await CreateProductAsync(client, "STK-MULTI-B", 5);

        var response = await client.PostAsJsonAsync("/api/stock/debit", new StockDebitRequest(
            Guid.NewGuid(),
            [
                new StockDebitItemRequest(first.Id, 2),
                new StockDebitItemRequest(second.Id, 3)
            ]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(8, await GetBalanceAsync(first.Id));
        Assert.Equal(2, await GetBalanceAsync(second.Id));
    }

    [Fact]
    public async Task DebitStock_rolls_back_all_items_when_one_product_has_insufficient_stock()
    {
        using var client = _factory.CreateClient();
        var sufficient = await CreateProductAsync(client, "STK-ROLL-A", 10);
        var insufficient = await CreateProductAsync(client, "STK-ROLL-B", 0);

        var response = await client.PostAsJsonAsync("/api/stock/debit", new StockDebitRequest(
            Guid.NewGuid(),
            [
                new StockDebitItemRequest(sufficient.Id, 2),
                new StockDebitItemRequest(insufficient.Id, 1)
            ]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(10, await GetBalanceAsync(sufficient.Id));
        Assert.Equal(0, await GetBalanceAsync(insufficient.Id));
    }

    [Fact]
    public async Task DebitStock_returns_not_found_for_unknown_product_without_registering_operation()
    {
        using var client = _factory.CreateClient();
        var operationId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/api/stock/debit", new StockDebitRequest(
            operationId,
            [new StockDebitItemRequest(Guid.NewGuid(), 1)]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(await HasStockOperationAsync(operationId));
    }

    [Fact]
    public async Task DebitStock_repeated_operation_id_does_not_debit_twice()
    {
        using var client = _factory.CreateClient();
        var product = await CreateProductAsync(client, "STK-IDEMP", 10);
        var operationId = Guid.NewGuid();
        var request = new StockDebitRequest(
            operationId,
            [new StockDebitItemRequest(product.Id, 2)]);

        var first = await client.PostAsJsonAsync("/api/stock/debit", request);
        var second = await client.PostAsJsonAsync("/api/stock/debit", request);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var repeated = await second.Content.ReadFromJsonAsync<StockDebitResponse>();
        Assert.NotNull(repeated);
        Assert.Equal("AlreadyProcessed", repeated.Status);
        Assert.Equal(8, await GetBalanceAsync(product.Id));
    }

    [Fact]
    public async Task DebitStock_operation_id_from_rolled_back_operation_can_be_reused()
    {
        using var client = _factory.CreateClient();
        var first = await CreateProductAsync(client, "STK-REUSE-A", 10);
        var second = await CreateProductAsync(client, "STK-REUSE-B", 0);
        var operationId = Guid.NewGuid();
        var request = new StockDebitRequest(
            operationId,
            [
                new StockDebitItemRequest(first.Id, 2),
                new StockDebitItemRequest(second.Id, 1)
            ]);

        var failed = await client.PostAsJsonAsync("/api/stock/debit", request);
        await SetBalanceAsync(second.Id, 3);
        var retried = await client.PostAsJsonAsync("/api/stock/debit", request);

        Assert.Equal(HttpStatusCode.Conflict, failed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retried.StatusCode);
        Assert.Equal(8, await GetBalanceAsync(first.Id));
        Assert.Equal(2, await GetBalanceAsync(second.Id));
        Assert.True(await HasStockOperationAsync(operationId));
    }

    [Fact]
    public async Task DebitStock_concurrent_operations_against_balance_one_debit_only_once()
    {
        using var client = _factory.CreateClient();
        var product = await CreateProductAsync(client, "STK-CONC", 1);

        var firstTask = client.PostAsJsonAsync("/api/stock/debit", new StockDebitRequest(
            Guid.NewGuid(),
            [new StockDebitItemRequest(product.Id, 1)]));
        var secondTask = client.PostAsJsonAsync("/api/stock/debit", new StockDebitRequest(
            Guid.NewGuid(),
            [new StockDebitItemRequest(product.Id, 1)]));

        var responses = await Task.WhenAll(firstTask, secondTask);
        var statusCodes = responses.Select(response => response.StatusCode).Order().ToArray();

        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Conflict], statusCodes);
        Assert.Equal(0, await GetBalanceAsync(product.Id));
    }

    [Fact]
    public async Task FailNextStockDebit_before_processing_returns_service_unavailable_without_mutating_database_then_consumes_failure()
    {
        using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();
        var product = await CreateProductAsync(client, "STK-FAIL-BEFORE", 10);
        var operationId = Guid.NewGuid();
        var request = new StockDebitRequest(
            operationId,
            [new StockDebitItemRequest(product.Id, 2)]);

        var armResponse = await client.PostAsJsonAsync("/debug/fail-next-stock-debit", new
        {
            mode = "BeforeProcessing"
        });
        var failed = await client.PostAsJsonAsync("/api/stock/debit", request);
        var balanceAfterFailure = await GetBalanceAsync(product.Id, factory);
        var hasOperationAfterFailure = await HasStockOperationAsync(operationId, factory);
        var retried = await client.PostAsJsonAsync("/api/stock/debit", request);

        Assert.Equal(HttpStatusCode.NoContent, armResponse.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        var problem = await failed.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Stock service temporarily unavailable", problem.Title);
        Assert.Equal(10, balanceAfterFailure);
        Assert.False(hasOperationAfterFailure);

        Assert.Equal(HttpStatusCode.OK, retried.StatusCode);
        var retryResult = await retried.Content.ReadFromJsonAsync<StockDebitResponse>();
        Assert.NotNull(retryResult);
        Assert.Equal("Processed", retryResult.Status);
        Assert.Equal(8, await GetBalanceAsync(product.Id, factory));
        Assert.True(await HasStockOperationAsync(operationId, factory));
    }

    [Fact]
    public async Task FailNextStockDebit_after_commit_returns_service_unavailable_after_persisting_operation_and_retry_is_idempotent()
    {
        using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();
        var product = await CreateProductAsync(client, "STK-FAIL-AFTER", 10);
        var operationId = Guid.NewGuid();
        var request = new StockDebitRequest(
            operationId,
            [new StockDebitItemRequest(product.Id, 2)]);

        var armResponse = await client.PostAsJsonAsync("/debug/fail-next-stock-debit", new
        {
            mode = "AfterCommit"
        });
        var failed = await client.PostAsJsonAsync("/api/stock/debit", request);
        var retried = await client.PostAsJsonAsync("/api/stock/debit", request);

        Assert.Equal(HttpStatusCode.NoContent, armResponse.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal(8, await GetBalanceAsync(product.Id, factory));
        Assert.True(await HasStockOperationAsync(operationId, factory));

        Assert.Equal(HttpStatusCode.OK, retried.StatusCode);
        var retryResult = await retried.Content.ReadFromJsonAsync<StockDebitResponse>();
        Assert.NotNull(retryResult);
        Assert.Equal("AlreadyProcessed", retryResult.Status);
        Assert.Equal(8, await GetBalanceAsync(product.Id, factory));
    }

    [Fact]
    public async Task FailNextStockDebit_rejects_unknown_mode()
    {
        using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/debug/fail-next-stock-debit", new
        {
            mode = "Unexpected"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FailNextStockDebit_does_not_affect_health_check()
    {
        using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        var armResponse = await client.PostAsJsonAsync("/debug/fail-next-stock-debit", new
        {
            mode = "BeforeProcessing"
        });
        var healthResponse = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.NoContent, armResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }

    [Fact]
    public async Task FailNextStockDebit_is_not_mapped_in_production()
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/debug/fail-next-stock-debit", new
        {
            mode = "BeforeProcessing"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.ConfigureServices(services =>
                {
                    services.AddDbContext<InventoryDbContext>(options =>
                        options.UseNpgsql(ConnectionString));
                });
            });
    }

    private static async Task<ProductResponse> CreateProductAsync(
        HttpClient client,
        string codePrefix,
        int balance)
    {
        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"{codePrefix}-{Guid.NewGuid():N}",
            $"{codePrefix} produto",
            balance));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    private async Task<int> GetBalanceAsync(
        Guid productId,
        WebApplicationFactory<Program>? factory = null)
    {
        using var scope = (factory ?? _factory).Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        return await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => product.Balance)
            .SingleAsync();
    }

    private async Task SetBalanceAsync(Guid productId, int balance)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var product = await dbContext.Products.SingleAsync(product => product.Id == productId);
        product.Balance = balance;
        product.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private async Task<bool> HasStockOperationAsync(
        Guid operationId,
        WebApplicationFactory<Program>? factory = null)
    {
        using var scope = (factory ?? _factory).Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        return await dbContext.StockOperations
            .AsNoTracking()
            .AnyAsync(operation => operation.OperationId == operationId);
    }
}
