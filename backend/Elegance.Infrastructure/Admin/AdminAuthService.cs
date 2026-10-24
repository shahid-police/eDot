using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Admin;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminAuthService(
    EleganceDbContext dbContext,
    TokenService tokenService,
    TotpService totpService,
    TwoFactorSecretProtector secretProtector,
    IAuditLogService auditLog) : IAdminAuthService
{
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();

    public async Task<AdminLoginResult> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken = default)
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

        var twoFactor = await dbContext.AdminTwoFactorCredentials
            .FirstOrDefaultAsync(c => c.AdminUserId == admin.Id && c.Status == AdminTwoFactorStatus.Active, cancellationToken);

        if (twoFactor is not null)
        {
            var (pendingToken, pendingExpiresAt) = tokenService.CreateAdminPendingTwoFactorToken(admin.Id);
            return new AdminLoginResult
            {
                TwoFactor = new AdminTwoFactorChallenge
                {
                    PendingToken = pendingToken,
                    PendingTokenExpiresAt = pendingExpiresAt,
                },
            };
        }

        var roles = admin.Roles.Select(r => r.Role.ToString()).OrderBy(r => r).ToList();
        var (token, expiresAt) = tokenService.CreateAdminAccessToken(admin, roles);

        await auditLog.RecordAsync(
            new AdminActor(admin.Id, admin.Email), "admin.login", "AdminUser", admin.Id.ToString(), "Signed in", cancellationToken);

        return new AdminLoginResult { Session = BuildSession(admin, roles, token, expiresAt) };
    }

    public async Task<AdminSession> VerifyTwoFactorAsync(VerifyAdminTwoFactorRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = await tokenService.ValidateAdminPendingTwoFactorTokenAsync(request.PendingToken);
        if (adminId is null)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "This sign-in has expired. Please sign in again.");
        }

        var admin = await dbContext.AdminUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == adminId.Value && u.IsActive, cancellationToken);

        var credential = admin is null
            ? null
            : await dbContext.AdminTwoFactorCredentials
                .FirstOrDefaultAsync(c => c.AdminUserId == admin.Id && c.Status == AdminTwoFactorStatus.Active, cancellationToken);

        if (admin is null || credential is null)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "This sign-in has expired. Please sign in again.");
        }

        var accepted = await AcceptTotpCodeAsync(credential, request.Code, cancellationToken);
        if (!accepted)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "Invalid or expired code.");
        }

        var roles = admin.Roles.Select(r => r.Role.ToString()).OrderBy(r => r).ToList();
        var (token, expiresAt) = tokenService.CreateAdminAccessToken(admin, roles);

        await auditLog.RecordAsync(
            new AdminActor(admin.Id, admin.Email), "admin.login.2fa", "AdminUser", admin.Id.ToString(), "Completed two-factor sign-in", cancellationToken);

        return BuildSession(admin, roles, token, expiresAt);
    }

    // Atomic: only succeeds if the matched time step is still strictly newer than the one
    // currently stored, so two concurrent requests replaying the same code cannot both pass.
    private async Task<bool> AcceptTotpCodeAsync(AdminTwoFactorCredential credential, string code, CancellationToken cancellationToken)
    {
        var secret = secretProtector.Decrypt(credential.EncryptedSecret);
        var matchedStep = totpService.VerifyCode(secret, code, credential.LastAcceptedTimeStep);
        if (matchedStep is null)
        {
            return false;
        }

        var rows = await dbContext.AdminTwoFactorCredentials
            .Where(c => c.AdminUserId == credential.AdminUserId && c.LastAcceptedTimeStep < matchedStep.Value)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.LastAcceptedTimeStep, matchedStep.Value), cancellationToken);

        return rows > 0;
    }

    private static AdminSession BuildSession(AdminUser admin, List<string> roles, string token, DateTime expiresAt) => new()
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
