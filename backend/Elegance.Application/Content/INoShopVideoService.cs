using Elegance.Application.Common;

namespace Elegance.Application.Content;

public interface INoShopVideoService
{
    Task<PagedResult<NoShopVideoListItemDto>> GetVideosAsync(NoShopVideoQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NoShopVideoListItemDto>> GetFeaturedVideosAsync(int take, CancellationToken cancellationToken = default);
    Task<NoShopVideoDetailDto?> GetVideoBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
