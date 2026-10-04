namespace Elegance.Application.Carts;

/// <summary>
/// Identifies whose cart is being read. A customer is identified by id; a guest by the
/// raw opaque token from their cart cookie. Exactly one of the two is set.
/// </summary>
public sealed record CartOwner(long? CustomerId, string? GuestToken);

public interface ICartService
{
    Task<CartDto> GetCartAsync(CartOwner owner, CancellationToken cancellationToken = default);
    Task<CartDto> AddItemAsync(CartOwner owner, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateItemAsync(CartOwner owner, long itemId, int quantity, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveItemAsync(CartOwner owner, long itemId, CancellationToken cancellationToken = default);
    Task MergeGuestCartAsync(long customerId, string guestToken, CancellationToken cancellationToken = default);
}
