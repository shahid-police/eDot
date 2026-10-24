using Elegance.Tests.TestSupport;
using OtpNet;

namespace Elegance.Tests.Auth;

public class TotpServiceTests
{
    [Fact]
    public void VerifyCode_accepts_a_currently_valid_code()
    {
        var clock = TestServices.CreateFakeTimeProvider();
        var totpService = TestServices.CreateTotpService(clock);
        var secret = totpService.GenerateSecret();
        var code = new Totp(secret).ComputeTotp(clock.GetUtcNow().UtcDateTime);

        var matchedStep = totpService.VerifyCode(secret, code, lastAcceptedTimeStep: -1);

        Assert.NotNull(matchedStep);
    }

    [Fact]
    public void VerifyCode_rejects_a_wrong_code()
    {
        var clock = TestServices.CreateFakeTimeProvider();
        var totpService = TestServices.CreateTotpService(clock);
        var secret = totpService.GenerateSecret();
        var realCode = new Totp(secret).ComputeTotp(clock.GetUtcNow().UtcDateTime);
        var wrongCode = realCode == "000000" ? "111111" : "000000";

        Assert.Null(totpService.VerifyCode(secret, wrongCode, lastAcceptedTimeStep: -1));
    }

    [Fact]
    public void VerifyCode_rejects_a_code_that_has_expired_beyond_the_verification_window()
    {
        var clock = TestServices.CreateFakeTimeProvider();
        var totpService = TestServices.CreateTotpService(clock);
        var secret = totpService.GenerateSecret();
        var code = new Totp(secret).ComputeTotp(clock.GetUtcNow().UtcDateTime);

        // Window is +/-1 step (30s each): 3 minutes later is well outside it.
        clock.Advance(TimeSpan.FromMinutes(3));

        Assert.Null(totpService.VerifyCode(secret, code, lastAcceptedTimeStep: -1));
    }

    [Fact]
    public void VerifyCode_handles_a_code_with_a_leading_zero()
    {
        var clock = TestServices.CreateFakeTimeProvider();
        var totpService = TestServices.CreateTotpService(clock);

        byte[] secret;
        string code;
        // Search for a secret whose current code happens to start with "0", so the test
        // actually exercises the leading-zero path rather than relying on chance in one attempt.
        do
        {
            secret = totpService.GenerateSecret();
            code = new Totp(secret).ComputeTotp(clock.GetUtcNow().UtcDateTime);
        } while (!code.StartsWith('0'));

        var matchedStep = totpService.VerifyCode(secret, code, lastAcceptedTimeStep: -1);

        Assert.NotNull(matchedStep);
        Assert.Equal(6, code.Length);
    }

    [Fact]
    public void VerifyCode_rejects_a_previously_accepted_time_step_replayed()
    {
        var clock = TestServices.CreateFakeTimeProvider();
        var totpService = TestServices.CreateTotpService(clock);
        var secret = totpService.GenerateSecret();
        var code = new Totp(secret).ComputeTotp(clock.GetUtcNow().UtcDateTime);

        var firstMatch = totpService.VerifyCode(secret, code, lastAcceptedTimeStep: -1);
        Assert.NotNull(firstMatch);

        // Same code, same step, now presented as "already accepted" - must be rejected.
        var replay = totpService.VerifyCode(secret, code, lastAcceptedTimeStep: firstMatch!.Value);
        Assert.Null(replay);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("12 345")]
    public void VerifyCode_rejects_malformed_input(string malformed)
    {
        var clock = TestServices.CreateFakeTimeProvider();
        var totpService = TestServices.CreateTotpService(clock);
        var secret = totpService.GenerateSecret();

        Assert.Null(totpService.VerifyCode(secret, malformed, lastAcceptedTimeStep: -1));
    }

    [Fact]
    public void BuildOtpAuthUri_is_a_standards_compliant_otpauth_uri_with_no_external_dependency()
    {
        var clock = TestServices.CreateFakeTimeProvider();
        var totpService = TestServices.CreateTotpService(clock);
        var secret = totpService.GenerateSecret();

        var uri = totpService.BuildOtpAuthUri(secret, "Elegance Admin", "admin@elegance.local");

        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains("digits=6", uri);
        Assert.Contains("period=30", uri);
        Assert.Contains("algorithm=SHA1", uri);
        Assert.DoesNotContain("google.com", uri, StringComparison.OrdinalIgnoreCase);
    }
}
