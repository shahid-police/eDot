using Elegance.Application.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Route("api/v1/admin/auth")]
public class AdminAuthController(IAdminAuthService adminAuthService) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AdminSession>> Login([FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var session = await adminAuthService.LoginAsync(request, cancellationToken);
        return Ok(session);
    }
}
