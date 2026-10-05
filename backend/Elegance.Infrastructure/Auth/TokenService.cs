using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Elegance.Domain.Admin;
using Elegance.Domain.Customers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Elegance.Infrastructure.Auth;

public sealed class TokenService(IOptions<JwtOptions> options)
{
    public const string CustomerRole = "Customer";
    public const string RoleClaim = "role";
    public const string TokenTypeClaim = "typ";
    public const string AdminTokenType = "admin";

    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAt) CreateAccessToken(Customer customer)
    {
        var now = DateTime.UtcNow;
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
        var now = DateTime.UtcNow;
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

    public DateTime RefreshTokenExpiry() => DateTime.UtcNow.AddDays(_options.RefreshTokenDays);

    public static string CreateOpaqueToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
