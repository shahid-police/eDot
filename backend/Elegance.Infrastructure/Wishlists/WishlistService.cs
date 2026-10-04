using Elegance.Application.Common;
using Elegance.Application.Wishlists;
using Elegance.Domain.Catalog;
using Elegance.Domain.Catalog.Enums;
using Elegance.Domain.Wishlists;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Wishlists;

public class WishlistService(EleganceDbContext dbContext) : IWishlistService
{
    public async Task<WishlistDto> GetWishlistAsync(ListOwner owner, CancellationToken cancellationToken = default)
    {
        var wishlist = await FindWishlistAsync(owner, cancellationToken);
        return wishlist is null ? new WishlistDto { Items = [], Count = 0 } : await BuildDtoAsync(wishlist.Id, cancellationToken);
    }

    public async Task<WishlistDto> AddItemAsync(ListOwner owner, AddWishlistItemRequest request, CancellationToken cancellationToken = default)
    {
        var (product, variant) = await ResolveSavableAsync(request.ProductId, request.VariantId, cancellationToken);

        var wishlist = await FindWishlistAsync(owner, cancellationToken)
            ?? await CreateWishlistAsync(owner, cancellationToken);

        var variantId = variant?.Id;
        var alreadySaved = await dbContext.WishlistItems.AnyAsync(
            i => i.WishlistId == wishlist.Id && i.ProductId == product.Id && i.VariantId == variantId,
            cancellationToken);

        if (!alreadySaved)
        {
            dbContext.WishlistItems.Add(new WishlistItem
            {
                WishlistId = wishlist.Id,
                ProductId = product.Id,
                VariantId = variantId,
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

    public async Task<WishlistDto> RemoveItemAsync(ListOwner owner, long itemId, CancellationToken cancellationToken = default)
    {
        var wishlist = await FindWishlistAsync(owner, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested wishlist item was not found.");

        var item = await dbContext.WishlistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WishlistId == wishlist.Id, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested wishlist item was not found.");

        dbContext.WishlistItems.Remove(item);
        wishlist.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(wishlist.Id, cancellationToken);
    }

    public async Task MergeGuestWishlistAsync(long customerId, string guestToken, CancellationToken cancellationToken = default)
    {
        var guestWishlist = await FindWishlistAsync(new ListOwner(null, guestToken), cancellationToken);
        if (guestWishlist is null)
        {
            return;
        }

        var customerWishlist = await FindWishlistAsync(new ListOwner(customerId, null), cancellationToken)
            ?? await CreateWishlistAsync(new ListOwner(customerId, null), cancellationToken);

        var guestItems = await dbContext.WishlistItems
            .Where(i => i.WishlistId == guestWishlist.Id)
            .ToListAsync(cancellationToken);

        foreach (var guestItem in guestItems)
        {
            var alreadySaved = await dbContext.WishlistItems.AnyAsync(
                i => i.WishlistId == customerWishlist.Id && i.ProductId == guestItem.ProductId && i.VariantId == guestItem.VariantId,
                cancellationToken);

            if (alreadySaved)
            {
                dbContext.WishlistItems.Remove(guestItem);
            }
            else
            {
                guestItem.WishlistId = customerWishlist.Id;
            }
        }

        dbContext.Wishlists.Remove(guestWishlist);
        customerWishlist.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<(Product Product, ProductVariant? Variant)> ResolveSavableAsync(
        long productId, long? variantId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "This product is not available.");

        if (product.ProductType == ProductType.Simple)
        {
            if (variantId is not null)
            {
                throw new ApplicationFailure(FailureKind.Validation, "This product does not have variants.");
            }
            return (product, null);
        }

        if (variantId is null)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Please choose an option before saving this product.");
        }

        var variant = await dbContext.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == product.Id && v.IsActive, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The selected option is not available.");

        return (product, variant);
    }

    private Task<Wishlist?> FindWishlistAsync(ListOwner owner, CancellationToken cancellationToken)
    {
        if (owner.CustomerId is { } customerId)
        {
            return dbContext.Wishlists.FirstOrDefaultAsync(w => w.CustomerId == customerId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(owner.GuestToken))
        {
            var hash = TokenService.HashToken(owner.GuestToken);
            return dbContext.Wishlists.FirstOrDefaultAsync(w => w.GuestTokenHash == hash, cancellationToken);
        }

        return Task.FromResult<Wishlist?>(null);
    }

    private async Task<Wishlist> CreateWishlistAsync(ListOwner owner, CancellationToken cancellationToken)
    {
        if (owner.CustomerId is null && string.IsNullOrEmpty(owner.GuestToken))
        {
            throw new InvalidOperationException("A wishlist requires either a customer or a guest token.");
        }

        var now = DateTime.UtcNow;
        var wishlist = new Wishlist
        {
            CustomerId = owner.CustomerId,
            GuestTokenHash = owner.GuestToken is null ? null : TokenService.HashToken(owner.GuestToken),
            CreatedAt = now,
            UpdatedAt = now,
        };
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
            .Include(i => i.Variant).ThenInclude(v => v!.ImageMediaFile)
            .ToListAsync(cancellationToken);

        var lines = items.Select(ToItem).ToList();
        return new WishlistDto { Items = lines, Count = lines.Count };
    }

    private static WishlistItemDto ToItem(WishlistItem item)
    {
        var product = item.Product;
        var variant = item.Variant;
        var primaryImage = product.Images
            .Where(img => img.ImageType == ProductImageType.Primary)
            .OrderBy(img => img.SortOrder)
            .FirstOrDefault();

        var available = variant is null
            ? product.StockQuantity
            : variant.StockQuantity;

        return new WishlistItemDto
        {
            Id = item.Id,
            ProductId = product.Id,
            VariantId = variant?.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = variant?.Sku ?? product.Sku,
            VariantName = variant?.VariantName,
            ImageUrl = variant?.ImageMediaFile?.Url ?? primaryImage?.MediaFile.Url,
            Price = variant?.Price ?? product.Price,
            SalePrice = variant is null ? product.SalePrice : variant.SalePrice,
            Currency = product.Currency,
            InStock = product.IsActive && (variant is null || variant.IsActive) && available > 0,
            AddedAt = item.AddedAt,
        };
    }
}
