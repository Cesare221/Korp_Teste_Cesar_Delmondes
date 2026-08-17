namespace Billing.Api.Application.Inventory;

public sealed class InventoryLookupResult
{
    private InventoryLookupResult(bool isUnavailable, IReadOnlyList<InventoryProduct> products)
    {
        IsUnavailable = isUnavailable;
        Products = products;
    }

    public bool IsUnavailable { get; }

    public IReadOnlyList<InventoryProduct> Products { get; }

    public static InventoryLookupResult Success(IReadOnlyList<InventoryProduct> products)
    {
        return new InventoryLookupResult(false, products);
    }

    public static InventoryLookupResult Unavailable()
    {
        return new InventoryLookupResult(true, []);
    }
}
