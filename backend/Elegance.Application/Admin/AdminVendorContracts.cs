using System.ComponentModel.DataAnnotations;
using Elegance.Application.Common;

namespace Elegance.Application.Admin;

// ---- Vendor account management ----

public class AdminVendorRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string ShopName { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string ContactPerson { get; init; } = string.Empty;

    [Required, StringLength(30)]
    public string Phone { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Address { get; init; }

    [StringLength(100)]
    public string? City { get; init; }

    [StringLength(2000)]
    public string? AdminRemarks { get; init; }
}

public class AdminVendorDto
{
    public required long Id { get; init; }
    public required string Email { get; init; }
    public required string VendorCode { get; init; }
    public required string ShopName { get; init; }
    public required string ContactPerson { get; init; }
    public required string Phone { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public required string Status { get; init; }
    public string? AdminRemarks { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required int SubmissionCount { get; init; }
    public required int PendingSubmissionCount { get; init; }
}

public class AdminVendorQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminSetVendorStatusRequest
{
    [Required]
    public string Status { get; init; } = string.Empty;
}

// Generates a one-time temporary password and returns it to the admin to relay to the vendor
// (same "admin sees it once, must communicate it out of band" pattern as account creation) -
// mirrors how AdminUser credential resets already work, rather than inventing a new mechanism.
public class AdminVendorCredentialResetDto
{
    public required string TemporaryPassword { get; init; }
}

public interface IAdminVendorService
{
    Task<PagedResult<AdminVendorDto>> ListVendorsAsync(AdminVendorQuery query, CancellationToken cancellationToken = default);
    Task<AdminVendorDto> GetVendorAsync(long vendorId, CancellationToken cancellationToken = default);
    Task<(AdminVendorDto Vendor, string TemporaryPassword)> CreateVendorAsync(AdminActor actor, AdminVendorRequest request, CancellationToken cancellationToken = default);
    Task<AdminVendorDto> UpdateVendorAsync(AdminActor actor, long vendorId, AdminVendorRequest request, CancellationToken cancellationToken = default);
    Task<AdminVendorDto> SetVendorStatusAsync(AdminActor actor, long vendorId, AdminSetVendorStatusRequest request, CancellationToken cancellationToken = default);
    Task<AdminVendorCredentialResetDto> ResetVendorCredentialsAsync(AdminActor actor, long vendorId, CancellationToken cancellationToken = default);
}

// ---- Vendor product submission review ----

// No raw storage URL here either - admins view vendor-original images only through the
// authenticated DownloadSubmissionImageAsync endpoint.
public class AdminVendorSubmissionImageDto
{
    public required long Id { get; init; }
    public required int SortOrder { get; init; }
}

public class AdminVendorSubmissionDto
{
    public required long Id { get; init; }
    public required long VendorId { get; init; }
    public required string VendorCode { get; init; }
    public required string ShopName { get; init; }
    public required string ProductName { get; init; }
    public required long CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required decimal WholesalePrice { get; init; }
    public required string VendorProductCode { get; init; }
    public required string Status { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public long? ReviewedByAdminId { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public string? AdminRemarks { get; init; }
    public string? CorrectionFieldsRequested { get; init; }
    public long? ProductId { get; init; }
    public string? ProductSku { get; init; }
    public required IReadOnlyList<AdminVendorSubmissionImageDto> Images { get; init; }
}

public class AdminVendorSubmissionQuery
{
    public long? VendorId { get; init; }
    public long? CategoryId { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminRejectSubmissionRequest
{
    [Required, StringLength(1000)]
    public string Reason { get; init; } = string.Empty;
}

public class AdminReturnSubmissionForCorrectionRequest
{
    [Required, StringLength(1000)]
    public string FieldsRequested { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Remarks { get; init; }
}

public class AdminLinkSubmissionToProductRequest
{
    [Required]
    public long ProductId { get; init; }
}

public interface IAdminVendorSubmissionService
{
    Task<PagedResult<AdminVendorSubmissionDto>> ListSubmissionsAsync(AdminVendorSubmissionQuery query, CancellationToken cancellationToken = default);
    Task<AdminVendorSubmissionDto> GetSubmissionAsync(long submissionId, CancellationToken cancellationToken = default);
    Task<AdminVendorSubmissionDto> ApproveAsync(AdminActor actor, long submissionId, CancellationToken cancellationToken = default);
    Task<AdminVendorSubmissionDto> RejectAsync(AdminActor actor, long submissionId, AdminRejectSubmissionRequest request, CancellationToken cancellationToken = default);
    Task<AdminVendorSubmissionDto> ReturnForCorrectionAsync(AdminActor actor, long submissionId, AdminReturnSubmissionForCorrectionRequest request, CancellationToken cancellationToken = default);
    Task<AdminVendorSubmissionDto> LinkProductAsync(AdminActor actor, long submissionId, AdminLinkSubmissionToProductRequest request, CancellationToken cancellationToken = default);

    // Streams a vendor's original image bytes back for the admin to inspect/download.
    Task<(Stream Content, string ContentType, string FileName)> DownloadSubmissionImageAsync(long submissionId, long imageId, CancellationToken cancellationToken = default);

    // Re-processes and re-uploads a vendor-original image through the existing, unmodified
    // AdminMediaService public upload pipeline - this is the only path by which a vendor image can
    // ever become publicly visible, and it always requires this explicit admin action.
    Task<AdminProductImageDto> PublishSubmissionImageAsync(AdminActor actor, long submissionId, long imageId, string imageType, CancellationToken cancellationToken = default);
}
