using Elegance.Api.Contracts;
using Elegance.Application.Products;
using Elegance.Application.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/search")]
public class SearchController(ISearchIntentService searchIntentService, IProductService productService) : ControllerBase
{
    [HttpPost("natural")]
    [EnableRateLimiting("ai-search")]
    public async Task<ActionResult<NaturalSearchResponse>> SearchNatural(
        [FromBody] NaturalSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest();
        }

        // Bounds the prompt (and LLM cost) regardless of how much text a client sends.
        var query = request.Query.Length > 300 ? request.Query[..300] : request.Query;

        var spec = await searchIntentService.InterpretAsync(query, cancellationToken);

        var productQuery = new ProductListQuery
        {
            CategorySlug = spec.CategorySlug,
            MinPrice = spec.MinPrice,
            MaxPrice = spec.MaxPrice,
            Material = spec.Material,
            Color = spec.Color,
            Occasion = spec.Occasion,
            Gender = spec.Gender,
            Search = spec.Keywords.Count > 0 ? string.Join(' ', spec.Keywords) : null,
            PageSize = 48,
        };

        var result = await productService.GetProductsAsync(productQuery, cancellationToken);
        return Ok(new NaturalSearchResponse(spec, result));
    }
}
