using Elegance.Application.Payments;

namespace Elegance.Application.Orders;

public class OrderSummaryDto
{
    public required long Id { get; init; }
    public required string OrderNumber { get; init; }
    public required string Status { get; init; }
    public required decimal GrandTotal { get; init; }
    public required string Currency { get; init; }
    public required int ItemCount { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public class OrderDetailDto
{
    public required long Id { get; init; }
    public required string OrderNumber { get; init; }
    public required string Status { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required decimal Subtotal { get; init; }
    public required decimal ShippingTotal { get; init; }
    public required decimal TaxTotal { get; init; }
    public required decimal GrandTotal { get; init; }
    public required string Currency { get; init; }
    public string? Notes { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required OrderAddressDto ShippingAddress { get; init; }
    public required IReadOnlyList<OrderItemDto> Items { get; init; }
    public required IReadOnlyList<OrderPaymentDto> Payments { get; init; }
    public required IReadOnlyList<OrderStatusEntryDto> History { get; init; }
}

public class OrderItemDto
{
    public required long ProductId { get; init; }
    public long? VariantId { get; init; }
    public required string Sku { get; init; }
    public required string ProductName { get; init; }
    public string? VariantName { get; init; }
    public required decimal UnitPrice { get; init; }
    public required int Quantity { get; init; }
    public required decimal LineTotal { get; init; }
}

public class OrderAddressDto
{
    public required string FullName { get; init; }
    public required string Phone { get; init; }
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public required string Province { get; init; }
    public string? PostalCode { get; init; }
    public required string Country { get; init; }
}

public class OrderPaymentDto
{
    public required string Method { get; init; }
    public required string Status { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required PaymentInstructions Instructions { get; init; }
}

public class OrderStatusEntryDto
{
    public required string Status { get; init; }
    public string? Note { get; init; }
    public required DateTime CreatedAt { get; init; }
}
