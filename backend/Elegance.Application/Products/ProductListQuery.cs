namespace Elegance.Application.Products;

public class ProductListQuery
{
    public string? CategorySlug { get; init; }
    public string? Search { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Material { get; init; }
    public string? Color { get; init; }
    public string? Occasion { get; init; }
    public string? Gender { get; init; }
    public bool? IsFeatured { get; init; }
    public bool? IsNew { get; init; }
    public bool? IsBestSeller { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 24;
}
