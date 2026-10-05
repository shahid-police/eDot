using Elegance.Application.Common;
using Elegance.Application.Orders;

namespace Elegance.Application.Checkout;

public interface ICheckoutService
{
    Task<CheckoutSummaryDto> GetSummaryAsync(ListOwner owner, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> PlaceOrderAsync(ListOwner owner, CheckoutRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersAsync(long customerId, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> GetOrderAsync(long customerId, long orderId, CancellationToken cancellationToken = default);
}
