using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Tests.TestSupport;
using OtpNet;

namespace Elegance.Tests.Admin;

public class AdminAuthServiceTests
{
    private const string Password = "Correct-Horse-Battery-Staple-1";

    [Fact]
    public async Task Login_without_two_factor_enrolled_returns_a_real_session_directly()
    {
        using var ctx = new AdminAuthTestContext();
        await TestAdmin.CreateAsync(ctx.Db.Context, email: "a@test.local", password: Password);

        var result = await ctx.AuthService.LoginAsync(new AdminLoginRequest { Email = "a@test.local", Password = Password });

        Assert.NotNull(result.Session);
        Assert.Null(result.TwoFactor);
    }

    [Fact]
    public async Task Login_rejects_a_wrong_password()
    {
        using var ctx = new AdminAuthTestContext();
        await TestAdmin.CreateAsync(ctx.Db.Context, email: "a@test.local", password: Password);

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() =>
            ctx.AuthService.LoginAsync(new AdminLoginRequest { Email = "a@test.local", Password = "wrong" }));

        Assert.Equal(FailureKind.Unauthorized, ex.Kind);
    }

    [Fact]
    public async Task Login_for_a_two_factor_enrolled_admin_returns_a_pending_challenge_not_a_session()
    {
        using var ctx = new AdminAuthTestContext();
        var admin = await EnrollTwoFactorAsync(ctx, "a@test.local");

        var result = await ctx.AuthService.LoginAsync(new AdminLoginRequest { Email = "a@test.local", Password = Password });

        Assert.Null(result.Session);
        Assert.NotNull(result.TwoFactor);
        Assert.NotEmpty(result.TwoFactor!.PendingToken);
    }

    [Fact]
    public async Task A_pending_token_alone_cannot_be_used_as_an_access_token()
    {
        // The pending token carries typ=admin-2fa-pending, not typ=admin. Confirms the claim an
        // AdminPolicies.RequireAdmin check relies on - see AdminAuthHttpTests for the full
        // HTTP-level proof that it is actually rejected by the real authorization pipeline.
        using var ctx = new AdminAuthTestContext();
        await EnrollTwoFactorAsync(ctx, "a@test.local");
        var login = await ctx.AuthService.LoginAsync(new AdminLoginRequest { Email = "a@test.local", Password = Password });

        var adminId = await ctx.TokenService.ValidateAdminPendingTwoFactorTokenAsync(login.TwoFactor!.PendingToken);

        Assert.NotNull(adminId);
        // And a real access token's claims never validate as a pending-2FA token.
        var realSession = await IssueSessionAsync(ctx, "b@test.local");
        Assert.Null(await ctx.TokenService.ValidateAdminPendingTwoFactorTokenAsync(realSession.AccessToken));
    }

    [Fact]
    public async Task VerifyTwoFactor_with_the_correct_code_completes_login()
    {
        using var ctx = new AdminAuthTestContext();
        var (admin, manualKey) = await EnrollTwoFactorWithKeyAsync(ctx, "a@test.local");
        var login = await ctx.AuthService.LoginAsync(new AdminLoginRequest { Email = "a@test.local", Password = Password });
        var code = ComputeCode(manualKey, ctx.Clock.GetUtcNow().UtcDateTime);

        var session = await ctx.AuthService.VerifyTwoFactorAsync(new VerifyAdminTwoFactorRequest
        {
            PendingToken = login.TwoFactor!.PendingToken,
            Code = code,
        });

        Assert.Equal(admin.Id, session.Admin.Id);
        Assert.NotEmpty(session.AccessToken);
    }

    [Fact]
    public async Task VerifyTwoFactor_rejects_an_invalid_code()
    {
        using var ctx = new AdminAuthTestContext();
        await EnrollTwoFactorAsync(ctx, "a@test.local");
        var login = await ctx.AuthService.LoginAsync(new AdminLoginRequest { Email = "a@test.local", Password = Password });

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() => ctx.AuthService.VerifyTwoFactorAsync(new VerifyAdminTwoFactorRequest
        {
            PendingToken = login.TwoFactor!.PendingToken,
            Code = "000000",
        }));

        Assert.Equal(FailureKind.Unauthorized, ex.Kind);
    }

    [Fact]
    public async Task VerifyTwoFactor_rejects_an_expired_pending_token()
    {
        using var ctx = new AdminAuthTestContext();
        var (_, manualKey) = await EnrollTwoFactorWithKeyAsync(ctx, "a@test.local");
        var login = await ctx.AuthService.LoginAsync(new AdminLoginRequest { Email = "a@test.local", Password = Password });

        ctx.Clock.Advance(TimeSpan.FromMinutes(10)); // pending token lifetime is 5 minutes

        var code = ComputeCode(manualKey, ctx.Clock.GetUtcNow().UtcDateTime);
        await Assert.ThrowsAsync<ApplicationFailure>(() => ctx.AuthService.VerifyTwoFactorAsync(new VerifyAdminTwoFactorRequest
        {
            PendingToken = login.TwoFactor!.PendingToken,
            Code = code,
        }));
    }

    [Fact]
    public async Task VerifyTwoFactor_cannot_replay_an_already_accepted_code()
    {
        using var ctx = new AdminAuthTestContext();
        var (_, manualKey) = await EnrollTwoFactorWithKeyAsync(ctx, "a@test.local");
        var login = await ctx.AuthService.LoginAsync(new AdminLoginRequest { Email = "a@test.local", Password = Password });
        var code = ComputeCode(manualKey, ctx.Clock.GetUtcNow().UtcDateTime);

        await ctx.AuthService.VerifyTwoFactorAsync(new VerifyAdminTwoFactorRequest { PendingToken = login.TwoFactor!.PendingToken, Code = code });

        // Same code again, against a fresh pending challenge for the same admin.
        var secondLogin = await ctx.AuthService.LoginAsync(new AdminLoginRequest { Email = "a@test.local", Password = Password });
        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() => ctx.AuthService.VerifyTwoFactorAsync(new VerifyAdminTwoFactorRequest
        {
            PendingToken = secondLogin.TwoFactor!.PendingToken,
            Code = code,
        }));
        Assert.Equal(FailureKind.Unauthorized, ex.Kind);
    }

    [Fact]
    public async Task Concurrent_verification_attempts_with_the_same_code_let_exactly_one_through()
    {
        // Two independent AdminAuthService instances, each with its own DbContext/connection
        // against the same underlying database - genuine concurrency, not two calls sharing one
        // (non-thread-safe) DbContext. Proves the atomic ExecuteUpdateAsync guard in
        // AdminAuthService.AcceptTotpCodeAsync, not just sequential replay rejection.
        using var ctx = new AdminAuthTestContext();
        var (_, manualKey) = await EnrollTwoFactorWithKeyAsync(ctx, "a@test.local");
        var login = await ctx.AuthService.LoginAsync(new AdminLoginRequest { Email = "a@test.local", Password = Password });
        var code = ComputeCode(manualKey, ctx.Clock.GetUtcNow().UtcDateTime);

        var serviceA = ctx.AuthService;
        var serviceB = ctx.CreateAdditionalAuthService();

        var attempt1 = SafeVerifyAsync(serviceA, login.TwoFactor!.PendingToken, code);
        var attempt2 = SafeVerifyAsync(serviceB, login.TwoFactor!.PendingToken, code);
        var results = await Task.WhenAll(attempt1, attempt2);

        Assert.Single(results, ok => ok);
    }

    private static async Task<bool> SafeVerifyAsync(IAdminAuthService service, string pendingToken, string code)
    {
        try
        {
            await service.VerifyTwoFactorAsync(new VerifyAdminTwoFactorRequest { PendingToken = pendingToken, Code = code });
            return true;
        }
        catch (ApplicationFailure)
        {
            return false;
        }
    }

    private static async Task<Elegance.Domain.Admin.AdminUser> EnrollTwoFactorAsync(AdminAuthTestContext ctx, string email)
    {
        var (admin, _) = await EnrollTwoFactorWithKeyAsync(ctx, email);
        return admin;
    }

    private static async Task<(Elegance.Domain.Admin.AdminUser Admin, string ManualKey)> EnrollTwoFactorWithKeyAsync(AdminAuthTestContext ctx, string email)
    {
        var admin = await TestAdmin.CreateAsync(ctx.Db.Context, email: email, password: Password);
        var enrollment = await ctx.TwoFactorService.StartEnrollmentAsync(admin.Id, new StartTwoFactorEnrollmentRequest { Password = Password });
        var code = ComputeCode(enrollment.ManualEntryKey, ctx.Clock.GetUtcNow().UtcDateTime);
        await ctx.TwoFactorService.ConfirmEnrollmentAsync(admin.Id, new VerifyTwoFactorEnrollmentRequest { Code = code });
        // Move to a fresh time step so a code computed by the caller right after this returns
        // isn't an immediate replay of the one just consumed by ConfirmEnrollmentAsync.
        ctx.Clock.Advance(TimeSpan.FromSeconds(31));
        return (admin, enrollment.ManualEntryKey);
    }

    private static async Task<AdminSession> IssueSessionAsync(AdminAuthTestContext ctx, string email)
    {
        await TestAdmin.CreateAsync(ctx.Db.Context, email: email, password: Password);
        var result = await ctx.AuthService.LoginAsync(new AdminLoginRequest { Email = email, Password = Password });
        return result.Session!;
    }

    private static string ComputeCode(string base32Secret, DateTime at) =>
        new Totp(Base32Encoding.ToBytes(base32Secret)).ComputeTotp(at);
}
