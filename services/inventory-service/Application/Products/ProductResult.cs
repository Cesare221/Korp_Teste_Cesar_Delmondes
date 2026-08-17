using Inventory.Api.Contracts;

namespace Inventory.Api.Application.Products;

public sealed record ProductResult(
    bool Success,
    ProductResponse? Product,
    string? ErrorCode,
    Dictionary<string, string[]>? ValidationErrors)
{
    public static ProductResult Created(ProductResponse product)
    {
        return new ProductResult(true, product, null, null);
    }

    public static ProductResult FailedValidation(Dictionary<string, string[]> errors)
    {
        return new ProductResult(false, null, null, errors);
    }

    public static ProductResult Failed(string errorCode)
    {
        return new ProductResult(false, null, errorCode, null);
    }
}
