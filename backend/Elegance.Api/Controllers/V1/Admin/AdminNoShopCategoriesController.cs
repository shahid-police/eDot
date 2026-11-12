using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Catalog)]
[Route("api/v1/admin/noshop/categories")]
public class AdminNoShopCategoriesController(IAdminNoShopService noShopService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminNoShopCategoryDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await noShopService.ListCategoriesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminNoShopCategoryDto>> Create([FromBody] AdminNoShopCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await noShopService.CreateCategoryAsync(User.GetAdminActor(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, category);
    }

    [HttpPut("{categoryId:long}")]
    public async Task<ActionResult<AdminNoShopCategoryDto>> Update(long categoryId, [FromBody] AdminNoShopCategoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await noShopService.UpdateCategoryAsync(User.GetAdminActor(), categoryId, request, cancellationToken));
    }
}
