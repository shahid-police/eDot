using System.Security.Cryptography;
using Elegance.Application.Common;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Marketing;

public static class PromoCodeGenerator
{
    private const int MaxAttempts = 15;

    // Cryptographically secure, uniform over [0, 999999] - not a predictable PRNG and not
    // sequential. Zero-padded to preserve leading zeros (e.g. "048213").
    public static string GenerateSixDigitCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    // A DB unique constraint is the real guarantee against a collision slipping through under
    // concurrent requests - this pre-check just makes a retry the common case instead of a thrown
    // exception, since the code space (1,000,000) is large relative to how many promoters/codes
    // this system will ever have.
    public static async Task<string> GenerateUniqueCodeAsync(EleganceDbContext dbContext, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = GenerateSixDigitCode();
            var exists = await dbContext.PromoCodes.AsNoTracking().AnyAsync(c => c.Code == code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }

        throw new ApplicationFailure(FailureKind.Conflict, "Could not generate a unique promo code. Please try again.");
    }
}
