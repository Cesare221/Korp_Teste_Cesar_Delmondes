using System.Net;
using System.Net.Http.Json;
using Inventory.Api.Contracts;
using Inventory.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Inventory.Tests;

public sealed class ProductApiTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductApiTests()
    {
        var databaseName = $"inventory-products-{Guid.NewGuid()}";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<InventoryDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<InventoryDbContext>>();
                    services.AddDbContext<InventoryDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                });
            });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task PostProducts_creates_valid_product_with_trimmed_values()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            " PROD-001 ",
            " Produto de teste ",
            10));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        Assert.Equal("PROD-001", product.Code);
        Assert.Equal("Produto de teste", product.Description);
        Assert.Equal(10, product.Balance);
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.NotEqual(default, product.CreatedAt);
        Assert.Equal(product.CreatedAt, product.UpdatedAt);
        Assert.NotNull(response.Headers.Location);
    }

    [Theory]
    [InlineData("", "Produto", 0)]
    [InlineData("   ", "Produto", 0)]
    public async Task PostProducts_rejects_missing_code(string code, string description, int balance)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            code,
            description,
            balance));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("PROD-001", "", 0)]
    [InlineData("PROD-001", "   ", 0)]
    public async Task PostProducts_rejects_missing_description(string code, string description, int balance)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            code,
            description,
            balance));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostProducts_rejects_negative_balance()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            "PROD-002",
            "Saldo invalido",
            -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostProducts_rejects_duplicate_code()
    {
        using var client = _factory.CreateClient();

        var first = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            "PROD-003",
            "Produto original",
            5));
        var second = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            "PROD-003",
            "Produto duplicado",
            8));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetProducts_returns_created_products()
    {
        using var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            "PROD-004",
            "Produto listado",
            11));

        var products = await client.GetFromJsonAsync<ProductResponse[]>("/api/products");

        Assert.NotNull(products);
        Assert.Contains(products, product => product.Code == "PROD-004");
    }

    [Fact]
    public async Task GetProduct_returns_not_found_for_unknown_id()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LookupProducts_returns_all_matching_products_without_mutating_them()
    {
        using var client = _factory.CreateClient();

        var first = await CreateProductAsync(client, "PROD-LOOK-001", "Produto lookup A", 7);
        var second = await CreateProductAsync(client, "PROD-LOOK-002", "Produto lookup B", 9);

        var response = await client.PostAsJsonAsync("/api/products/lookup", new
        {
            ids = new[] { first.Id, second.Id }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<ProductResponse[]>();

        Assert.NotNull(products);
        Assert.Equal(2, products.Length);
        Assert.Contains(products, product =>
            product.Id == first.Id &&
            product.Code == "PROD-LOOK-001" &&
            product.Description == "Produto lookup A" &&
            product.Balance == 7);
        Assert.Contains(products, product =>
            product.Id == second.Id &&
            product.Code == "PROD-LOOK-002" &&
            product.Description == "Produto lookup B" &&
            product.Balance == 9);

        var afterLookup = await client.GetFromJsonAsync<ProductResponse[]>("/api/products");
        Assert.Contains(afterLookup!, product => product.Id == first.Id && product.Balance == 7);
        Assert.Contains(afterLookup!, product => product.Id == second.Id && product.Balance == 9);
    }

    [Fact]
    public async Task LookupProducts_ignores_unknown_ids()
    {
        using var client = _factory.CreateClient();
        var existing = await CreateProductAsync(client, "PROD-LOOK-003", "Produto lookup C", 3);

        var response = await client.PostAsJsonAsync("/api/products/lookup", new
        {
            ids = new[] { existing.Id, Guid.NewGuid() }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<ProductResponse[]>();

        Assert.NotNull(products);
        var product = Assert.Single(products);
        Assert.Equal(existing.Id, product.Id);
    }

    [Fact]
    public async Task LookupProducts_accepts_empty_id_list()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products/lookup", new
        {
            ids = Array.Empty<Guid>()
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<ProductResponse[]>();

        Assert.NotNull(products);
        Assert.Empty(products);
    }

    private static async Task<ProductResponse> CreateProductAsync(
        HttpClient client,
        string code,
        string description,
        int balance)
    {
        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            code,
            description,
            balance));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }
}
