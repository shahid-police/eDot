using Elegance.Domain.Admin;
using Elegance.Domain.Audit;
using Elegance.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
    }
}

public class AdminRoleAssignmentConfiguration : IEntityTypeConfiguration<AdminRoleAssignment>
{
    public void Configure(EntityTypeBuilder<AdminRoleAssignment> builder)
    {
        builder.ToTable("admin_role_assignments");
        builder.HasKey(r => new { r.AdminUserId, r.Role });

        builder.Property(r => r.Role).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(r => r.AdminUser)
            .WithMany(u => u.Roles)
            .HasForeignKey(r => r.AdminUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AdminTwoFactorCredentialConfiguration : IEntityTypeConfiguration<AdminTwoFactorCredential>
{
    public void Configure(EntityTypeBuilder<AdminTwoFactorCredential> builder)
    {
        builder.ToTable("admin_two_factor_credentials");
        builder.HasKey(c => c.AdminUserId);

        builder.Property(c => c.EncryptedSecret).HasColumnType("text").IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(c => c.AdminUser)
            .WithOne()
            .HasForeignKey<AdminTwoFactorCredential>(c => c.AdminUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdminEmail).HasMaxLength(254).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(64);
        builder.Property(a => a.Summary).HasMaxLength(1000).IsRequired();

        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.AdminUserId);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Reason).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Note).HasMaxLength(500);

        builder.HasOne<Elegance.Domain.Catalog.Product>()
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.ProductId);
        builder.HasIndex(m => m.CreatedAt);
    }
}
