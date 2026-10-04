using Elegance.Application.Carts;
using Elegance.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/cart")]
public class CartController(ICartService cartService) : ControllerBase
{
    public const string GuestCookieName = "elegance_cart";
    private static readonly TimeSpan GuestCookieLifetime = TimeSpan.FromDays(30);

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
        if (User.Identity?.IsAuthenticated == true
            && long.TryParse(User.FindFirst("sub")?.Value, out var customerId))
        {
            return new CartOwner(customerId, null);
        }

        var guestToken = Request.Cookies[GuestCookieName];
        if (string.IsNullOrEmpty(guestToken) && issueGuestToken)
        {
            guestToken = TokenService.CreateOpaqueToken();
            Response.Cookies.Append(GuestCookieName, guestToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                MaxAge = GuestCookieLifetime,
            });
        }

        return new CartOwner(null, string.IsNullOrEmpty(guestToken) ? null : guestToken);
    }
}
