namespace Elegance.Application.Products;

public class ProductVariantDto
{
    public required long Id { get; init; }
    public required string Sku { get; init; }
    public required string VariantName { get; init; }
    public required decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public required bool InStock { get; init; }
    public string? Size { get; init; }
    public string? Color { get; init; }
    public string? ImageUrl { get; init; }
}
