using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Content;
using Elegance.Domain.Content.Enums;
using Elegance.Infrastructure.Content;
using Elegance.Infrastructure.Media;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminNoShopService(EleganceDbContext dbContext, IFileStorage fileStorage, IAuditLogService auditLog) : IAdminNoShopService
{
    public async Task<PagedResult<AdminNoShopVideoSummaryDto>> ListVideosAsync(AdminNoShopVideoQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var videos = dbContext.NoShopVideos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            videos = videos.Where(v => v.Title.Contains(search) || v.Slug.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<NoShopVideoStatus>(query.Status, ignoreCase: true, out var status))
        {
            videos = videos.Where(v => v.Status == status);
        }
        if (query.CategoryId is { } categoryId)
        {
            videos = videos.Where(v => v.CategoryId == categoryId);
        }
        if (!string.IsNullOrWhiteSpace(query.VideoType) && Enum.TryParse<NoShopVideoType>(query.VideoType, ignoreCase: true, out var videoType))
        {
            videos = videos.Where(v => v.VideoType == videoType);
        }

        var total = await videos.CountAsync(cancellationToken);
        var items = await videos
            .OrderBy(v => v.DisplayOrder)
            .ThenByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new AdminNoShopVideoSummaryDto
            {
                Id = v.Id,
                Title = v.Title,
                Slug = v.Slug,
                ThumbnailUrl = v.CustomThumbnailMediaFile != null
                    ? v.CustomThumbnailMediaFile.Url
                    : "https://img.youtube.com/vi/" + v.YouTubeVideoId + "/hqdefault.jpg",
                CategoryName = v.Category.Name,
                VideoType = v.VideoType.ToString(),
                IsFeatured = v.IsFeatured,
                Status = v.Status.ToString(),
                DisplayOrder = v.DisplayOrder,
                CreatedAt = v.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminNoShopVideoSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<AdminNoShopVideoDto> GetVideoAsync(long videoId, CancellationToken cancellationToken = default)
    {
        var video = await dbContext.NoShopVideos
            .AsNoTracking()
            .Include(v => v.Category)
            .Include(v => v.CustomThumbnailMediaFile)
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken)
            ?? throw NotFound();
        return ToDto(video);
    }

    public async Task<AdminNoShopVideoDto> CreateVideoAsync(AdminActor actor, AdminNoShopVideoRequest request, CancellationToken cancellationToken = default)
    {
        var videoType = ParseVideoType(request.VideoType);
        var status = ParseStatus(request.Status);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        await EnsureSlugFreeAsync(request.Slug, excludeVideoId: null, cancellationToken);
        var youTubeVideoId = YouTubeUrlParser.ExtractVideoId(request.YouTubeUrl);

        var now = DateTime.UtcNow;
        var video = new NoShopVideo
        {
            Title = request.Title.Trim(),
            Slug = request.Slug.Trim(),
            Description = request.Description,
            YouTubeUrl = request.YouTubeUrl.Trim(),
            YouTubeVideoId = youTubeVideoId,
            CategoryId = request.CategoryId,
            VideoType = videoType,
            IsFeatured = request.IsFeatured,
            DisplayOrder = request.DisplayOrder,
            Status = status,
            PublishedAt = ResolvePublishedAt(status, request.PublishedAt, existingPublishedAt: null),
            SeoTitle = request.SeoTitle,
            SeoDescription = request.SeoDescription,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByAdminUserId = actor.Id,
            UpdatedByAdminUserId = actor.Id,
        };
        dbContext.NoShopVideos.Add(video);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "noshop.video.create", "NoShopVideo", video.Id.ToString(), video.Title, cancellationToken);
        return await GetVideoAsync(video.Id, cancellationToken);
    }

    public async Task<AdminNoShopVideoDto> UpdateVideoAsync(AdminActor actor, long videoId, AdminNoShopVideoRequest request, CancellationToken cancellationToken = default)
    {
        var video = await dbContext.NoShopVideos.FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken)
            ?? throw NotFound();

        var videoType = ParseVideoType(request.VideoType);
        var status = ParseStatus(request.Status);
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        await EnsureSlugFreeAsync(request.Slug, excludeVideoId: videoId, cancellationToken);
        var youTubeVideoId = YouTubeUrlParser.ExtractVideoId(request.YouTubeUrl);

        video.Title = request.Title.Trim();
        video.Slug = request.Slug.Trim();
        video.Description = request.Description;
        video.YouTubeUrl = request.YouTubeUrl.Trim();
        video.YouTubeVideoId = youTubeVideoId;
        video.CategoryId = request.CategoryId;
        video.VideoType = videoType;
        video.IsFeatured = request.IsFeatured;
        video.DisplayOrder = request.DisplayOrder;
        video.PublishedAt = ResolvePublishedAt(status, request.PublishedAt, video.PublishedAt);
        video.Status = status;
        video.SeoTitle = request.SeoTitle;
        video.SeoDescription = request.SeoDescription;
        video.UpdatedAt = DateTime.UtcNow;
        video.UpdatedByAdminUserId = actor.Id;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(actor, "noshop.video.update", "NoShopVideo", video.Id.ToString(), video.Title, cancellationToken);
        return await GetVideoAsync(video.Id, cancellationToken);
    }

    public async Task<AdminNoShopVideoDto> SetVideoStatusAsync(AdminActor actor, long videoId, string status, CancellationToken cancellationToken = default)
    {
        var video = await dbContext.NoShopVideos.FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken)
            ?? throw NotFound();

        var parsed = ParseStatus(status);
        video.PublishedAt = ResolvePublishedAt(parsed, requestedPublishedAt: null, existingPublishedAt: video.PublishedAt);
        video.Status = parsed;
        video.UpdatedAt = DateTime.UtcNow;
        video.UpdatedByAdminUserId = actor.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "noshop.video.status", "NoShopVideo", video.Id.ToString(), $"Status -> {parsed}", cancellationToken);
        return await GetVideoAsync(video.Id, cancellationToken);
    }

    public async Task DeleteVideoAsync(AdminActor actor, long videoId, CancellationToken cancellationToken = default)
    {
        var video = await dbContext.NoShopVideos
            .Include(v => v.CustomThumbnailMediaFile)
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken)
            ?? throw NotFound();

        var thumbnail = video.CustomThumbnailMediaFile;
        var title = video.Title;

        dbContext.NoShopVideos.Remove(video);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (thumbnail is not null)
        {
            dbContext.MediaFiles.Remove(thumbnail);
            await dbContext.SaveChangesAsync(cancellationToken);
            await fileStorage.DeleteAsync(thumbnail.StorageKey, cancellationToken);
        }

        await auditLog.RecordAsync(actor, "noshop.video.delete", "NoShopVideo", videoId.ToString(), title, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminNoShopCategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.NoShopCategories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new AdminNoShopCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageMediaFile != null ? c.ImageMediaFile.Url : null,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                // Every video regardless of status - this count is for the admin's own
                // visibility, unlike the public category listing's published-only count.
                VideoCount = c.Videos.Count(),
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminNoShopCategoryDto> CreateCategoryAsync(AdminActor actor, AdminNoShopCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCategorySlugFreeAsync(request.Slug, excludeCategoryId: null, cancellationToken);

        var now = DateTime.UtcNow;
        var category = new NoShopCategory
        {
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim(),
            Description = request.Description,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.NoShopCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "noshop.category.create", "NoShopCategory", category.Id.ToString(), category.Name, cancellationToken);
        return await GetCategoryAsync(category.Id, cancellationToken);
    }

    public async Task<AdminNoShopCategoryDto> UpdateCategoryAsync(AdminActor actor, long categoryId, AdminNoShopCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.NoShopCategories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken)
            ?? throw NotFound();
        await EnsureCategorySlugFreeAsync(request.Slug, excludeCategoryId: categoryId, cancellationToken);

        category.Name = request.Name.Trim();
        category.Slug = request.Slug.Trim();
        category.Description = request.Description;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "noshop.category.update", "NoShopCategory", category.Id.ToString(), category.Name, cancellationToken);
        return await GetCategoryAsync(categoryId, cancellationToken);
    }

    private async Task<AdminNoShopCategoryDto> GetCategoryAsync(long categoryId, CancellationToken cancellationToken) =>
        (await ListCategoriesAsync(cancellationToken)).First(c => c.Id == categoryId);

    // Published keeps (or starts) a real publish timestamp - an explicit request wins (lets an
    // admin schedule ahead), otherwise an already-recorded date is reused (so Unpublished ->
    // Published again doesn't look like a brand-new publish), and only defaults to "now" the
    // first time a video is ever published. Any non-Published status keeps whatever date was
    // already on record (or none) rather than inventing or erasing one.
    private static DateTime? ResolvePublishedAt(NoShopVideoStatus status, DateTime? requestedPublishedAt, DateTime? existingPublishedAt) =>
        status == NoShopVideoStatus.Published
            ? requestedPublishedAt ?? existingPublishedAt ?? DateTime.UtcNow
            : requestedPublishedAt ?? existingPublishedAt;

    private static NoShopVideoType ParseVideoType(string value) =>
        Enum.TryParse<NoShopVideoType>(value, ignoreCase: true, out var type) && Enum.IsDefined(type)
            ? type
            : throw new ApplicationFailure(FailureKind.Validation, "Choose Entertainment, Promotion, BrandStory, or Advertisement.");

    private static NoShopVideoStatus ParseStatus(string value) =>
        Enum.TryParse<NoShopVideoStatus>(value, ignoreCase: true, out var status) && Enum.IsDefined(status)
            ? status
            : throw new ApplicationFailure(FailureKind.Validation, "Choose Draft, Published, or Unpublished.");

    private async Task EnsureCategoryExistsAsync(long categoryId, CancellationToken cancellationToken)
    {
        if (!await dbContext.NoShopCategories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Choose a valid category.");
        }
    }

    private async Task EnsureSlugFreeAsync(string slug, long? excludeVideoId, CancellationToken cancellationToken)
    {
        var taken = await dbContext.NoShopVideos.AnyAsync(
            v => v.Slug == slug.Trim() && (excludeVideoId == null || v.Id != excludeVideoId),
            cancellationToken);
        if (taken)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Another video already uses this slug.");
        }
    }

    private async Task EnsureCategorySlugFreeAsync(string slug, long? excludeCategoryId, CancellationToken cancellationToken)
    {
        var taken = await dbContext.NoShopCategories.AnyAsync(
            c => c.Slug == slug.Trim() && (excludeCategoryId == null || c.Id != excludeCategoryId),
            cancellationToken);
        if (taken)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Another category already uses this slug.");
        }
    }

    private static AdminNoShopVideoDto ToDto(NoShopVideo v) => new()
    {
        Id = v.Id,
        Title = v.Title,
        Slug = v.Slug,
        Description = v.Description,
        YouTubeUrl = v.YouTubeUrl,
        YouTubeVideoId = v.YouTubeVideoId,
        ThumbnailUrl = v.CustomThumbnailMediaFile != null ? v.CustomThumbnailMediaFile.Url : YouTubeThumbnails.DefaultUrl(v.YouTubeVideoId),
        CategoryId = v.CategoryId,
        CategoryName = v.Category.Name,
        VideoType = v.VideoType.ToString(),
        IsFeatured = v.IsFeatured,
        DisplayOrder = v.DisplayOrder,
        Status = v.Status.ToString(),
        PublishedAt = v.PublishedAt,
        SeoTitle = v.SeoTitle,
        SeoDescription = v.SeoDescription,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt,
    };

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");
}
