using System.Security.Claims;
using Elegance.Application.Common;

namespace Elegance.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static long GetCustomerId(this ClaimsPrincipal principal) =>
        long.TryParse(principal.FindFirst("sub")?.Value, out var id)
            ? id
            : throw new ApplicationFailure(FailureKind.Unauthorized, "Your session has expired. Please sign in again.");
}
