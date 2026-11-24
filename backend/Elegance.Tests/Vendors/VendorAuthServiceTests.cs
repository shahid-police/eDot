using Elegance.Application.Common;
using Elegance.Application.Vendors;
using Elegance.Domain.Vendors;
using Elegance.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace Elegance.Tests.Vendors;

public class VendorAuthServiceTests
{
    private const string Password = "Correct-Horse-Battery-Staple-1";

    [Fact]
    public async Task Login_with_wrong_password_fails()
    {
        using var ctx = new VendorAuthTestContext();
        await TestVendor.CreateAsync(ctx.Db.Context, password: Password);

        await Assert.ThrowsAsync<ApplicationFailure>(() =>
            ctx.AuthService.LoginAsync(new VendorLoginRequest { Email = "vendor@test.local", Password = "wrong" }));
    }

    [Fact]
    public async Task Login_never_issues_a_real_session_directly_even_with_the_correct_password()
    {
        using var ctx = new VendorAuthTestContext();
        await TestVendor.CreateAsync(ctx.Db.Context, password: Password);

        var result = await ctx.AuthService.LoginAsync(new VendorLoginRequest { Email = "vendor@test.local", Password = Password });

        Assert.NotNull(result.TwoFactor);
        Assert.True(result.TwoFactor.RequiresEnrollment);
    }

    [Fact]
    public async Task A_brand_new_vendor_must_enroll_before_a_pending_token_yields_a_session()
    {
        using var ctx = new VendorAuthTestContext();
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context, password: Password);
        var login = await ctx.AuthService.LoginAsync(new VendorLoginRequest { Email = vendor.Email, Password = Password });

        var enrollment = await ctx.AuthService.StartEnrollmentAsync(new VendorStartTwoFactorEnrollmentRequest { PendingToken = login.TwoFactor.PendingToken });
        var code = ComputeCode(enrollment.ManualEntryKey, ctx.Clock.GetUtcNow().UtcDateTime);

        var session = await ctx.AuthService.ConfirmEnrollmentAsync(new VendorConfirmTwoFactorEnrollmentRequest
        {
            PendingToken = login.TwoFactor.PendingToken,
            Code = code,
        });

        Assert.Equal(vendor.Id, session.Vendor.Id);
        Assert.NotEmpty(session.AccessToken);

        var credential = await ctx.Db.Context.VendorTwoFactorCredentials.SingleAsync(c => c.VendorId == vendor.Id);
        Assert.Equal(VendorTwoFactorStatus.Active, credential.Status);
    }

    [Fact]
    public async Task Once_enrolled_a_later_login_requires_verify_not_enrollment()
    {
        using var ctx = new VendorAuthTestContext();
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context, password: Password);
        await EnrollAsync(ctx, vendor.Email);

        var login = await ctx.AuthService.LoginAsync(new VendorLoginRequest { Email = vendor.Email, Password = Password });

        Assert.False(login.TwoFactor.RequiresEnrollment);
    }

    [Fact]
    public async Task VerifyTwoFactor_with_the_wrong_code_does_not_issue_a_session()
    {
        using var ctx = new VendorAuthTestContext();
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context, password: Password);
        await EnrollAsync(ctx, vendor.Email);
        var login = await ctx.AuthService.LoginAsync(new VendorLoginRequest { Email = vendor.Email, Password = Password });

        await Assert.ThrowsAsync<ApplicationFailure>(() =>
            ctx.AuthService.VerifyTwoFactorAsync(new VerifyVendorTwoFactorRequest { PendingToken = login.TwoFactor.PendingToken, Code = "000000" }));
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_reuse_of_the_old_one_is_rejected()
    {
        using var ctx = new VendorAuthTestContext();
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context, password: Password);
        var session = await EnrollAsync(ctx, vendor.Email);

        var refreshed = await ctx.AuthService.RefreshAsync(session.RefreshToken);
        Assert.NotEqual(session.RefreshToken, refreshed.RefreshToken);

        // Reusing the original (now-rotated) refresh token must fail.
        await Assert.ThrowsAsync<ApplicationFailure>(() => ctx.AuthService.RefreshAsync(session.RefreshToken));
    }

    [Fact]
    public async Task A_suspended_vendor_cannot_log_in()
    {
        using var ctx = new VendorAuthTestContext();
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context, password: Password, status: VendorStatus.Suspended);

        await Assert.ThrowsAsync<ApplicationFailure>(() =>
            ctx.AuthService.LoginAsync(new VendorLoginRequest { Email = vendor.Email, Password = Password }));
    }

    private async Task<VendorSession> EnrollAsync(VendorAuthTestContext ctx, string email)
    {
        var login = await ctx.AuthService.LoginAsync(new VendorLoginRequest { Email = email, Password = Password });
        var enrollment = await ctx.AuthService.StartEnrollmentAsync(new VendorStartTwoFactorEnrollmentRequest { PendingToken = login.TwoFactor.PendingToken });
        var code = ComputeCode(enrollment.ManualEntryKey, ctx.Clock.GetUtcNow().UtcDateTime);
        return await ctx.AuthService.ConfirmEnrollmentAsync(new VendorConfirmTwoFactorEnrollmentRequest
        {
            PendingToken = login.TwoFactor.PendingToken,
            Code = code,
        });
    }

    private static string ComputeCode(string base32Secret, DateTime at) =>
        new Totp(Base32Encoding.ToBytes(base32Secret)).ComputeTotp(at);
}
