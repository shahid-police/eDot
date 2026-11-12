using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Marketing)]
[Route("api/v1/admin/promoter-payments")]
public class AdminPromoterPaymentsController(IAdminPromoterPaymentService paymentService) : ControllerBase
{
    [HttpGet("balance/{promoterId:long}")]
    public async Task<ActionResult<AdminPromoterBalanceDto>> GetBalance(long promoterId, CancellationToken cancellationToken)
    {
        return Ok(await paymentService.GetPromoterBalanceAsync(promoterId, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminPromoterPaymentDto>>> List([FromQuery] AdminPromoterPaymentQuery query, CancellationToken cancellationToken)
    {
        return Ok(await paymentService.ListPaymentsAsync(query, cancellationToken));
    }

    [HttpGet("{paymentId:long}")]
    public async Task<ActionResult<AdminPromoterPaymentDto>> Get(long paymentId, CancellationToken cancellationToken)
    {
        return Ok(await paymentService.GetPaymentAsync(paymentId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminPromoterPaymentDto>> Create([FromBody] AdminPromoterPaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = await paymentService.CreatePaymentAsync(User.GetAdminActor(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, payment);
    }

    [HttpPost("{paymentId:long}/void")]
    public async Task<ActionResult<AdminPromoterPaymentDto>> Void(long paymentId, [FromBody] AdminVoidPaymentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await paymentService.VoidPaymentAsync(User.GetAdminActor(), paymentId, request, cancellationToken));
    }
}
