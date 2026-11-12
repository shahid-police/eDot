using Elegance.Domain.Marketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class PromoterConfiguration : IEntityTypeConfiguration<Promoter>
{
    public void Configure(EntityTypeBuilder<Promoter> builder)
    {
        builder.ToTable("promoters");

        builder.Property(p => p.Name).HasMaxLength(200);
        builder.Property(p => p.WhatsAppNumber).HasMaxLength(32);
        builder.Property(p => p.Email).HasMaxLength(254);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.CommissionPercentage).HasPrecision(5, 2);

        builder.HasIndex(p => p.Status);
    }
}
