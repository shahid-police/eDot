using System.Security.Claims;
using Elegance.Application.Common;
using Elegance.Infrastructure.Auth;

namespace Elegance.Api.Auth;

public static class ListOwnerResolver
{
    private static readonly TimeSpan GuestCookieLifetime = TimeSpan.FromDays(30);

    public static ListOwner Resolve(ClaimsPrincipal user, HttpRequest request, HttpResponse response, string guestCookieName, bool issueGuestToken)
    {
        if (user.Identity?.IsAuthenticated == true && long.TryParse(user.FindFirst("sub")?.Value, out var customerId))
        {
            return new ListOwner(customerId, null);
        }

        var guestToken = request.Cookies[guestCookieName];
        if (string.IsNullOrEmpty(guestToken) && issueGuestToken)
        {
            guestToken = TokenService.CreateOpaqueToken();
            response.Cookies.Append(guestCookieName, guestToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                MaxAge = GuestCookieLifetime,
            });
        }

        return new ListOwner(null, string.IsNullOrEmpty(guestToken) ? null : guestToken);
    }
}
