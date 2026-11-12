using Elegance.Domain.Customers;
using Elegance.Domain.Marketing;

namespace Elegance.Domain.Carts;

public class Cart
{
    public long Id { get; set; }

    public long? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? GuestTokenHash { get; set; }

    // Set by CheckoutService.ApplyPromoCodeAsync, cleared by RemovePromoCodeAsync or once the
    // cart is emptied at order placement - only one code applies per cart/order at a time.
    public long? AppliedPromoCodeId { get; set; }
    public PromoCode? AppliedPromoCode { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<CartItem> Items { get; set; } = [];
}
