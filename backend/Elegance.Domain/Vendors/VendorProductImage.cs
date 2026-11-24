using Elegance.Domain.Media;

namespace Elegance.Domain.Vendors;

// A vendor's original/reference image for a submission. Reuses the existing MediaFile table for
// storage bookkeeping (storage key, url, dimensions) but is never linked to Catalog.ProductImage -
// the public catalogue API has no knowledge of this table at all, so these can never leak
// publicly by accident. The only path from here to a public image is an explicit Admin action
// that streams these bytes into the existing AdminMediaService upload pipeline (see
// AdminVendorSubmissionsController.PublishImage).
public class VendorProductImage
{
    public long Id { get; set; }
    public long SubmissionId { get; set; }
    public VendorProductSubmission Submission { get; set; } = null!;
    public long VendorId { get; set; }

    public long MediaFileId { get; set; }
    public MediaFile MediaFile { get; set; } = null!;

    public int SortOrder { get; set; }
    public DateTime UploadedAt { get; set; }
}
