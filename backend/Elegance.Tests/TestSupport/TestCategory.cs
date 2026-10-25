using Elegance.Domain.Catalog;
using Elegance.Infrastructure.Persistence;

namespace Elegance.Tests.TestSupport;

public static class TestCategory
{
    public static async Task<Category> CreateAsync(
        EleganceDbContext db,
        string name = "Necklace Sets",
        string? slug = null,
        string? code = "NSET")
    {
        var category = new Category
        {
            Name = name,
            Slug = slug ?? name.ToLowerInvariant().Replace(" ", "-"),
            Code = code,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }
}
