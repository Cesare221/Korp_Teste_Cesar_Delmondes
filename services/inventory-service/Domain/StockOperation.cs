namespace Inventory.Api.Domain;

public sealed class StockOperation
{
    public Guid OperationId { get; set; }

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
