using Elegance.Api.Auth;
using Elegance.Application.Checkout;
using Microsoft.AspNetCore.Mvc;

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
}
