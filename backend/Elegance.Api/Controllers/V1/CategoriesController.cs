using Elegance.Application.Categories;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryBySlug(string slug, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetCategoryBySlugAsync(slug, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return Ok(category);
    }
}
