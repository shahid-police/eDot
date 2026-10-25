using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Catalog;
using Elegance.Domain.Catalog.Enums;
using Elegance.Domain.Inventory;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminCatalogService(EleganceDbContext dbContext, IAuditLogService auditLog) : IAdminCatalogService
{
    // JEW-{CategoryCode}-{number, zero-padded to at least 3 digits}.
    private const string SkuPrefix = "JEW";
    private const int MaxSkuGenerationAttempts = 5;
    public async Task<PagedResult<AdminProductSummaryDto>> ListProductsAsync(AdminProductQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var products = dbContext.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            products = products.Where(p => p.Name.Contains(search) || p.Sku.Contains(search) || p.Slug.Contains(search));
        }
        if (query.IsActive is { } isActive)
        {
            products = products.Where(p => p.IsActive == isActive);
        }
        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(p => p.CategoryId == categoryId);
        }

        var total = await products.CountAsync(cancellationToken);
        var rows = await products
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Sku,
                p.Name,
                p.Slug,
                CategoryName = p.Category.Name,
                p.ProductType,
                p.Price,
                p.SalePrice,
                p.StockQuantity,
                p.IsActive,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminProductSummaryDto>
        {
            Items = rows.Select(p => new AdminProductSummaryDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Slug = p.Slug,
                CategoryName = p.CategoryName,
                ProductType = p.ProductType.ToString(),
                Price = p.Price,
                SalePrice = p.SalePrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive,
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<AdminProductDto> GetProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(p => p.Variants).ThenInclude(v => v.ImageMediaFile)
            .Include(p => p.Images).ThenInclude(i => i.MediaFile)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw NotFound();
        return ToDto(product);
    }

    public async Task<AdminProductDto> CreateProductAsync(AdminActor actor, AdminProductRequest request, CancellationToken cancellationToken = default)
    {
        var productType = ParseProductType(request.ProductType);
        ValidateVariants(productType, request.Variants);
        var category = await dbContext.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.Validation, "Choose a valid category.");
        await EnsureVariantSkusFreeAsync(request.Variants.Select(v => v.Sku), excludeProductId: null, cancellationToken);

        var autoGenerateSku = string.IsNullOrWhiteSpace(request.Sku);
        var now = DateTime.UtcNow;

        // Auto-generated SKUs are computed from the current max in the category (see
        // ComputeNextSkuAsync), so two admins creating in the same category at once can both
        // compute the same "next" number. The unique index on Product.Sku is the real guarantee;
        // on a collision there, this retries with a freshly computed number instead of failing
        // the request. A manually-entered SKU is never silently changed - it either saves as
        // given or is rejected as a duplicate, same as before this feature existed.
        for (var attempt = 1; ; attempt++)
        {
            var sku = autoGenerateSku ? await ComputeNextSkuAsync(category, cancellationToken) : request.Sku!.Trim();
            await EnsureProductKeysFreeAsync(sku, request.Slug, excludeProductId: null, cancellationToken);

            var product = new Product
            {
                Sku = sku,
                Name = request.Name.Trim(),
                Slug = request.Slug.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                CategoryId = request.CategoryId,
                ProductType = productType,
                StockQuantity = productType == ProductType.Simple ? request.StockQuantity : 0,
            };
            ApplyProductFields(product, request);

            foreach (var variantRequest in request.Variants)
            {
                product.Variants.Add(NewVariant(variantRequest, now));
            }

            dbContext.Products.Add(product);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException) when (autoGenerateSku && attempt < MaxSkuGenerationAttempts)
            {
                dbContext.ChangeTracker.Clear();
                if (!await dbContext.Products.AnyAsync(p => p.Sku == sku, cancellationToken))
                {
                    throw; // Some other failure - not the SKU race this retry exists for.
                }
                continue;
            }

            var openingMovements = new List<StockMovement>();
            if (product.ProductType == ProductType.Simple && product.StockQuantity > 0)
            {
                openingMovements.Add(Movement(actor, product.Id, null, product.StockQuantity, "Opening stock"));
            }
            foreach (var variant in product.Variants.Where(v => v.StockQuantity > 0))
            {
                openingMovements.Add(Movement(actor, product.Id, variant.Id, variant.StockQuantity, "Opening stock"));
            }
            dbContext.StockMovements.AddRange(openingMovements);
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditLog.RecordAsync(actor, "product.create", "Product", product.Id.ToString(), $"Created {product.Sku} {product.Name}", cancellationToken);
            return await GetProductAsync(product.Id, cancellationToken);
        }
    }

    public async Task<NextSkuDto> PreviewNextSkuAsync(long categoryId, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken)
            ?? throw NotFound();
        return new NextSkuDto { Sku = await ComputeNextSkuAsync(category, cancellationToken) };
    }

    // Looks at every existing SKU for this category's prefix and takes the highest number found,
    // not the row count - so a gap (e.g. ...-001 then ...-005, if 002-004 were deleted or
    // reassigned) still produces ...-006, never a reused or duplicate number.
    private async Task<string> ComputeNextSkuAsync(Category category, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(category.Code))
        {
            throw new ApplicationFailure(FailureKind.Validation,
                $"'{category.Name}' has no SKU code configured yet. Set one on the category, or enter a SKU manually.");
        }

        var prefix = $"{SkuPrefix}-{category.Code}-";
        var existingSkus = await dbContext.Products.AsNoTracking()
            .Where(p => p.Sku.StartsWith(prefix))
            .Select(p => p.Sku)
            .ToListAsync(cancellationToken);

        var maxNumber = 0;
        foreach (var sku in existingSkus)
        {
            var suffix = sku[prefix.Length..];
            if (int.TryParse(suffix, out var number) && number > maxNumber)
            {
                maxNumber = number;
            }
        }

        return $"{prefix}{(maxNumber + 1).ToString().PadLeft(3, '0')}";
    }

    public async Task<AdminProductDto> UpdateProductAsync(AdminActor actor, long productId, AdminProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw NotFound();

        var productType = ParseProductType(request.ProductType);
        ValidateVariants(productType, request.Variants);
        // Auto-generation only applies to new products (CreateProductAsync) - an existing
        // product already has a real SKU, so editing it still requires one explicitly.
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            throw new ApplicationFailure(FailureKind.Validation, "SKU is required.");
        }
        await EnsureProductKeysFreeAsync(request.Sku, request.Slug, excludeProductId: productId, cancellationToken);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        await EnsureVariantSkusFreeAsync(request.Variants.Select(v => v.Sku), excludeProductId: productId, cancellationToken);

        var now = DateTime.UtcNow;
        var pendingMovements = new List<(ProductVariant? Variant, int Delta, string Note)>();

        var stockBefore = product.StockQuantity;
        product.Sku = request.Sku.Trim();
        product.Name = request.Name.Trim();
        product.Slug = request.Slug.Trim();
        product.CategoryId = request.CategoryId;
        product.ProductType = productType;
        product.StockQuantity = productType == ProductType.Simple ? request.StockQuantity : 0;
        product.UpdatedAt = now;
        ApplyProductFields(product, request);

        if (product.StockQuantity != stockBefore)
        {
            pendingMovements.Add((null, product.StockQuantity - stockBefore, "Stock set from product edit"));
        }

        var kept = new HashSet<long>();
        foreach (var variantRequest in request.Variants)
        {
            if (variantRequest.Id is { } variantId)
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == variantId)
                    ?? throw new ApplicationFailure(FailureKind.Validation, "One of the options does not belong to this product.");
                var delta = variantRequest.StockQuantity - variant.StockQuantity;
                ApplyVariantFields(variant, variantRequest, now);
                if (delta != 0)
                {
                    pendingMovements.Add((variant, delta, "Stock set from product edit"));
                }
                kept.Add(variantId);
            }
            else
            {
                var variant = NewVariant(variantRequest, now);
                product.Variants.Add(variant);
                if (variant.StockQuantity != 0)
                {
                    pendingMovements.Add((variant, variant.StockQuantity, "Option added"));
                }
            }
        }

        foreach (var retired in product.Variants.Where(v => v.Id != 0 && !kept.Contains(v.Id) && v.IsActive))
        {
            retired.IsActive = false;
            retired.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var movements = pendingMovements
            .Select(m => Movement(actor, product.Id, m.Variant?.Id, m.Delta, m.Note))
            .ToList();
        dbContext.StockMovements.AddRange(movements);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "product.update", "Product", product.Id.ToString(), $"Updated {product.Sku} {product.Name}", cancellationToken);
        return await GetProductAsync(product.Id, cancellationToken);
    }

    public async Task<AdminProductDto> SetProductActiveAsync(AdminActor actor, long productId, bool isActive, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw NotFound();
        product.IsActive = isActive;
        product.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, isActive ? "product.activate" : "product.deactivate", "Product", product.Id.ToString(), product.Sku, cancellationToken);
        return await GetProductAsync(productId, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminCategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new AdminCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count(),
                Code = c.Code,
            })
            .ToListAsync(cancellationToken);
        return categories;
    }

    public async Task<AdminCategoryDto> CreateCategoryAsync(AdminActor actor, AdminCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCategoryKeyFreeAsync(request.Slug, excludeCategoryId: null, cancellationToken);
        await EnsureParentValidAsync(request.ParentCategoryId, selfId: null, cancellationToken);
        var code = await NormalizeAndValidateCodeAsync(request.Code, excludeCategoryId: null, cancellationToken);

        var now = DateTime.UtcNow;
        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim(),
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            Code = code,
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "category.create", "Category", category.Id.ToString(), category.Name, cancellationToken);
        return await GetCategoryAsync(category.Id, cancellationToken);
    }

    public async Task<AdminCategoryDto> UpdateCategoryAsync(AdminActor actor, long categoryId, AdminCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken)
            ?? throw NotFound();
        await EnsureCategoryKeyFreeAsync(request.Slug, excludeCategoryId: categoryId, cancellationToken);
        await EnsureParentValidAsync(request.ParentCategoryId, selfId: categoryId, cancellationToken);
        var code = await NormalizeAndValidateCodeAsync(request.Code, excludeCategoryId: categoryId, cancellationToken);

        category.Name = request.Name.Trim();
        category.Slug = request.Slug.Trim();
        category.Description = request.Description;
        category.ParentCategoryId = request.ParentCategoryId;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        category.Code = code;
        category.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "category.update", "Category", category.Id.ToString(), category.Name, cancellationToken);
        return await GetCategoryAsync(categoryId, cancellationToken);
    }

    private async Task<AdminCategoryDto> GetCategoryAsync(long categoryId, CancellationToken cancellationToken)
    {
        return (await ListCategoriesAsync(cancellationToken)).First(c => c.Id == categoryId);
    }

    private StockMovement Movement(AdminActor actor, long productId, long? variantId, int delta, string note) => new()
    {
        ProductId = productId,
        VariantId = variantId,
        QuantityDelta = delta,
        Reason = StockMovementReason.Adjustment,
        Note = note,
        AdminUserId = actor.Id,
        CreatedAt = DateTime.UtcNow,
    };

    private static ProductVariant NewVariant(AdminVariantRequest request, DateTime now) => new()
    {
        Sku = request.Sku.Trim(),
        VariantName = request.VariantName.Trim(),
        Price = request.Price,
        SalePrice = request.SalePrice,
        StockQuantity = request.StockQuantity,
        WeightGrams = request.WeightGrams,
        Size = request.Size?.Trim(),
        Color = request.Color?.Trim(),
        IsActive = request.IsActive,
        CreatedAt = now,
        UpdatedAt = now,
    };

    private static void ApplyVariantFields(ProductVariant variant, AdminVariantRequest request, DateTime now)
    {
        variant.Sku = request.Sku.Trim();
        variant.VariantName = request.VariantName.Trim();
        variant.Price = request.Price;
        variant.SalePrice = request.SalePrice;
        variant.StockQuantity = request.StockQuantity;
        variant.WeightGrams = request.WeightGrams;
        variant.Size = request.Size?.Trim();
        variant.Color = request.Color?.Trim();
        variant.IsActive = request.IsActive;
        variant.UpdatedAt = now;
    }

    private static void ApplyProductFields(Product product, AdminProductRequest request)
    {
        product.ShortDescription = request.ShortDescription;
        product.Description = request.Description;
        product.Price = request.Price;
        product.SalePrice = request.SalePrice;
        product.CostPrice = request.CostPrice;
        product.Currency = request.Currency.Trim().ToUpperInvariant();
        product.Material = request.Material;
        product.Color = request.Color;
        product.Occasion = request.Occasion;
        product.WeightGrams = request.WeightGrams;
        product.LowStockThreshold = request.LowStockThreshold;
        product.IsFeatured = request.IsFeatured;
        product.IsNew = request.IsNew;
        product.IsBestSeller = request.IsBestSeller;
        product.IsActive = request.IsActive;
        product.SeoTitle = request.SeoTitle;
        product.SeoDescription = request.SeoDescription;
        product.SeoKeywords = request.SeoKeywords;
        product.VideoUrl = string.IsNullOrWhiteSpace(request.VideoUrl) ? null : request.VideoUrl.Trim();
    }

    private static AdminProductDto ToDto(Product product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        Name = product.Name,
        Slug = product.Slug,
        ShortDescription = product.ShortDescription,
        Description = product.Description,
        Price = product.Price,
        SalePrice = product.SalePrice,
        CostPrice = product.CostPrice,
        Currency = product.Currency,
        CategoryId = product.CategoryId,
        ProductType = product.ProductType.ToString(),
        Material = product.Material,
        Color = product.Color,
        Occasion = product.Occasion,
        WeightGrams = product.WeightGrams,
        StockQuantity = product.StockQuantity,
        LowStockThreshold = product.LowStockThreshold,
        IsFeatured = product.IsFeatured,
        IsNew = product.IsNew,
        IsBestSeller = product.IsBestSeller,
        IsActive = product.IsActive,
        SeoTitle = product.SeoTitle,
        SeoDescription = product.SeoDescription,
        SeoKeywords = product.SeoKeywords,
        VideoUrl = product.VideoUrl,
        Variants = product.Variants.OrderBy(v => v.Id).Select(v => new AdminVariantDto
        {
            Id = v.Id,
            Sku = v.Sku,
            VariantName = v.VariantName,
            Price = v.Price,
            SalePrice = v.SalePrice,
            StockQuantity = v.StockQuantity,
            WeightGrams = v.WeightGrams,
            Size = v.Size,
            Color = v.Color,
            IsActive = v.IsActive,
            ImageUrl = v.ImageMediaFile != null ? v.ImageMediaFile.Url : null,
        }).ToList(),
        Images = product.Images.OrderBy(i => i.SortOrder).Select(i => new AdminProductImageDto
        {
            Id = i.Id,
            Url = i.MediaFile.Url,
            ImageType = i.ImageType.ToString(),
            SortOrder = i.SortOrder,
        }).ToList(),
    };

    private static ProductType ParseProductType(string value) =>
        Enum.TryParse<ProductType>(value, ignoreCase: true, out var type) && Enum.IsDefined(type)
            ? type
            : throw new ApplicationFailure(FailureKind.Validation, "Choose Simple or Variant as the product type.");

    private static void ValidateVariants(ProductType type, IReadOnlyCollection<AdminVariantRequest> variants)
    {
        if (type == ProductType.Simple && variants.Count > 0)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Simple products cannot have options. Change the type to Variant first.");
        }
        if (type == ProductType.Variant && variants.Count == 0)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Add at least one option for a variant product.");
        }
        if (variants.Select(v => v.Sku.Trim().ToUpperInvariant()).Distinct().Count() != variants.Count)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Option SKUs must be unique.");
        }
    }

    private async Task EnsureProductKeysFreeAsync(string sku, string slug, long? excludeProductId, CancellationToken cancellationToken)
    {
        var taken = await dbContext.Products.AnyAsync(
            p => (p.Sku == sku.Trim() || p.Slug == slug.Trim()) && (excludeProductId == null || p.Id != excludeProductId),
            cancellationToken);
        if (taken)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Another product already uses this SKU or slug.");
        }
    }

    private async Task EnsureVariantSkusFreeAsync(IEnumerable<string> skus, long? excludeProductId, CancellationToken cancellationToken)
    {
        var list = skus.Select(s => s.Trim()).ToList();
        if (list.Count == 0) return;
        var taken = await dbContext.ProductVariants.AnyAsync(
            v => list.Contains(v.Sku) && (excludeProductId == null || v.ProductId != excludeProductId),
            cancellationToken);
        if (taken)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "An option already uses one of these SKUs.");
        }
    }

    private async Task EnsureCategoryExistsAsync(long categoryId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Choose a valid category.");
        }
    }

    private async Task EnsureCategoryKeyFreeAsync(string slug, long? excludeCategoryId, CancellationToken cancellationToken)
    {
        var taken = await dbContext.Categories.AnyAsync(
            c => c.Slug == slug.Trim() && (excludeCategoryId == null || c.Id != excludeCategoryId),
            cancellationToken);
        if (taken)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Another category already uses this slug.");
        }
    }

    // Normalizes to uppercase, validates the shape (2-10 letters/digits, matching the SKU
    // examples like NSET/RING), and ensures no other category already uses it - since two
    // categories sharing a code would make their generated SKUs collide. Blank/null is allowed
    // (the category simply has no SKU code configured yet).
    private async Task<string?> NormalizeAndValidateCodeAsync(string? code, long? excludeCategoryId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length is < 2 or > 10 || !normalized.All(char.IsLetterOrDigit))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Category code must be 2-10 letters or digits.");
        }

        var taken = await dbContext.Categories.AnyAsync(
            c => c.Code == normalized && (excludeCategoryId == null || c.Id != excludeCategoryId),
            cancellationToken);
        if (taken)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Another category already uses this code.");
        }

        return normalized;
    }

    private async Task EnsureParentValidAsync(long? parentId, long? selfId, CancellationToken cancellationToken)
    {
        if (parentId is null) return;
        if (parentId == selfId)
        {
            throw new ApplicationFailure(FailureKind.Validation, "A category cannot be its own parent.");
        }
        if (!await dbContext.Categories.AnyAsync(c => c.Id == parentId, cancellationToken))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Choose a valid parent category.");
        }
    }

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");
}
