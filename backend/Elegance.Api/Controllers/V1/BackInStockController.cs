using Elegance.Application.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/back-in-stock")]
public class BackInStockController(IBackInStockService backInStockService) : ControllerBase
{
    [HttpPost("subscribe")]
    [EnableRateLimiting("back-in-stock")]
    public async Task<ActionResult<BackInStockSubscriptionDto>> Subscribe(
        [FromBody] BackInStockSubscribeRequest request, CancellationToken cancellationToken)
    {
        var subscription = await backInStockService.SubscribeAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, subscription);
    }
}
