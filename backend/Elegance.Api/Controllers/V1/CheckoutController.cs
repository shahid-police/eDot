using Elegance.Api.Auth;
using Elegance.Application.Checkout;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/checkout")]
public class CheckoutController(ICheckoutService checkoutService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<CheckoutSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var owner = ListOwnerResolver.Resolve(User, Request, Response, CartController.GuestCookieName, issueGuestToken: false);
        return Ok(await checkoutService.GetSummaryAsync(owner, cancellationToken));
    }

    [HttpPost("promo-code")]
    [EnableRateLimiting("promo-code")]
    public async Task<ActionResult<CheckoutSummaryDto>> ApplyPromoCode([FromBody] ApplyPromoCodeRequest request, CancellationToken cancellationToken)
    {
        var owner = ListOwnerResolver.Resolve(User, Request, Response, CartController.GuestCookieName, issueGuestToken: false);
        return Ok(await checkoutService.ApplyPromoCodeAsync(owner, request, cancellationToken));
    }

    [HttpDelete("promo-code")]
    public async Task<ActionResult<CheckoutSummaryDto>> RemovePromoCode(CancellationToken cancellationToken)
    {
        var owner = ListOwnerResolver.Resolve(User, Request, Response, CartController.GuestCookieName, issueGuestToken: false);
        return Ok(await checkoutService.RemovePromoCodeAsync(owner, cancellationToken));
    }
}
