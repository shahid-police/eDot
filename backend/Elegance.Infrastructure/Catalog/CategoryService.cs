using Elegance.Application.Categories;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Catalog;

public class CategoryService(EleganceDbContext dbContext) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageMediaFile != null ? c.ImageMediaFile.Url : null,
                ParentCategoryId = c.ParentCategoryId,
                SortOrder = c.SortOrder,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && c.Slug == slug)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageMediaFile != null ? c.ImageMediaFile.Url : null,
                ParentCategoryId = c.ParentCategoryId,
                SortOrder = c.SortOrder,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
