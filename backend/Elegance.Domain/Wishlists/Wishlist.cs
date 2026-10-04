using Elegance.Domain.Customers;

namespace Elegance.Domain.Wishlists;

public class Wishlist
{
    public long Id { get; set; }

    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<WishlistItem> Items { get; set; } = [];
}
