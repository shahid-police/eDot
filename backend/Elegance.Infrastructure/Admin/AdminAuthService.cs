using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Admin;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminAuthService(EleganceDbContext dbContext, TokenService tokenService, IAuditLogService auditLog) : IAdminAuthService
{
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();

    public async Task<AdminSession> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var admin = await dbContext.AdminUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (admin is null
            || !admin.IsActive
            || _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "Invalid email or password.");
        }

        var roles = admin.Roles.Select(r => r.Role.ToString()).OrderBy(r => r).ToList();
        var (token, expiresAt) = tokenService.CreateAdminAccessToken(admin, roles);

        await auditLog.RecordAsync(
            new AdminActor(admin.Id, admin.Email), "admin.login", "AdminUser", admin.Id.ToString(), "Signed in", cancellationToken);

        return new AdminSession
        {
            AccessToken = token,
            AccessTokenExpiresAt = expiresAt,
            Admin = new AdminProfileDto
            {
                Id = admin.Id,
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                Roles = roles,
            },
        };
    }
}
