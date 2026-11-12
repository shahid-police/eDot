using Elegance.Domain.Catalog;

namespace Elegance.Domain.Marketing;

// Composite key (PromoCodeId, ProductId) - a plain restriction row, no fields of its own.
public class PromoCodeProduct
{
    public long PromoCodeId { get; set; }
    public PromoCode PromoCode { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
