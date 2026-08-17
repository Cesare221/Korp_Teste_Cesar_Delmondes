using Inventory.Api.Contracts;

namespace Inventory.Api.Application.Stock;

public sealed class StockDebitResult
{
    private StockDebitResult(
        StockDebitResponse? response,
        Dictionary<string, string[]>? validationErrors,
        string? errorCode,
        IReadOnlyList<Guid>? productIds)
    {
        Response = response;
        ValidationErrors = validationErrors;
        ErrorCode = errorCode;
        ProductIds = productIds;
    }

    public StockDebitResponse? Response { get; }

    public Dictionary<string, string[]>? ValidationErrors { get; }

    public string? ErrorCode { get; }

    public IReadOnlyList<Guid>? ProductIds { get; }

    public static StockDebitResult Processed(Guid operationId)
    {
        return new StockDebitResult(new StockDebitResponse(operationId, "Processed"), null, null, null);
    }

    public static StockDebitResult AlreadyProcessed(Guid operationId)
    {
        return new StockDebitResult(new StockDebitResponse(operationId, "AlreadyProcessed"), null, null, null);
    }

    public static StockDebitResult FailedValidation(Dictionary<string, string[]> validationErrors)
    {
        return new StockDebitResult(null, validationErrors, null, null);
    }

    public static StockDebitResult Failed(string errorCode, IReadOnlyList<Guid> productIds)
    {
        return new StockDebitResult(null, null, errorCode, productIds);
    }

    public static StockDebitResult TemporarilyUnavailable()
    {
        return new StockDebitResult(null, null, StockErrors.TemporarilyUnavailable, null);
    }
}
