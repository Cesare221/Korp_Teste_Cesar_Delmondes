using System.Net;
using System.Net.Http.Json;

namespace Billing.Api.Application.Inventory;

public sealed class InventoryClient(
    HttpClient httpClient,
    ILogger<InventoryClient> logger) : IInventoryClient
{
    public async Task<InventoryLookupResult> LookupProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/products/lookup",
                new ProductLookupRequest(productIds),
                cancellationToken);

            if (response.StatusCode >= HttpStatusCode.InternalServerError)
            {
                logger.LogWarning(
                    "Inventory validation failed with status {StatusCode}",
                    (int)response.StatusCode);
                return InventoryLookupResult.Unavailable();
            }

            response.EnsureSuccessStatusCode();

            var products = await response.Content.ReadFromJsonAsync<List<InventoryProduct>>(
                cancellationToken);

            return InventoryLookupResult.Success(products ?? []);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Inventory validation timed out");
            return InventoryLookupResult.Unavailable();
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Inventory validation failed");
            return InventoryLookupResult.Unavailable();
        }
    }

    public async Task<InventoryDebitResult> DebitStockAsync(
        Guid operationId,
        IReadOnlyCollection<InventoryDebitItem> items,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/stock/debit",
                new StockDebitRequest(operationId, items),
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Conflict ||
                response.StatusCode == HttpStatusCode.NotFound)
            {
                return InventoryDebitResult.InsufficientStock();
            }

            if (response.StatusCode >= HttpStatusCode.InternalServerError)
            {
                logger.LogWarning(
                    "Stock debit failed with status {StatusCode} for OperationId {OperationId}",
                    (int)response.StatusCode,
                    operationId);
                return InventoryDebitResult.Unavailable();
            }

            response.EnsureSuccessStatusCode();

            var debitResponse = await response.Content.ReadFromJsonAsync<StockDebitResponse>(
                cancellationToken);

            if (debitResponse?.Status == "AlreadyProcessed")
            {
                logger.LogInformation(
                    "Inventory operation already processed for OperationId {OperationId}",
                    operationId);
            }

            return debitResponse?.Status == "AlreadyProcessed"
                ? InventoryDebitResult.AlreadyProcessed()
                : InventoryDebitResult.Processed();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Stock debit timed out for OperationId {OperationId}",
                operationId);
            return InventoryDebitResult.Unavailable();
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Stock debit failed for OperationId {OperationId}",
                operationId);
            return InventoryDebitResult.Unavailable();
        }
    }

    private sealed record ProductLookupRequest(IReadOnlyCollection<Guid> Ids);

    private sealed record StockDebitRequest(
        Guid OperationId,
        IReadOnlyCollection<InventoryDebitItem> Items);

    private sealed record StockDebitResponse(Guid OperationId, string Status);
}
