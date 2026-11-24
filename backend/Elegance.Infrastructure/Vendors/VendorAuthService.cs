using Elegance.Application.Common;
using Elegance.Application.Vendors;
using Elegance.Domain.Vendors;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Elegance.Infrastructure.Vendors;

public class VendorAuthService(
    EleganceDbContext dbContext,
    TokenService tokenService,
    TotpService totpService,
    TwoFactorSecretProtector secretProtector) : IVendorAuthService
{
    private const string Issuer = "Empress Vendor Portal";
    private static readonly TimeSpan PendingEnrollmentLifetime = TimeSpan.FromMinutes(10);
    private readonly PasswordHasher<Vendor> _passwordHasher = new();

    public async Task<VendorLoginResult> LoginAsync(VendorLoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var vendor = await dbContext.Vendors.FirstOrDefaultAsync(v => v.Email == email, cancellationToken);

        if (vendor is null
            || vendor.Status != VendorStatus.Active
            || _passwordHasher.VerifyHashedPassword(vendor, vendor.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "Invalid email or password.");
        }

        var credential = await dbContext.VendorTwoFactorCredentials
            .FirstOrDefaultAsync(c => c.VendorId == vendor.Id, cancellationToken);
        var requiresEnrollment = credential is not { Status: VendorTwoFactorStatus.Active };

        var (pendingToken, pendingExpiresAt) = tokenService.CreateVendorPendingTwoFactorToken(vendor.Id, requiresEnrollment);
        return new VendorLoginResult
        {
            TwoFactor = new VendorTwoFactorPending
            {
                PendingToken = pendingToken,
                PendingTokenExpiresAt = pendingExpiresAt,
                RequiresEnrollment = requiresEnrollment,
            },
        };
    }

    public async Task<VendorTwoFactorEnrollmentDto> StartEnrollmentAsync(VendorStartTwoFactorEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var vendorId = await RequirePendingTokenAsync(request.PendingToken);

        var existing = await dbContext.VendorTwoFactorCredentials
            .FirstOrDefaultAsync(c => c.VendorId == vendorId, cancellationToken);
        if (existing is { Status: VendorTwoFactorStatus.Active })
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Two-factor authentication is already enabled for this account.");
        }

        var vendor = await dbContext.Vendors.FirstAsync(v => v.Id == vendorId, cancellationToken);
        var secret = totpService.GenerateSecret();
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            dbContext.VendorTwoFactorCredentials.Add(new VendorTwoFactorCredential
            {
                VendorId = vendorId,
                EncryptedSecret = secretProtector.Encrypt(secret),
                Status = VendorTwoFactorStatus.Pending,
                LastAcceptedTimeStep = -1,
                CreatedAt = now,
                PendingExpiresAt = now.Add(PendingEnrollmentLifetime),
            });
        }
        else
        {
            existing.EncryptedSecret = secretProtector.Encrypt(secret);
            existing.Status = VendorTwoFactorStatus.Pending;
            existing.LastAcceptedTimeStep = -1;
            existing.CreatedAt = now;
            existing.PendingExpiresAt = now.Add(PendingEnrollmentLifetime);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var otpAuthUri = totpService.BuildOtpAuthUri(secret, Issuer, vendor.Email);
        return new VendorTwoFactorEnrollmentDto
        {
            ManualEntryKey = totpService.ToBase32(secret),
            QrCodePngBase64 = BuildQrCodePng(otpAuthUri),
        };
    }

    public async Task<VendorSession> ConfirmEnrollmentAsync(VendorConfirmTwoFactorEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var vendorId = await RequirePendingTokenAsync(request.PendingToken);

        var credential = await dbContext.VendorTwoFactorCredentials
            .FirstOrDefaultAsync(c => c.VendorId == vendorId, cancellationToken);

        if (credential is null
            || credential.Status != VendorTwoFactorStatus.Pending
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

        credential.Status = VendorTwoFactorStatus.Active;
        credential.ActivatedAt = DateTime.UtcNow;
        credential.PendingExpiresAt = null;
        credential.LastAcceptedTimeStep = matchedStep.Value;
        await dbContext.SaveChangesAsync(cancellationToken);

        var vendor = await dbContext.Vendors.FirstAsync(v => v.Id == vendorId, cancellationToken);
        return await IssueSessionAsync(vendor, cancellationToken);
    }

    public async Task<VendorSession> VerifyTwoFactorAsync(VerifyVendorTwoFactorRequest request, CancellationToken cancellationToken = default)
    {
        var vendorId = await RequirePendingTokenAsync(request.PendingToken);

        var vendor = await dbContext.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId && v.Status == VendorStatus.Active, cancellationToken);
        var credential = vendor is null
            ? null
            : await dbContext.VendorTwoFactorCredentials
                .FirstOrDefaultAsync(c => c.VendorId == vendor.Id && c.Status == VendorTwoFactorStatus.Active, cancellationToken);

        if (vendor is null || credential is null)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "This sign-in has expired. Please sign in again.");
        }

        var secret = secretProtector.Decrypt(credential.EncryptedSecret);
        var matchedStep = totpService.VerifyCode(secret, request.Code, credential.LastAcceptedTimeStep);
        if (matchedStep is null)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "Invalid or expired code.");
        }

        var rows = await dbContext.VendorTwoFactorCredentials
            .Where(c => c.VendorId == vendor.Id && c.LastAcceptedTimeStep < matchedStep.Value)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.LastAcceptedTimeStep, matchedStep.Value), cancellationToken);
        if (rows == 0)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "Invalid or expired code.");
        }

        return await IssueSessionAsync(vendor, cancellationToken);
    }

    public async Task<VendorSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = TokenService.HashToken(refreshToken);
        var stored = await dbContext.VendorRefreshTokens
            .Include(t => t.Vendor)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (stored is null)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "Your session has expired. Please sign in again.");
        }

        var now = DateTime.UtcNow;

        if (stored.RevokedAt is not null)
        {
            // Reuse of an already-rotated token means the refresh token chain has been
            // compromised - revoke the whole chain for this vendor, same as the customer flow.
            await dbContext.VendorRefreshTokens
                .Where(t => t.VendorId == stored.VendorId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
            throw new ApplicationFailure(FailureKind.Unauthorized, "Your session has expired. Please sign in again.");
        }

        if (!stored.IsActive(now) || stored.Vendor.Status != VendorStatus.Active)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "Your session has expired. Please sign in again.");
        }

        var replacement = TokenService.CreateOpaqueToken();
        var replacementHash = TokenService.HashToken(replacement);
        var replacementExpiresAt = tokenService.RefreshTokenExpiry();

        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = replacementHash;
        dbContext.VendorRefreshTokens.Add(new VendorRefreshToken
        {
            VendorId = stored.VendorId,
            TokenHash = replacementHash,
            ExpiresAt = replacementExpiresAt,
            CreatedAt = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildSession(stored.Vendor, replacement, replacementExpiresAt);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        var tokenHash = TokenService.HashToken(refreshToken);
        var stored = await dbContext.VendorRefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<long> RequirePendingTokenAsync(string pendingToken)
    {
        var validated = await tokenService.ValidateVendorPendingTwoFactorTokenAsync(pendingToken);
        if (validated is null)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, "This sign-in has expired. Please sign in again.");
        }

        return validated.Value.VendorId;
    }

    private async Task<VendorSession> IssueSessionAsync(Vendor vendor, CancellationToken cancellationToken)
    {
        var refreshToken = TokenService.CreateOpaqueToken();
        var refreshExpiresAt = tokenService.RefreshTokenExpiry();

        dbContext.VendorRefreshTokens.Add(new VendorRefreshToken
        {
            VendorId = vendor.Id,
            TokenHash = TokenService.HashToken(refreshToken),
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildSession(vendor, refreshToken, refreshExpiresAt);
    }

    private VendorSession BuildSession(Vendor vendor, string refreshToken, DateTime refreshExpiresAt)
    {
        var (accessToken, accessExpiresAt) = tokenService.CreateVendorAccessToken(vendor);

        return new VendorSession
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            Vendor = ToProfileDto(vendor),
        };
    }

    private static VendorProfileDto ToProfileDto(Vendor vendor) => new()
    {
        Id = vendor.Id,
        Email = vendor.Email,
        VendorCode = vendor.VendorCode,
        ShopName = vendor.ShopName,
        ContactPerson = vendor.ContactPerson,
        Phone = vendor.Phone,
        Address = vendor.Address,
        City = vendor.City,
    };

    private static string BuildQrCodePng(string otpAuthUri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.M);
        using var pngCode = new PngByteQRCode(data);
        return Convert.ToBase64String(pngCode.GetGraphic(10));
    }
}
