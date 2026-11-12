using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Marketing;
using Elegance.Domain.Marketing.Enums;
using Elegance.Infrastructure.Marketing;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminPromoCodeService(EleganceDbContext dbContext, IAuditLogService auditLog) : IAdminPromoCodeService
{
    // Same reasoning as AdminPromoterService.PromoterRow - keep the live IQueryable projection to
    // plain scalars only, map to the real DTO (with .ToString() on enums) after materializing.
    private sealed record PromoCodeRow(
        long Id, string Code, long PromoterId, string PromoterName,
        bool GrantsCustomerDiscount, decimal CustomerDiscountPercentage, bool GrantsCommission,
        decimal? CommissionPercentageOverride, decimal? MinimumOrderAmount, decimal? MaximumDiscountAmount,
        int? MaximumTotalUses, int? MaximumUsesPerCustomer, int UsedCount,
        DateTime StartDate, DateTime? ExpiryDate, PromoCodeStatus Status, DateTime CreatedAt);

    public async Task<PagedResult<AdminPromoCodeDto>> ListPromoCodesAsync(AdminPromoCodeQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var codes = dbContext.PromoCodes.AsNoTracking().AsQueryable();

        if (query.PromoterId is { } promoterId)
        {
            codes = codes.Where(c => c.PromoterId == promoterId);
        }

        if (Enum.TryParse<PromoCodeStatus>(query.Status, true, out var status))
        {
            codes = codes.Where(c => c.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            codes = codes.Where(c => c.Code.Contains(term) || c.Promoter.Name.Contains(term));
        }

        var total = await codes.CountAsync(cancellationToken);
        var rows = await codes
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new PromoCodeRow(
                c.Id, c.Code, c.PromoterId, c.Promoter.Name,
                c.GrantsCustomerDiscount, c.CustomerDiscountPercentage, c.GrantsCommission,
                c.CommissionPercentageOverride, c.MinimumOrderAmount, c.MaximumDiscountAmount,
                c.MaximumTotalUses, c.MaximumUsesPerCustomer, c.UsedCount,
                c.StartDate, c.ExpiryDate, c.Status, c.CreatedAt))
            .ToListAsync(cancellationToken);

        var (productIds, categoryIds) = await LoadAssociationsAsync(rows.Select(r => r.Id).ToList(), cancellationToken);
        var now = DateTime.UtcNow;

        return new PagedResult<AdminPromoCodeDto>
        {
            Items = rows.Select(r => ToDto(r, productIds.GetValueOrDefault(r.Id, []), categoryIds.GetValueOrDefault(r.Id, []), now)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<AdminPromoCodeDto> GetPromoCodeAsync(long promoCodeId, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.PromoCodes
            .AsNoTracking()
            .Where(c => c.Id == promoCodeId)
            .Select(c => new PromoCodeRow(
                c.Id, c.Code, c.PromoterId, c.Promoter.Name,
                c.GrantsCustomerDiscount, c.CustomerDiscountPercentage, c.GrantsCommission,
                c.CommissionPercentageOverride, c.MinimumOrderAmount, c.MaximumDiscountAmount,
                c.MaximumTotalUses, c.MaximumUsesPerCustomer, c.UsedCount,
                c.StartDate, c.ExpiryDate, c.Status, c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();

        var (productIds, categoryIds) = await LoadAssociationsAsync([row.Id], cancellationToken);
        return ToDto(row, productIds.GetValueOrDefault(row.Id, []), categoryIds.GetValueOrDefault(row.Id, []), DateTime.UtcNow);
    }

    public async Task<AdminPromoCodeDto> CreatePromoCodeAsync(AdminActor actor, AdminPromoCodeRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var now = DateTime.UtcNow;
        var code = await PromoCodeGenerator.GenerateUniqueCodeAsync(dbContext, cancellationToken);

        var promoCode = new PromoCode
        {
            Code = code,
            PromoterId = request.PromoterId,
            GrantsCustomerDiscount = request.GrantsCustomerDiscount,
            CustomerDiscountPercentage = request.GrantsCustomerDiscount ? request.CustomerDiscountPercentage : 0,
            GrantsCommission = request.GrantsCommission,
            CommissionPercentageOverride = request.CommissionPercentageOverride,
            MinimumOrderAmount = request.MinimumOrderAmount,
            MaximumDiscountAmount = request.MaximumDiscountAmount,
            MaximumTotalUses = request.MaximumTotalUses,
            MaximumUsesPerCustomer = request.MaximumUsesPerCustomer,
            StartDate = request.StartDate,
            ExpiryDate = request.ExpiryDate,
            Status = PromoCodeStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.PromoCodes.Add(promoCode);
        await dbContext.SaveChangesAsync(cancellationToken);

        await ReplaceAssociationsAsync(promoCode.Id, request.ProductIds, request.CategoryIds, cancellationToken);

        await auditLog.RecordAsync(actor, "promocode.create", "PromoCode", promoCode.Id.ToString(), $"Created promo code {code} for promoter {request.PromoterId}", cancellationToken);

        return await GetPromoCodeAsync(promoCode.Id, cancellationToken);
    }

    public async Task<AdminPromoCodeDto> UpdatePromoCodeAsync(AdminActor actor, long promoCodeId, AdminPromoCodeRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var promoCode = await dbContext.PromoCodes.FirstOrDefaultAsync(c => c.Id == promoCodeId, cancellationToken) ?? throw NotFound();

        promoCode.PromoterId = request.PromoterId;
        promoCode.GrantsCustomerDiscount = request.GrantsCustomerDiscount;
        promoCode.CustomerDiscountPercentage = request.GrantsCustomerDiscount ? request.CustomerDiscountPercentage : 0;
        promoCode.GrantsCommission = request.GrantsCommission;
        promoCode.CommissionPercentageOverride = request.CommissionPercentageOverride;
        promoCode.MinimumOrderAmount = request.MinimumOrderAmount;
        promoCode.MaximumDiscountAmount = request.MaximumDiscountAmount;
        promoCode.MaximumTotalUses = request.MaximumTotalUses;
        promoCode.MaximumUsesPerCustomer = request.MaximumUsesPerCustomer;
        promoCode.StartDate = request.StartDate;
        promoCode.ExpiryDate = request.ExpiryDate;
        promoCode.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await ReplaceAssociationsAsync(promoCodeId, request.ProductIds, request.CategoryIds, cancellationToken);

        await auditLog.RecordAsync(actor, "promocode.update", "PromoCode", promoCodeId.ToString(), $"Updated promo code {promoCodeId}", cancellationToken);

        return await GetPromoCodeAsync(promoCodeId, cancellationToken);
    }

    public async Task<AdminPromoCodeDto> SetPromoCodeStatusAsync(AdminActor actor, long promoCodeId, string status, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<PromoCodeStatus>(status, true, out var parsed))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Enter a valid promo code status.");
        }

        var taken = await dbContext.PromoCodes
            .Where(c => c.Id == promoCodeId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, parsed)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow), cancellationToken);

        if (taken == 0)
        {
            throw NotFound();
        }

        await auditLog.RecordAsync(actor, "promocode.status", "PromoCode", promoCodeId.ToString(), $"Set promo code {promoCodeId} status to {parsed}", cancellationToken);

        return await GetPromoCodeAsync(promoCodeId, cancellationToken);
    }

    public async Task<AdminPromoCodeDto> RegeneratePromoCodeAsync(AdminActor actor, long promoCodeId, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.PromoCodes.AsNoTracking().AnyAsync(c => c.Id == promoCodeId, cancellationToken);
        if (!exists)
        {
            throw NotFound();
        }

        var newCode = await PromoCodeGenerator.GenerateUniqueCodeAsync(dbContext, cancellationToken);
        await dbContext.PromoCodes
            .Where(c => c.Id == promoCodeId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Code, newCode)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow), cancellationToken);

        await auditLog.RecordAsync(actor, "promocode.regenerate", "PromoCode", promoCodeId.ToString(), $"Regenerated promo code {promoCodeId} to {newCode}", cancellationToken);

        return await GetPromoCodeAsync(promoCodeId, cancellationToken);
    }

    private async Task ValidateAsync(AdminPromoCodeRequest request, CancellationToken cancellationToken)
    {
        if (!request.GrantsCustomerDiscount && !request.GrantsCommission)
        {
            throw new ApplicationFailure(FailureKind.Validation, "A promo code must grant a customer discount, a promoter commission, or both.");
        }

        if (request.GrantsCustomerDiscount && request.CustomerDiscountPercentage <= 0)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Enter a customer discount percentage greater than 0.");
        }

        if (request.ExpiryDate is { } expiry && expiry <= request.StartDate)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Expiry date must be after the start date.");
        }

        var promoterExists = await dbContext.Promoters.AsNoTracking().AnyAsync(p => p.Id == request.PromoterId, cancellationToken);
        if (!promoterExists)
        {
            throw new ApplicationFailure(FailureKind.NotFound, "That promoter could not be found.");
        }

        if (request.ProductIds.Count > 0)
        {
            var distinctIds = request.ProductIds.Distinct().ToList();
            var found = await dbContext.Products.AsNoTracking().CountAsync(p => distinctIds.Contains(p.Id), cancellationToken);
            if (found != distinctIds.Count)
            {
                throw new ApplicationFailure(FailureKind.Validation, "One or more selected products could not be found.");
            }
        }

        if (request.CategoryIds.Count > 0)
        {
            var distinctIds = request.CategoryIds.Distinct().ToList();
            var found = await dbContext.Categories.AsNoTracking().CountAsync(c => distinctIds.Contains(c.Id), cancellationToken);
            if (found != distinctIds.Count)
            {
                throw new ApplicationFailure(FailureKind.Validation, "One or more selected categories could not be found.");
            }
        }
    }

    private async Task ReplaceAssociationsAsync(long promoCodeId, List<long> productIds, List<long> categoryIds, CancellationToken cancellationToken)
    {
        await dbContext.PromoCodeProducts.Where(pp => pp.PromoCodeId == promoCodeId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.PromoCodeCategories.Where(pc => pc.PromoCodeId == promoCodeId).ExecuteDeleteAsync(cancellationToken);

        foreach (var productId in productIds.Distinct())
        {
            dbContext.PromoCodeProducts.Add(new PromoCodeProduct { PromoCodeId = promoCodeId, ProductId = productId });
        }
        foreach (var categoryId in categoryIds.Distinct())
        {
            dbContext.PromoCodeCategories.Add(new PromoCodeCategory { PromoCodeId = promoCodeId, CategoryId = categoryId });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<(Dictionary<long, List<long>> ProductIds, Dictionary<long, List<long>> CategoryIds)> LoadAssociationsAsync(
        List<long> promoCodeIds, CancellationToken cancellationToken)
    {
        if (promoCodeIds.Count == 0)
        {
            return ([], []);
        }

        var productLinks = await dbContext.PromoCodeProducts
            .AsNoTracking()
            .Where(pp => promoCodeIds.Contains(pp.PromoCodeId))
            .Select(pp => new { pp.PromoCodeId, pp.ProductId })
            .ToListAsync(cancellationToken);

        var categoryLinks = await dbContext.PromoCodeCategories
            .AsNoTracking()
            .Where(pc => promoCodeIds.Contains(pc.PromoCodeId))
            .Select(pc => new { pc.PromoCodeId, pc.CategoryId })
            .ToListAsync(cancellationToken);

        return (
            productLinks.GroupBy(l => l.PromoCodeId).ToDictionary(g => g.Key, g => g.Select(l => l.ProductId).ToList()),
            categoryLinks.GroupBy(l => l.PromoCodeId).ToDictionary(g => g.Key, g => g.Select(l => l.CategoryId).ToList()));
    }

    private static AdminPromoCodeDto ToDto(PromoCodeRow r, IReadOnlyList<long> productIds, IReadOnlyList<long> categoryIds, DateTime now) => new()
    {
        Id = r.Id,
        Code = r.Code,
        PromoterId = r.PromoterId,
        PromoterName = r.PromoterName,
        GrantsCustomerDiscount = r.GrantsCustomerDiscount,
        CustomerDiscountPercentage = r.CustomerDiscountPercentage,
        GrantsCommission = r.GrantsCommission,
        CommissionPercentageOverride = r.CommissionPercentageOverride,
        MinimumOrderAmount = r.MinimumOrderAmount,
        MaximumDiscountAmount = r.MaximumDiscountAmount,
        MaximumTotalUses = r.MaximumTotalUses,
        MaximumUsesPerCustomer = r.MaximumUsesPerCustomer,
        UsedCount = r.UsedCount,
        StartDate = r.StartDate,
        ExpiryDate = r.ExpiryDate,
        Status = r.Status.ToString(),
        EffectiveStatus = ComputeEffectiveStatus(r.Status, r.StartDate, r.ExpiryDate, now),
        ProductIds = productIds,
        CategoryIds = categoryIds,
        CreatedAt = r.CreatedAt,
    };

    private static string ComputeEffectiveStatus(PromoCodeStatus status, DateTime start, DateTime? expiry, DateTime now)
    {
        if (status == PromoCodeStatus.Inactive) return "Inactive";
        if (now < start) return "Scheduled";
        if (expiry is { } e && now > e) return "Expired";
        return "Active";
    }

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested promo code was not found.");
}
