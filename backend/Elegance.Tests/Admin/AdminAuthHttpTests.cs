using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Elegance.Application.Admin;
using Elegance.Infrastructure.Persistence;
using Elegance.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;

namespace Elegance.Tests.Admin;

// HTTP-level tests against the real pipeline (JwtBearer authentication, AdminPolicies
// authorization, the rate limiter, the exception handler) - the layer unit tests on the services
// alone cannot cover. Each test gets its own factory/database so they can run independently.
public class AdminAuthHttpTests : IAsyncDisposable
{
    private const string Password = "Correct-Horse-Battery-Staple-1";
    private readonly AdminApiFactory _factory = new();

    private async Task<HttpClient> CreateClientAsync()
    {
        await _factory.InitializeDatabaseAsync();
        return _factory.CreateClient();
    }

    private async Task<long> CreateAdminAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EleganceDbContext>();
        var admin = await TestAdmin.CreateAsync(db, email: email, password: Password);
        return admin.Id;
    }

    [Fact]
    public async Task Unauthenticated_requests_cannot_reach_a_protected_admin_endpoint()
    {
        var client = await CreateClientAsync();

        var response = await client.GetAsync("/api/v1/admin/2fa/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_without_two_factor_returns_a_usable_bearer_token()
    {
        var client = await CreateClientAsync();
        await CreateAdminAsync("a@test.local");

        var loginResponse = await client.PostAsJsonAsync("/api/v1/admin/auth/login", new { email = "a@test.local", password = Password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<AdminLoginResult>();
        Assert.NotNull(login!.Session);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Session!.AccessToken);
        var statusResponse = await client.GetAsync("/api/v1/admin/2fa/status");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
    }

    [Fact]
    public async Task A_two_factor_pending_token_is_rejected_by_every_protected_admin_endpoint()
    {
        var client = await CreateClientAsync();
        var adminId = await CreateAdminAsync("a@test.local");
        var manualKey = await EnrollAsync(client, adminId, "a@test.local");

        var loginResponse = await client.PostAsJsonAsync("/api/v1/admin/auth/login", new { email = "a@test.local", password = Password });
        var login = await loginResponse.Content.ReadFromJsonAsync<AdminLoginResult>();
        Assert.Null(login!.Session);
        Assert.NotNull(login.TwoFactor);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.TwoFactor!.PendingToken);

        // The pending token must not work as a bearer token anywhere admin-protected -
        // including the very endpoints this feature itself introduces. It authenticates (it's a
        // validly signed token) but fails AdminPolicies' RequireClaim(typ, "admin") check, so
        // ASP.NET Core correctly reports 403 Forbidden here rather than 401 Unauthorized - the
        // behavior that matters is that neither call reaches the controller action.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/2fa/status")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/admin/2fa/enroll/start", new { password = Password })).StatusCode);

        _ = manualKey; // enrollment already completed above; kept for clarity of intent
    }

    [Fact]
    public async Task Full_login_then_two_factor_flow_succeeds_over_real_http_and_the_resulting_token_works()
    {
        var client = await CreateClientAsync();
        var adminId = await CreateAdminAsync("a@test.local");
        var manualKey = await EnrollAsync(client, adminId, "a@test.local");

        var loginResponse = await client.PostAsJsonAsync("/api/v1/admin/auth/login", new { email = "a@test.local", password = Password });
        var login = (await loginResponse.Content.ReadFromJsonAsync<AdminLoginResult>())!;
        var code = NextStepCode(manualKey);

        var verifyResponse = await client.PostAsJsonAsync("/api/v1/admin/auth/2fa/verify", new { pendingToken = login.TwoFactor!.PendingToken, code });
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        var session = await verifyResponse.Content.ReadFromJsonAsync<AdminSession>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        var statusResponse = await client.GetAsync("/api/v1/admin/2fa/status");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var status = await statusResponse.Content.ReadFromJsonAsync<TwoFactorStatusDto>();
        Assert.True(status!.Enabled);
    }

    [Fact]
    public async Task Repeated_failed_logins_are_rate_limited()
    {
        await using var factory = new AdminApiFactory { AuthRequestsPerMinute = 3 };
        await factory.InitializeDatabaseAsync();
        var client = factory.CreateClient();

        HttpStatusCode? lastStatus = null;
        for (var i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/admin/auth/login", new { email = "nobody@test.local", password = "wrong" });
            lastStatus = response.StatusCode;
            if (lastStatus == HttpStatusCode.TooManyRequests)
            {
                break;
            }
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }

    private async Task<string> EnrollAsync(HttpClient client, long adminId, string email)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/v1/admin/auth/login", new { email, password = Password });
        var login = (await loginResponse.Content.ReadFromJsonAsync<AdminLoginResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Session!.AccessToken);

        var startResponse = await client.PostAsJsonAsync("/api/v1/admin/2fa/enroll/start", new { password = Password });
        var enrollment = (await startResponse.Content.ReadFromJsonAsync<TwoFactorEnrollmentDto>())!;
        var code = new Totp(Base32Encoding.ToBytes(enrollment.ManualEntryKey)).ComputeTotp();

        var confirmResponse = await client.PostAsJsonAsync("/api/v1/admin/2fa/enroll/verify", new { code });
        Assert.Equal(HttpStatusCode.NoContent, confirmResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        return enrollment.ManualEntryKey;
    }

    // A code for the *next* 30s step, computed from the real clock with no server-side clock
    // manipulation. The server's own TOTP verification window tolerates +1 step into the future
    // (ordinary clock-drift allowance between an authenticator app and the server), so this is
    // accepted - and, since it is a different step than whatever EnrollAsync just consumed to
    // confirm enrollment, it also is not rejected as a replay.
    private static string NextStepCode(string manualEntryKey) =>
        new Totp(Base32Encoding.ToBytes(manualEntryKey)).ComputeTotp(DateTime.UtcNow.AddSeconds(30));

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}
