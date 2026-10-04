using Elegance.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();

        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
    }
}
