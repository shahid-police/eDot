using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Inventory)]
[Route("api/v1/admin/inventory")]
public class AdminInventoryController(IAdminInventoryService inventoryService) : ControllerBase
{
    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyList<LowStockItemDto>>> LowStock(CancellationToken cancellationToken)
    {
        return Ok(await inventoryService.GetLowStockAsync(cancellationToken));
    }

    [HttpPost("adjustments")]
    public async Task<ActionResult<StockMovementDto>> Adjust([FromBody] StockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var movement = await inventoryService.AdjustStockAsync(User.GetAdminActor(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, movement);
    }

    [HttpGet("movements")]
    public async Task<ActionResult<PagedResult<StockMovementDto>>> Movements(
        [FromQuery] long? productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        return Ok(await inventoryService.ListMovementsAsync(productId, page, pageSize, cancellationToken));
    }
}
