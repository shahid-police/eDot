namespace Elegance.Application.Common;

/// <summary>Identifies whose saved list is being read: a customer by id, or a guest by their raw cookie token.</summary>
public sealed record ListOwner(long? CustomerId, string? GuestToken);
