namespace Elegance.Application.Content;

public class NoShopVideoQuery
{
    public string? CategorySlug { get; init; }
    public string? VideoType { get; init; }
    public bool? IsFeatured { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 24;
}
