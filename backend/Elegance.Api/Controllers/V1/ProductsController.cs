using Elegance.Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/products")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetProducts(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] bool? isFeatured,
        [FromQuery] bool? isNew,
        [FromQuery] bool? isBestSeller,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var query = new ProductListQuery
        {
            CategorySlug = category,
            Search = search,
            IsFeatured = isFeatured,
            IsNew = isNew,
            IsBestSeller = isBestSeller,
            Page = page,
            PageSize = pageSize,
        };

        var result = await productService.GetProductsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ProductDetailDto>> GetProductBySlug(string slug, CancellationToken cancellationToken)
    {
        var product = await productService.GetProductBySlugAsync(slug, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }
}
