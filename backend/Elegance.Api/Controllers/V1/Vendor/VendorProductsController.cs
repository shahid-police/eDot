using Elegance.Api.Auth;
using Elegance.Application.Common;
using Elegance.Application.Vendors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Vendor;

[ApiController]
[Authorize(Policy = VendorPolicies.Portal)]
[Route("api/v1/vendor")]
public class VendorProductsController(IVendorCatalogService vendorCatalogService) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<VendorProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await vendorCatalogService.GetProfileAsync(User.GetVendorId(), cancellationToken);
        return Ok(profile);
    }

    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<VendorSubmissionDto>>> ListProducts([FromQuery] VendorSubmissionQuery query, CancellationToken cancellationToken)
    {
        var result = await vendorCatalogService.ListMySubmissionsAsync(User.GetVendorId(), query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("products/{submissionId:long}")]
    public async Task<ActionResult<VendorSubmissionDto>> GetProduct(long submissionId, CancellationToken cancellationToken)
    {
        var dto = await vendorCatalogService.GetSubmissionAsync(User.GetVendorId(), submissionId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("products")]
    public async Task<ActionResult<VendorSubmissionDto>> CreateProduct([FromBody] VendorSubmissionRequest request, CancellationToken cancellationToken)
    {
        var dto = await vendorCatalogService.CreateSubmissionAsync(User.GetVendorId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPut("products/{submissionId:long}")]
    public async Task<ActionResult<VendorSubmissionDto>> UpdateProduct(long submissionId, [FromBody] VendorSubmissionRequest request, CancellationToken cancellationToken)
    {
        var dto = await vendorCatalogService.UpdateSubmissionAsync(User.GetVendorId(), submissionId, request, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("products/{submissionId:long}/resubmit")]
    public async Task<ActionResult<VendorSubmissionDto>> Resubmit(long submissionId, CancellationToken cancellationToken)
    {
        var dto = await vendorCatalogService.ResubmitAsync(User.GetVendorId(), submissionId, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("products/{submissionId:long}")]
    public async Task<IActionResult> Withdraw(long submissionId, CancellationToken cancellationToken)
    {
        await vendorCatalogService.WithdrawAsync(User.GetVendorId(), submissionId, cancellationToken);
        return NoContent();
    }

    [HttpPost("products/{submissionId:long}/images")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<VendorSubmissionImageDto>> UploadImage(long submissionId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var image = await vendorCatalogService.UploadSubmissionImageAsync(
            User.GetVendorId(), submissionId, stream, file.ContentType, file.Length, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, image);
    }

    [HttpDelete("products/{submissionId:long}/images/{imageId:long}")]
    public async Task<IActionResult> RemoveImage(long submissionId, long imageId, CancellationToken cancellationToken)
    {
        await vendorCatalogService.RemoveSubmissionImageAsync(User.GetVendorId(), submissionId, imageId, cancellationToken);
        return NoContent();
    }

    // Streams the vendor's own original image bytes - the only way to view it, since no public
    // URL for a vendor-original image is ever returned by this API.
    [HttpGet("products/{submissionId:long}/images/{imageId:long}")]
    public async Task<IActionResult> DownloadImage(long submissionId, long imageId, CancellationToken cancellationToken)
    {
        var (content, contentType, fileName) = await vendorCatalogService.DownloadSubmissionImageAsync(
            User.GetVendorId(), submissionId, imageId, cancellationToken);
        return File(content, contentType, fileName);
    }
}
