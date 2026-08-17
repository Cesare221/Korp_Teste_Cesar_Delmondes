namespace Inventory.Api.Contracts;

public sealed record StockDebitRequest(Guid OperationId, IReadOnlyList<StockDebitItemRequest>? Items);
