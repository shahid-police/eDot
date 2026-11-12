using Elegance.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class NoShopCategoryConfiguration : IEntityTypeConfiguration<NoShopCategory>
{
    public void Configure(EntityTypeBuilder<NoShopCategory> builder)
    {
        builder.ToTable("no_shop_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();

        builder.Property(c => c.Slug).HasMaxLength(220).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasOne(c => c.ImageMediaFile)
            .WithMany()
            .HasForeignKey(c => c.ImageMediaFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.IsActive);
    }
}
