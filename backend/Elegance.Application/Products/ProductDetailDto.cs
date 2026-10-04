namespace Elegance.Application.Products;

public class ProductDetailDto
{
    public required long Id { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public required decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public required string Currency { get; init; }
    public required string CategorySlug { get; init; }
    public required string CategoryName { get; init; }
    public required string ProductType { get; init; }
    public string? Material { get; init; }
    public string? Color { get; init; }
    public string? Occasion { get; init; }
    public required bool InStock { get; init; }
    public required bool IsFeatured { get; init; }
    public required bool IsNew { get; init; }
    public required bool IsBestSeller { get; init; }
    public required IReadOnlyList<ProductImageDto> Images { get; init; }
    public required IReadOnlyList<ProductVariantDto> Variants { get; init; }
}
