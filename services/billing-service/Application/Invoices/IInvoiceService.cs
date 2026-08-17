using Billing.Api.Contracts;

namespace Billing.Api.Application.Invoices;

public interface IInvoiceService
{
    Task<InvoiceResult> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<InvoiceListItemResponse>> ListAsync(CancellationToken cancellationToken);

    Task<InvoiceResult> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<InvoiceResult> PrintAsync(Guid id, CancellationToken cancellationToken);
}
