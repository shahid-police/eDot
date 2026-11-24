using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

public class PublishSubmissionImageRequest
{
    public string ImageType { get; init; } = "Gallery";
}

[ApiController]
[Authorize(Policy = AdminPolicies.Vendors)]
[Route("api/v1/admin/vendor-submissions")]
public class AdminVendorSubmissionsController(IAdminVendorSubmissionService submissionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminVendorSubmissionDto>>> List([FromQuery] AdminVendorSubmissionQuery query, CancellationToken cancellationToken)
    {
        var result = await submissionService.ListSubmissionsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{submissionId:long}")]
    public async Task<ActionResult<AdminVendorSubmissionDto>> Get(long submissionId, CancellationToken cancellationToken)
    {
        var dto = await submissionService.GetSubmissionAsync(submissionId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{submissionId:long}/approve")]
    public async Task<ActionResult<AdminVendorSubmissionDto>> Approve(long submissionId, CancellationToken cancellationToken)
    {
        var dto = await submissionService.ApproveAsync(User.GetAdminActor(), submissionId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{submissionId:long}/reject")]
    public async Task<ActionResult<AdminVendorSubmissionDto>> Reject(long submissionId, [FromBody] AdminRejectSubmissionRequest request, CancellationToken cancellationToken)
    {
        var dto = await submissionService.RejectAsync(User.GetAdminActor(), submissionId, request, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{submissionId:long}/return-for-correction")]
    public async Task<ActionResult<AdminVendorSubmissionDto>> ReturnForCorrection(long submissionId, [FromBody] AdminReturnSubmissionForCorrectionRequest request, CancellationToken cancellationToken)
    {
        var dto = await submissionService.ReturnForCorrectionAsync(User.GetAdminActor(), submissionId, request, cancellationToken);
        return Ok(dto);
    }

    // Lets an admin inspect/download a vendor's original image. Only reachable via this
    // Admin.Vendors-gated controller - there is no public or vendor-facing route to any other
    // vendor's image.
    [HttpGet("{submissionId:long}/images/{imageId:long}/download")]
    public async Task<IActionResult> DownloadImage(long submissionId, long imageId, CancellationToken cancellationToken)
    {
        var (content, contentType, fileName) = await submissionService.DownloadSubmissionImageAsync(submissionId, imageId, cancellationToken);
        return File(content, contentType, fileName);
    }

    // The only path by which a vendor-original image can ever become a public ProductImage - see
    // AdminVendorSubmissionService.PublishSubmissionImageAsync for why this is safe by
    // construction rather than by a flag.
    [HttpPost("{submissionId:long}/images/{imageId:long}/publish")]
    public async Task<ActionResult<AdminProductImageDto>> PublishImage(long submissionId, long imageId, [FromBody] PublishSubmissionImageRequest request, CancellationToken cancellationToken)
    {
        var dto = await submissionService.PublishSubmissionImageAsync(User.GetAdminActor(), submissionId, imageId, request.ImageType, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, dto);
    }
}
