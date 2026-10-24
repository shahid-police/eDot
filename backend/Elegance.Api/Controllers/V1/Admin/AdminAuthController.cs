using Elegance.Application.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Route("api/v1/admin/auth")]
public class AdminAuthController(IAdminAuthService adminAuthService) : ControllerBase
{
    // Returns either { session: {...} } or { twoFactor: { pendingToken, ... } }. The frontend
    // must branch on which one came back - a pendingToken is not usable as a bearer token.
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AdminLoginResult>> Login([FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await adminAuthService.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("2fa/verify")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AdminSession>> VerifyTwoFactor([FromBody] VerifyAdminTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var session = await adminAuthService.VerifyTwoFactorAsync(request, cancellationToken);
        return Ok(session);
    }
}
