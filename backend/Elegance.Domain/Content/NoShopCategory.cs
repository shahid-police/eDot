using Elegance.Domain.Media;

namespace Elegance.Domain.Content;

public class NoShopCategory
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }

    public long? ImageMediaFileId { get; set; }
    public MediaFile? ImageMediaFile { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<NoShopVideo> Videos { get; set; } = [];
}
