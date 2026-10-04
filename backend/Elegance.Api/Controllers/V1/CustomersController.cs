using Elegance.Application.Common;
using Elegance.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/v1/customers/me")]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CustomerDto>> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await customerService.GetProfileAsync(CurrentCustomerId, cancellationToken);
        return Ok(profile);
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> GetAddresses(CancellationToken cancellationToken)
    {
        var addresses = await customerService.GetAddressesAsync(CurrentCustomerId, cancellationToken);
        return Ok(addresses);
    }

    [HttpPost("addresses")]
    public async Task<ActionResult<AddressDto>> AddAddress([FromBody] AddressRequest request, CancellationToken cancellationToken)
    {
        var address = await customerService.AddAddressAsync(CurrentCustomerId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, address);
    }

    [HttpPut("addresses/{addressId:long}")]
    public async Task<ActionResult<AddressDto>> UpdateAddress(long addressId, [FromBody] AddressRequest request, CancellationToken cancellationToken)
    {
        var address = await customerService.UpdateAddressAsync(CurrentCustomerId, addressId, request, cancellationToken);
        return Ok(address);
    }

    [HttpDelete("addresses/{addressId:long}")]
    public async Task<IActionResult> DeleteAddress(long addressId, CancellationToken cancellationToken)
    {
        await customerService.DeleteAddressAsync(CurrentCustomerId, addressId, cancellationToken);
        return NoContent();
    }

    private long CurrentCustomerId
    {
        get
        {
            var subject = User.FindFirst("sub")?.Value;
            return long.TryParse(subject, out var id)
                ? id
                : throw new ApplicationFailure(FailureKind.Unauthorized, "Your session has expired. Please sign in again.");
        }
    }
}
