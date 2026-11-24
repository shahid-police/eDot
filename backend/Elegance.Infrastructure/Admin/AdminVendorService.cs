using System.Security.Cryptography;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Vendors;
using Elegance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminVendorService(EleganceDbContext dbContext, IAuditLogService auditLog) : IAdminVendorService
{
    private const string VendorCodePrefix = "VEN";
    private const int MaxCodeGenerationAttempts = 5;
    private readonly PasswordHasher<Vendor> _passwordHasher = new();

    public async Task<PagedResult<AdminVendorDto>> ListVendorsAsync(AdminVendorQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var vendors = dbContext.Vendors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            vendors = vendors.Where(v => v.ShopName.Contains(search) || v.Email.Contains(search) || v.VendorCode.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<VendorStatus>(query.Status, true, out var status))
        {
            vendors = vendors.Where(v => v.Status == status);
        }

        var total = await vendors.CountAsync(cancellationToken);
        var ids = await vendors
            .OrderByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        var items = new List<AdminVendorDto>(ids.Count);
        foreach (var id in ids)
        {
            items.Add(await LoadDtoAsync(id, cancellationToken));
        }

        return new PagedResult<AdminVendorDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<AdminVendorDto> GetVendorAsync(long vendorId, CancellationToken cancellationToken = default) =>
        await LoadDtoAsync(vendorId, cancellationToken);

    public async Task<(AdminVendorDto Vendor, string TemporaryPassword)> CreateVendorAsync(AdminActor actor, AdminVendorRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Vendors.AsNoTracking().AnyAsync(v => v.Email == email, cancellationToken))
        {
            throw new ApplicationFailure(FailureKind.Conflict, "A vendor with this email already exists.");
        }

        var now = DateTime.UtcNow;
        var temporaryPassword = GenerateTemporaryPassword();

        for (var attempt = 1; ; attempt++)
        {
            var vendor = new Vendor
            {
                Email = email,
                PasswordHash = string.Empty,
                VendorCode = await ComputeNextVendorCodeAsync(cancellationToken),
                ShopName = request.ShopName.Trim(),
                ContactPerson = request.ContactPerson.Trim(),
                Phone = request.Phone.Trim(),
                Address = request.Address?.Trim(),
                City = request.City?.Trim(),
                Status = VendorStatus.Active,
                AdminRemarks = request.AdminRemarks?.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
            };
            vendor.PasswordHash = _passwordHasher.HashPassword(vendor, temporaryPassword);

            dbContext.Vendors.Add(vendor);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException) when (attempt < MaxCodeGenerationAttempts)
            {
                dbContext.ChangeTracker.Clear();
                continue;
            }

            await auditLog.RecordAsync(actor, "vendor.create", "Vendor", vendor.Id.ToString(), $"Created vendor {vendor.VendorCode} ({vendor.ShopName})", cancellationToken);
            return (await LoadDtoAsync(vendor.Id, cancellationToken), temporaryPassword);
        }
    }

    public async Task<AdminVendorDto> UpdateVendorAsync(AdminActor actor, long vendorId, AdminVendorRequest request, CancellationToken cancellationToken = default)
    {
        var vendor = await dbContext.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId, cancellationToken) ?? throw NotFound();

        var email = request.Email.Trim().ToLowerInvariant();
        if (email != vendor.Email && await dbContext.Vendors.AsNoTracking().AnyAsync(v => v.Email == email && v.Id != vendorId, cancellationToken))
        {
            throw new ApplicationFailure(FailureKind.Conflict, "A vendor with this email already exists.");
        }

        vendor.Email = email;
        vendor.ShopName = request.ShopName.Trim();
        vendor.ContactPerson = request.ContactPerson.Trim();
        vendor.Phone = request.Phone.Trim();
        vendor.Address = request.Address?.Trim();
        vendor.City = request.City?.Trim();
        vendor.AdminRemarks = request.AdminRemarks?.Trim();
        vendor.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "vendor.update", "Vendor", vendor.Id.ToString(), $"Updated vendor {vendor.VendorCode}", cancellationToken);
        return await LoadDtoAsync(vendorId, cancellationToken);
    }

    public async Task<AdminVendorDto> SetVendorStatusAsync(AdminActor actor, long vendorId, AdminSetVendorStatusRequest request, CancellationToken cancellationToken = default)
    {
        var vendor = await dbContext.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId, cancellationToken) ?? throw NotFound();
        if (!Enum.TryParse<VendorStatus>(request.Status, true, out var status) || !Enum.IsDefined(status))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Choose Active, Suspended, or Deactivated.");
        }

        vendor.Status = status;
        vendor.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "vendor.status", "Vendor", vendor.Id.ToString(), $"Set {vendor.VendorCode} status to {status}", cancellationToken);
        return await LoadDtoAsync(vendorId, cancellationToken);
    }

    public async Task<AdminVendorCredentialResetDto> ResetVendorCredentialsAsync(AdminActor actor, long vendorId, CancellationToken cancellationToken = default)
    {
        var vendor = await dbContext.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId, cancellationToken) ?? throw NotFound();

        var temporaryPassword = GenerateTemporaryPassword();
        vendor.PasswordHash = _passwordHasher.HashPassword(vendor, temporaryPassword);
        vendor.UpdatedAt = DateTime.UtcNow;

        // Revoke every outstanding refresh token so a reset password actually ends existing
        // sessions, same intent as a customer/admin credential reset would carry.
        await dbContext.VendorRefreshTokens
            .Where(t => t.VendorId == vendorId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "vendor.credentials.reset", "Vendor", vendor.Id.ToString(), $"Reset credentials for {vendor.VendorCode}", cancellationToken);
        return new AdminVendorCredentialResetDto { TemporaryPassword = temporaryPassword };
    }

    // Scans existing codes rather than counting rows, so a gap never produces a reused number -
    // same technique as AdminCatalogService.ComputeNextSkuAsync for product SKUs.
    private async Task<string> ComputeNextVendorCodeAsync(CancellationToken cancellationToken)
    {
        var prefix = $"{VendorCodePrefix}-";
        var existingCodes = await dbContext.Vendors.AsNoTracking()
            .Where(v => v.VendorCode.StartsWith(prefix))
            .Select(v => v.VendorCode)
            .ToListAsync(cancellationToken);

        var maxNumber = 0;
        foreach (var code in existingCodes)
        {
            var suffix = code[prefix.Length..];
            if (int.TryParse(suffix, out var number) && number > maxNumber)
            {
                maxNumber = number;
            }
        }

        return $"{prefix}{(maxNumber + 1).ToString().PadLeft(4, '0')}";
    }

    private static string GenerateTemporaryPassword()
    {
        // Avoids visually-confusable characters (0/O, 1/I/l) since this is read aloud/typed by a
        // human relaying it to the vendor out of band.
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        var bytes = RandomNumberGenerator.GetBytes(16);
        var chars = new char[16];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }
        return new string(chars);
    }

    private async Task<AdminVendorDto> LoadDtoAsync(long vendorId, CancellationToken cancellationToken)
    {
        var vendor = await dbContext.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vendorId, cancellationToken) ?? throw NotFound();
        var submissionCount = await dbContext.VendorProductSubmissions.AsNoTracking().CountAsync(s => s.VendorId == vendorId, cancellationToken);
        var pendingCount = await dbContext.VendorProductSubmissions.AsNoTracking()
            .CountAsync(s => s.VendorId == vendorId && s.Status == Domain.Vendors.VendorSubmissionStatus.PendingApproval, cancellationToken);

        return new AdminVendorDto
        {
            Id = vendor.Id,
            Email = vendor.Email,
            VendorCode = vendor.VendorCode,
            ShopName = vendor.ShopName,
            ContactPerson = vendor.ContactPerson,
            Phone = vendor.Phone,
            Address = vendor.Address,
            City = vendor.City,
            Status = vendor.Status.ToString(),
            AdminRemarks = vendor.AdminRemarks,
            CreatedAt = vendor.CreatedAt,
            SubmissionCount = submissionCount,
            PendingSubmissionCount = pendingCount,
        };
    }

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");
}
