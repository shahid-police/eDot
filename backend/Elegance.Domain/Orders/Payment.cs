using Elegance.Domain.Orders.Enums;

namespace Elegance.Domain.Orders;

public class Payment
{
    public long Id { get; set; }

    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public required string Currency { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
