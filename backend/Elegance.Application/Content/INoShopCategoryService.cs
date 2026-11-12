namespace Elegance.Application.Content;

public interface INoShopCategoryService
{
    Task<IReadOnlyList<NoShopCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
