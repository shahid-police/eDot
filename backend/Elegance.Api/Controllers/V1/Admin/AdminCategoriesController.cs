using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Catalog)]
[Route("api/v1/admin/categories")]
public class AdminCategoriesController(IAdminCatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminCategoryDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await catalogService.ListCategoriesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCategoryDto>> Create([FromBody] AdminCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await catalogService.CreateCategoryAsync(User.GetAdminActor(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, category);
    }

    [HttpPut("{categoryId:long}")]
    public async Task<ActionResult<AdminCategoryDto>> Update(long categoryId, [FromBody] AdminCategoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.UpdateCategoryAsync(User.GetAdminActor(), categoryId, request, cancellationToken));
    }
}
