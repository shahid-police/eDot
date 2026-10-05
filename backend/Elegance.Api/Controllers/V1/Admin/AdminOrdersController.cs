using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Orders)]
[Route("api/v1/admin/orders")]
public class AdminOrdersController(IAdminOrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminOrderSummaryDto>>> List([FromQuery] AdminOrderQuery query, CancellationToken cancellationToken)
    {
        return Ok(await orderService.ListOrdersAsync(query, cancellationToken));
    }

    [HttpGet("{orderId:long}")]
    public async Task<ActionResult<OrderDetailDto>> Get(long orderId, CancellationToken cancellationToken)
    {
        return Ok(await orderService.GetOrderAsync(orderId, cancellationToken));
    }

    [HttpPut("{orderId:long}/status")]
    public async Task<ActionResult<OrderDetailDto>> ChangeStatus(long orderId, [FromBody] OrderStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await orderService.ChangeStatusAsync(User.GetAdminActor(), orderId, request, cancellationToken));
    }

    [HttpPut("{orderId:long}/payment-status")]
    public async Task<ActionResult<OrderDetailDto>> SetPaymentStatus(long orderId, [FromBody] PaymentStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await orderService.SetPaymentStatusAsync(User.GetAdminActor(), orderId, request, cancellationToken));
    }
}
