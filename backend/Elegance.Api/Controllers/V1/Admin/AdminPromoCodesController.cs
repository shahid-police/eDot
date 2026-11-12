using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Marketing)]
[Route("api/v1/admin/promo-codes")]
public class AdminPromoCodesController(IAdminPromoCodeService promoCodeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminPromoCodeDto>>> List([FromQuery] AdminPromoCodeQuery query, CancellationToken cancellationToken)
    {
        return Ok(await promoCodeService.ListPromoCodesAsync(query, cancellationToken));
    }

    [HttpGet("{promoCodeId:long}")]
    public async Task<ActionResult<AdminPromoCodeDto>> Get(long promoCodeId, CancellationToken cancellationToken)
    {
        return Ok(await promoCodeService.GetPromoCodeAsync(promoCodeId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminPromoCodeDto>> Create([FromBody] AdminPromoCodeRequest request, CancellationToken cancellationToken)
    {
        var promoCode = await promoCodeService.CreatePromoCodeAsync(User.GetAdminActor(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, promoCode);
    }

    [HttpPut("{promoCodeId:long}")]
    public async Task<ActionResult<AdminPromoCodeDto>> Update(long promoCodeId, [FromBody] AdminPromoCodeRequest request, CancellationToken cancellationToken)
    {
        return Ok(await promoCodeService.UpdatePromoCodeAsync(User.GetAdminActor(), promoCodeId, request, cancellationToken));
    }

    [HttpPut("{promoCodeId:long}/status")]
    public async Task<ActionResult<AdminPromoCodeDto>> SetStatus(long promoCodeId, [FromBody] AdminPromoCodeStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await promoCodeService.SetPromoCodeStatusAsync(User.GetAdminActor(), promoCodeId, request.Status, cancellationToken));
    }

    [HttpPost("{promoCodeId:long}/regenerate")]
    public async Task<ActionResult<AdminPromoCodeDto>> Regenerate(long promoCodeId, CancellationToken cancellationToken)
    {
        return Ok(await promoCodeService.RegeneratePromoCodeAsync(User.GetAdminActor(), promoCodeId, cancellationToken));
    }
}
