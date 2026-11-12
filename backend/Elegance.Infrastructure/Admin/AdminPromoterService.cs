using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Marketing;
using Elegance.Domain.Marketing.Enums;
using Elegance.Infrastructure.Marketing;
using Elegance.Infrastructure.Notifications;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminPromoterService(EleganceDbContext dbContext, IAuditLogService auditLog) : IAdminPromoterService
{
    // EF Core can't reliably translate a call to a C# helper method (ToDto, which calls
    // .ToString() on enums) inside a live IQueryable .Select() - project to this plain-scalar
    // shape first (fully translatable, including the Count subquery), materialize, then map.
    private sealed record PromoterRow(
        long Id, string Name, string WhatsAppNumber, string? Email, string? Address,
        PromoterType PromoterType, decimal CommissionPercentage, PromoterStatus Status, string? Notes,
        int PromoCodeCount, DateTime CreatedAt);

    public async Task<PagedResult<AdminPromoterDto>> ListPromotersAsync(AdminPromoterQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var promoters = dbContext.Promoters.AsNoTracking().AsQueryable();

        if (Enum.TryParse<PromoterStatus>(query.Status, true, out var status))
        {
            promoters = promoters.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            promoters = promoters.Where(p =>
                p.Name.Contains(term) ||
                p.WhatsAppNumber.Contains(term) ||
                (p.Email != null && p.Email.Contains(term)));
        }

        var total = await promoters.CountAsync(cancellationToken);
        var rows = await promoters
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PromoterRow(
                p.Id, p.Name, p.WhatsAppNumber, p.Email, p.Address,
                p.PromoterType, p.CommissionPercentage, p.Status, p.Notes,
                p.PromoCodes.Count, p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminPromoterDto> { Items = rows.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<AdminPromoterDto> GetPromoterAsync(long promoterId, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.Promoters
            .AsNoTracking()
            .Where(p => p.Id == promoterId)
            .Select(p => new PromoterRow(
                p.Id, p.Name, p.WhatsAppNumber, p.Email, p.Address,
                p.PromoterType, p.CommissionPercentage, p.Status, p.Notes,
                p.PromoCodes.Count, p.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();

        return ToDto(row);
    }

    public async Task<AdminPromoterDto> CreatePromoterAsync(AdminActor actor, AdminPromoterRequest request, CancellationToken cancellationToken = default)
    {
        var promoterType = ParsePromoterType(request.PromoterType);

        if (!PhoneNumberNormalizer.TryNormalize(request.WhatsAppNumber, out var normalizedNumber))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Enter a valid WhatsApp number, including the country code if it isn't a Pakistani number.");
        }

        var now = DateTime.UtcNow;
        var promoter = new Promoter
        {
            Name = request.Name.Trim(),
            WhatsAppNumber = normalizedNumber,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            PromoterType = promoterType,
            CommissionPercentage = request.CommissionPercentage,
            Status = PromoterStatus.Active,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.Promoters.Add(promoter);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Every promoter gets one default code at registration, generated right away - a
        // campaign-specific code with its own rules can be added afterwards via Promo Codes.
        var code = await PromoCodeGenerator.GenerateUniqueCodeAsync(dbContext, cancellationToken);
        dbContext.PromoCodes.Add(new PromoCode
        {
            Code = code,
            PromoterId = promoter.Id,
            GrantsCustomerDiscount = false,
            CustomerDiscountPercentage = 0,
            GrantsCommission = true,
            StartDate = now,
            Status = PromoCodeStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "promoter.create", "Promoter", promoter.Id.ToString(), $"Registered promoter {promoter.Name}", cancellationToken);

        return await GetPromoterAsync(promoter.Id, cancellationToken);
    }

    public async Task<AdminPromoterDto> UpdatePromoterAsync(AdminActor actor, long promoterId, AdminPromoterRequest request, CancellationToken cancellationToken = default)
    {
        var promoterType = ParsePromoterType(request.PromoterType);

        if (!PhoneNumberNormalizer.TryNormalize(request.WhatsAppNumber, out var normalizedNumber))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Enter a valid WhatsApp number, including the country code if it isn't a Pakistani number.");
        }

        var promoter = await dbContext.Promoters.FirstOrDefaultAsync(p => p.Id == promoterId, cancellationToken) ?? throw NotFound();

        promoter.Name = request.Name.Trim();
        promoter.WhatsAppNumber = normalizedNumber;
        promoter.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        promoter.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        promoter.PromoterType = promoterType;
        promoter.CommissionPercentage = request.CommissionPercentage;
        promoter.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        promoter.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "promoter.update", "Promoter", promoter.Id.ToString(), $"Updated promoter {promoter.Name}", cancellationToken);

        return await GetPromoterAsync(promoterId, cancellationToken);
    }

    public async Task<AdminPromoterDto> SetPromoterStatusAsync(AdminActor actor, long promoterId, string status, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<PromoterStatus>(status, true, out var parsed))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Enter a valid promoter status.");
        }

        var taken = await dbContext.Promoters
            .Where(p => p.Id == promoterId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, parsed)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), cancellationToken);

        if (taken == 0)
        {
            throw NotFound();
        }

        await auditLog.RecordAsync(actor, "promoter.status", "Promoter", promoterId.ToString(), $"Set promoter {promoterId} status to {parsed}", cancellationToken);

        return await GetPromoterAsync(promoterId, cancellationToken);
    }

    private static PromoterType ParsePromoterType(string value) =>
        Enum.TryParse<PromoterType>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ApplicationFailure(FailureKind.Validation, "Enter a valid promoter type.");

    private static AdminPromoterDto ToDto(PromoterRow r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        WhatsAppNumber = r.WhatsAppNumber,
        Email = r.Email,
        Address = r.Address,
        PromoterType = r.PromoterType.ToString(),
        CommissionPercentage = r.CommissionPercentage,
        Status = r.Status.ToString(),
        Notes = r.Notes,
        PromoCodeCount = r.PromoCodeCount,
        CreatedAt = r.CreatedAt,
    };

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested promoter was not found.");
}
