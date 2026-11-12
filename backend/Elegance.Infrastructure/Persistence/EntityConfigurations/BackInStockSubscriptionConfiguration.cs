using Elegance.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class BackInStockSubscriptionConfiguration : IEntityTypeConfiguration<BackInStockSubscription>
{
    public void Configure(EntityTypeBuilder<BackInStockSubscription> builder)
    {
        builder.ToTable("back_in_stock_subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.WhatsAppNumber).HasMaxLength(32).IsRequired();
        builder.Property(s => s.NormalizedWhatsAppNumber).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ProductVariant)
            .WithMany()
            .HasForeignKey(s => s.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.ProductId);
        builder.HasIndex(s => s.NormalizedWhatsAppNumber);
        builder.HasIndex(s => s.Status);

        // MySQL has no partial/filtered unique index, so "unique only while Active" (a customer
        // may subscribe again after being notified or unsubscribing) is enforced in
        // BackInStockService via an existence check before insert, same pattern as slug/SKU
        // uniqueness elsewhere in this codebase. This index exists for that lookup's performance,
        // not as the uniqueness guarantee itself.
        builder.HasIndex(s => new { s.ProductId, s.ProductVariantId, s.NormalizedWhatsAppNumber, s.Status });
    }
}
