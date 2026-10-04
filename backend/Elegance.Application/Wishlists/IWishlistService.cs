using Elegance.Application.Common;

namespace Elegance.Application.Wishlists;

public interface IWishlistService
{
    Task<WishlistDto> GetWishlistAsync(ListOwner owner, CancellationToken cancellationToken = default);
    Task<WishlistDto> AddItemAsync(ListOwner owner, AddWishlistItemRequest request, CancellationToken cancellationToken = default);
    Task<WishlistDto> RemoveItemAsync(ListOwner owner, long itemId, CancellationToken cancellationToken = default);
    Task MergeGuestWishlistAsync(long customerId, string guestToken, CancellationToken cancellationToken = default);
}
