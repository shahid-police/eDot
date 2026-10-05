using Elegance.Application.Common;
using Elegance.Application.Orders;

namespace Elegance.Application.Checkout;

public interface ICheckoutService
{
    Task<CheckoutSummaryDto> GetSummaryAsync(ListOwner owner, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> PlaceOrderAsync(ListOwner owner, CheckoutRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersAsync(ListOwner owner, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> GetOrderAsync(ListOwner owner, long orderId, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> GetOrderByNumberAsync(ListOwner owner, string orderNumber, CancellationToken cancellationToken = default);
    Task AdoptGuestOrdersAsync(long customerId, string guestToken, CancellationToken cancellationToken = default);
}
