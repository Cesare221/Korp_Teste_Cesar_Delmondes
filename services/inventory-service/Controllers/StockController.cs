using Inventory.Api.Application.Stock;
using Inventory.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/stock")]
public sealed class StockController(IStockService stockService) : ControllerBase
{
    [HttpPost("debit")]
    [ProducesResponseType(typeof(StockDebitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StockDebitResponse>> Debit(
        StockDebitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await stockService.DebitAsync(request, cancellationToken);

        if (result.ValidationErrors is not null)
        {
            return BadRequest(WithTraceId(new ValidationProblemDetails(result.ValidationErrors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Instance = HttpContext.Request.Path
            }));
        }

        if (result.ErrorCode == StockErrors.TemporarilyUnavailable)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, WithTraceId(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Stock service temporarily unavailable",
                Detail = "A transient stock processing failure was simulated.",
                Instance = HttpContext.Request.Path
            }));
        }

        if (result.ErrorCode == StockErrors.ProductNotFound)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Product not found",
                Detail = "One or more products do not exist.",
                Instance = HttpContext.Request.Path
            };
            problemDetails = WithTraceId(problemDetails);
            problemDetails.Extensions["productIds"] = result.ProductIds;

            return NotFound(problemDetails);
        }

        if (result.ErrorCode == StockErrors.InsufficientStock)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Insufficient stock",
                Detail = "One or more products do not have sufficient stock.",
                Instance = HttpContext.Request.Path
            };
            problemDetails = WithTraceId(problemDetails);
            problemDetails.Extensions["productIds"] = result.ProductIds;

            return Conflict(problemDetails);
        }

        return Ok(result.Response);
    }

    private ProblemDetails WithTraceId(ProblemDetails problemDetails)
    {
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return problemDetails;
    }
}
