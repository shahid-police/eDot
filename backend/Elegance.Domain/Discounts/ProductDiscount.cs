using Elegance.Domain.Catalog;
using Elegance.Domain.Discounts.Enums;

namespace Elegance.Domain.Discounts;

// Create + Cancel only - a discount is never edited in place once created, so its percentage,
// snapshot prices and dates stay a trustworthy historical record. Replacing one means cancelling
// this row and creating a new one (see AdminProductDiscountService.CreateProductDiscountAsync,
// which does exactly that for the previous Active row on the same product/variant).
public class ProductDiscount
{
    public long Id { get; set; }

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Null means the discount applies to the simple product itself; set means it applies to one
    // specific variant only, using that variant's own price rather than the product's.
    public long? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public decimal OriginalPriceSnapshot { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalPriceSnapshot { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public ProductDiscountStatus Status { get; set; } = ProductDiscountStatus.Active;

    public long CreatedByAdminUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
