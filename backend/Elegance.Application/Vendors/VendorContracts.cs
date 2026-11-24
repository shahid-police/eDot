using System.ComponentModel.DataAnnotations;
using Elegance.Application.Common;

namespace Elegance.Application.Vendors;

public sealed record VendorActor(long Id, string Email);

// ---- Auth ----

public class VendorLoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}

public class VendorProfileDto
{
    public required long Id { get; init; }
    public required string Email { get; init; }
    public required string VendorCode { get; init; }
    public required string ShopName { get; init; }
    public required string ContactPerson { get; init; }
    public required string Phone { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
}

public class VendorSession
{
    public required string AccessToken { get; init; }
    public required DateTime AccessTokenExpiresAt { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime RefreshTokenExpiresAt { get; init; }
    public required VendorProfileDto Vendor { get; init; }
}

public class VendorTwoFactorPending
{
    public required string PendingToken { get; init; }
    public required DateTime PendingTokenExpiresAt { get; init; }
    // True when this vendor has never completed 2FA enrollment yet - the frontend must send the
    // vendor to the enrollment screen rather than a "enter your code" screen. 2FA is mandatory
    // for vendors (unlike admins), so a vendor can never reach a real session without this step.
    public required bool RequiresEnrollment { get; init; }
}

// Login always returns a pending challenge - a vendor never gets a real Session directly from
// password login alone, since 2FA is mandatory.
public class VendorLoginResult
{
    public required VendorTwoFactorPending TwoFactor { get; init; }
}

public class VendorStartTwoFactorEnrollmentRequest
{
    [Required]
    public string PendingToken { get; init; } = string.Empty;
}

public class VendorTwoFactorEnrollmentDto
{
    public required string ManualEntryKey { get; init; }
    public required string QrCodePngBase64 { get; init; }
}

public class VendorConfirmTwoFactorEnrollmentRequest
{
    [Required]
    public string PendingToken { get; init; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; init; } = string.Empty;
}

public class VerifyVendorTwoFactorRequest
{
    [Required]
    public string PendingToken { get; init; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; init; } = string.Empty;
}

// Enrollment lives on the auth service (not a separate 2FA service like Admin's) because, unlike
// an admin opting into 2FA from an already-authenticated dashboard, a vendor's mandatory
// first-login enrollment has no session yet - ConfirmEnrollmentAsync must itself mint the real
// VendorSession once the code is verified.
public interface IVendorAuthService
{
    Task<VendorLoginResult> LoginAsync(VendorLoginRequest request, CancellationToken cancellationToken = default);
    Task<VendorTwoFactorEnrollmentDto> StartEnrollmentAsync(VendorStartTwoFactorEnrollmentRequest request, CancellationToken cancellationToken = default);
    Task<VendorSession> ConfirmEnrollmentAsync(VendorConfirmTwoFactorEnrollmentRequest request, CancellationToken cancellationToken = default);
    Task<VendorSession> VerifyTwoFactorAsync(VerifyVendorTwoFactorRequest request, CancellationToken cancellationToken = default);
    Task<VendorSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
}

// ---- Product submission ----

// Deliberately the ONLY four fields a vendor may ever set for a product. Price, SKU,
// description, variants, SEO, publication, etc. have no representation on this type at all, so a
// hand-crafted request cannot smuggle them in regardless of what the client sends.
public class VendorSubmissionRequest
{
    [Required, StringLength(300)]
    public string ProductName { get; init; } = string.Empty;

    [Required]
    public long CategoryId { get; init; }

    [Required, Range(0, 100000000)]
    public decimal WholesalePrice { get; init; }

    [Required, StringLength(100)]
    public string VendorProductCode { get; init; } = string.Empty;
}

// Deliberately carries no direct storage URL - vendor-original images are private and must only
// ever be fetched through the authenticated, ownership-checked download endpoint
// (GET /api/v1/vendor/products/{submissionId}/images/{imageId}), never via a raw, unauthenticated
// static-file link that could be shared or cached outside the portal.
public class VendorSubmissionImageDto
{
    public required long Id { get; init; }
    public required int SortOrder { get; init; }
}

public class VendorSubmissionDto
{
    public required long Id { get; init; }
    public required string ProductName { get; init; }
    public required long CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required decimal WholesalePrice { get; init; }
    public required string VendorProductCode { get; init; }
    public required string Status { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public string? RejectionReason { get; init; }
    public string? AdminRemarks { get; init; }
    public string? CorrectionFieldsRequested { get; init; }
    public long? ProductId { get; init; }
    public string? ProductSku { get; init; }
    public required IReadOnlyList<VendorSubmissionImageDto> Images { get; init; }
}

public class VendorSubmissionQuery
{
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public interface IVendorCatalogService
{
    Task<VendorProfileDto> GetProfileAsync(long vendorId, CancellationToken cancellationToken = default);
    Task<PagedResult<VendorSubmissionDto>> ListMySubmissionsAsync(long vendorId, VendorSubmissionQuery query, CancellationToken cancellationToken = default);
    Task<VendorSubmissionDto> GetSubmissionAsync(long vendorId, long submissionId, CancellationToken cancellationToken = default);
    Task<VendorSubmissionDto> CreateSubmissionAsync(long vendorId, VendorSubmissionRequest request, CancellationToken cancellationToken = default);
    Task<VendorSubmissionDto> UpdateSubmissionAsync(long vendorId, long submissionId, VendorSubmissionRequest request, CancellationToken cancellationToken = default);
    Task<VendorSubmissionDto> ResubmitAsync(long vendorId, long submissionId, CancellationToken cancellationToken = default);
    Task WithdrawAsync(long vendorId, long submissionId, CancellationToken cancellationToken = default);

    Task<VendorSubmissionImageDto> UploadSubmissionImageAsync(
        long vendorId, long submissionId, Stream content, string contentType, long contentLength, CancellationToken cancellationToken = default);
    Task RemoveSubmissionImageAsync(long vendorId, long submissionId, long imageId, CancellationToken cancellationToken = default);

    // Streams the vendor's own original image bytes back - ownership-checked, never a public URL.
    Task<(Stream Content, string ContentType, string FileName)> DownloadSubmissionImageAsync(
        long vendorId, long submissionId, long imageId, CancellationToken cancellationToken = default);
}
