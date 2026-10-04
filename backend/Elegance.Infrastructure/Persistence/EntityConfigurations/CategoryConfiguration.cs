using Elegance.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();

        builder.Property(c => c.Slug).HasMaxLength(220).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Property(c => c.SeoTitle).HasMaxLength(200);
        builder.Property(c => c.SeoDescription).HasMaxLength(500);

        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.ChildCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ImageMediaFile)
            .WithMany()
            .HasForeignKey(c => c.ImageMediaFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.ParentCategoryId);
        builder.HasIndex(c => c.IsActive);
    }
}
