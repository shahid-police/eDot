using Elegance.Application.Admin;
using Elegance.Application.Auth;
using Elegance.Application.Carts;
using Elegance.Infrastructure.Admin;
using Elegance.Infrastructure.Media;
using Elegance.Application.Checkout;
using Elegance.Application.Payments;
using Elegance.Application.Compare;
using Elegance.Application.Categories;
using Elegance.Application.Customers;
using Elegance.Application.Products;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Carts;
using Elegance.Infrastructure.Checkout;
using Elegance.Infrastructure.Orders;
using Elegance.Infrastructure.Payments;
using Elegance.Infrastructure.Compare;
using Elegance.Infrastructure.Catalog;
using Elegance.Infrastructure.Customers;
using Elegance.Infrastructure.Wishlists;
using Elegance.Application.Wishlists;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elegance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Missing ConnectionStrings:DefaultConnection. Set it via appsettings, user-secrets, or the ConnectionStrings__DefaultConnection environment variable.");

        var serverVersion = new MySqlServerVersion(new Version(8, 4, 0));

        services.AddDbContext<EleganceDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));

        services.AddHealthChecks()
            .AddDbContextCheck<EleganceDbContext>("database");

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => options.Key.Length >= 32, "Jwt:Key must be set to at least 32 characters.")
            .ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<TokenService>();
        services.AddSingleton<TotpService>();
        services.AddSingleton<TwoFactorSecretProtector>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<ICompareService, CompareService>();
        services.AddOptions<CheckoutOptions>()
            .Bind(configuration.GetSection(CheckoutOptions.SectionName));
        services.AddSingleton<IPaymentGateway, CashOnDeliveryGateway>();
        services.AddSingleton<IPaymentGateway, BankTransferGateway>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<OrderDetailBuilder>();

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IAdminTwoFactorService, AdminTwoFactorService>();
        services.AddScoped<IAdminCatalogService, AdminCatalogService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAdminInventoryService, AdminInventoryService>();
        services.AddScoped<IAdminCustomerService, AdminCustomerService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName));
        services.AddScoped<IFileStorage>(sp =>
        {
            var storageOptions = sp.GetRequiredService<IOptions<StorageOptions>>();
            return storageOptions.Value.Provider.Equals("R2", StringComparison.OrdinalIgnoreCase)
                ? new R2FileStorage(storageOptions)
                : new LocalDiskFileStorage(sp.GetRequiredService<IHostEnvironment>(), storageOptions);
        });
        services.AddScoped<IAdminMediaService, AdminMediaService>();

        return services;
    }
}
