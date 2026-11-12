using Elegance.Application.Content;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/noshop")]
public class NoShopController(INoShopVideoService videoService, INoShopCategoryService categoryService) : ControllerBase
{
    [HttpGet("videos")]
    public async Task<ActionResult> GetVideos(
        [FromQuery] string? category,
        [FromQuery] string? type,
        [FromQuery] bool? featured,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var query = new NoShopVideoQuery
        {
            CategorySlug = category,
            VideoType = type,
            IsFeatured = featured,
            Page = page,
            PageSize = pageSize,
        };

        return Ok(await videoService.GetVideosAsync(query, cancellationToken));
    }

    [HttpGet("videos/featured")]
    public async Task<ActionResult<IReadOnlyList<NoShopVideoListItemDto>>> GetFeaturedVideos(
        [FromQuery] int take = 5, CancellationToken cancellationToken = default)
    {
        return Ok(await videoService.GetFeaturedVideosAsync(take, cancellationToken));
    }

    [HttpGet("videos/{slug}")]
    public async Task<ActionResult<NoShopVideoDetailDto>> GetVideoBySlug(string slug, CancellationToken cancellationToken)
    {
        var video = await videoService.GetVideoBySlugAsync(slug, cancellationToken);
        if (video is null)
        {
            return NotFound();
        }

        return Ok(video);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<NoShopCategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        return Ok(await categoryService.GetCategoriesAsync(cancellationToken));
    }
}
