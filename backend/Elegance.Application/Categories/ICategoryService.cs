namespace Elegance.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
