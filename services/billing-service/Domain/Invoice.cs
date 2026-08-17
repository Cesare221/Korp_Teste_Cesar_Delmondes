namespace Billing.Api.Domain;

public sealed class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public long Number { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    public List<InvoiceItem> Items { get; set; } = [];
}
