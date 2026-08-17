namespace Billing.Api.Contracts;

public sealed record CreateInvoiceItemRequest(Guid ProductId, int Quantity);
