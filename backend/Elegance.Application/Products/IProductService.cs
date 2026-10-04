using Elegance.Application.Common;

namespace Elegance.Application.Products;

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
