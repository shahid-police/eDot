using Elegance.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();

        builder.Property(o => o.Email).HasMaxLength(254).IsRequired();
        builder.Property(o => o.Phone).HasMaxLength(20).IsRequired();
        builder.Property(o => o.Subtotal).HasPrecision(12, 2);
        builder.Property(o => o.ShippingTotal).HasPrecision(12, 2);
        builder.Property(o => o.TaxTotal).HasPrecision(12, 2);
        builder.Property(o => o.GrandTotal).HasPrecision(12, 2);
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(o => o.Notes).HasMaxLength(1000);

        builder.HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.GuestTokenHash).HasMaxLength(128);
        builder.HasIndex(o => o.GuestTokenHash);
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CreatedAt);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Sku).HasMaxLength(64).IsRequired();
        builder.Property(i => i.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(i => i.VariantName).HasMaxLength(200);
        builder.Property(i => i.UnitPrice).HasPrecision(12, 2);
        builder.Property(i => i.LineTotal).HasPrecision(12, 2);

        builder.HasOne(i => i.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.OrderId);
    }
}

public class OrderAddressConfiguration : IEntityTypeConfiguration<OrderAddress>
{
    public void Configure(EntityTypeBuilder<OrderAddress> builder)
    {
        builder.ToTable("order_addresses");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.FullName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Phone).HasMaxLength(20).IsRequired();
        builder.Property(a => a.AddressLine1).HasMaxLength(200).IsRequired();
        builder.Property(a => a.AddressLine2).HasMaxLength(200);
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Province).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(20);
        builder.Property(a => a.Country).HasMaxLength(2).IsRequired();

        builder.HasOne(a => a.Order)
            .WithMany(o => o.Addresses)
            .HasForeignKey(a => a.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(h => h.Note).HasMaxLength(500);

        builder.HasOne(h => h.Order)
            .WithMany(o => o.StatusHistory)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.OrderId);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Amount).HasPrecision(12, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();

        builder.HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.OrderId);
    }
}
