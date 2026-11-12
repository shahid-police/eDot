using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Inventory)]
[Route("api/v1/admin/back-in-stock")]
public class AdminBackInStockController(IAdminBackInStockService backInStockService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<AdminBackInStockSummaryDto>> Summary(CancellationToken cancellationToken)
    {
        return Ok(await backInStockService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("subscriptions")]
    public async Task<ActionResult<PagedResult<AdminBackInStockSubscriptionDto>>> Subscriptions(
        [FromQuery] AdminBackInStockSubscriptionQuery query, CancellationToken cancellationToken)
    {
        return Ok(await backInStockService.ListSubscriptionsAsync(query, cancellationToken));
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<PagedResult<AdminWhatsAppNotificationDto>>> Notifications(
        [FromQuery] AdminWhatsAppNotificationQuery query, CancellationToken cancellationToken)
    {
        return Ok(await backInStockService.ListNotificationsAsync(query, cancellationToken));
    }
}
