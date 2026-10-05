using Elegance.Application.Carts;
using Elegance.Application.Common;
using Elegance.Domain.Carts;
using Elegance.Domain.Catalog;
using Elegance.Domain.Catalog.Enums;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Catalog;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Carts;

public class CartService(EleganceDbContext dbContext) : ICartService
{
    private const string DefaultCurrency = "PKR";
    private const int MaxQuantityPerLine = 99;

    public async Task<CartDto> GetCartAsync(CartOwner owner, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(owner, cancellationToken);
        return cart is null ? CartDto.Empty(DefaultCurrency) : await BuildDtoAsync(cart.Id, cancellationToken);
    }

    public async Task<CartDto> AddItemAsync(CartOwner owner, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var (product, variant) = await ResolvePurchasableAsync(request.ProductId, request.VariantId, cancellationToken);
        var available = AvailableStock(product, variant);

        var cart = await FindCartAsync(owner, cancellationToken)
            ?? await CreateCartAsync(owner, cancellationToken);

        var existing = await FindLineAsync(cart.Id, product.Id, variant?.Id, cancellationToken);
        var newQuantity = (existing?.Quantity ?? 0) + request.Quantity;
        EnsureQuantityAllowed(newQuantity, available, product.Name);

        if (existing is null)
        {
            var now = DateTime.UtcNow;
            dbContext.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                VariantId = variant?.Id,
                Quantity = newQuantity,
                AddedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            existing.Quantity = newQuantity;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(cart.Id, cancellationToken);
    }

    public async Task<CartDto> UpdateItemAsync(CartOwner owner, long itemId, int quantity, CancellationToken cancellationToken = default)
    {
        var item = await FindOwnedItemAsync(owner, itemId, cancellationToken);

        var product = await dbContext.Products.AsNoTracking().FirstAsync(p => p.Id == item.ProductId, cancellationToken);
        var variant = item.VariantId is null
            ? null
            : await dbContext.ProductVariants.AsNoTracking().FirstAsync(v => v.Id == item.VariantId, cancellationToken);

        EnsureQuantityAllowed(quantity, AvailableStock(product, variant), product.Name);

        item.Quantity = quantity;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(item.CartId, cancellationToken);
    }

    public async Task<CartDto> RemoveItemAsync(CartOwner owner, long itemId, CancellationToken cancellationToken = default)
    {
        var item = await FindOwnedItemAsync(owner, itemId, cancellationToken);
        var cartId = item.CartId;

        dbContext.CartItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(cartId, cancellationToken);
    }

    public async Task MergeGuestCartAsync(long customerId, string guestToken, CancellationToken cancellationToken = default)
    {
        var guestCart = await FindCartAsync(new CartOwner(null, guestToken), cancellationToken);
        if (guestCart is null)
        {
            return;
        }

        var customerCart = await FindCartAsync(new CartOwner(customerId, null), cancellationToken)
            ?? await CreateCartAsync(new CartOwner(customerId, null), cancellationToken);

        var guestItems = await dbContext.CartItems
            .Where(i => i.CartId == guestCart.Id)
            .ToListAsync(cancellationToken);

        foreach (var guestItem in guestItems)
        {
            var existing = await FindLineAsync(customerCart.Id, guestItem.ProductId, guestItem.VariantId, cancellationToken);
            if (existing is null)
            {
                guestItem.CartId = customerCart.Id;
                guestItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existing.Quantity = Math.Min(existing.Quantity + guestItem.Quantity, MaxQuantityPerLine);
                existing.UpdatedAt = DateTime.UtcNow;
                dbContext.CartItems.Remove(guestItem);
            }
        }

        dbContext.Carts.Remove(guestCart);
        customerCart.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<(Product Product, ProductVariant? Variant)> ResolvePurchasableAsync(long productId, long? variantId, CancellationToken cancellationToken)
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
            throw new ApplicationFailure(FailureKind.Validation, "Please choose an option before adding this product.");
        }

        var variant = await dbContext.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == product.Id && v.IsActive, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The selected option is not available.");

        return (product, variant);
    }

