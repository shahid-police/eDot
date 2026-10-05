using Elegance.Domain.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Persistence.Seed;

/// <summary>
/// Creates one SuperAdmin for local development. Runs only from Development startup and only
/// when both the email and the password are configured. The password is never stored in source:
/// set Seed:AdminPassword through user-secrets or the Seed__AdminPassword environment variable.
/// </summary>
public static class DevelopmentAdminSeeder
{
    public static async Task SeedAsync(EleganceDbContext dbContext, string? email, string? password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await dbContext.AdminUsers.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var admin = new AdminUser
        {
            Email = normalizedEmail,
            PasswordHash = string.Empty,
            FirstName = "Dev",
            LastName = "Admin",
            CreatedAt = now,
            UpdatedAt = now,
        };
        admin.PasswordHash = new PasswordHasher<AdminUser>().HashPassword(admin, password);
        admin.Roles.Add(new AdminRoleAssignment { Role = AdminRole.SuperAdmin });

        dbContext.AdminUsers.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
