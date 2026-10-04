using Elegance.Api.Auth;
using Elegance.Application.Wishlists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/v1/wishlist")]
public class WishlistController(IWishlistService wishlistService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WishlistDto>> GetWishlist(CancellationToken cancellationToken)
    {
        var wishlist = await wishlistService.GetWishlistAsync(User.GetCustomerId(), cancellationToken);
        return Ok(wishlist);
    }

    [HttpPost("items")]
    public async Task<ActionResult<WishlistDto>> AddItem([FromBody] AddWishlistItemRequest request, CancellationToken cancellationToken)
    {
        var wishlist = await wishlistService.AddItemAsync(User.GetCustomerId(), request.ProductId, cancellationToken);
        return Ok(wishlist);
    }

    [HttpDelete("items/{itemId:long}")]
    public async Task<ActionResult<WishlistDto>> RemoveItem(long itemId, CancellationToken cancellationToken)
    {
        var wishlist = await wishlistService.RemoveItemAsync(User.GetCustomerId(), itemId, cancellationToken);
        return Ok(wishlist);
    }
}
