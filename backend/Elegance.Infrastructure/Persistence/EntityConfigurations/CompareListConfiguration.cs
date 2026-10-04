using Elegance.Domain.Compare;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class CompareListConfiguration : IEntityTypeConfiguration<CompareList>
{
    public void Configure(EntityTypeBuilder<CompareList> builder)
    {
        builder.ToTable("compare_lists");

        builder.HasKey(c => c.Id);

        builder.HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.CustomerId).IsUnique();

        builder.Property(c => c.GuestTokenHash).HasMaxLength(128);
        builder.HasIndex(c => c.GuestTokenHash).IsUnique();
    }
}
