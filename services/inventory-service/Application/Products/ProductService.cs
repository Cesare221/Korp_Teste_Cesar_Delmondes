using Inventory.Api.Contracts;
using Inventory.Api.Domain;
using Inventory.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inventory.Api.Application.Products;

public sealed class ProductService(InventoryDbContext dbContext) : IProductService
{
    private const string UniqueViolation = "23505";

    public async Task<ProductResult> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var code = request.Code?.Trim() ?? string.Empty;
        var description = request.Description?.Trim() ?? string.Empty;

        var validationErrors = Validate(code, description, request.Balance);
        if (validationErrors.Count > 0)
        {
            return ProductResult.FailedValidation(validationErrors);
        }

        var codeAlreadyExists = await dbContext.Products
            .AsNoTracking()
            .AnyAsync(product => product.Code == code, cancellationToken);

        if (codeAlreadyExists)
        {
            return ProductResult.Failed(ProductErrors.DuplicateCode);
        }

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = code,
            Description = description,
            Balance = request.Balance,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Products.Add(product);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueCodeViolation(exception))
        {
            return ProductResult.Failed(ProductErrors.DuplicateCode);
        }

        return ProductResult.Created(ToResponse(product));
    }

    public async Task<IReadOnlyList<ProductResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Code)
            .Select(product => new ProductResponse(
                product.Id,
                product.Code,
                product.Description,
                product.Balance,
                product.CreatedAt,
                product.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new ProductResponse(
                product.Id,
                product.Code,
                product.Description,
                product.Balance,
                product.CreatedAt,
                product.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductResponse>> LookupAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var distinctIds = ids.Distinct().ToArray();

        return await dbContext.Products
            .AsNoTracking()
            .Where(product => distinctIds.Contains(product.Id))
            .OrderBy(product => product.Code)
            .Select(product => new ProductResponse(
                product.Id,
                product.Code,
                product.Description,
                product.Balance,
                product.CreatedAt,
                product.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    private static Dictionary<string, string[]> Validate(string code, string description, int balance)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(code))
        {
            errors[nameof(CreateProductRequest.Code)] = ["Code is required."];
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors[nameof(CreateProductRequest.Description)] = ["Description is required."];
        }

        if (balance < 0)
        {
            errors[nameof(CreateProductRequest.Balance)] = ["Balance cannot be negative."];
        }

        return errors;
    }

    private static bool IsUniqueCodeViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == UniqueViolation
            && postgresException.ConstraintName == "ux_products_code";
    }

    private static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Code,
            product.Description,
            product.Balance,
            product.CreatedAt,
            product.UpdatedAt);
    }
}
