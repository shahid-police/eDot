using Elegance.Domain.Admin;
using Elegance.Domain.Audit;
using Elegance.Domain.Carts;
using Elegance.Domain.Catalog;
using Elegance.Domain.Compare;
using Elegance.Domain.Content;
using Elegance.Domain.Customers;
using Elegance.Domain.Discounts;
using Elegance.Domain.Inventory;
using Elegance.Domain.Marketing;
using Elegance.Domain.Media;
using Elegance.Domain.Notifications;
using Elegance.Domain.Orders;
using Elegance.Domain.Vendors;
using Elegance.Domain.Wishlists;
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
    public DbSet<ProductDiscount> ProductDiscounts => Set<ProductDiscount>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<CompareList> CompareLists => Set<CompareList>();
    public DbSet<CompareItem> CompareItems => Set<CompareItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderAddress> OrderAddresses => Set<OrderAddress>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminRoleAssignment> AdminRoleAssignments => Set<AdminRoleAssignment>();
    public DbSet<AdminTwoFactorCredential> AdminTwoFactorCredentials => Set<AdminTwoFactorCredential>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<RestockEvent> RestockEvents => Set<RestockEvent>();
    public DbSet<NoShopCategory> NoShopCategories => Set<NoShopCategory>();
    public DbSet<NoShopVideo> NoShopVideos => Set<NoShopVideo>();
    public DbSet<BackInStockSubscription> BackInStockSubscriptions => Set<BackInStockSubscription>();
    public DbSet<WhatsAppNotification> WhatsAppNotifications => Set<WhatsAppNotification>();
    public DbSet<Promoter> Promoters => Set<Promoter>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<PromoCodeProduct> PromoCodeProducts => Set<PromoCodeProduct>();
    public DbSet<PromoCodeCategory> PromoCodeCategories => Set<PromoCodeCategory>();
    public DbSet<PromoCodeUsage> PromoCodeUsages => Set<PromoCodeUsage>();
    public DbSet<PromoterCommission> PromoterCommissions => Set<PromoterCommission>();
    public DbSet<PromoterPayment> PromoterPayments => Set<PromoterPayment>();
    public DbSet<PromoterPaymentAllocation> PromoterPaymentAllocations => Set<PromoterPaymentAllocation>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorTwoFactorCredential> VendorTwoFactorCredentials => Set<VendorTwoFactorCredential>();
    public DbSet<VendorRefreshToken> VendorRefreshTokens => Set<VendorRefreshToken>();
    public DbSet<VendorProductSubmission> VendorProductSubmissions => Set<VendorProductSubmission>();
    public DbSet<VendorProductImage> VendorProductImages => Set<VendorProductImage>();
    public DbSet<VendorProductSubmissionHistory> VendorProductSubmissionHistory => Set<VendorProductSubmissionHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EleganceDbContext).Assembly);
    }
}
