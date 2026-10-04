using Elegance.Domain.Catalog;

namespace Elegance.Domain.Carts;

public class CartItem
{
    public long Id { get; set; }

    public long CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public long? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public int Quantity { get; set; }

    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
