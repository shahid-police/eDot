using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Marketing)]
[Route("api/v1/admin/promoters")]
public class AdminPromotersController(IAdminPromoterService promoterService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminPromoterDto>>> List([FromQuery] AdminPromoterQuery query, CancellationToken cancellationToken)
    {
        return Ok(await promoterService.ListPromotersAsync(query, cancellationToken));
    }

    [HttpGet("{promoterId:long}")]
    public async Task<ActionResult<AdminPromoterDto>> Get(long promoterId, CancellationToken cancellationToken)
    {
        return Ok(await promoterService.GetPromoterAsync(promoterId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminPromoterDto>> Create([FromBody] AdminPromoterRequest request, CancellationToken cancellationToken)
    {
        var promoter = await promoterService.CreatePromoterAsync(User.GetAdminActor(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, promoter);
    }

    [HttpPut("{promoterId:long}")]
    public async Task<ActionResult<AdminPromoterDto>> Update(long promoterId, [FromBody] AdminPromoterRequest request, CancellationToken cancellationToken)
    {
        return Ok(await promoterService.UpdatePromoterAsync(User.GetAdminActor(), promoterId, request, cancellationToken));
    }

    [HttpPut("{promoterId:long}/status")]
    public async Task<ActionResult<AdminPromoterDto>> SetStatus(long promoterId, [FromBody] AdminPromoterStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await promoterService.SetPromoterStatusAsync(User.GetAdminActor(), promoterId, request.Status, cancellationToken));
    }
}
