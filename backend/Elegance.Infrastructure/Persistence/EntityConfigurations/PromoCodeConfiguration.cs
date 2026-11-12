using Elegance.Domain.Marketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("promo_codes");

        builder.Property(c => c.Code).HasMaxLength(6).IsFixedLength();
        builder.Property(c => c.CustomerDiscountPercentage).HasPrecision(5, 2);
        builder.Property(c => c.CommissionPercentageOverride).HasPrecision(5, 2);
        builder.Property(c => c.MinimumOrderAmount).HasPrecision(10, 2);
        builder.Property(c => c.MaximumDiscountAmount).HasPrecision(10, 2);

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.PromoterId);
        builder.HasIndex(c => c.Status);

        builder.HasOne(c => c.Promoter)
            .WithMany(p => p.PromoCodes)
            .HasForeignKey(c => c.PromoterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PromoCodeProductConfiguration : IEntityTypeConfiguration<PromoCodeProduct>
{
    public void Configure(EntityTypeBuilder<PromoCodeProduct> builder)
    {
        builder.ToTable("promo_code_products");
        builder.HasKey(pp => new { pp.PromoCodeId, pp.ProductId });

        builder.HasOne(pp => pp.PromoCode)
            .WithMany(c => c.Products)
            .HasForeignKey(pp => pp.PromoCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.Product)
            .WithMany()
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PromoCodeCategoryConfiguration : IEntityTypeConfiguration<PromoCodeCategory>
{
    public void Configure(EntityTypeBuilder<PromoCodeCategory> builder)
    {
        builder.ToTable("promo_code_categories");
        builder.HasKey(pc => new { pc.PromoCodeId, pc.CategoryId });

        builder.HasOne(pc => pc.PromoCode)
            .WithMany(c => c.Categories)
            .HasForeignKey(pc => pc.PromoCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.Category)
            .WithMany()
            .HasForeignKey(pc => pc.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PromoterCommissionConfiguration : IEntityTypeConfiguration<PromoterCommission>
{
    public void Configure(EntityTypeBuilder<PromoterCommission> builder)
    {
        builder.ToTable("promoter_commissions");

        builder.Property(c => c.CommissionPercentageSnapshot).HasPrecision(5, 2);
        builder.Property(c => c.EligibleSaleAmount).HasPrecision(12, 2);
        builder.Property(c => c.CommissionAmount).HasPrecision(12, 2);
        builder.Property(c => c.Currency).HasMaxLength(3).IsRequired();
        builder.Property(c => c.Remarks).HasMaxLength(1000);

        builder.HasOne(c => c.Promoter)
            .WithMany()
            .HasForeignKey(c => c.PromoterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.PromoCode)
            .WithMany()
            .HasForeignKey(c => c.PromoCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Order)
            .WithMany()
            .HasForeignKey(c => c.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.PromoterId);
        builder.HasIndex(c => c.Status);
        // One promo code per order (Phase 3's own rule) means one commission per order too.
        builder.HasIndex(c => c.OrderId).IsUnique();
    }
}

public class PromoterPaymentConfiguration : IEntityTypeConfiguration<PromoterPayment>
{
    public void Configure(EntityTypeBuilder<PromoterPayment> builder)
    {
        builder.ToTable("promoter_payments");

        builder.Property(p => p.PaymentReference).HasMaxLength(40);
        builder.Property(p => p.TotalPaymentAmount).HasPrecision(12, 2);
        builder.Property(p => p.PaymentMethod).HasMaxLength(50);
        builder.Property(p => p.TransactionReference).HasMaxLength(100);
        builder.Property(p => p.Remarks).HasMaxLength(1000);

        builder.HasOne(p => p.Promoter)
            .WithMany()
            .HasForeignKey(p => p.PromoterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.PaymentReference).IsUnique();
        builder.HasIndex(p => p.PromoterId);
        builder.HasIndex(p => p.Status);
    }
}

public class PromoterPaymentAllocationConfiguration : IEntityTypeConfiguration<PromoterPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PromoterPaymentAllocation> builder)
    {
        builder.ToTable("promoter_payment_allocations");

        builder.Property(a => a.AllocatedAmount).HasPrecision(12, 2);

        builder.HasOne(a => a.PromoterPayment)
            .WithMany(p => p.Allocations)
            .HasForeignKey(a => a.PromoterPaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.PromoterCommission)
            .WithMany()
            .HasForeignKey(a => a.PromoterCommissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.PromoterPaymentId);
        builder.HasIndex(a => a.PromoterCommissionId);
    }
}

public class PromoCodeUsageConfiguration : IEntityTypeConfiguration<PromoCodeUsage>
{
    public void Configure(EntityTypeBuilder<PromoCodeUsage> builder)
    {
        builder.ToTable("promo_code_usages");

        builder.Property(u => u.DiscountAmount).HasPrecision(12, 2);

        builder.HasOne(u => u.PromoCode)
            .WithMany()
            .HasForeignKey(u => u.PromoCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Promoter)
            .WithMany()
            .HasForeignKey(u => u.PromoterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Customer)
            .WithMany()
            .HasForeignKey(u => u.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Order)
            .WithMany()
            .HasForeignKey(u => u.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.PromoCodeId);
        builder.HasIndex(u => new { u.PromoCodeId, u.CustomerId });
        builder.HasIndex(u => u.PromoterId);
        builder.HasIndex(u => u.OrderId).IsUnique();
    }
}
