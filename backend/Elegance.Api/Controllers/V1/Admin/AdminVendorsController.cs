using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Vendors)]
[Route("api/v1/admin/vendors")]
public class AdminVendorsController(IAdminVendorService vendorService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminVendorDto>>> List([FromQuery] AdminVendorQuery query, CancellationToken cancellationToken)
    {
        var result = await vendorService.ListVendorsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{vendorId:long}")]
    public async Task<ActionResult<AdminVendorDto>> Get(long vendorId, CancellationToken cancellationToken)
    {
        var dto = await vendorService.GetVendorAsync(vendorId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] AdminVendorRequest request, CancellationToken cancellationToken)
    {
        var (vendor, temporaryPassword) = await vendorService.CreateVendorAsync(User.GetAdminActor(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { vendor, temporaryPassword });
    }

    [HttpPut("{vendorId:long}")]
    public async Task<ActionResult<AdminVendorDto>> Update(long vendorId, [FromBody] AdminVendorRequest request, CancellationToken cancellationToken)
    {
        var dto = await vendorService.UpdateVendorAsync(User.GetAdminActor(), vendorId, request, cancellationToken);
        return Ok(dto);
    }

    [HttpPut("{vendorId:long}/status")]
    public async Task<ActionResult<AdminVendorDto>> SetStatus(long vendorId, [FromBody] AdminSetVendorStatusRequest request, CancellationToken cancellationToken)
    {
        var dto = await vendorService.SetVendorStatusAsync(User.GetAdminActor(), vendorId, request, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{vendorId:long}/reset-credentials")]
    public async Task<ActionResult<AdminVendorCredentialResetDto>> ResetCredentials(long vendorId, CancellationToken cancellationToken)
    {
        var dto = await vendorService.ResetVendorCredentialsAsync(User.GetAdminActor(), vendorId, cancellationToken);
        return Ok(dto);
    }
}
