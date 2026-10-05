using System.ComponentModel.DataAnnotations;
using Elegance.Application.Customers;

namespace Elegance.Application.Checkout;

public class CheckoutSummaryDto
{
    public required bool IsEmpty { get; init; }
    public required int ItemCount { get; init; }
    public required decimal Subtotal { get; init; }
    public required decimal ShippingTotal { get; init; }
    public required decimal TaxTotal { get; init; }
    public required decimal GrandTotal { get; init; }
    public required string Currency { get; init; }
    public required decimal FreeShippingThreshold { get; init; }
}

public class CheckoutRequest
{
    [StringLength(254)]
    public string? Email { get; init; }

    [Required, StringLength(30)]
    public string PaymentMethod { get; init; } = string.Empty;

    public long? ShippingAddressId { get; init; }

    public AddressRequest? ShippingAddress { get; init; }

    [StringLength(1000)]
    public string? Notes { get; init; }
}
