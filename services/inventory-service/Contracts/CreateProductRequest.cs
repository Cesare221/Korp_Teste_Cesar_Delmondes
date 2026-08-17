namespace Inventory.Api.Contracts;

public sealed record CreateProductRequest(
    string? Code,
    string? Description,
    int Balance);
