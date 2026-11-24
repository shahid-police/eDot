using Elegance.Domain.Vendors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("vendors");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(v => v.Email).IsUnique();

        builder.Property(v => v.PasswordHash).HasMaxLength(512).IsRequired();

        builder.Property(v => v.VendorCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(v => v.VendorCode).IsUnique();

        builder.Property(v => v.ShopName).HasMaxLength(200).IsRequired();
        builder.Property(v => v.ContactPerson).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Phone).HasMaxLength(30).IsRequired();
        builder.Property(v => v.Address).HasMaxLength(500);
        builder.Property(v => v.City).HasMaxLength(100);
        builder.Property(v => v.AdminRemarks).HasMaxLength(2000);

        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(v => v.Status);
    }
}

public class VendorTwoFactorCredentialConfiguration : IEntityTypeConfiguration<VendorTwoFactorCredential>
{
    public void Configure(EntityTypeBuilder<VendorTwoFactorCredential> builder)
    {
        builder.ToTable("vendor_two_factor_credentials");
        builder.HasKey(c => c.VendorId);

        builder.Property(c => c.EncryptedSecret).HasColumnType("text").IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(c => c.Vendor)
            .WithOne()
            .HasForeignKey<VendorTwoFactorCredential>(c => c.VendorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VendorRefreshTokenConfiguration : IEntityTypeConfiguration<VendorRefreshToken>
{
    public void Configure(EntityTypeBuilder<VendorRefreshToken> builder)
    {
        builder.ToTable("vendor_refresh_tokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.VendorId);

        builder.HasOne(t => t.Vendor)
            .WithMany()
            .HasForeignKey(t => t.VendorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VendorProductSubmissionConfiguration : IEntityTypeConfiguration<VendorProductSubmission>
{
    public void Configure(EntityTypeBuilder<VendorProductSubmission> builder)
    {
        builder.ToTable("vendor_product_submissions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(s => s.WholesalePrice).HasPrecision(10, 2);
        builder.Property(s => s.VendorProductCode).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.RejectionReason).HasMaxLength(1000);
        builder.Property(s => s.AdminRemarks).HasMaxLength(2000);
        builder.Property(s => s.CorrectionFieldsRequested).HasMaxLength(1000);

        builder.HasOne(s => s.Vendor)
            .WithMany(v => v.Submissions)
            .HasForeignKey(s => s.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Elegance.Domain.Catalog.Category>()
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Elegance.Domain.Catalog.Product>()
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // A vendor's own product code must be unique within their own submissions, not globally -
        // different vendors may freely reuse the same code.
        builder.HasIndex(s => new { s.VendorId, s.VendorProductCode }).IsUnique();
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.SubmittedAt);
    }
}

public class VendorProductImageConfiguration : IEntityTypeConfiguration<VendorProductImage>
{
    public void Configure(EntityTypeBuilder<VendorProductImage> builder)
    {
        builder.ToTable("vendor_product_images");
        builder.HasKey(i => i.Id);

        builder.HasOne(i => i.Submission)
            .WithMany(s => s.Images)
            .HasForeignKey(i => i.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.MediaFile)
            .WithMany()
            .HasForeignKey(i => i.MediaFileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.SubmissionId);
        builder.HasIndex(i => i.VendorId);
    }
}

public class VendorProductSubmissionHistoryConfiguration : IEntityTypeConfiguration<VendorProductSubmissionHistory>
{
    public void Configure(EntityTypeBuilder<VendorProductSubmissionHistory> builder)
    {
        builder.ToTable("vendor_product_submission_history");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Action).HasMaxLength(100).IsRequired();
        builder.Property(h => h.PreviousStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(h => h.NewStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(h => h.PerformedByType).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Remarks).HasMaxLength(2000);

        builder.HasOne(h => h.Submission)
            .WithMany(s => s.History)
            .HasForeignKey(h => h.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.SubmissionId);
    }
}
