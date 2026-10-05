using Elegance.Domain.Admin;
using Elegance.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Elegance.Api.Auth;

// Admin policies need an admin token (typ=admin) and one of the listed admin roles.
// Customer tokens carry no typ=admin claim, so they cannot pass any of these.
public static class AdminPolicies
{
    public const string Dashboard = "Admin.Dashboard";
    public const string Catalog = "Admin.Catalog";
    public const string Orders = "Admin.Orders";
    public const string Inventory = "Admin.Inventory";
    public const string Customers = "Admin.Customers";
    public const string Audit = "Admin.Audit";

    public static void AddAdminPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(Dashboard, policy => RequireAdmin(policy, Enum.GetValues<AdminRole>()));
        options.AddPolicy(Catalog, policy => RequireAdmin(policy, AdminRole.SuperAdmin, AdminRole.Admin, AdminRole.CatalogManager));
        options.AddPolicy(Orders, policy => RequireAdmin(policy, AdminRole.SuperAdmin, AdminRole.Admin, AdminRole.OrderManager));
        options.AddPolicy(Inventory, policy => RequireAdmin(policy, AdminRole.SuperAdmin, AdminRole.Admin, AdminRole.InventoryManager));
        options.AddPolicy(Customers, policy => RequireAdmin(policy, AdminRole.SuperAdmin, AdminRole.Admin, AdminRole.OrderManager));
        options.AddPolicy(Audit, policy => RequireAdmin(policy, AdminRole.SuperAdmin, AdminRole.Admin));
    }

    private static void RequireAdmin(AuthorizationPolicyBuilder policy, params AdminRole[] roles) =>
        policy
            .RequireClaim(TokenService.TokenTypeClaim, TokenService.AdminTokenType)
            .RequireRole(roles.Select(r => r.ToString()).ToArray());
}
