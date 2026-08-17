using Billing.Api.Domain;
using Billing.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Billing.Tests;

public sealed class InvoiceTests
{
    [Fact]
    public void Constructor_creates_open_invoice_without_closed_date()
    {
        var invoice = new Invoice();

        Assert.NotEqual(Guid.Empty, invoice.Id);
        Assert.Equal(InvoiceStatus.Open, invoice.Status);
        Assert.Null(invoice.ClosedAt);
        Assert.Empty(invoice.Items);
    }

    [Fact]
    public void Model_configures_invoice_number_as_unique_sequence_backed_value()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var invoiceEntity = model.FindEntityType(typeof(Invoice));
        var numberProperty = invoiceEntity?.FindProperty(nameof(Invoice.Number));
        var numberIndex = invoiceEntity?.GetIndexes()
            .SingleOrDefault(index => index.Properties.Any(property => property.Name == nameof(Invoice.Number)));
        var sequence = model.FindSequence("invoice_number_seq", "billing");

        Assert.NotNull(numberProperty);
        Assert.Equal("nextval('billing.invoice_number_seq')", numberProperty.GetDefaultValueSql());
        Assert.NotNull(numberIndex);
        Assert.True(numberIndex.IsUnique);
        Assert.NotNull(sequence);
    }

    [Fact]
    public void Model_configures_invoice_item_quantity_as_positive_check_constraint()
    {
        using var context = CreateContext();

        var itemEntity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(InvoiceItem));
        var quantityConstraint = itemEntity?.GetCheckConstraints()
            .SingleOrDefault(constraint => constraint.Name == "ck_invoice_items_quantity_positive");

        Assert.NotNull(quantityConstraint);
        Assert.Equal("quantity > 0", quantityConstraint.Sql);
    }

    private static BillingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql("Host=localhost;Database=billing_test;Username=test;Password=test")
            .Options;

        return new BillingDbContext(options);
    }
}
