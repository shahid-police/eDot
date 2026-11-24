using Elegance.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Elegance.Api.Auth;

// A vendor token needs typ=vendor and role=Vendor. Admin and customer tokens carry neither, so
// they cannot pass this policy, and a vendor token cannot pass any AdminPolicies/Customer check.
public static class VendorPolicies
{
    public const string Portal = "Vendor.Portal";

    public static void AddVendorPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(Portal, policy => policy
            .RequireClaim(TokenService.TokenTypeClaim, TokenService.VendorTokenType)
            .RequireRole(TokenService.VendorRole));
    }
}
