namespace Elegance.Application.Content;

public class NoShopCategoryDto
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public int SortOrder { get; init; }
    public int VideoCount { get; init; }
}
