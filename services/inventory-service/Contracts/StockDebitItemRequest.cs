namespace Inventory.Api.Contracts;

public sealed record StockDebitItemRequest(Guid ProductId, int Quantity);
