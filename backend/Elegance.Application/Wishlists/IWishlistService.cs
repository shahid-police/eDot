namespace Elegance.Application.Wishlists;

public interface IWishlistService
{
    Task<WishlistDto> GetWishlistAsync(long customerId, CancellationToken cancellationToken = default);
    Task<WishlistDto> AddItemAsync(long customerId, long productId, CancellationToken cancellationToken = default);
    Task<WishlistDto> RemoveItemAsync(long customerId, long itemId, CancellationToken cancellationToken = default);
}
