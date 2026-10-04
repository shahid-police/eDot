using Elegance.Domain.Catalog.Enums;
using Elegance.Domain.Media;

namespace Elegance.Domain.Catalog;

public class ProductImage
{
    public long Id { get; set; }

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public long? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public long MediaFileId { get; set; }
    public MediaFile MediaFile { get; set; } = null!;

    public int SortOrder { get; set; }
    public ProductImageType ImageType { get; set; } = ProductImageType.Gallery;

    public DateTime CreatedAt { get; set; }
}
