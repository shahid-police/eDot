namespace Elegance.Application.Search;

public interface ISearchIntentService
{
    Task<ProductSearchSpec> InterpretAsync(string query, CancellationToken cancellationToken = default);
}
