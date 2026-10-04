namespace Elegance.Application.Categories;

public class CategoryDto
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public long? ParentCategoryId { get; init; }
    public int SortOrder { get; init; }
}
