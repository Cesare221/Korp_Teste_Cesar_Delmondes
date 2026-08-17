namespace Billing.Api.Contracts;

public sealed record InvoiceItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductDescription,
    int Quantity);
