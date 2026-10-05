using Elegance.Api.Auth;
using Elegance.Application.Checkout;
using Elegance.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController(ICheckoutService checkoutService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDetailDto>> PlaceOrder([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        var owner = ListOwnerResolver.Resolve(User, Request, Response, CartController.GuestCookieName, issueGuestToken: false);
        var order = await checkoutService.PlaceOrderAsync(owner, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, order);
    }

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryDto>>> GetOrders(CancellationToken cancellationToken)
    {
        return Ok(await checkoutService.GetOrdersAsync(User.GetCustomerId(), cancellationToken));
    }

    [HttpGet("{orderId:long}")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(long orderId, CancellationToken cancellationToken)
    {
        return Ok(await checkoutService.GetOrderAsync(User.GetCustomerId(), orderId, cancellationToken));
    }
}
