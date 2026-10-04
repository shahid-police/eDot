using Elegance.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Label).HasMaxLength(50);
        builder.Property(a => a.FullName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Phone).HasMaxLength(20).IsRequired();
        builder.Property(a => a.AddressLine1).HasMaxLength(200).IsRequired();
        builder.Property(a => a.AddressLine2).HasMaxLength(200);
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Province).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(20);
        builder.Property(a => a.Country).HasMaxLength(2).IsRequired();

        builder.HasOne(a => a.Customer)
            .WithMany(c => c.Addresses)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.CustomerId);
    }
}
