namespace Billing.Api.Application.Inventory;

public sealed class InventoryDebitResult
{
    private InventoryDebitResult(string status, bool isSuccess, bool isInsufficientStock, bool isUnavailable)
    {
        Status = status;
        IsSuccess = isSuccess;
        IsInsufficientStock = isInsufficientStock;
        IsUnavailable = isUnavailable;
    }

    public string Status { get; }

    public bool IsSuccess { get; }

    public bool IsInsufficientStock { get; }

    public bool IsUnavailable { get; }

    public static InventoryDebitResult Processed()
    {
        return new InventoryDebitResult("Processed", true, false, false);
    }

    public static InventoryDebitResult AlreadyProcessed()
    {
        return new InventoryDebitResult("AlreadyProcessed", true, false, false);
    }

    public static InventoryDebitResult InsufficientStock()
    {
        return new InventoryDebitResult("InsufficientStock", false, true, false);
    }

    public static InventoryDebitResult Unavailable()
    {
        return new InventoryDebitResult("Unavailable", false, false, true);
    }
}
