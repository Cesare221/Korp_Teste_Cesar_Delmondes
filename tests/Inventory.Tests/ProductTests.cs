using Inventory.Api.Domain;
using Inventory.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Inventory.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Constructor_initializes_identity_and_timestamps_for_new_product()
    {
        var product = new Product();

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.NotEqual(default, product.CreatedAt);
        Assert.NotEqual(default, product.UpdatedAt);
    }

    [Fact]
    public void Model_configures_product_code_as_unique_index()
    {
        using var context = CreateContext();

        var productEntity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Product));
        var codeIndex = productEntity?.GetIndexes()
            .SingleOrDefault(index => index.Properties.Any(property => property.Name == nameof(Product.Code)));

        Assert.NotNull(codeIndex);
        Assert.True(codeIndex.IsUnique);
        Assert.Equal("ux_products_code", codeIndex.GetDatabaseName());
    }

    [Fact]
    public void Model_configures_balance_as_non_negative_check_constraint()
    {
        using var context = CreateContext();

        var productEntity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Product));
        var balanceConstraint = productEntity?.GetCheckConstraints()
            .SingleOrDefault(constraint => constraint.Name == "ck_products_balance_non_negative");

        Assert.NotNull(balanceConstraint);
        Assert.Equal("balance >= 0", balanceConstraint.Sql);
    }

    private static InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql("Host=localhost;Database=inventory_test;Username=test;Password=test")
            .Options;

        return new InventoryDbContext(options);
    }
}
