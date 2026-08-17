namespace Inventory.Api.Contracts;

public sealed record StockDebitResponse(Guid OperationId, string Status);
