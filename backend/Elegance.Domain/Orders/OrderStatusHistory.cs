using Elegance.Domain.Orders.Enums;

namespace Elegance.Domain.Orders;

public class OrderStatusHistory
{
    public long Id { get; set; }

    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public OrderStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
