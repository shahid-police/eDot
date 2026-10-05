using System.Security.Claims;
using Elegance.Application.Common;
using Elegance.Infrastructure.Auth;

namespace Elegance.Api.Auth;

public static class ListOwnerResolver
{
    public static readonly TimeSpan GuestSessionLifetime = TimeSpan.FromDays(30);

    public static ListOwner Resolve(ClaimsPrincipal user, HttpRequest request, HttpResponse response, string guestCookieName, bool issueGuestToken)
    {
        if (user.Identity?.IsAuthenticated == true && long.TryParse(user.FindFirst("sub")?.Value, out var customerId))
        {
            return new ListOwner(customerId, null);
        }

        var guestToken = request.Cookies[guestCookieName];
        if (string.IsNullOrEmpty(guestToken))
        {
            if (!issueGuestToken)
            {
                return new ListOwner(null, null);
            }
            guestToken = TokenService.CreateOpaqueToken();
        }

        // Renewed on every guest request so an active guest session does not expire.
        response.Cookies.Append(guestCookieName, guestToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = GuestSessionLifetime,
        });

        return new ListOwner(null, guestToken);
    }
}
