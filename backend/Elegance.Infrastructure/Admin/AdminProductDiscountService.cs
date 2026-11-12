using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Catalog.Enums;
using Elegance.Domain.Discounts;
using Elegance.Domain.Discounts.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminProductDiscountService(EleganceDbContext dbContext, IAuditLogService auditLog) : IAdminProductDiscountService
{
    private sealed record DiscountRow(
        long Id, long ProductId, long? ProductVariantId, string? VariantName,
        decimal OriginalPriceSnapshot, decimal DiscountPercentage, decimal DiscountAmount, decimal FinalPriceSnapshot,
        DateTime StartDate, DateTime EndDate, ProductDiscountStatus Status, long CreatedByAdminUserId, DateTime CreatedAt);

    public async Task<IReadOnlyList<AdminProductDiscountDto>> ListProductDiscountsAsync(long productId, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.ProductDiscounts
            .AsNoTracking()
            .Where(d => d.ProductId == productId)
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id)
            .Select(d => new DiscountRow(
                d.Id, d.ProductId, d.ProductVariantId,
                d.ProductVariant != null ? d.ProductVariant.VariantName : null,
                d.OriginalPriceSnapshot, d.DiscountPercentage, d.DiscountAmount, d.FinalPriceSnapshot,
                d.StartDate, d.EndDate, d.Status, d.CreatedByAdminUserId, d.CreatedAt))
            .ToListAsync(cancellationToken);

        var adminIds = rows.Select(r => r.CreatedByAdminUserId).Distinct().ToList();
        var emails = await dbContext.AdminUsers
            .AsNoTracking()
            .Where(u => adminIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        var now = DateTime.UtcNow;
        return rows.Select(r => ToDto(r, emails.GetValueOrDefault(r.CreatedByAdminUserId), now)).ToList();
    }

    public async Task<AdminProductDiscountDto> CreateProductDiscountAsync(
        AdminActor actor, long productId, AdminProductDiscountRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DiscountPercentage <= 0 || request.DiscountPercentage > 100)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Discount percentage must be greater than 0 and at most 100.");
        }

        if (request.EndDate <= request.StartDate)
        {
            throw new ApplicationFailure(FailureKind.Validation, "End date must be after the start date.");
        }

        var now = DateTime.UtcNow;
        if (request.EndDate <= now)
        {
            throw new ApplicationFailure(FailureKind.Validation, "End date must be in the future.");
        }

        var product = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.Id, p.ProductType, p.Price })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();

        decimal originalPrice;
        if (request.ProductVariantId is { } variantId)
        {
            var variant = await dbContext.ProductVariants
                .AsNoTracking()
                .Where(v => v.Id == variantId && v.ProductId == productId)
                .Select(v => new { v.Price })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new ApplicationFailure(FailureKind.NotFound, "That option could not be found.");
            originalPrice = variant.Price ?? product.Price;
        }
        else
        {
            if (product.ProductType != ProductType.Simple)
            {
                throw new ApplicationFailure(FailureKind.Validation, "This product has options - choose which option the discount applies to.");
            }
            originalPrice = product.Price;
        }

        // Exactly one discount should ever be "in force" for a given product/variant at a time.
        // Cancelling the previous Active row (not deleting it) replaces it while keeping the
        // history this feature needs - this is how "editing" a discount works: cancel + recreate.
        await dbContext.ProductDiscounts
            .Where(d => d.ProductId == productId && d.ProductVariantId == request.ProductVariantId && d.Status == ProductDiscountStatus.Active)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, ProductDiscountStatus.Cancelled)
                .SetProperty(d => d.UpdatedAt, now), cancellationToken);

        var discountAmount = Math.Round(originalPrice * request.DiscountPercentage / 100m, 2, MidpointRounding.AwayFromZero);
        var finalPrice = originalPrice - discountAmount;

        var discount = new ProductDiscount
        {
            ProductId = productId,
            ProductVariantId = request.ProductVariantId,
            OriginalPriceSnapshot = originalPrice,
            DiscountPercentage = request.DiscountPercentage,
            DiscountAmount = discountAmount,
            FinalPriceSnapshot = finalPrice,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = ProductDiscountStatus.Active,
            CreatedByAdminUserId = actor.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.ProductDiscounts.Add(discount);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(
            actor, "product_discount.create", "Product", productId.ToString(),
            $"Created a {request.DiscountPercentage}% discount on product {productId}"
                + (request.ProductVariantId is { } v ? $" (option {v})" : "")
                + $", {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}",
            cancellationToken);

        return ToDto(
            new DiscountRow(discount.Id, discount.ProductId, discount.ProductVariantId, null,
                discount.OriginalPriceSnapshot, discount.DiscountPercentage, discount.DiscountAmount, discount.FinalPriceSnapshot,
                discount.StartDate, discount.EndDate, discount.Status, discount.CreatedByAdminUserId, discount.CreatedAt),
            actor.Email, now);
    }

    public async Task CancelProductDiscountAsync(AdminActor actor, long productId, long discountId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var taken = await dbContext.ProductDiscounts
            .Where(d => d.Id == discountId && d.ProductId == productId && d.Status == ProductDiscountStatus.Active)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, ProductDiscountStatus.Cancelled)
                .SetProperty(d => d.UpdatedAt, now), cancellationToken);

        if (taken == 0)
        {
            throw NotFound();
        }

        await auditLog.RecordAsync(actor, "product_discount.cancel", "Product", productId.ToString(), $"Cancelled discount {discountId}", cancellationToken);
    }

    private static AdminProductDiscountDto ToDto(DiscountRow r, string? createdByEmail, DateTime now) => new()
    {
        Id = r.Id,
        ProductId = r.ProductId,
        ProductVariantId = r.ProductVariantId,
        VariantName = r.VariantName,
        OriginalPriceSnapshot = r.OriginalPriceSnapshot,
        DiscountPercentage = r.DiscountPercentage,
        DiscountAmount = r.DiscountAmount,
        FinalPriceSnapshot = r.FinalPriceSnapshot,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        Status = r.Status.ToString(),
        EffectiveStatus = ComputeEffectiveStatus(r.Status, r.StartDate, r.EndDate, now),
        CreatedByEmail = createdByEmail,
        CreatedAt = r.CreatedAt,
    };

    private static string ComputeEffectiveStatus(ProductDiscountStatus status, DateTime start, DateTime end, DateTime now)
    {
        if (status == ProductDiscountStatus.Cancelled) return "Cancelled";
        if (now < start) return "Scheduled";
        if (now > end) return "Expired";
        return "Active";
    }

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");
}
