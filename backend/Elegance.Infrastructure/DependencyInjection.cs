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

        return services;
    }
}
