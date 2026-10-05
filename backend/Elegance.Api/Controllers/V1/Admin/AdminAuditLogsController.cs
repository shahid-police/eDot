using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Audit)]
[Route("api/v1/admin/audit-logs")]
public class AdminAuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        return Ok(await auditLogService.ListAsync(page, pageSize, cancellationToken));
    }
}
