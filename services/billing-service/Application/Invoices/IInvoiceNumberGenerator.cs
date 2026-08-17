namespace Billing.Api.Application.Invoices;

public interface IInvoiceNumberGenerator
{
    Task<long> GenerateAsync(CancellationToken cancellationToken);
}
