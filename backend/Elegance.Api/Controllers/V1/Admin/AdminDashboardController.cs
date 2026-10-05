using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Dashboard)]
[Route("api/v1/admin/dashboard")]
public class AdminDashboardController(IAdminDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminDashboardDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetDashboardAsync(cancellationToken));
    }
}
