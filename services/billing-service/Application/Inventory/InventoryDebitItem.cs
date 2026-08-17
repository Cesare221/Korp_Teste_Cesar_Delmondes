namespace Billing.Api.Application.Inventory;

public sealed record InventoryDebitItem(Guid ProductId, int Quantity);
