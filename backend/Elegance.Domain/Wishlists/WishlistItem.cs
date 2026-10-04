using Elegance.Domain.Catalog;

namespace Elegance.Domain.Wishlists;

public class WishlistItem
{
    public long Id { get; set; }

    public long WishlistId { get; set; }
    public Wishlist Wishlist { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public long? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public DateTime AddedAt { get; set; }
}
