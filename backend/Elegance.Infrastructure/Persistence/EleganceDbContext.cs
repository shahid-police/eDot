using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Persistence;

public class EleganceDbContext : DbContext
{
    public EleganceDbContext(DbContextOptions<EleganceDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
