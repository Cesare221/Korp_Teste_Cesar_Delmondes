using Billing.Api.Application.Inventory;
using Billing.Api.Contracts;
using Billing.Api.Domain;
using Billing.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api.Application.Invoices;

public sealed class InvoiceService(
    BillingDbContext dbContext,
    IInventoryClient inventoryClient,
    IInvoiceNumberGenerator invoiceNumberGenerator,
    ILogger<InvoiceService> logger) : IInvoiceService
{
    public async Task<InvoiceResult> CreateAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Invoice creation started");

        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return InvoiceResult.FailedValidation(validationErrors);
        }

        var requestedItems = request.Items!;
        var requestedProductIds = requestedItems
            .Select(item => item.ProductId)
            .Distinct()
            .ToArray();

        var lookupResult = await inventoryClient.LookupProductsAsync(
            requestedProductIds,
            cancellationToken);

        if (lookupResult.IsUnavailable)
        {
            logger.LogWarning("Inventory validation failed because Inventory Service is unavailable");
            return InvoiceResult.Failed(InvoiceErrors.InventoryUnavailable);
        }

        var productsById = lookupResult.Products
            .ToDictionary(product => product.Id);

        var invalidProductIds = requestedProductIds
            .Where(productId => !productsById.ContainsKey(productId))
            .ToArray();

        if (invalidProductIds.Length > 0)
        {
            logger.LogWarning(
                "Inventory validation failed for {InvalidProductCount} products",
                invalidProductIds.Length);
            return InvoiceResult.Failed(InvoiceErrors.InvalidProducts, invalidProductIds);
        }

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Number = await invoiceNumberGenerator.GenerateAsync(cancellationToken),
            Status = InvoiceStatus.Open,
            CreatedAt = DateTime.UtcNow,
            ClosedAt = null,
            Items = requestedItems
                .Select(item =>
                {
                    var product = productsById[item.ProductId];
                    return new InvoiceItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        ProductCode = product.Code,
                        ProductDescription = product.Description,
                        Quantity = item.Quantity
                    };
                })
                .ToList()
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Invoice created with InvoiceId {InvoiceId} and InvoiceNumber {InvoiceNumber}",
            invoice.Id,
            invoice.Number);

        return InvoiceResult.Created(ToResponse(invoice));
    }

    public async Task<IReadOnlyList<InvoiceListItemResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Invoices
            .AsNoTracking()
            .OrderByDescending(invoice => invoice.Number)
            .Select(invoice => new InvoiceListItemResponse(
                invoice.Id,
                invoice.Number,
                invoice.Status.ToString(),
                invoice.CreatedAt,
                invoice.Items.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<InvoiceResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.Id == id)
            .Select(invoice => new InvoiceResponse(
                invoice.Id,
                invoice.Number,
                invoice.Status.ToString(),
                invoice.CreatedAt,
                invoice.ClosedAt,
                invoice.Items
                    .OrderBy(item => item.ProductCode)
                    .Select(item => new InvoiceItemResponse(
                        item.Id,
                        item.ProductId,
                        item.ProductCode,
                        item.ProductDescription,
                        item.Quantity))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            return InvoiceResult.Failed(InvoiceErrors.NotFound);
        }

        return InvoiceResult.Found(invoice);
    }

    public async Task<InvoiceResult> PrintAsync(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Invoice print started for InvoiceId {InvoiceId}", id);

        var invoice = await dbContext.Invoices
            .Include(invoice => invoice.Items)
            .SingleOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);

        if (invoice is null)
        {
            return InvoiceResult.Failed(InvoiceErrors.NotFound);
        }

        if (invoice.Status != InvoiceStatus.Open)
        {
            logger.LogWarning(
                "Invoice print failed because InvoiceId {InvoiceId} with InvoiceNumber {InvoiceNumber} is not open",
                invoice.Id,
                invoice.Number);
            return InvoiceResult.Failed(InvoiceErrors.CannotPrint);
        }

        var debitResult = await inventoryClient.DebitStockAsync(
            invoice.Id,
            invoice.Items
                .Select(item => new InventoryDebitItem(item.ProductId, item.Quantity))
                .ToArray(),
            cancellationToken);

        if (debitResult.IsInsufficientStock)
        {
            logger.LogWarning(
                "Invoice print failed due to insufficient stock for InvoiceId {InvoiceId} and InvoiceNumber {InvoiceNumber}",
                invoice.Id,
                invoice.Number);
            return InvoiceResult.Failed(InvoiceErrors.InsufficientStock);
        }

        if (debitResult.IsUnavailable)
        {
            logger.LogWarning(
                "Inventory unavailable; invoice remains open for InvoiceId {InvoiceId} and InvoiceNumber {InvoiceNumber}",
                invoice.Id,
                invoice.Number);
            return InvoiceResult.Failed(InvoiceErrors.InventoryUnavailable);
        }

        if (debitResult.Status == "AlreadyProcessed")
        {
            logger.LogInformation(
                "Inventory operation already processed for InvoiceId {InvoiceId} and OperationId {OperationId}",
                invoice.Id,
                invoice.Id);
        }

        invoice.Status = InvoiceStatus.Closed;
        invoice.ClosedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Invoice {InvoiceId} closed after stock operation {OperationId}",
            invoice.Id,
            invoice.Id);

        return InvoiceResult.Found(ToResponse(invoice));
    }

    private static Dictionary<string, string[]> Validate(CreateInvoiceRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Items is null || request.Items.Count == 0)
        {
            errors[nameof(CreateInvoiceRequest.Items)] = ["At least one invoice item is required."];
            return errors;
        }

        var invalidQuantityIndexes = request.Items
            .Select((item, index) => new { item.Quantity, index })
            .Where(item => item.Quantity <= 0)
            .Select(item => item.index)
            .ToArray();

        if (invalidQuantityIndexes.Length > 0)
        {
            errors[nameof(CreateInvoiceItemRequest.Quantity)] = ["Quantity must be greater than zero."];
        }

        var duplicatedProductIds = request.Items
            .GroupBy(item => item.ProductId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicatedProductIds.Length > 0)
        {
            errors[nameof(CreateInvoiceItemRequest.ProductId)] = ["The same product cannot be added twice."];
        }

        return errors;
    }

    private static InvoiceResponse ToResponse(Invoice invoice)
    {
        return new InvoiceResponse(
            invoice.Id,
            invoice.Number,
            invoice.Status.ToString(),
            invoice.CreatedAt,
            invoice.ClosedAt,
            invoice.Items
                .OrderBy(item => item.ProductCode)
                .Select(item => new InvoiceItemResponse(
                    item.Id,
                    item.ProductId,
                    item.ProductCode,
                    item.ProductDescription,
                    item.Quantity))
                .ToList());
    }
}
