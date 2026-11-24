using Elegance.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class VendorNotificationConfiguration : IEntityTypeConfiguration<VendorNotification>
{
    public void Configure(EntityTypeBuilder<VendorNotification> builder)
    {
        builder.ToTable("vendor_notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Event).HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.WhatsAppNumber).HasMaxLength(20).IsRequired();
        builder.Property(n => n.WhatsAppMessageId).HasMaxLength(128);
        builder.Property(n => n.MessageTemplateName).HasMaxLength(100);
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.FailureReason).HasMaxLength(500);

        builder.HasOne<Elegance.Domain.Vendors.Vendor>()
            .WithMany()
            .HasForeignKey(n => n.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Elegance.Domain.Vendors.VendorProductSubmission>()
            .WithMany()
            .HasForeignKey(n => n.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.NextAttemptAt);
        builder.HasIndex(n => n.VendorId);
        builder.HasIndex(n => n.SubmissionId);
    }
}
