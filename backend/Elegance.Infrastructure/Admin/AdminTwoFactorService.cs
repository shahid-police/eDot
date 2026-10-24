using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Admin;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Elegance.Infrastructure.Admin;

public class AdminTwoFactorService(
    EleganceDbContext dbContext,
    TotpService totpService,
    TwoFactorSecretProtector secretProtector,
    IAuditLogService auditLog) : IAdminTwoFactorService
{
    private const string Issuer = "Elegance Admin";
    private static readonly TimeSpan PendingEnrollmentLifetime = TimeSpan.FromMinutes(10);
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();

    public async Task<TwoFactorEnrollmentDto> StartEnrollmentAsync(long adminId, StartTwoFactorEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await RequirePasswordAsync(adminId, request.Password, cancellationToken);

        var existing = await dbContext.AdminTwoFactorCredentials
            .FirstOrDefaultAsync(c => c.AdminUserId == adminId, cancellationToken);
        if (existing is { Status: AdminTwoFactorStatus.Active })
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Two-factor authentication is already enabled. Disable it before re-enrolling.");
        }

        var secret = totpService.GenerateSecret();
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            // New row. If two enrollment requests race, the primary key on AdminUserId makes the
            // second insert fail rather than silently create two credentials for one admin.
            dbContext.AdminTwoFactorCredentials.Add(new AdminTwoFactorCredential
            {
                AdminUserId = adminId,
                EncryptedSecret = secretProtector.Encrypt(secret),
                Status = AdminTwoFactorStatus.Pending,
                LastAcceptedTimeStep = -1,
                CreatedAt = now,
                PendingExpiresAt = now.Add(PendingEnrollmentLifetime),
            });
        }
        else
        {
            // Restarting an abandoned pending enrollment overwrites it; no cleanup job needed.
            existing.EncryptedSecret = secretProtector.Encrypt(secret);
            existing.Status = AdminTwoFactorStatus.Pending;
            existing.LastAcceptedTimeStep = -1;
            existing.CreatedAt = now;
            existing.PendingExpiresAt = now.Add(PendingEnrollmentLifetime);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "An enrollment is already in progress. Try again in a moment.");
        }

        await auditLog.RecordAsync(
            new AdminActor(admin.Id, admin.Email), "admin.2fa.enroll.start", "AdminUser", admin.Id.ToString(), "Started two-factor enrollment", cancellationToken);

        var otpAuthUri = totpService.BuildOtpAuthUri(secret, Issuer, admin.Email);
        return new TwoFactorEnrollmentDto
        {
            ManualEntryKey = totpService.ToBase32(secret),
            QrCodePngBase64 = BuildQrCodePng(otpAuthUri),
        };
    }

    public async Task ConfirmEnrollmentAsync(long adminId, VerifyTwoFactorEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var credential = await dbContext.AdminTwoFactorCredentials
            .FirstOrDefaultAsync(c => c.AdminUserId == adminId, cancellationToken);

        if (credential is null
            || credential.Status != AdminTwoFactorStatus.Pending
            || credential.PendingExpiresAt is null
            || credential.PendingExpiresAt < DateTime.UtcNow)
        {
            throw new ApplicationFailure(FailureKind.Validation, "No pending enrollment found. Start enrollment again.");
        }

        var secret = secretProtector.Decrypt(credential.EncryptedSecret);
        var matchedStep = totpService.VerifyCode(secret, request.Code, credential.LastAcceptedTimeStep);
        if (matchedStep is null)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "Invalid or expired code.");
        }

        credential.Status = AdminTwoFactorStatus.Active;
        credential.ActivatedAt = DateTime.UtcNow;
        credential.PendingExpiresAt = null;
        credential.LastAcceptedTimeStep = matchedStep.Value;
        await dbContext.SaveChangesAsync(cancellationToken);

        var admin = await dbContext.AdminUsers.FirstAsync(u => u.Id == adminId, cancellationToken);
        await auditLog.RecordAsync(
            new AdminActor(admin.Id, admin.Email), "admin.2fa.enabled", "AdminUser", admin.Id.ToString(), "Enabled two-factor authentication", cancellationToken);
    }

    public async Task DisableAsync(long adminId, DisableTwoFactorRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await RequirePasswordAsync(adminId, request.Password, cancellationToken);

        var credential = await dbContext.AdminTwoFactorCredentials
            .FirstOrDefaultAsync(c => c.AdminUserId == adminId && c.Status == AdminTwoFactorStatus.Active, cancellationToken);
        if (credential is null)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Two-factor authentication is not enabled.");
        }

        var secret = secretProtector.Decrypt(credential.EncryptedSecret);
        var matchedStep = totpService.VerifyCode(secret, request.Code, credential.LastAcceptedTimeStep);
        if (matchedStep is null)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "Invalid or expired code.");
        }

        dbContext.AdminTwoFactorCredentials.Remove(credential);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(
            new AdminActor(admin.Id, admin.Email), "admin.2fa.disabled", "AdminUser", admin.Id.ToString(), "Disabled two-factor authentication", cancellationToken);
    }

    public async Task<TwoFactorStatusDto> GetStatusAsync(long adminId, CancellationToken cancellationToken = default)
    {
        var enabled = await dbContext.AdminTwoFactorCredentials
            .AnyAsync(c => c.AdminUserId == adminId && c.Status == AdminTwoFactorStatus.Active, cancellationToken);
        return new TwoFactorStatusDto { Enabled = enabled };
    }

    private async Task<AdminUser> RequirePasswordAsync(long adminId, string password, CancellationToken cancellationToken)
    {
        var admin = await dbContext.AdminUsers.FirstOrDefaultAsync(u => u.Id == adminId && u.IsActive, cancellationToken);
        if (admin is null
            || _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, password) == PasswordVerificationResult.Failed)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "Incorrect password.");
        }

        return admin;
    }

    private static string BuildQrCodePng(string otpAuthUri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.M);
        using var pngCode = new PngByteQRCode(data);
        return Convert.ToBase64String(pngCode.GetGraphic(10));
    }
}
