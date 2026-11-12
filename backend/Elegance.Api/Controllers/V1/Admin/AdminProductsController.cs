using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Catalog)]
[Route("api/v1/admin/products")]
public class AdminProductsController(IAdminCatalogService catalogService, IAdminProductDiscountService discountService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminProductSummaryDto>>> List([FromQuery] AdminProductQuery query, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.ListProductsAsync(query, cancellationToken));
    }

    [HttpGet("{productId:long}")]
    public async Task<ActionResult<AdminProductDto>> Get(long productId, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.GetProductAsync(productId, cancellationToken));
    }

    // "next-sku" never collides with the {productId:long} route above - it isn't a valid long.
    [HttpGet("next-sku")]
    public async Task<ActionResult<NextSkuDto>> NextSku([FromQuery] long categoryId, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.PreviewNextSkuAsync(categoryId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminProductDto>> Create([FromBody] AdminProductRequest request, CancellationToken cancellationToken)
    {
        var product = await catalogService.CreateProductAsync(User.GetAdminActor(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, product);
    }

    [HttpPut("{productId:long}")]
    public async Task<ActionResult<AdminProductDto>> Update(long productId, [FromBody] AdminProductRequest request, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.UpdateProductAsync(User.GetAdminActor(), productId, request, cancellationToken));
    }

    [HttpPut("{productId:long}/status")]
    public async Task<ActionResult<AdminProductDto>> SetStatus(long productId, [FromBody] ActiveStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.SetProductActiveAsync(User.GetAdminActor(), productId, request.IsActive, cancellationToken));
    }

    [HttpGet("{productId:long}/discounts")]
    public async Task<ActionResult<IReadOnlyList<AdminProductDiscountDto>>> ListDiscounts(long productId, CancellationToken cancellationToken)
    {
        return Ok(await discountService.ListProductDiscountsAsync(productId, cancellationToken));
    }

    [HttpPost("{productId:long}/discounts")]
    public async Task<ActionResult<AdminProductDiscountDto>> CreateDiscount(long productId, [FromBody] AdminProductDiscountRequest request, CancellationToken cancellationToken)
    {
        var discount = await discountService.CreateProductDiscountAsync(User.GetAdminActor(), productId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, discount);
    }

    [HttpPatch("{productId:long}/discounts/{discountId:long}/cancel")]
    public async Task<IActionResult> CancelDiscount(long productId, long discountId, CancellationToken cancellationToken)
    {
        await discountService.CancelProductDiscountAsync(User.GetAdminActor(), productId, discountId, cancellationToken);
        return NoContent();
    }
}
