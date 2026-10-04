using Elegance.Domain.Catalog;
using Elegance.Domain.Customers;
using Elegance.Domain.Media;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Persistence;

public class EleganceDbContext : DbContext
{
    public EleganceDbContext(DbContextOptions<EleganceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EleganceDbContext).Assembly);
    }
}
