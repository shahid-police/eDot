using Elegance.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class WhatsAppNotificationConfiguration : IEntityTypeConfiguration<WhatsAppNotification>
{
    public void Configure(EntityTypeBuilder<WhatsAppNotification> builder)
    {
        builder.ToTable("whatsapp_notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.WhatsAppNumber).HasMaxLength(20).IsRequired();
        builder.Property(n => n.WhatsAppMessageId).HasMaxLength(128);
        builder.Property(n => n.MessageTemplateName).HasMaxLength(100);
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.FailureReason).HasMaxLength(500);

        builder.HasOne(n => n.Subscription)
            .WithMany()
            .HasForeignKey(n => n.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.RestockEvent)
            .WithMany()
            .HasForeignKey(n => n.RestockEventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Product)
            .WithMany()
            .HasForeignKey(n => n.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // The idempotency guarantee: a given subscription can only ever get one notification row
        // per restock event, even if the detection step somehow ran twice for the same event.
        builder.HasIndex(n => new { n.SubscriptionId, n.RestockEventId }).IsUnique();

        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.NextAttemptAt);
    }
}
