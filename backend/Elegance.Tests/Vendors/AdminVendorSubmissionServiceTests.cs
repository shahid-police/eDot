using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Application.Vendors;
using Elegance.Domain.Catalog;
using Elegance.Domain.Catalog.Enums;
using Elegance.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Tests.Vendors;

public class AdminVendorSubmissionServiceTests
{
    private static readonly AdminActor Actor = new(1, "admin@test.local");

    [Fact]
    public async Task Submitting_auto_creates_a_hidden_product_with_an_auto_generated_sku_via_the_existing_mechanism()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context, code: "RING");
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);

        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Gold Ring", CategoryId = category.Id, WholesalePrice = 1500, VendorProductCode = "GR-1",
        });

        Assert.NotNull(submission.ProductId);
        var product = await ctx.Db.Context.Products.AsNoTracking().SingleAsync(p => p.Id == submission.ProductId);

        // SKU came from AdminCatalogService's existing ComputeNextSkuAsync - nothing in the
        // vendor module computes or guesses a SKU itself.
        Assert.StartsWith("JEW-RING-", product.Sku);
        Assert.Equal(vendor.Id, product.VendorId);
        Assert.Equal(1500, product.CostPrice);
        Assert.False(product.IsActive, "a freshly submitted product must never be publicly visible");
    }

    [Fact]
    public async Task Approve_then_publish_makes_the_image_public_on_the_auto_created_product()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Gold Ring", CategoryId = category.Id, WholesalePrice = 1500, VendorProductCode = "GR-1",
        });

        var bytes = TestImage.ValidPngBytes();
        var vendorImage = await ctx.CatalogService.UploadSubmissionImageAsync(vendor.Id, submission.Id, new MemoryStream(bytes), "image/png", bytes.Length);

        await ctx.AdminSubmissionService.ApproveAsync(Actor, submission.Id);

        var published = await ctx.AdminSubmissionService.PublishSubmissionImageAsync(Actor, submission.Id, vendorImage.Id, "Primary");

        var productImage = await ctx.Db.Context.ProductImages.AsNoTracking().Include(i => i.MediaFile)
            .SingleAsync(i => i.Id == published.Id);
        Assert.Equal(submission.ProductId, productImage.ProductId);
        Assert.Equal(ProductImageType.Primary, productImage.ImageType);

        // The public ProductImage's MediaFile must be a distinct row/storage object from the
        // vendor's private original - publishing re-processes and re-uploads, it never repoints
        // the public catalogue at the vendor's own storage key.
        var vendorImageRow = await ctx.Db.Context.VendorProductImages.AsNoTracking().Include(i => i.MediaFile)
            .SingleAsync(i => i.Id == vendorImage.Id);
        Assert.NotEqual(vendorImageRow.MediaFileId, productImage.MediaFileId);
        Assert.NotEqual(vendorImageRow.MediaFile.StorageKey, productImage.MediaFile.StorageKey);
        Assert.StartsWith("vendor-originals/", vendorImageRow.MediaFile.StorageKey);
        Assert.StartsWith("products/", productImage.MediaFile.StorageKey);
    }

    [Fact]
    public async Task An_image_can_be_published_even_before_the_submission_is_approved()
    {
        // The draft product exists from the moment of submission, so an admin reviewing a
        // still-Pending submission can already pick a primary image for it - approval is purely
        // about the vendor-facing status, not a gate on editing/publishing the product.
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Gold Ring", CategoryId = category.Id, WholesalePrice = 1500, VendorProductCode = "GR-1",
        });
        var bytes = TestImage.ValidPngBytes();
        var vendorImage = await ctx.CatalogService.UploadSubmissionImageAsync(vendor.Id, submission.Id, new MemoryStream(bytes), "image/png", bytes.Length);

        var published = await ctx.AdminSubmissionService.PublishSubmissionImageAsync(Actor, submission.Id, vendorImage.Id, "Primary");

        var productImage = await ctx.Db.Context.ProductImages.AsNoTracking().SingleAsync(i => i.Id == published.Id);
        Assert.Equal(submission.ProductId, productImage.ProductId);
    }

    [Fact]
    public async Task Reject_requires_a_reason_and_stops_further_approval()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Gold Ring", CategoryId = category.Id, WholesalePrice = 1500, VendorProductCode = "GR-1",
        });

        var rejected = await ctx.AdminSubmissionService.RejectAsync(Actor, submission.Id, new AdminRejectSubmissionRequest { Reason = "Not enough detail" });
        Assert.Equal("Rejected", rejected.Status);

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() => ctx.AdminSubmissionService.ApproveAsync(Actor, submission.Id));
        Assert.Equal(FailureKind.Conflict, ex.Kind);
    }

    [Fact]
    public async Task ReturnForCorrection_lets_the_vendor_edit_and_resubmit()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Gold Ring", CategoryId = category.Id, WholesalePrice = 1500, VendorProductCode = "GR-1",
        });

        await ctx.AdminSubmissionService.ReturnForCorrectionAsync(Actor, submission.Id, new AdminReturnSubmissionForCorrectionRequest
        {
            FieldsRequested = "Product Name", Remarks = "Please be more specific",
        });

        var updated = await ctx.CatalogService.UpdateSubmissionAsync(vendor.Id, submission.Id, new VendorSubmissionRequest
        {
            ProductName = "Gold Ring - 22K", CategoryId = category.Id, WholesalePrice = 1500, VendorProductCode = "GR-1",
        });
        Assert.Equal("ReturnedForCorrection", updated.Status);

        var resubmitted = await ctx.CatalogService.ResubmitAsync(vendor.Id, submission.Id);
        Assert.Equal("PendingApproval", resubmitted.Status);
    }

    [Fact]
    public async Task DownloadSubmissionImage_returns_the_exact_bytes_that_were_uploaded()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Gold Ring", CategoryId = category.Id, WholesalePrice = 1500, VendorProductCode = "GR-1",
        });
        var bytes = TestImage.ValidPngBytes();
        var vendorImage = await ctx.CatalogService.UploadSubmissionImageAsync(vendor.Id, submission.Id, new MemoryStream(bytes), "image/png", bytes.Length);

        var (content, contentType, _) = await ctx.AdminSubmissionService.DownloadSubmissionImageAsync(submission.Id, vendorImage.Id);
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);

        Assert.Equal("image/png", contentType);
        Assert.True(buffer.Length > 0);
    }
}
