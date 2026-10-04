using Elegance.Application.Common;

namespace Elegance.Application.Compare;

public interface ICompareService
{
    Task<CompareDto> GetCompareAsync(ListOwner owner, CancellationToken cancellationToken = default);
    Task<CompareDto> AddItemAsync(ListOwner owner, AddCompareItemRequest request, CancellationToken cancellationToken = default);
    Task<CompareDto> RemoveItemAsync(ListOwner owner, long itemId, CancellationToken cancellationToken = default);
    Task MergeGuestCompareAsync(long customerId, string guestToken, CancellationToken cancellationToken = default);
}
