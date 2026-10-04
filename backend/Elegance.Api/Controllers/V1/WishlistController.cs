using Elegance.Api.Auth;
using Elegance.Application.Wishlists;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/wishlist")]
public class WishlistController(IWishlistService wishlistService) : ControllerBase
{
    public const string GuestCookieName = "elegance_wishlist";

    [HttpGet]
    public async Task<ActionResult<WishlistDto>> GetWishlist(CancellationToken cancellationToken)
    {
        var owner = ResolveOwner(issueGuestToken: false);
        return Ok(await wishlistService.GetWishlistAsync(owner, cancellationToken));
    }

    [HttpPost("items")]
    public async Task<ActionResult<WishlistDto>> AddItem([FromBody] AddWishlistItemRequest request, CancellationToken cancellationToken)
    {
        var owner = ResolveOwner(issueGuestToken: true);
        return Ok(await wishlistService.AddItemAsync(owner, request, cancellationToken));
    }

    [HttpDelete("items/{itemId:long}")]
    public async Task<ActionResult<WishlistDto>> RemoveItem(long itemId, CancellationToken cancellationToken)
    {
        var owner = ResolveOwner(issueGuestToken: false);
        return Ok(await wishlistService.RemoveItemAsync(owner, itemId, cancellationToken));
    }

    private Elegance.Application.Common.ListOwner ResolveOwner(bool issueGuestToken) =>
        ListOwnerResolver.Resolve(User, Request, Response, GuestCookieName, issueGuestToken);
}
