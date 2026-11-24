using Elegance.Application.Common;
using Elegance.Application.Vendors;
using Elegance.Domain.Vendors;
using Elegance.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Tests.Vendors;

public class VendorCatalogServiceTests
{
    // Regression guard for the mass-assignment defence described in the vendor module's design:
    // a vendor submission request must never gain a price/SKU/description/etc. property, because
    // the whole safety argument rests on those simply not existing on this type.
    [Fact]
    public void VendorSubmissionRequest_exposes_only_the_four_vendor_editable_fields()
    {
        var properties = typeof(VendorSubmissionRequest).GetProperties().Select(p => p.Name).OrderBy(n => n).ToArray();

        Assert.Equal(
            new[] { "CategoryId", "ProductName", "VendorProductCode", "WholesalePrice" },
            properties);
    }

    [Fact]
    public async Task A_vendor_cannot_read_another_vendors_submission()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendorA = await TestVendor.CreateAsync(ctx.Db.Context, email: "a@test.local", vendorCode: "VEN-0001");
        var vendorB = await TestVendor.CreateAsync(ctx.Db.Context, email: "b@test.local", vendorCode: "VEN-0002");

        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendorA.Id, new VendorSubmissionRequest
        {
            ProductName = "Gold Bangle",
            CategoryId = category.Id,
            WholesalePrice = 1000,
            VendorProductCode = "GB-1",
        });

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() => ctx.CatalogService.GetSubmissionAsync(vendorB.Id, submission.Id));
        Assert.Equal(FailureKind.NotFound, ex.Kind);
    }

    [Fact]
    public async Task Two_different_vendors_may_reuse_the_same_product_code()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendorA = await TestVendor.CreateAsync(ctx.Db.Context, email: "a@test.local", vendorCode: "VEN-0001");
        var vendorB = await TestVendor.CreateAsync(ctx.Db.Context, email: "b@test.local", vendorCode: "VEN-0002");

        var request = new VendorSubmissionRequest { ProductName = "Ring", CategoryId = category.Id, WholesalePrice = 500, VendorProductCode = "SAME-CODE" };
        await ctx.CatalogService.CreateSubmissionAsync(vendorA.Id, request);
        var secondSubmission = await ctx.CatalogService.CreateSubmissionAsync(vendorB.Id, request);

        Assert.Equal("SAME-CODE", secondSubmission.VendorProductCode);
    }

    [Fact]
    public async Task The_same_vendor_cannot_reuse_their_own_product_code()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);

        var request = new VendorSubmissionRequest { ProductName = "Ring", CategoryId = category.Id, WholesalePrice = 500, VendorProductCode = "DUP" };
        await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, request);

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() => ctx.CatalogService.CreateSubmissionAsync(vendor.Id, request));
        Assert.Equal(FailureKind.Conflict, ex.Kind);
    }

    [Fact]
    public async Task A_submission_can_no_longer_be_edited_once_approved()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Ring", CategoryId = category.Id, WholesalePrice = 500, VendorProductCode = "R-1",
        });

        await ctx.AdminSubmissionService.ApproveAsync(new Elegance.Application.Admin.AdminActor(1, "admin@test.local"), submission.Id);

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() => ctx.CatalogService.UpdateSubmissionAsync(vendor.Id, submission.Id, new VendorSubmissionRequest
        {
            ProductName = "Changed", CategoryId = category.Id, WholesalePrice = 999, VendorProductCode = "R-1",
        }));
        Assert.Equal(FailureKind.Conflict, ex.Kind);
    }

    [Fact]
    public async Task Uploading_and_removing_a_submission_image_round_trips_through_storage()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Ring", CategoryId = category.Id, WholesalePrice = 500, VendorProductCode = "R-1",
        });

        var bytes = TestImage.ValidPngBytes();
        var image = await ctx.CatalogService.UploadSubmissionImageAsync(vendor.Id, submission.Id, new MemoryStream(bytes), "image/png", bytes.Length);

        var dto = await ctx.CatalogService.GetSubmissionAsync(vendor.Id, submission.Id);
        Assert.Single(dto.Images);

        await ctx.CatalogService.RemoveSubmissionImageAsync(vendor.Id, submission.Id, image.Id);
        dto = await ctx.CatalogService.GetSubmissionAsync(vendor.Id, submission.Id);
        Assert.Empty(dto.Images);
    }

    [Fact]
    public async Task A_vendor_cannot_upload_an_image_to_another_vendors_submission()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendorA = await TestVendor.CreateAsync(ctx.Db.Context, email: "a@test.local", vendorCode: "VEN-0001");
        var vendorB = await TestVendor.CreateAsync(ctx.Db.Context, email: "b@test.local", vendorCode: "VEN-0002");
        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendorA.Id, new VendorSubmissionRequest
        {
            ProductName = "Ring", CategoryId = category.Id, WholesalePrice = 500, VendorProductCode = "R-1",
        });

        var bytes = TestImage.ValidPngBytes();
        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() =>
            ctx.CatalogService.UploadSubmissionImageAsync(vendorB.Id, submission.Id, new MemoryStream(bytes), "image/png", bytes.Length));
        Assert.Equal(FailureKind.NotFound, ex.Kind);
    }
}
