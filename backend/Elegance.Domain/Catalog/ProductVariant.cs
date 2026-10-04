using Elegance.Domain.Media;

namespace Elegance.Domain.Catalog;

public class ProductVariant
{
    public long Id { get; set; }

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public required string Sku { get; set; }
    public required string VariantName { get; set; }

    public decimal? Price { get; set; }
    public decimal? SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public decimal? WeightGrams { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }

    public long? ImageMediaFileId { get; set; }
    public MediaFile? ImageMediaFile { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
