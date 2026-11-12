using Elegance.Domain.Catalog;

namespace Elegance.Domain.Marketing;

// Composite key (PromoCodeId, CategoryId) - a plain restriction row, no fields of its own.
public class PromoCodeCategory
{
    public long PromoCodeId { get; set; }
    public PromoCode PromoCode { get; set; } = null!;

    public long CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
