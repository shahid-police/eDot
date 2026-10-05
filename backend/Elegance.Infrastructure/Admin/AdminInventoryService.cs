using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Catalog.Enums;
using Elegance.Domain.Inventory;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminInventoryService(EleganceDbContext dbContext, IAuditLogService auditLog) : IAdminInventoryService
{
    // Used when a product has no LowStockThreshold of its own.
    public const int DefaultLowStockThreshold = 5;

    public async Task<IReadOnlyList<LowStockItemDto>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        var simple = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.ProductType == ProductType.Simple
                && p.StockQuantity <= (p.LowStockThreshold ?? DefaultLowStockThreshold))
            .Select(p => new LowStockItemDto
            {
                ProductId = p.Id,
                VariantId = null,
                Sku = p.Sku,
                Name = p.Name,
                StockQuantity = p.StockQuantity,
                Threshold = p.LowStockThreshold ?? DefaultLowStockThreshold,
            })
            .ToListAsync(cancellationToken);

        var variants = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.IsActive && v.Product.IsActive
                && v.StockQuantity <= (v.Product.LowStockThreshold ?? DefaultLowStockThreshold))
            .Select(v => new LowStockItemDto
            {
                ProductId = v.ProductId,
                VariantId = v.Id,
                Sku = v.Sku,
                Name = v.Product.Name + " - " + v.VariantName,
                StockQuantity = v.StockQuantity,
                Threshold = v.Product.LowStockThreshold ?? DefaultLowStockThreshold,
            })
            .ToListAsync(cancellationToken);

        return simple.Concat(variants).OrderBy(i => i.StockQuantity).ThenBy(i => i.Sku).ToList();
    }

    public async Task<StockMovementDto> AdjustStockAsync(AdminActor actor, StockAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var delta = request.QuantityDelta;
        if (delta == 0)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Enter a quantity other than zero.");
        }

        var productType = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == request.ProductId)
            .Select(p => (ProductType?)p.ProductType)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();

        if (request.VariantId is { } variantId)
        {
            var belongs = await dbContext.ProductVariants.AnyAsync(
                v => v.Id == variantId && v.ProductId == request.ProductId, cancellationToken);
            if (!belongs)
            {
                throw NotFound();
            }

            var taken = await dbContext.ProductVariants
                .Where(v => v.Id == variantId && (delta > 0 || v.StockQuantity >= -delta))
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + delta), cancellationToken);
            EnsureTaken(taken);
        }
        else
        {
            if (productType == ProductType.Variant)
            {
                throw new ApplicationFailure(FailureKind.Validation, "This product has options. Adjust stock on the option instead.");
            }

            var taken = await dbContext.Products
                .Where(p => p.Id == request.ProductId && (delta > 0 || p.StockQuantity >= -delta))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + delta), cancellationToken);
            EnsureTaken(taken);
        }

        var movement = new StockMovement
        {
            ProductId = request.ProductId,
            VariantId = request.VariantId,
            QuantityDelta = delta,
            Reason = StockMovementReason.Adjustment,
            Note = request.Note.Trim(),
            AdminUserId = actor.Id,
            CreatedAt = DateTime.UtcNow,
        };
        dbContext.StockMovements.Add(movement);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(
            actor, "stock.adjust", "Product", request.ProductId.ToString(),
            $"Stock {delta:+#;-#} on product {request.ProductId}", cancellationToken);

        return ToDto(movement, actor.Email);
    }

    public async Task<PagedResult<StockMovementDto>> ListMovementsAsync(long? productId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var movements = dbContext.StockMovements.AsNoTracking().AsQueryable();
        if (productId is { } id)
        {
            movements = movements.Where(m => m.ProductId == id);
        }

        var total = await movements.CountAsync(cancellationToken);
        var rows = await movements
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var adminIds = rows.Where(m => m.AdminUserId is not null).Select(m => m.AdminUserId!.Value).Distinct().ToList();
        var emails = await dbContext.AdminUsers
            .AsNoTracking()
            .Where(u => adminIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        return new PagedResult<StockMovementDto>
        {
            Items = rows.Select(m => ToDto(m, m.AdminUserId is { } adminId && emails.TryGetValue(adminId, out var email) ? email : null)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    private static void EnsureTaken(int taken)
    {
        if (taken == 0)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Not enough stock to remove that quantity.");
        }
    }

    private static StockMovementDto ToDto(StockMovement movement, string? adminEmail) => new()
    {
        Id = movement.Id,
        ProductId = movement.ProductId,
        VariantId = movement.VariantId,
        QuantityDelta = movement.QuantityDelta,
        Reason = movement.Reason.ToString(),
        OrderId = movement.OrderId,
        Note = movement.Note,
        AdminEmail = adminEmail,
        CreatedAt = movement.CreatedAt,
    };

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");
}
