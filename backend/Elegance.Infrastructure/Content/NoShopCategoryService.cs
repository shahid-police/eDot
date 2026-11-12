using Elegance.Application.Content;
using Elegance.Domain.Content.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Content;

public class NoShopCategoryService(EleganceDbContext dbContext) : INoShopCategoryService
{
    public async Task<IReadOnlyList<NoShopCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await dbContext.NoShopCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new NoShopCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageMediaFile != null ? c.ImageMediaFile.Url : null,
                SortOrder = c.SortOrder,
                // Same "visible to the public" definition as NoShopVideoService.PublishedVideos -
                // a scheduled-but-not-yet-due video must not inflate this count.
                VideoCount = c.Videos.Count(v => v.Status == NoShopVideoStatus.Published && (v.PublishedAt == null || v.PublishedAt <= now)),
            })
            .ToListAsync(cancellationToken);
    }
}
