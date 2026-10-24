using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Elegance.Domain.Admin;
using Elegance.Domain.Customers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Elegance.Infrastructure.Auth;

// TimeProvider (not DateTime.UtcNow) drives every expiry here, including inside the validation
// call below, so tests can advance a fake clock and deterministically prove a token has expired
// rather than needing to actually wait out a 5/15-minute real-time window.
public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    public const string CustomerRole = "Customer";
    public const string RoleClaim = "role";
    public const string TokenTypeClaim = "typ";
    public const string AdminTokenType = "admin";
    public const string AdminPendingTwoFactorTokenType = "admin-2fa-pending";
    private const int PendingTwoFactorTokenMinutes = 5;

    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAt) CreateAccessToken(Customer customer)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, customer.Email),
            new Claim(RoleClaim, CustomerRole),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expiresAt);
    }

    public (string Token, DateTime ExpiresAt) CreateAdminAccessToken(AdminUser admin, IReadOnlyList<string> roles)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, admin.Email),
            new(TokenTypeClaim, AdminTokenType),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(role => new Claim(RoleClaim, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    // A password-only login for a 2FA-enabled admin gets one of these instead of a real access
    // token. It carries typ=admin-2fa-pending, never typ=admin, so AdminPolicies (which requires
    // typ==admin via RequireClaim) rejects it outright on every existing protected endpoint -
    // no changes needed to any of those policies or controllers.
    public (string Token, DateTime ExpiresAt) CreateAdminPendingTwoFactorToken(long adminId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(PendingTwoFactorTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, adminId.ToString()),
            new Claim(TokenTypeClaim, AdminPendingTwoFactorTokenType),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    // Validates a pending-2FA token and returns the admin id it was issued for, or null if the
    // token is missing, expired, tampered with, or not actually a pending-2FA token (e.g. a real
    // admin access token submitted here is rejected too - its typ is "admin", not the pending one).
    public async Task<long?> ValidateAdminPendingTwoFactorTokenAsync(string token)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            LifetimeValidator = (notBefore, expires, _, validationParameters) =>
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                var skew = validationParameters.ClockSkew;
                return (notBefore is null || notBefore <= now + skew) && (expires is null || expires >= now - skew);
            },
        };

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, validationParameters);
        if (!result.IsValid)
        {
            return null;
        }

        var typ = result.ClaimsIdentity.FindFirst(TokenTypeClaim)?.Value;
        if (typ != AdminPendingTwoFactorTokenType)
        {
            return null;
        }

        return long.TryParse(result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var adminId)
            ? adminId
            : null;
    }

    public DateTime RefreshTokenExpiry() => timeProvider.GetUtcNow().UtcDateTime.AddDays(_options.RefreshTokenDays);

    public static string CreateOpaqueToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
