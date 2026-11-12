using Elegance.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class RestockEventConfiguration : IEntityTypeConfiguration<RestockEvent>
{
    public void Configure(EntityTypeBuilder<RestockEvent> builder)
    {
        builder.ToTable("restock_events");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.VariantId);
        builder.HasIndex(e => e.DetectedAt);
        builder.HasIndex(e => e.ProcessedAt);
    }
}
