using Elegance.Application.Common;
using Elegance.Application.Wishlists;
using Elegance.Domain.Catalog;
using Elegance.Domain.Catalog.Enums;
using Elegance.Domain.Wishlists;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Wishlists;

public class WishlistService(EleganceDbContext dbContext) : IWishlistService
{
    public async Task<WishlistDto> GetWishlistAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var wishlist = await FindWishlistAsync(customerId, cancellationToken);
        return wishlist is null ? Empty() : await BuildDtoAsync(wishlist.Id, cancellationToken);
    }

    public async Task<WishlistDto> AddItemAsync(long customerId, long productId, CancellationToken cancellationToken = default)
    {
        var productExists = await dbContext.Products
            .AnyAsync(p => p.Id == productId && p.IsActive, cancellationToken);
        if (!productExists)
        {
            throw new ApplicationFailure(FailureKind.NotFound, "This product is not available.");
        }

        var wishlist = await FindWishlistAsync(customerId, cancellationToken)
            ?? await CreateWishlistAsync(customerId, cancellationToken);

        var alreadySaved = await dbContext.WishlistItems
            .AnyAsync(i => i.WishlistId == wishlist.Id && i.ProductId == productId, cancellationToken);
        if (!alreadySaved)
        {
            dbContext.WishlistItems.Add(new WishlistItem
            {
                WishlistId = wishlist.Id,
                ProductId = productId,
                AddedAt = DateTime.UtcNow,
            });
            wishlist.UpdatedAt = DateTime.UtcNow;
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        return await BuildDtoAsync(wishlist.Id, cancellationToken);
    }

    public async Task<WishlistDto> RemoveItemAsync(long customerId, long itemId, CancellationToken cancellationToken = default)
    {
        var wishlist = await FindWishlistAsync(customerId, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested wishlist item was not found.");

        var item = await dbContext.WishlistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WishlistId == wishlist.Id, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested wishlist item was not found.");

        dbContext.WishlistItems.Remove(item);
        wishlist.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(wishlist.Id, cancellationToken);
    }

    private Task<Wishlist?> FindWishlistAsync(long customerId, CancellationToken cancellationToken) =>
        dbContext.Wishlists.FirstOrDefaultAsync(w => w.CustomerId == customerId, cancellationToken);

    private async Task<Wishlist> CreateWishlistAsync(long customerId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var wishlist = new Wishlist { CustomerId = customerId, CreatedAt = now, UpdatedAt = now };
        dbContext.Wishlists.Add(wishlist);
        await dbContext.SaveChangesAsync(cancellationToken);
        return wishlist;
    }

    private async Task<WishlistDto> BuildDtoAsync(long wishlistId, CancellationToken cancellationToken)
    {
        var items = await dbContext.WishlistItems
            .AsNoTracking()
            .Where(i => i.WishlistId == wishlistId)
            .OrderBy(i => i.AddedAt)
            .ThenBy(i => i.Id)
            .Include(i => i.Product).ThenInclude(p => p.Images).ThenInclude(img => img.MediaFile)
            .Include(i => i.Product).ThenInclude(p => p.Variants)
            .ToListAsync(cancellationToken);

        var lines = items.Select(ToItem).ToList();
        return new WishlistDto { Items = lines, Count = lines.Count };
    }

    private static WishlistItemDto ToItem(WishlistItem item)
    {
        var product = item.Product;
        var primaryImage = product.Images
            .Where(img => img.ImageType == ProductImageType.Primary)
            .OrderBy(img => img.SortOrder)
            .FirstOrDefault();

        return new WishlistItemDto
        {
            Id = item.Id,
            ProductId = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = product.Sku,
            ImageUrl = primaryImage?.MediaFile.Url,
            Price = product.Price,
            SalePrice = product.SalePrice,
            Currency = product.Currency,
            InStock = InStock(product),
            AddedAt = item.AddedAt,
        };
    }

    private static bool InStock(Product product) =>
        product.IsActive && (product.ProductType == ProductType.Simple
            ? product.StockQuantity > 0
            : product.Variants.Any(v => v.IsActive && v.StockQuantity > 0));

    private static WishlistDto Empty() => new() { Items = [], Count = 0 };
}
