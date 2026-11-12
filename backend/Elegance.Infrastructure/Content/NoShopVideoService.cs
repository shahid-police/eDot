using Elegance.Application.Common;
using Elegance.Application.Content;
using Elegance.Domain.Content;
using Elegance.Domain.Content.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Content;

public class NoShopVideoService(EleganceDbContext dbContext) : INoShopVideoService
{
    public async Task<PagedResult<NoShopVideoListItemDto>> GetVideosAsync(NoShopVideoQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var videos = PublishedVideos();

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            videos = videos.Where(v => v.Category.Slug == query.CategorySlug);
        }

        if (!string.IsNullOrWhiteSpace(query.VideoType) &&
            Enum.TryParse<NoShopVideoType>(query.VideoType, ignoreCase: true, out var videoType))
        {
            videos = videos.Where(v => v.VideoType == videoType);
        }

        if (query.IsFeatured.HasValue)
        {
            videos = videos.Where(v => v.IsFeatured == query.IsFeatured.Value);
        }

        var totalCount = await videos.CountAsync(cancellationToken);

        var items = await videos
            .OrderBy(v => v.DisplayOrder)
            .ThenByDescending(v => v.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new NoShopVideoListItemDto
            {
                Id = v.Id,
                Title = v.Title,
                Slug = v.Slug,
                Description = v.Description,
                YouTubeVideoId = v.YouTubeVideoId,
                // String concatenation here (not a helper method call) because EF Core cannot
                // translate arbitrary C# method calls inside a projection - only expressions it
                // recognizes, like this, make it into the generated SQL.
                ThumbnailUrl = v.CustomThumbnailMediaFile != null
                    ? v.CustomThumbnailMediaFile.Url
                    : "https://img.youtube.com/vi/" + v.YouTubeVideoId + "/hqdefault.jpg",
                CategorySlug = v.Category.Slug,
                CategoryName = v.Category.Name,
                VideoType = v.VideoType.ToString(),
                IsFeatured = v.IsFeatured,
                DisplayOrder = v.DisplayOrder,
                PublishedAt = v.PublishedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<NoShopVideoListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<IReadOnlyList<NoShopVideoListItemDto>> GetFeaturedVideosAsync(int take, CancellationToken cancellationToken = default)
    {
        return await PublishedVideos()
            .Where(v => v.IsFeatured)
            .OrderBy(v => v.DisplayOrder)
            .ThenByDescending(v => v.PublishedAt)
            .Take(Math.Clamp(take, 1, 20))
            .Select(v => new NoShopVideoListItemDto
            {
                Id = v.Id,
                Title = v.Title,
                Slug = v.Slug,
                Description = v.Description,
                YouTubeVideoId = v.YouTubeVideoId,
                ThumbnailUrl = v.CustomThumbnailMediaFile != null
                    ? v.CustomThumbnailMediaFile.Url
                    : "https://img.youtube.com/vi/" + v.YouTubeVideoId + "/hqdefault.jpg",
                CategorySlug = v.Category.Slug,
                CategoryName = v.Category.Name,
                VideoType = v.VideoType.ToString(),
                IsFeatured = v.IsFeatured,
                DisplayOrder = v.DisplayOrder,
                PublishedAt = v.PublishedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<NoShopVideoDetailDto?> GetVideoBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await PublishedVideos()
            .Where(v => v.Slug == slug)
            .Select(v => new NoShopVideoDetailDto
            {
                Id = v.Id,
                Title = v.Title,
                Slug = v.Slug,
                Description = v.Description,
                YouTubeVideoId = v.YouTubeVideoId,
                ThumbnailUrl = v.CustomThumbnailMediaFile != null
                    ? v.CustomThumbnailMediaFile.Url
                    : "https://img.youtube.com/vi/" + v.YouTubeVideoId + "/hqdefault.jpg",
                CategorySlug = v.Category.Slug,
                CategoryName = v.Category.Name,
                VideoType = v.VideoType.ToString(),
                IsFeatured = v.IsFeatured,
                PublishedAt = v.PublishedAt,
                SeoTitle = v.SeoTitle,
                SeoDescription = v.SeoDescription,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Published, and if a publish date is set it must have already passed - lets an admin
    // schedule a video ahead of time without it leaking through the public API early.
    private IQueryable<NoShopVideo> PublishedVideos()
    {
        var now = DateTime.UtcNow;
        return dbContext.NoShopVideos
            .AsNoTracking()
            .Where(v => v.Status == NoShopVideoStatus.Published && (v.PublishedAt == null || v.PublishedAt <= now));
    }
}
