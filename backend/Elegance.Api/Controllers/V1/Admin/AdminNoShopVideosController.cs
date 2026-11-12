using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Catalog)]
[Route("api/v1/admin/noshop/videos")]
public class AdminNoShopVideosController(IAdminNoShopService noShopService, IAdminMediaService mediaService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminNoShopVideoSummaryDto>>> List([FromQuery] AdminNoShopVideoQuery query, CancellationToken cancellationToken)
    {
        return Ok(await noShopService.ListVideosAsync(query, cancellationToken));
    }

    [HttpGet("{videoId:long}")]
    public async Task<ActionResult<AdminNoShopVideoDto>> Get(long videoId, CancellationToken cancellationToken)
    {
        return Ok(await noShopService.GetVideoAsync(videoId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminNoShopVideoDto>> Create([FromBody] AdminNoShopVideoRequest request, CancellationToken cancellationToken)
    {
        var video = await noShopService.CreateVideoAsync(User.GetAdminActor(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, video);
    }

    [HttpPut("{videoId:long}")]
    public async Task<ActionResult<AdminNoShopVideoDto>> Update(long videoId, [FromBody] AdminNoShopVideoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await noShopService.UpdateVideoAsync(User.GetAdminActor(), videoId, request, cancellationToken));
    }

    [HttpPatch("{videoId:long}/status")]
    public async Task<ActionResult<AdminNoShopVideoDto>> SetStatus(long videoId, [FromBody] AdminNoShopVideoStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await noShopService.SetVideoStatusAsync(User.GetAdminActor(), videoId, request.Status, cancellationToken));
    }

    [HttpDelete("{videoId:long}")]
    public async Task<IActionResult> Delete(long videoId, CancellationToken cancellationToken)
    {
        await noShopService.DeleteVideoAsync(User.GetAdminActor(), videoId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{videoId:long}/thumbnail")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<object>> UploadThumbnail(long videoId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var url = await mediaService.UploadNoShopVideoThumbnailAsync(
            User.GetAdminActor(), videoId, stream, file.ContentType, file.Length, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { url });
    }

    [HttpDelete("{videoId:long}/thumbnail")]
    public async Task<IActionResult> DeleteThumbnail(long videoId, CancellationToken cancellationToken)
    {
        await mediaService.RemoveNoShopVideoThumbnailAsync(User.GetAdminActor(), videoId, cancellationToken);
        return NoContent();
    }
}
