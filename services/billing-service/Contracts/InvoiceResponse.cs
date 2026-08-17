namespace Billing.Api.Contracts;

public sealed record InvoiceResponse(
    Guid Id,
    long Number,
    string Status,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    IReadOnlyList<InvoiceItemResponse> Items);
