using Billing.Api.Application.Invoices;
using Billing.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InvoiceResponse>> Create(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await invoiceService.CreateAsync(request, cancellationToken);

        if (result.ValidationErrors is not null)
        {
            return BadRequest(WithTraceId(new ValidationProblemDetails(result.ValidationErrors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Instance = HttpContext.Request.Path
            }));
        }

        if (result.ErrorCode == InvoiceErrors.InvalidProducts)
        {
            var problemDetails = WithTraceId(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Invalid invoice items",
                Detail = "One or more products do not exist.",
                Instance = HttpContext.Request.Path
            });
            problemDetails.Extensions["invalidProductIds"] = result.InvalidProductIds;

            return UnprocessableEntity(problemDetails);
        }

        if (result.ErrorCode == InvoiceErrors.InventoryUnavailable)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, WithTraceId(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Inventory Service unavailable",
                Detail = "Products could not be validated at this moment.",
                Instance = HttpContext.Request.Path
            }));
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Invoice!.Id }, result.Invoice);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<InvoiceListItemResponse>>> List(
        CancellationToken cancellationToken)
    {
        var invoices = await invoiceService.ListAsync(cancellationToken);

        return Ok(invoices);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InvoiceResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await invoiceService.GetByIdAsync(id, cancellationToken);

        if (result.ErrorCode == InvoiceErrors.NotFound)
        {
            return NotFound(WithTraceId(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Invoice not found",
                Detail = "No invoice was found for the provided id.",
                Instance = HttpContext.Request.Path
            }));
        }

        return Ok(result.Invoice);
    }

    [HttpPost("{id:guid}/print")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InvoiceResponse>> Print(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await invoiceService.PrintAsync(id, cancellationToken);

        if (result.ErrorCode == InvoiceErrors.NotFound)
        {
            return NotFound(WithTraceId(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Invoice not found",
                Detail = "No invoice was found for the provided id.",
                Instance = HttpContext.Request.Path
            }));
        }

        if (result.ErrorCode == InvoiceErrors.CannotPrint)
        {
            return Conflict(WithTraceId(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Invoice cannot be printed",
                Detail = "Only open invoices can be printed.",
                Instance = HttpContext.Request.Path
            }));
        }

        if (result.ErrorCode == InvoiceErrors.InsufficientStock)
        {
            return Conflict(WithTraceId(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Insufficient stock",
                Detail = "One or more products do not have sufficient stock.",
                Instance = HttpContext.Request.Path
            }));
        }

        if (result.ErrorCode == InvoiceErrors.InventoryUnavailable)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, WithTraceId(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Inventory Service unavailable",
                Detail = "Stock could not be debited at this moment.",
                Instance = HttpContext.Request.Path
            }));
        }

        return Ok(result.Invoice);
    }

    private ProblemDetails WithTraceId(ProblemDetails problemDetails)
    {
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return problemDetails;
    }
}
