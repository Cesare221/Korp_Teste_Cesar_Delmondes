using Inventory.Api.Application.Debug;
using Inventory.Api.Application.Products;
using Inventory.Api.Application.Stock;
using Inventory.Api.Contracts;
using Inventory.Api.Infrastructure;
using Inventory.Api.Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddSingleton<IFailureSimulationService, FailureSimulationService>();

var connectionString = builder.Configuration.GetConnectionString("InventoryDb")
    ?? throw new InvalidOperationException("Connection string 'InventoryDb' was not configured.");

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddCheck<DbContextHealthCheck<InventoryDbContext>>("inventory-db");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDevelopment", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Unexpected server error",
            Detail = app.Environment.IsDevelopment()
                ? "An unhandled exception occurred while processing the request."
                : null,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = problemDetails.Status.Value;
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapPost("/debug/fail-next-stock-debit", (
        ArmFailureSimulationRequest request,
        IFailureSimulationService failureSimulationService,
        ILogger<FailureSimulationService> logger) =>
    {
        if (request.Mode is null ||
            !Enum.TryParse<FailureSimulationMode>(request.Mode, ignoreCase: false, out var mode) ||
            !Enum.IsDefined(mode))
        {
            return Results.BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(ArmFailureSimulationRequest.Mode)] =
                [
                    "Mode must be either BeforeProcessing or AfterCommit."
                ]
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            });
        }

        failureSimulationService.Arm(mode);
        logger.LogWarning(
            "Failure simulation armed for next stock debit with Mode {FailureSimulationMode}",
            mode);

        return Results.NoContent();
    })
    .WithName("FailNextStockDebit")
    .WithTags("Debug")
    .Produces(StatusCodes.Status204NoContent)
    .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseCors("AngularDevelopment");
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
        };

        await context.Response.WriteAsJsonAsync(response);
    }
});

app.Run();

public partial class Program;
