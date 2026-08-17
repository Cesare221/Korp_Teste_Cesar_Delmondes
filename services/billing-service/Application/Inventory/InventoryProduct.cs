namespace Billing.Api.Application.Inventory;

public sealed record InventoryProduct(Guid Id, string Code, string Description, int Balance);
