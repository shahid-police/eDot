namespace Elegance.Domain.Vendors;

public enum VendorSubmissionPerformedByType
{
    Vendor,
    Admin,
}

// Lightweight, vendor-visible status trail for a single submission (who changed its status, when,
// and any remarks). Broader admin audit (vendor CRUD, image publish actions, IP, etc.) goes
// through the existing IAuditLogService instead - this table exists only to show the vendor a
// clear history of their own submission without exposing internal admin audit data.
public class VendorProductSubmissionHistory
{
    public long Id { get; set; }
    public long SubmissionId { get; set; }
    public VendorProductSubmission Submission { get; set; } = null!;

    public required string Action { get; set; }
    public VendorSubmissionStatus? PreviousStatus { get; set; }
    public VendorSubmissionStatus NewStatus { get; set; }
    public VendorSubmissionPerformedByType PerformedByType { get; set; }
    public long PerformedById { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}
