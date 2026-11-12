using Elegance.Domain.Content.Enums;
using Elegance.Domain.Media;

namespace Elegance.Domain.Content;

public class NoShopVideo
{
    public long Id { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }

    // The source URL as entered by the admin, kept verbatim for reference/editing even though
    // playback and thumbnails are driven entirely by YouTubeVideoId.
    public required string YouTubeUrl { get; set; }
    public required string YouTubeVideoId { get; set; }

    public string? Description { get; set; }

    public long CategoryId { get; set; }
    public NoShopCategory Category { get; set; } = null!;

    public NoShopVideoType VideoType { get; set; } = NoShopVideoType.Entertainment;

    // Falls back to YouTube's own thumbnail (img.youtube.com/vi/{id}/hqdefault.jpg) when null -
    // this only needs to be set when an admin wants a custom override.
    public long? CustomThumbnailMediaFileId { get; set; }
    public MediaFile? CustomThumbnailMediaFile { get; set; }

    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }

    public NoShopVideoStatus Status { get; set; } = NoShopVideoStatus.Draft;
    public DateTime? PublishedAt { get; set; }

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? CreatedByAdminUserId { get; set; }
    public long? UpdatedByAdminUserId { get; set; }
}
