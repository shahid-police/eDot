namespace Elegance.Application.Products;

public class ProductImageDto
{
    public required string Url { get; init; }
    public string? AltText { get; init; }
    public required string ImageType { get; init; }
    public required int SortOrder { get; init; }
}
