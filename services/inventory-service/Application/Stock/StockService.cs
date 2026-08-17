using Inventory.Api.Contracts;
using Inventory.Api.Application.Debug;
using Inventory.Api.Domain;
using Inventory.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Application.Stock;

public sealed class StockService(
    InventoryDbContext dbContext,
    IFailureSimulationService failureSimulationService,
    ILogger<StockService> logger) : IStockService
{
    public async Task<StockDebitResult> DebitAsync(
        StockDebitRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Stock debit started for OperationId {OperationId}",
            request.OperationId);

        var shouldSimulateFailure = failureSimulationService.TryConsume(out var failureMode);
        if (shouldSimulateFailure && failureMode == FailureSimulationMode.BeforeProcessing)
        {
            logger.LogWarning(
                "Simulated failure before processing stock debit for OperationId {OperationId}",
                request.OperationId);
            return StockDebitResult.TemporarilyUnavailable();
        }

        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return StockDebitResult.FailedValidation(validationErrors);
        }

        var orderedItems = request.Items!
            .OrderBy(item => item.ProductId)
            .ToArray();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var insertedOperation = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO stock_operations (operation_id, processed_at)
            VALUES ({request.OperationId}, {now})
            ON CONFLICT (operation_id) DO NOTHING
            """, cancellationToken);

        if (insertedOperation == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation(
                "Stock operation already processed for OperationId {OperationId}",
                request.OperationId);
            return StockDebitResult.AlreadyProcessed(request.OperationId);
        }

        var failedProductIds = new List<Guid>();
        var missingProductIds = new List<Guid>();

        foreach (var item in orderedItems)
        {
            var affectedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE products
                SET balance = balance - {item.Quantity},
                    updated_at = {now}
                WHERE id = {item.ProductId}
                AND balance >= {item.Quantity}
                """, cancellationToken);

            if (affectedRows > 0)
            {
                continue;
            }

            var productExists = await dbContext.Products
                .AsNoTracking()
                .AnyAsync(product => product.Id == item.ProductId, cancellationToken);

            if (productExists)
            {
                failedProductIds.Add(item.ProductId);
            }
            else
            {
                missingProductIds.Add(item.ProductId);
            }
        }

        if (missingProductIds.Count > 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogWarning(
                "Stock debit failed because products were not found for OperationId {OperationId}",
                request.OperationId);
            return StockDebitResult.Failed(StockErrors.ProductNotFound, missingProductIds);
        }

        if (failedProductIds.Count > 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogWarning(
                "Insufficient stock for OperationId {OperationId}",
                request.OperationId);
            return StockDebitResult.Failed(StockErrors.InsufficientStock, failedProductIds);
        }

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation(
            "Stock debit committed for OperationId {OperationId}",
            request.OperationId);

        if (shouldSimulateFailure && failureMode == FailureSimulationMode.AfterCommit)
        {
            logger.LogWarning(
                "Simulated communication failure after commit for OperationId {OperationId}",
                request.OperationId);
            return StockDebitResult.TemporarilyUnavailable();
        }

        return StockDebitResult.Processed(request.OperationId);
    }

    private static Dictionary<string, string[]> Validate(StockDebitRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.OperationId == Guid.Empty)
        {
            errors[nameof(StockDebitRequest.OperationId)] = ["OperationId is required."];
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            errors[nameof(StockDebitRequest.Items)] = ["At least one stock item is required."];
            return errors;
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            errors[nameof(StockDebitItemRequest.Quantity)] = ["Quantity must be greater than zero."];
        }

        if (request.Items
            .GroupBy(item => item.ProductId)
            .Any(group => group.Count() > 1))
        {
            errors[nameof(StockDebitItemRequest.ProductId)] = ["The same product cannot be debited twice."];
        }

        return errors;
    }
}
