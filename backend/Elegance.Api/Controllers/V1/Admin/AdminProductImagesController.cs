using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

public class ReorderImageRequest
{
    public int SortOrder { get; init; }
}

[ApiController]
[Authorize(Policy = AdminPolicies.Catalog)]
[Route("api/v1/admin/products/{productId:long}")]
public class AdminProductImagesController(IAdminMediaService mediaService) : ControllerBase
{
    [HttpPost("images")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<AdminProductImageDto>> UploadImage(
        long productId, [FromForm] string imageType, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var image = await mediaService.UploadProductImageAsync(
            User.GetAdminActor(), productId, imageType, stream, file.ContentType, file.Length, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, image);
    }

    [HttpDelete("images/{imageId:long}")]
    public async Task<IActionResult> DeleteImage(long productId, long imageId, CancellationToken cancellationToken)
    {
        await mediaService.DeleteProductImageAsync(User.GetAdminActor(), productId, imageId, cancellationToken);
        return NoContent();
    }

    [HttpPut("images/{imageId:long}")]
    public async Task<ActionResult<AdminProductImageDto>> ReorderImage(
        long productId, long imageId, [FromBody] ReorderImageRequest request, CancellationToken cancellationToken)
    {
        var image = await mediaService.ReorderProductImageAsync(User.GetAdminActor(), productId, imageId, request.SortOrder, cancellationToken);
        return Ok(image);
    }

    [HttpPost("variants/{variantId:long}/image")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<object>> UploadVariantImage(
        long productId, long variantId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var url = await mediaService.UploadVariantImageAsync(
            User.GetAdminActor(), productId, variantId, stream, file.ContentType, file.Length, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { url });
    }

    [HttpDelete("variants/{variantId:long}/image")]
    public async Task<IActionResult> DeleteVariantImage(long productId, long variantId, CancellationToken cancellationToken)
    {
        await mediaService.RemoveVariantImageAsync(User.GetAdminActor(), productId, variantId, cancellationToken);
        return NoContent();
    }
}
