using Elegance.Domain.Admin;
using Elegance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Elegance.Tests.TestSupport;

public static class TestAdmin
{
    public static async Task<AdminUser> CreateAsync(
        EleganceDbContext db,
        string email = "admin@test.local",
        string password = "Correct-Horse-Battery-Staple-1",
        AdminRole role = AdminRole.SuperAdmin)
    {
        var hasher = new PasswordHasher<AdminUser>();
        var admin = new AdminUser
        {
            Email = email,
            PasswordHash = string.Empty,
            FirstName = "Test",
            LastName = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        admin.PasswordHash = hasher.HashPassword(admin, password);

        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();

        db.AdminRoleAssignments.Add(new AdminRoleAssignment { AdminUserId = admin.Id, Role = role });
        await db.SaveChangesAsync();

        return admin;
    }
}
