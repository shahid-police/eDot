using Elegance.Domain.Orders;
using Elegance.Domain.Orders.Enums;
using Elegance.Infrastructure.Persistence;

namespace Elegance.Tests.TestSupport;

public static class TestOrder
{
    public static async Task<Order> CreateAsync(
        EleganceDbContext db,
        long productId,
        string productSku,
        string productName,
        int quantity,
        decimal unitPrice,
        OrderStatus status = OrderStatus.Pending,
        string? orderNumber = null,
        DateTime? createdAt = null,
        string city = "Lahore",
        string province = "Punjab",
        PaymentStatus paymentStatus = PaymentStatus.Pending)
    {
        var now = createdAt ?? DateTime.UtcNow;
        var lineTotal = unitPrice * quantity;

        var order = new Order
        {
            OrderNumber = orderNumber ?? $"ORD-{Guid.NewGuid():N}"[..12],
            Email = "customer@test.local",
            Phone = "+923000000000",
            Subtotal = lineTotal,
            GrandTotal = lineTotal,
            Currency = "PKR",
            Status = status,
            CreatedAt = now,
            UpdatedAt = now,
        };
        order.Items.Add(new OrderItem
        {
            ProductId = productId,
            Sku = productSku,
            ProductName = productName,
            OriginalUnitPrice = unitPrice,
            UnitPrice = unitPrice,
            Quantity = quantity,
            LineTotal = lineTotal,
        });
        order.Addresses.Add(new OrderAddress
        {
            Type = AddressType.Shipping,
            FullName = "Jane Customer",
            Phone = "+923000000000",
            AddressLine1 = "123 Secret Street",
            City = city,
            Province = province,
            Country = "Pakistan",
        });
        order.Payments.Add(new Payment
        {
            Method = PaymentMethod.CashOnDelivery,
            Status = paymentStatus,
            Amount = lineTotal,
            Currency = "PKR",
            CreatedAt = now,
            UpdatedAt = now,
        });

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }
}
