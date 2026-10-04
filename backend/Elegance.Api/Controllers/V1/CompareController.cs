using Elegance.Api.Auth;
using Elegance.Application.Compare;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/compare")]
public class CompareController(ICompareService compareService) : ControllerBase
{
    public const string GuestCookieName = "elegance_compare";

    [HttpGet]
    public async Task<ActionResult<CompareDto>> GetCompare(CancellationToken cancellationToken)
    {
        var owner = ListOwnerResolver.Resolve(User, Request, Response, GuestCookieName, issueGuestToken: false);
        return Ok(await compareService.GetCompareAsync(owner, cancellationToken));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CompareDto>> AddItem([FromBody] AddCompareItemRequest request, CancellationToken cancellationToken)
    {
        var owner = ListOwnerResolver.Resolve(User, Request, Response, GuestCookieName, issueGuestToken: true);
        return Ok(await compareService.AddItemAsync(owner, request, cancellationToken));
    }

    [HttpDelete("items/{itemId:long}")]
    public async Task<ActionResult<CompareDto>> RemoveItem(long itemId, CancellationToken cancellationToken)
    {
        var owner = ListOwnerResolver.Resolve(User, Request, Response, GuestCookieName, issueGuestToken: false);
        return Ok(await compareService.RemoveItemAsync(owner, itemId, cancellationToken));
    }
}
