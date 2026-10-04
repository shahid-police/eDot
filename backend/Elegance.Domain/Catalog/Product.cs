using Elegance.Domain.Catalog.Enums;

namespace Elegance.Domain.Catalog;

public class Product
{
    public long Id { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? CostPrice { get; set; }
    public string Currency { get; set; } = "PKR";

    public long CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ProductType ProductType { get; set; } = ProductType.Simple;

    public string? Material { get; set; }
    public string? Color { get; set; }
    public string? Occasion { get; set; }
    public ProductGender? Gender { get; set; }
    public decimal? WeightGrams { get; set; }

    // Denormalized read-optimization for simple (non-variant) products.
    // Authoritative inventory ledger arrives in a later phase.
    public int StockQuantity { get; set; }
    public int? LowStockThreshold { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsActive { get; set; } = true;

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
}
