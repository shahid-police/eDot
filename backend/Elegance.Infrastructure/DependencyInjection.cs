using Elegance.Application.Auth;
using Elegance.Application.Carts;
using Elegance.Application.Categories;
using Elegance.Application.Customers;
using Elegance.Application.Products;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Carts;
using Elegance.Infrastructure.Catalog;
using Elegance.Infrastructure.Customers;
using Elegance.Infrastructure.Wishlists;
using Elegance.Application.Wishlists;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IWishlistService, WishlistService>();

        return services;
    }
}
