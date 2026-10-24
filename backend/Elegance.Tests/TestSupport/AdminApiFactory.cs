using Elegance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elegance.Tests.TestSupport;

// Boots the real ASP.NET pipeline (auth, authorization policies, rate limiting, exception
// handling) exactly as Program.cs wires it, but swaps the MySQL connection for an isolated
// in-memory SQLite database and skips the Development-only seeders (environment is "Testing",
// so Program.cs's `if (app.Environment.IsDevelopment())` block never runs).
public sealed class AdminApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _anchorConnection;
    public int AuthRequestsPerMinute { get; init; } = 100;

    public AdminApiFactory()
    {
        _anchorConnection = new SqliteConnection(_connectionString);
        _anchorConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs reads Jwt:Key and ConnectionStrings:DefaultConnection into local variables
        // *before* builder.Build() runs. WebApplicationFactory's ConfigureAppConfiguration only
        // takes effect as part of Build() itself, which is too late for anything already read
        // out of builder.Configuration by then - so those two values must come from environment
        // variables instead, since AddEnvironmentVariables() is wired in at CreateBuilder() time,
        // before Program.cs's own code runs at all. This method (unlike the constructor) runs
        // after any object-initializer properties (e.g. AuthRequestsPerMinute) have been set.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "unused-sqlite-override-below");
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-signing-key-at-least-32-characters-long");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "elegance-api-test");
        Environment.SetEnvironmentVariable("Jwt__Audience", "elegance-web-test");
        Environment.SetEnvironmentVariable("RateLimiting__AuthRequestsPerMinute", AuthRequestsPerMinute.ToString());
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:3000");

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // AddInfrastructure already called AddDbContext for EleganceDbContext (pointed at
            // MySQL). A second AddDbContext call does not replace that configuration - EF Core
            // composes every registered IDbContextOptionsConfiguration<T> - so both the old
            // (empty-string) MySQL config and this one would run. Removing both registration
            // kinds is required to actually replace it, not just add to it.
            services.RemoveAll<DbContextOptions<EleganceDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<EleganceDbContext>>();
            services.AddDbContext<EleganceDbContext>(options => options.UseSqlite(_connectionString));

            // Keep test runs hermetic: no key files written to the real machine's profile.
            services.RemoveAll<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
            services.AddSingleton<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>(
                new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EleganceDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _anchorConnection.Dispose();
            foreach (var name in new[] { "ConnectionStrings__DefaultConnection", "Jwt__Key", "Jwt__Issuer", "Jwt__Audience", "RateLimiting__AuthRequestsPerMinute", "Cors__AllowedOrigins__0" })
            {
                Environment.SetEnvironmentVariable(name, null);
            }
        }
    }
}
