using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Customers)]
[Route("api/v1/admin/customers")]
public class AdminCustomersController(IAdminCustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminCustomerSummaryDto>>> List([FromQuery] AdminCustomerQuery query, CancellationToken cancellationToken)
    {
        return Ok(await customerService.ListCustomersAsync(query, cancellationToken));
    }

    [HttpGet("{customerId:long}")]
    public async Task<ActionResult<AdminCustomerDetailDto>> Get(long customerId, CancellationToken cancellationToken)
    {
        return Ok(await customerService.GetCustomerAsync(customerId, cancellationToken));
    }

    [HttpPut("{customerId:long}/status")]
    public async Task<ActionResult<AdminCustomerSummaryDto>> SetStatus(long customerId, [FromBody] ActiveStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await customerService.SetCustomerActiveAsync(User.GetAdminActor(), customerId, request.IsActive, cancellationToken));
    }
}
