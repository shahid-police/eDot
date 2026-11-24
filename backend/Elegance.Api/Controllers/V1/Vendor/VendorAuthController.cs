using Elegance.Api.Contracts;
using Elegance.Application.Vendors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elegance.Api.Controllers.V1.Vendor;

[ApiController]
[Route("api/v1/vendor/auth")]
public class VendorAuthController(IVendorAuthService vendorAuthService) : ControllerBase
{
    public const string RefreshCookieName = "elegance_vendor_refresh";
    private const string RefreshCookiePath = "/api/v1/vendor/auth";

    // Always returns a TwoFactor challenge - a vendor never gets a session straight from password
    // login, since 2FA is mandatory. The frontend branches on TwoFactor.RequiresEnrollment to show
    // either the enrollment screen (first login) or the "enter your code" screen.
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<VendorLoginResult>> Login([FromBody] VendorLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await vendorAuthService.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("2fa/enroll/start")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<VendorTwoFactorEnrollmentDto>> StartEnrollment([FromBody] VendorStartTwoFactorEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var dto = await vendorAuthService.StartEnrollmentAsync(request, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("2fa/enroll/confirm")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<VendorAuthResponse>> ConfirmEnrollment([FromBody] VendorConfirmTwoFactorEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var session = await vendorAuthService.ConfirmEnrollmentAsync(request, cancellationToken);
        SetRefreshCookie(session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(ToResponse(session));
    }

    [HttpPost("2fa/verify")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<VendorAuthResponse>> VerifyTwoFactor([FromBody] VerifyVendorTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var session = await vendorAuthService.VerifyTwoFactorAsync(request, cancellationToken);
        SetRefreshCookie(session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(ToResponse(session));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<VendorAuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName] ?? string.Empty;
        var session = await vendorAuthService.RefreshAsync(refreshToken, cancellationToken);
        SetRefreshCookie(session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(ToResponse(session));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await vendorAuthService.LogoutAsync(Request.Cookies[RefreshCookieName], cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, RefreshCookieOptions(expires: null));
        return NoContent();
    }

    private void SetRefreshCookie(string refreshToken, DateTime expiresAt)
    {
        Response.Cookies.Append(RefreshCookieName, refreshToken, RefreshCookieOptions(new DateTimeOffset(expiresAt)));
    }

    private CookieOptions RefreshCookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = RefreshCookiePath,
        Expires = expires,
    };

    private static VendorAuthResponse ToResponse(VendorSession session) =>
        new(session.AccessToken, session.AccessTokenExpiresAt, session.Vendor);
}
