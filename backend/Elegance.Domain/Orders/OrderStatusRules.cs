using Elegance.Domain.Orders.Enums;

namespace Elegance.Domain.Orders;

public static class OrderStatusRules
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Packed, OrderStatus.Cancelled],
        [OrderStatus.Packed] = [OrderStatus.Shipped],
        [OrderStatus.Shipped] = [OrderStatus.OutForDelivery, OrderStatus.Delivered],
        [OrderStatus.OutForDelivery] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [OrderStatus.Returned],
        [OrderStatus.Returned] = [OrderStatus.Refunded],
        [OrderStatus.Cancelled] = [OrderStatus.Refunded],
        [OrderStatus.Refunded] = [],
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    // Stock taken at placement goes back when an order is cancelled before it ships,
    // and when goods come back from a customer.
    public static bool RestoresStockOn(OrderStatus status) =>
        status is OrderStatus.Cancelled or OrderStatus.Returned;
}
