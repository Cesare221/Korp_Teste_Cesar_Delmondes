using Inventory.Api.Contracts;

namespace Inventory.Api.Application.Products;

public interface IProductService
{
    Task<ProductResult> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductResponse>> ListAsync(CancellationToken cancellationToken);

    Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductResponse>> LookupAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
}
