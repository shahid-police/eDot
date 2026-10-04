using Elegance.Domain.Wishlists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("wishlist_items");

        builder.HasKey(i => i.Id);

        builder.HasOne(i => i.Wishlist)
            .WithMany(w => w.Items)
            .HasForeignKey(i => i.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.WishlistId, i.ProductId, i.VariantId }).IsUnique();
    }
}
