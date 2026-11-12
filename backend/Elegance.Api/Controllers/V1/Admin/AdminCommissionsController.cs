using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Marketing)]
[Route("api/v1/admin/commissions")]
public class AdminCommissionsController(IAdminCommissionService commissionService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<AdminCommissionSummaryDto>> Summary(CancellationToken cancellationToken)
    {
        return Ok(await commissionService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminCommissionDto>>> List([FromQuery] AdminCommissionQuery query, CancellationToken cancellationToken)
    {
        return Ok(await commissionService.ListCommissionsAsync(query, cancellationToken));
    }

    [HttpPost("{commissionId:long}/approve")]
    public async Task<ActionResult<AdminCommissionDto>> Approve(long commissionId, CancellationToken cancellationToken)
    {
        return Ok(await commissionService.ApproveCommissionAsync(User.GetAdminActor(), commissionId, cancellationToken));
    }

    [HttpPost("{commissionId:long}/mark-payable")]
    public async Task<ActionResult<AdminCommissionDto>> MarkPayable(long commissionId, CancellationToken cancellationToken)
    {
        return Ok(await commissionService.MarkPayableAsync(User.GetAdminActor(), commissionId, cancellationToken));
    }

    [HttpPost("{commissionId:long}/reverse")]
    public async Task<ActionResult<AdminCommissionDto>> Reverse(long commissionId, [FromBody] AdminCommissionRemarksRequest request, CancellationToken cancellationToken)
    {
        return Ok(await commissionService.ReverseCommissionAsync(User.GetAdminActor(), commissionId, request, cancellationToken));
    }

    [HttpPost("{commissionId:long}/cancel")]
    public async Task<ActionResult<AdminCommissionDto>> Cancel(long commissionId, [FromBody] AdminCommissionRemarksRequest request, CancellationToken cancellationToken)
    {
        return Ok(await commissionService.CancelCommissionAsync(User.GetAdminActor(), commissionId, request, cancellationToken));
    }
}
