using Elegance.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(r => r.TokenHash).IsUnique();

        builder.Property(r => r.ReplacedByTokenHash).HasMaxLength(128);

        builder.HasOne(r => r.Customer)
            .WithMany(c => c.RefreshTokens)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.CustomerId);
    }
}
