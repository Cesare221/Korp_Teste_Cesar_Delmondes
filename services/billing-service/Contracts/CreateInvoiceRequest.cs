namespace Billing.Api.Contracts;

public sealed record CreateInvoiceRequest(IReadOnlyList<CreateInvoiceItemRequest>? Items);
