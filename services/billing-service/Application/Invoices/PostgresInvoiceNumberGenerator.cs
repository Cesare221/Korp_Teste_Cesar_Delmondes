using Billing.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api.Application.Invoices;

public sealed class PostgresInvoiceNumberGenerator(BillingDbContext dbContext) : IInvoiceNumberGenerator
{
    public async Task<long> GenerateAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Database
            .SqlQueryRaw<long>("SELECT nextval('billing.invoice_number_seq') AS \"Value\"")
            .SingleAsync(cancellationToken);
    }
}
