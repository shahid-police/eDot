using System.Text;
using Elegance.Api.Auth;
using Elegance.Api.Middleware;
using Elegance.Infrastructure;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Persistence;
using Elegance.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new Elegance.Api.Json.UtcDateTimeConverter());
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

// Encrypts admin TOTP secrets at rest (see TwoFactorSecretProtector). The key ring is the only
// thing that can decrypt those secrets, so it must live outside the database and survive
// restarts/redeploys: a database copy without the key ring cannot produce valid codes, and
// losing the key ring means every enrolled authenticator must be re-enrolled (there is no
// plaintext fallback). In production, point DataProtection:KeyPath at a persistent,
// access-controlled volume or secrets mount (or swap in a cloud KMS-backed provider) rather than
// using the default below, which is fine for single-instance local development only.
var dataProtectionKeyPath = builder.Configuration["DataProtection:KeyPath"]
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Elegance", "dataprotection-keys");
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("Elegance.Admin")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));

// Without this, each key in the ring above is written to disk unencrypted (DataProtection
// authenticates and encrypts application data with the key, but by default does nothing to
// protect the key file itself). DPAPI ties it to this Windows machine/user, which is right for
// local dev but not portable - a Linux/container production deployment needs
// ProtectKeysWithCertificate(...) or a cloud KMS provider instead, so the key ring survives
// redeploys and isn't tied to one host's DPAPI profile.
if (OperatingSystem.IsWindows())
{
    dataProtectionBuilder.ProtectKeysWithDpapi();
}

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
    });
builder.Services.AddAuthorization(options => options.AddAdminPolicies());

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationFailureExceptionHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:AuthRequestsPerMinute", 10),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
    // Each request costs a real LLM API call, so this is capped tighter than plain product
    // browsing to keep abuse from driving up AI spend.
    options.AddPolicy("ai-search", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:AiSearchRequestsPerMinute", 15),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
    // A public, unauthenticated form that writes a row and (eventually) costs a WhatsApp send -
    // capped to keep it from being used to spam numbers or flood the notification queue.
    options.AddPolicy("back-in-stock", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:BackInStockRequestsPerMinute", 10),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
    // A 6-digit code is only 1,000,000 possibilities - capped per IP so it can't be brute-forced
    // by guessing codes quickly, not just to stop spam like the policies above.
    options.AddPolicy("promo-code", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:PromoCodeRequestsPerMinute", 10),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EleganceDbContext>();
    await DevelopmentSeeder.SeedAsync(dbContext);
    await DevelopmentAdminSeeder.SeedAsync(dbContext, app.Configuration["Seed:AdminEmail"], app.Configuration["Seed:AdminPassword"]);
    await CatalogDemoSeeder.SeedAsync(dbContext, app.Configuration.GetValue("Seed:LoadDemoCatalog", false));
    await NoShopCategorySeeder.SeedAsync(dbContext);
}

app.UseExceptionHandler();

// nginx terminates TLS and proxies to Kestrel over plain HTTP, so without this Request.IsHttps
// (and anything that keys off it, like the Secure cookie flag) always sees the internal HTTP
// hop instead of what the client actually connected with.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();

// Serves wwwroot/uploads when Storage:Provider is "Local" (development only - see
// Elegance.Infrastructure/Media/LocalDiskFileStorage.cs). Harmless, and unused, under "R2".
// Every upload gets a fresh GUID filename and a changed product/variant always re-uploads under a
// new key rather than overwriting one in place, so a given URL's bytes never change - safe to
// cache aggressively and immutably.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    },
});

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// Exposes the top-level-statement Program for WebApplicationFactory<Program> in Elegance.Tests.
public partial class Program { }
