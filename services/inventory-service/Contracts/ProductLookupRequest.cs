namespace Inventory.Api.Contracts;

public sealed record ProductLookupRequest(IReadOnlyCollection<Guid>? Ids);
