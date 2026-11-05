using Elegance.Application.Common;
using Elegance.Application.Products;
using Elegance.Application.Search;

namespace Elegance.Api.Contracts;

public record NaturalSearchResponse(ProductSearchSpec AppliedFilters, PagedResult<ProductListItemDto> Products);
