using Elegance.Api.Contracts;
using Elegance.Application.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    public const string RefreshCookieName = "elegance_refresh";
    private const string RefreshCookiePath = "/api/v1/auth";

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var session = await authService.RegisterAsync(request, cancellationToken);
        SetRefreshCookie(session.RefreshToken, session.RefreshTokenExpiresAt);
        return StatusCode(StatusCodes.Status201Created, ToResponse(session));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var session = await authService.LoginAsync(request, cancellationToken);
        SetRefreshCookie(session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(ToResponse(session));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName] ?? string.Empty;
        var session = await authService.RefreshAsync(refreshToken, cancellationToken);
        SetRefreshCookie(session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(ToResponse(session));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(Request.Cookies[RefreshCookieName], cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, RefreshCookieOptions(expires: null));
        return NoContent();
    }

    private void SetRefreshCookie(string refreshToken, DateTime expiresAt)
    {
        Response.Cookies.Append(RefreshCookieName, refreshToken, RefreshCookieOptions(new DateTimeOffset(expiresAt)));
    }

    private static CookieOptions RefreshCookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = RefreshCookiePath,
        Expires = expires,
    };

    private static AuthResponse ToResponse(AuthSession session) =>
        new(session.AccessToken, session.AccessTokenExpiresAt, session.Customer);
}
