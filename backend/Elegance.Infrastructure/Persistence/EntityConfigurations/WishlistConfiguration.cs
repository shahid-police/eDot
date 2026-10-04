using Elegance.Domain.Wishlists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("wishlists");

        builder.HasKey(w => w.Id);

        builder.HasOne(w => w.Customer)
            .WithMany()
            .HasForeignKey(w => w.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(w => w.CustomerId).IsUnique();
    }
}