    private static int AvailableStock(Product product, ProductVariant? variant) =>
        product.IsActive && (variant is null || variant.IsActive)
            ? variant?.StockQuantity ?? product.StockQuantity
            : 0;

    private static void EnsureQuantityAllowed(int quantity, int available, string productName)
    {
        if (available <= 0)
        {
            throw new ApplicationFailure(FailureKind.Conflict, $"{productName} is currently out of stock.");
        }
        if (quantity > available)
        {
            throw new ApplicationFailure(FailureKind.Conflict, $"Only {available} of {productName} available.");
        }
        if (quantity > MaxQuantityPerLine)
        {
            throw new ApplicationFailure(FailureKind.Validation, $"You can order up to {MaxQuantityPerLine} of each item.");
        }
    }

    private async Task<CartItem> FindOwnedItemAsync(CartOwner owner, long itemId, CancellationToken cancellationToken)
    {
        var cart = await FindCartAsync(owner, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested cart item was not found.");

        return await dbContext.CartItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cart.Id, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested cart item was not found.");
    }

    private Task<CartItem?> FindLineAsync(long cartId, long productId, long? variantId, CancellationToken cancellationToken) =>
        dbContext.CartItems.FirstOrDefaultAsync(
            i => i.CartId == cartId && i.ProductId == productId && i.VariantId == variantId,
            cancellationToken);

    private Task<Cart?> FindCartAsync(CartOwner owner, CancellationToken cancellationToken)
    {
        if (owner.CustomerId is { } customerId)
        {
            return dbContext.Carts.FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(owner.GuestToken))
        {
            var hash = TokenService.HashToken(owner.GuestToken);
            return dbContext.Carts.FirstOrDefaultAsync(c => c.GuestTokenHash == hash, cancellationToken);
        }

        return Task.FromResult<Cart?>(null);
    }

    private async Task<Cart> CreateCartAsync(CartOwner owner, CancellationToken cancellationToken)
    {
        if (owner.CustomerId is null && string.IsNullOrEmpty(owner.GuestToken))
        {
            throw new InvalidOperationException("A cart requires either a customer or a guest token.");
        }

        var now = DateTime.UtcNow;
        var cart = new Cart
        {
            CustomerId = owner.CustomerId,
            GuestTokenHash = owner.GuestToken is null ? null : TokenService.HashToken(owner.GuestToken),
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private async Task<CartDto> BuildDtoAsync(long cartId, CancellationToken cancellationToken)
    {
        var items = await dbContext.CartItems
            .AsNoTracking()
            .Where(i => i.CartId == cartId)
            .OrderBy(i => i.AddedAt)
            .ThenBy(i => i.Id)
            .Include(i => i.Product).ThenInclude(p => p.Images).ThenInclude(img => img.MediaFile)
            .Include(i => i.Variant).ThenInclude(v => v!.ImageMediaFile)
            .ToListAsync(cancellationToken);

        var lines = items.Select(ToLine).ToList();

        return new CartDto
        {
            Items = lines,
            ItemCount = lines.Sum(l => l.Quantity),
            Subtotal = lines.Sum(l => l.LineTotal),
            Currency = items.FirstOrDefault()?.Product.Currency ?? DefaultCurrency,
        };
    }

    private static CartItemDto ToLine(CartItem item)
    {
        var product = item.Product;
        var variant = item.Variant;

        var unitPrice = PricingRules.UnitPrice(product, variant);

        var available = AvailableStock(product, variant);
        var primaryImage = product.Images
            .Where(img => img.ImageType == ProductImageType.Primary)
            .OrderBy(img => img.SortOrder)
            .FirstOrDefault();

        return new CartItemDto
        {
            Id = item.Id,
            ProductId = product.Id,
            VariantId = variant?.Id,
            ProductName = product.Name,
            ProductSlug = product.Slug,
            Sku = variant?.Sku ?? product.Sku,
            VariantName = variant?.VariantName,
            ImageUrl = variant?.ImageMediaFile?.Url ?? primaryImage?.MediaFile.Url,
            UnitPrice = unitPrice,
            LineTotal = unitPrice * item.Quantity,
            Quantity = item.Quantity,
            AvailableQuantity = available,
            IsAvailable = available >= item.Quantity,
        };
    }
}
