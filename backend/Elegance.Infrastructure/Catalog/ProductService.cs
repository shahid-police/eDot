using Elegance.Application.Common;
using Elegance.Application.Products;
using Elegance.Domain.Catalog.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Catalog;

public class ProductService(EleganceDbContext dbContext) : IProductService
{
    public async Task<PagedResult<ProductListItemDto>> GetProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var products = dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            // Matches the category itself plus its direct children, so filtering by a parent
            // (e.g. "Bangles & Bracelets") also returns products filed under its sub-categories
            // (e.g. "Kada", "Bracelets") rather than only products assigned to the parent directly.
            products = products.Where(p =>
                p.Category.Slug == query.CategorySlug ||
                p.Category.ParentCategory!.Slug == query.CategorySlug);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Split into individual terms and OR them, rather than one exact-phrase Contains,
            // so a multi-word natural-language query ("elegant wedding earrings") still matches
            // a product whose name/description only contains some of those words.
            var searchTerms = query.Search
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(10)
                .ToArray();

            if (searchTerms.Length > 0)
            {
                products = products.Where(p => searchTerms.Any(term =>
                    p.Name.Contains(term) ||
                    p.Sku.Contains(term) ||
                    (p.ShortDescription != null && p.ShortDescription.Contains(term))));
            }
        }

        if (query.MinPrice.HasValue)
        {
            products = products.Where(p => (p.SalePrice ?? p.Price) >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            products = products.Where(p => (p.SalePrice ?? p.Price) <= query.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Material))
        {
            products = products.Where(p => p.Material != null && p.Material.Contains(query.Material));
        }

        if (!string.IsNullOrWhiteSpace(query.Color))
        {
            products = products.Where(p => p.Color != null && p.Color.Contains(query.Color));
        }

        if (!string.IsNullOrWhiteSpace(query.Occasion))
        {
            products = products.Where(p => p.Occasion != null && p.Occasion.Contains(query.Occasion));
        }

        if (!string.IsNullOrWhiteSpace(query.Gender) && Enum.TryParse<ProductGender>(query.Gender, true, out var gender))
        {
            products = products.Where(p => p.Gender == gender);
        }

        if (query.IsFeatured.HasValue)
        {
            products = products.Where(p => p.IsFeatured == query.IsFeatured.Value);
        }

        if (query.IsNew.HasValue)
        {
            products = products.Where(p => p.IsNew == query.IsNew.Value);
        }

        if (query.IsBestSeller.HasValue)
        {
            products = products.Where(p => p.IsBestSeller == query.IsBestSeller.Value);
        }

        var totalCount = await products.CountAsync(cancellationToken);

        var items = await products
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Slug = p.Slug,
                Price = p.Price,
                SalePrice = p.SalePrice,
                Currency = p.Currency,
                ImageUrl = p.Images
                    .Where(i => i.ImageType == ProductImageType.Primary)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.MediaFile.Url)
                    .FirstOrDefault(),
                HoverImageUrl = p.Images
                    .Where(i => i.ImageType == ProductImageType.Hover)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.MediaFile.Url)
                    .FirstOrDefault(),
                CategorySlug = p.Category.Slug,
                Material = p.Material,
                Color = p.Color,
                InStock = p.ProductType == ProductType.Simple
                    ? p.StockQuantity > 0
                    : p.Variants.Any(v => v.IsActive && v.StockQuantity > 0),
                IsFeatured = p.IsFeatured,
                IsNew = p.IsNew,
                IsBestSeller = p.IsBestSeller,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Slug == slug)
            .Select(p => new ProductDetailDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Slug = p.Slug,
                ShortDescription = p.ShortDescription,
                Description = p.Description,
                Price = p.Price,
                SalePrice = p.SalePrice,
                Currency = p.Currency,
                CategorySlug = p.Category.Slug,
                CategoryName = p.Category.Name,
                ProductType = p.ProductType.ToString(),
                Material = p.Material,
                Color = p.Color,
                Occasion = p.Occasion,
                InStock = p.ProductType == ProductType.Simple
                    ? p.StockQuantity > 0
                    : p.Variants.Any(v => v.IsActive && v.StockQuantity > 0),
                IsFeatured = p.IsFeatured,
                IsNew = p.IsNew,
                IsBestSeller = p.IsBestSeller,
                Images = p.Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new ProductImageDto
                    {
                        Url = i.MediaFile.Url,
                        AltText = i.MediaFile.AltText,
                        ImageType = i.ImageType.ToString(),
                        SortOrder = i.SortOrder,
                    })
                    .ToList(),
                Variants = p.Variants
                    .Where(v => v.IsActive)
                    .Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        Sku = v.Sku,
                        VariantName = v.VariantName,
                        Price = v.Price ?? p.Price,
                        SalePrice = v.SalePrice,
                        InStock = v.StockQuantity > 0,
                        Size = v.Size,
                        Color = v.Color,
                        ImageUrl = v.ImageMediaFile != null ? v.ImageMediaFile.Url : null,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
