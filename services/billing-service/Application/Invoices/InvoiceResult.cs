using Billing.Api.Contracts;

namespace Billing.Api.Application.Invoices;

public sealed class InvoiceResult
{
    private InvoiceResult(
        InvoiceResponse? invoice,
        IReadOnlyList<InvoiceListItemResponse>? invoices,
        Dictionary<string, string[]>? validationErrors,
        string? errorCode,
        IReadOnlyList<Guid>? invalidProductIds)
    {
        Invoice = invoice;
        Invoices = invoices;
        ValidationErrors = validationErrors;
        ErrorCode = errorCode;
        InvalidProductIds = invalidProductIds;
    }

    public InvoiceResponse? Invoice { get; }

    public IReadOnlyList<InvoiceListItemResponse>? Invoices { get; }

    public Dictionary<string, string[]>? ValidationErrors { get; }

    public string? ErrorCode { get; }

    public IReadOnlyList<Guid>? InvalidProductIds { get; }

    public static InvoiceResult Created(InvoiceResponse invoice)
    {
        return new InvoiceResult(invoice, null, null, null, null);
    }

    public static InvoiceResult Listed(IReadOnlyList<InvoiceListItemResponse> invoices)
    {
        return new InvoiceResult(null, invoices, null, null, null);
    }

    public static InvoiceResult Found(InvoiceResponse invoice)
    {
        return new InvoiceResult(invoice, null, null, null, null);
    }

    public static InvoiceResult FailedValidation(Dictionary<string, string[]> validationErrors)
    {
        return new InvoiceResult(null, null, validationErrors, null, null);
    }

    public static InvoiceResult Failed(string errorCode, IReadOnlyList<Guid>? invalidProductIds = null)
    {
        return new InvoiceResult(null, null, null, errorCode, invalidProductIds);
    }
}
