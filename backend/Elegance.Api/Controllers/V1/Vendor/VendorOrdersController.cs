using Elegance.Api.Auth;
using Elegance.Application.Common;
using Elegance.Application.Vendors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Vendor;

[ApiController]
[Authorize(Policy = VendorPolicies.Portal)]
[Route("api/v1/vendor/orders")]
public class VendorOrdersController(IVendorOrderService vendorOrderService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<VendorOrderSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await vendorOrderService.GetSummaryAsync(User.GetVendorId(), cancellationToken);
        return Ok(summary);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<VendorOrderItemDto>>> List([FromQuery] VendorOrderQuery query, CancellationToken cancellationToken)
    {
        var result = await vendorOrderService.ListOrderItemsAsync(User.GetVendorId(), query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<VendorSalesAnalyticsDto>> GetAnalytics(CancellationToken cancellationToken)
    {
        var analytics = await vendorOrderService.GetSalesAnalyticsAsync(User.GetVendorId(), cancellationToken);
        return Ok(analytics);
    }

    [HttpGet("{orderId:long}")]
    public async Task<ActionResult<VendorOrderDetailDto>> GetDetail(long orderId, CancellationToken cancellationToken)
    {
        var detail = await vendorOrderService.GetOrderDetailAsync(User.GetVendorId(), orderId, cancellationToken);
        return Ok(detail);
    }
}
