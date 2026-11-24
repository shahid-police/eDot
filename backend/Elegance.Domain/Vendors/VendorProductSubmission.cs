namespace Elegance.Domain.Vendors;

public enum VendorSubmissionStatus
{
    PendingApproval,
    Approved,
    Rejected,
    ReturnedForCorrection,
}

// A vendor's proposal for a product. Deliberately carries only the four fields a vendor may
// set (ProductName, CategoryId, WholesalePrice, VendorProductCode) plus system-managed review
// state - nothing here lets a vendor influence price, SKU, description, or publication, even if
// the HTTP request is hand-crafted, because those fields simply do not exist on this type.
public class VendorProductSubmission
{
    public long Id { get; set; }
    public long VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    // Set once an Admin creates/links the real catalog Product for this submission (Approved).
    public long? ProductId { get; set; }

    public required string ProductName { get; set; }
    public long CategoryId { get; set; }
    public decimal WholesalePrice { get; set; }
    public required string VendorProductCode { get; set; }

    public VendorSubmissionStatus Status { get; set; } = VendorSubmissionStatus.PendingApproval;
    public DateTime SubmittedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public long? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? AdminRemarks { get; set; }
    // Free-text list of which vendor-editable fields need fixing, shown to the vendor when
    // Status == ReturnedForCorrection (e.g. "Product Name, Images").
    public string? CorrectionFieldsRequested { get; set; }

    public ICollection<VendorProductImage> Images { get; set; } = [];
    public ICollection<VendorProductSubmissionHistory> History { get; set; } = [];
}
