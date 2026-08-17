namespace Billing.Api.Contracts;

public sealed record InvoiceListItemResponse(
    Guid Id,
    long Number,
    string Status,
    DateTime CreatedAt,
    int ItemCount);
