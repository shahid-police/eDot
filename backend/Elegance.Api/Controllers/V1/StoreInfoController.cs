using Elegance.Application.Notifications;
using Elegance.Infrastructure.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Elegance.Api.Controllers.V1;

// Small, rarely-changing, publicly-displayed settings the storefront needs - not worth a whole
// service layer for one field. Add more fields here only while they stay this trivial; anything
// with real logic behind it gets its own controller/service instead.
[ApiController]
[Route("api/v1/store-info")]
public class StoreInfoController(IOptions<WhatsAppOptions> whatsAppOptions) : ControllerBase
{
    [HttpGet]
    public ActionResult<StoreInfoDto> Get()
    {
        return Ok(new StoreInfoDto { WhatsAppNumber = whatsAppOptions.Value.StoreContactNumber });
    }
}
