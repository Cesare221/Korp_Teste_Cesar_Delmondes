using Inventory.Api.Contracts;

namespace Inventory.Api.Application.Stock;

public interface IStockService
{
    Task<StockDebitResult> DebitAsync(StockDebitRequest request, CancellationToken cancellationToken);
}
