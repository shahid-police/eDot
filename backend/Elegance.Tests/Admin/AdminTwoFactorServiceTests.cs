using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Admin;
using Elegance.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace Elegance.Tests.Admin;

public class AdminTwoFactorServiceTests
{
    private const string Password = "Correct-Horse-Battery-Staple-1";

    [Fact]
    public async Task StartEnrollment_fails_with_the_wrong_password()
    {
        using var ctx = new AdminAuthTestContext();
        var admin = await TestAdmin.CreateAsync(ctx.Db.Context, password: Password);

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() =>
            ctx.TwoFactorService.StartEnrollmentAsync(admin.Id, new StartTwoFactorEnrollmentRequest { Password = "wrong-password" }));

        Assert.Equal(FailureKind.Unauthorized, ex.Kind);
    }

    [Fact]
    public async Task StartEnrollment_creates_a_pending_credential_and_never_returns_the_raw_secret_alongside_a_plaintext_db_row()
    {
        using var ctx = new AdminAuthTestContext();
        var admin = await TestAdmin.CreateAsync(ctx.Db.Context, password: Password);

        var result = await ctx.TwoFactorService.StartEnrollmentAsync(admin.Id, new StartTwoFactorEnrollmentRequest { Password = Password });

        Assert.NotEmpty(result.ManualEntryKey);
        Assert.NotEmpty(result.QrCodePngBase64);

        var row = await ctx.Db.Context.AdminTwoFactorCredentials.SingleAsync(c => c.AdminUserId == admin.Id);
        Assert.Equal(AdminTwoFactorStatus.Pending, row.Status);
        // The stored value must not be the plaintext base32 secret handed to the admin.
        Assert.DoesNotContain(result.ManualEntryKey, row.EncryptedSecret);
    }

    [Fact]
    public async Task PendingEnrollment_is_not_active_until_a_valid_code_confirms_it()
    {
        using var ctx = new AdminAuthTestContext();
        var admin = await TestAdmin.CreateAsync(ctx.Db.Context, password: Password);
        await ctx.TwoFactorService.StartEnrollmentAsync(admin.Id, new StartTwoFactorEnrollmentRequest { Password = Password });

        var status = await ctx.TwoFactorService.GetStatusAsync(admin.Id);

        Assert.False(status.Enabled);
    }

    [Fact]
    public async Task ConfirmEnrollment_with_wrong_code_does_not_activate()
    {
        using var ctx = new AdminAuthTestContext();
        var admin = await TestAdmin.CreateAsync(ctx.Db.Context, password: Password);
        await ctx.TwoFactorService.StartEnrollmentAsync(admin.Id, new StartTwoFactorEnrollmentRequest { Password = Password });

        await Assert.ThrowsAsync<ApplicationFailure>(() =>
            ctx.TwoFactorService.ConfirmEnrollmentAsync(admin.Id, new VerifyTwoFactorEnrollmentRequest { Code = "000000" }));

        var status = await ctx.TwoFactorService.GetStatusAsync(admin.Id);
        Assert.False(status.Enabled);
    }

    [Fact]
    public async Task ConfirmEnrollment_with_the_correct_code_activates_two_factor()
    {
        using var ctx = new AdminAuthTestContext();
        var admin = await TestAdmin.CreateAsync(ctx.Db.Context, password: Password);
        var enrollment = await ctx.TwoFactorService.StartEnrollmentAsync(admin.Id, new StartTwoFactorEnrollmentRequest { Password = Password });

        var code = ComputeCode(enrollment.ManualEntryKey, ctx.Clock.GetUtcNow().UtcDateTime);
        await ctx.TwoFactorService.ConfirmEnrollmentAsync(admin.Id, new VerifyTwoFactorEnrollmentRequest { Code = code });

        var status = await ctx.TwoFactorService.GetStatusAsync(admin.Id);
        Assert.True(status.Enabled);
    }

    [Fact]
    public async Task StartEnrollment_twice_overwrites_the_abandoned_pending_row_instead_of_creating_a_second_one()
    {
        using var ctx = new AdminAuthTestContext();
        var admin = await TestAdmin.CreateAsync(ctx.Db.Context, password: Password);

        await ctx.TwoFactorService.StartEnrollmentAsync(admin.Id, new StartTwoFactorEnrollmentRequest { Password = Password });
        await ctx.TwoFactorService.StartEnrollmentAsync(admin.Id, new StartTwoFactorEnrollmentRequest { Password = Password });

        var count = await ctx.Db.Context.AdminTwoFactorCredentials.CountAsync(c => c.AdminUserId == admin.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task StartEnrollment_is_rejected_once_two_factor_is_already_active()
    {
        using var ctx = new AdminAuthTestContext();
        var admin = await TestAdmin.CreateAsync(ctx.Db.Context, password: Password);
        var enrollment = await ctx.TwoFactorService.StartEnrollmentAsync(admin.Id, new StartTwoFactorEnrollmentRequest { Password = Password });
        var code = ComputeCode(enrollment.ManualEntryKey, ctx.Clock.GetUtcNow().UtcDateTime);
        await ctx.TwoFactorService.ConfirmEnrollmentAsync(admin.Id, new VerifyTwoFactorEnrollmentRequest { Code = code });

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() =>
            ctx.TwoFactorService.StartEnrollmentAsync(admin.Id, new StartTwoFactorEnrollmentRequest { Password = Password }));

        Assert.Equal(FailureKind.Conflict, ex.Kind);
    }

    [Fact]
    public async Task Disable_requires_both_the_correct_password_and_a_valid_code()
    {
        using var ctx = new AdminAuthTestContext();
        var admin = await TestAdmin.CreateAsync(ctx.Db.Context, password: Password);
        var enrollment = await ctx.TwoFactorService.StartEnrollmentAsync(admin.Id, new StartTwoFactorEnrollmentRequest { Password = Password });
        var code = ComputeCode(enrollment.ManualEntryKey, ctx.Clock.GetUtcNow().UtcDateTime);
        await ctx.TwoFactorService.ConfirmEnrollmentAsync(admin.Id, new VerifyTwoFactorEnrollmentRequest { Code = code });
        // Move to a fresh, not-yet-accepted time step so the codes computed below aren't replays
        // of the one just consumed by ConfirmEnrollmentAsync (the fake clock does not advance on
        // its own, unlike a real clock between two test steps).
        ctx.Clock.Advance(TimeSpan.FromSeconds(31));

        // Wrong password, even with a soon-to-be-valid code shape, is rejected.
        await Assert.ThrowsAsync<ApplicationFailure>(() =>
            ctx.TwoFactorService.DisableAsync(admin.Id, new DisableTwoFactorRequest { Password = "nope", Code = "123456" }));
        Assert.True((await ctx.TwoFactorService.GetStatusAsync(admin.Id)).Enabled);

        // Correct password, wrong code is rejected too.
        await Assert.ThrowsAsync<ApplicationFailure>(() =>
            ctx.TwoFactorService.DisableAsync(admin.Id, new DisableTwoFactorRequest { Password = Password, Code = "000000" }));
        Assert.True((await ctx.TwoFactorService.GetStatusAsync(admin.Id)).Enabled);

        // Both correct: disables.
        var freshCode = ComputeCode(enrollment.ManualEntryKey, ctx.Clock.GetUtcNow().UtcDateTime);
        await ctx.TwoFactorService.DisableAsync(admin.Id, new DisableTwoFactorRequest { Password = Password, Code = freshCode });
        Assert.False((await ctx.TwoFactorService.GetStatusAsync(admin.Id)).Enabled);
    }

    [Fact]
    public async Task One_admins_enrollment_never_touches_another_admins_credential_row()
    {
        using var ctx = new AdminAuthTestContext();
        var adminA = await TestAdmin.CreateAsync(ctx.Db.Context, email: "a@test.local", password: Password);
        var adminB = await TestAdmin.CreateAsync(ctx.Db.Context, email: "b@test.local", password: Password);

        await ctx.TwoFactorService.StartEnrollmentAsync(adminA.Id, new StartTwoFactorEnrollmentRequest { Password = Password });

        var bStatus = await ctx.TwoFactorService.GetStatusAsync(adminB.Id);
        Assert.False(bStatus.Enabled);
        Assert.False(await ctx.Db.Context.AdminTwoFactorCredentials.AnyAsync(c => c.AdminUserId == adminB.Id));
    }

    private static string ComputeCode(string base32Secret, DateTime at) =>
        new Totp(Base32Encoding.ToBytes(base32Secret)).ComputeTotp(at);
}
