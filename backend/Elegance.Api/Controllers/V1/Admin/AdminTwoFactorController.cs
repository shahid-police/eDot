using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elegance.Api.Controllers.V1.Admin;

// Every endpoint here requires a real admin session (Dashboard policy - the broadest admin
// policy, since any admin manages their own 2FA regardless of functional role) and acts only on
// the caller's own account: the admin id always comes from the token's claims, never from the
// request body, so one admin cannot enroll or disable another admin's authenticator.
[ApiController]
[Authorize(Policy = AdminPolicies.Dashboard)]
[Route("api/v1/admin/2fa")]
public class AdminTwoFactorController(IAdminTwoFactorService twoFactorService) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<TwoFactorStatusDto>> Status(CancellationToken cancellationToken)
    {
        return Ok(await twoFactorService.GetStatusAsync(User.GetAdminActor().Id, cancellationToken));
    }

    [HttpPost("enroll/start")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<TwoFactorEnrollmentDto>> StartEnrollment([FromBody] StartTwoFactorEnrollmentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await twoFactorService.StartEnrollmentAsync(User.GetAdminActor().Id, request, cancellationToken));
    }

    [HttpPost("enroll/verify")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ConfirmEnrollment([FromBody] VerifyTwoFactorEnrollmentRequest request, CancellationToken cancellationToken)
    {
        await twoFactorService.ConfirmEnrollmentAsync(User.GetAdminActor().Id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("disable")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Disable([FromBody] DisableTwoFactorRequest request, CancellationToken cancellationToken)
    {
        await twoFactorService.DisableAsync(User.GetAdminActor().Id, request, cancellationToken);
        return NoContent();
    }
}
