namespace Elegance.Domain.Orders.Enums;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Packed,
    Shipped,
    OutForDelivery,
    Delivered,
    Cancelled,
    Returned,
    Refunded,
}
