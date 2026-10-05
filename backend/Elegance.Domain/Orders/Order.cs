using Elegance.Domain.Customers;
using Elegance.Domain.Orders.Enums;

namespace Elegance.Domain.Orders;

public class Order
{
    public long Id { get; set; }
    public required string OrderNumber { get; set; }

    public long? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? GuestTokenHash { get; set; }

    public required string Email { get; set; }
    public required string Phone { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public required string Currency { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderAddress> Addresses { get; set; } = [];
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
