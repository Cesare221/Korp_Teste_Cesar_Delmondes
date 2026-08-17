namespace Inventory.Api.Contracts;

public sealed record ProductResponse(
    Guid Id,
    string Code,
    string Description,
    int Balance,
    DateTime CreatedAt,
    DateTime UpdatedAt);
