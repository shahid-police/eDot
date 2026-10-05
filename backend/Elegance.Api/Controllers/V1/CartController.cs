using Elegance.Application.Carts;
using Elegance.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/cart")]
public class CartController(ICartService cartService) : ControllerBase
{
    public const string GuestCookieName = "elegance_cart";

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken cancellationToken)
    {
        var owner = ResolveOwner(issueGuestToken: false);
        return Ok(await cartService.GetCartAsync(owner, cancellationToken));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
    {
        var owner = ResolveOwner(issueGuestToken: true);
        return Ok(await cartService.AddItemAsync(owner, request, cancellationToken));
    }

    [HttpPut("items/{itemId:long}")]
    public async Task<ActionResult<CartDto>> UpdateItem(long itemId, [FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
    {
        var owner = ResolveOwner(issueGuestToken: false);
        return Ok(await cartService.UpdateItemAsync(owner, itemId, request.Quantity, cancellationToken));
    }

    [HttpDelete("items/{itemId:long}")]
    public async Task<ActionResult<CartDto>> RemoveItem(long itemId, CancellationToken cancellationToken)
    {
        var owner = ResolveOwner(issueGuestToken: false);
        return Ok(await cartService.RemoveItemAsync(owner, itemId, cancellationToken));
    }

    private CartOwner ResolveOwner(bool issueGuestToken)
    {
        var owner = ListOwnerResolver.Resolve(User, Request, Response, GuestCookieName, issueGuestToken);
        return new CartOwner(owner.CustomerId, owner.GuestToken);
    }
}
