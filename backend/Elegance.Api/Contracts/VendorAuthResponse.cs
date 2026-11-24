using Elegance.Application.Vendors;

namespace Elegance.Api.Contracts;

// Mirrors AuthResponse: the refresh token travels only in the HttpOnly cookie, never in the JSON
// body, so it strips RefreshToken/RefreshTokenExpiresAt off VendorSession before returning it.
public record VendorAuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, VendorProfileDto Vendor);
