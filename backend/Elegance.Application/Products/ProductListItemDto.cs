namespace Elegance.Application.Products;

public class ProductListItemDto
{
    public required long Id { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public required string Currency { get; init; }
    public string? ImageUrl { get; init; }
    public string? HoverImageUrl { get; init; }
    public required string CategorySlug { get; init; }
    public required bool InStock { get; init; }
    public required bool IsFeatured { get; init; }
    public required bool IsNew { get; init; }
    public required bool IsBestSeller { get; init; }
}
