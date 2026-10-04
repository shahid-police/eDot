using Elegance.Domain.Compare;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class CompareItemConfiguration : IEntityTypeConfiguration<CompareItem>
{
    public void Configure(EntityTypeBuilder<CompareItem> builder)
    {
        builder.ToTable("compare_items");

        builder.HasKey(i => i.Id);

        builder.HasOne(i => i.CompareList)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CompareListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.CompareListId, i.ProductId }).IsUnique();
    }
}
