using Elegance.Domain.Media;

namespace Elegance.Domain.Catalog;

public class Category
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }

    public long? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> ChildCategories { get; set; } = [];

    public long? ImageMediaFileId { get; set; }
    public MediaFile? ImageMediaFile { get; set; }

    public int SortOrder { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
