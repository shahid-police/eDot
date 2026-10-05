using System.Security.Claims;
using Elegance.Application.Admin;
using Elegance.Application.Common;

namespace Elegance.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static long GetCustomerId(this ClaimsPrincipal principal) =>
        long.TryParse(principal.FindFirst("sub")?.Value, out var id)
            ? id
            : throw new ApplicationFailure(FailureKind.Unauthorized, "Your session has expired. Please sign in again.");

    public static AdminActor GetAdminActor(this ClaimsPrincipal principal) =>
        long.TryParse(principal.FindFirst("sub")?.Value, out var id)
            ? new AdminActor(id, principal.FindFirst("email")?.Value ?? string.Empty)
            : throw new ApplicationFailure(FailureKind.Unauthorized, "Your session has expired. Please sign in again.");
}
