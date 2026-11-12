namespace Elegance.Application.Content;

public class NoShopVideoListItemDto
{
    public required long Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public required string YouTubeVideoId { get; init; }
    public required string ThumbnailUrl { get; init; }
    public required string CategorySlug { get; init; }
    public required string CategoryName { get; init; }
    public required string VideoType { get; init; }
    public bool IsFeatured { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime? PublishedAt { get; init; }
}
