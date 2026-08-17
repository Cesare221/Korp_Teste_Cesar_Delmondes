namespace Billing.Api.Application.Inventory;

public interface IInventoryClient
{
    Task<InventoryLookupResult> LookupProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    Task<InventoryDebitResult> DebitStockAsync(
        Guid operationId,
        IReadOnlyCollection<InventoryDebitItem> items,
        CancellationToken cancellationToken);
}
