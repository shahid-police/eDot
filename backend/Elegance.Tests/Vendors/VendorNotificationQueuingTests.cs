using Elegance.Application.Admin;
using Elegance.Application.Vendors;
using Elegance.Domain.Notifications;
using Elegance.Domain.Notifications.Enums;
using Elegance.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Tests.Vendors;

public class VendorNotificationQueuingTests
{
    private static readonly AdminActor Actor = new(1, "admin@test.local");

    [Fact]
    public async Task Creating_a_submission_queues_nothing_when_no_admin_number_is_configured()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);

        await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Ring", CategoryId = category.Id, WholesalePrice = 500, VendorProductCode = "R-1",
        });

        Assert.Empty(await ctx.Db.Context.VendorNotifications.ToListAsync());
    }

    [Fact]
    public async Task Creating_a_submission_queues_an_admin_facing_notification_once_a_number_is_configured()
    {
        using var ctx = new VendorAuthTestContext();
        ctx.NotificationOptions.AdminWhatsAppNumber = "+923001112222";
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);

        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Ring", CategoryId = category.Id, WholesalePrice = 500, VendorProductCode = "R-1",
        });

        var notification = await ctx.Db.Context.VendorNotifications.SingleAsync();
        Assert.Equal(VendorNotificationEvent.VendorSubmitted, notification.Event);
        Assert.Equal("+923001112222", notification.WhatsAppNumber);
        Assert.Equal(submission.Id, notification.SubmissionId);
        Assert.Null(notification.VendorId);
        Assert.Equal(WhatsAppNotificationStatus.Pending, notification.Status);
    }

    [Fact]
    public async Task Approving_a_submission_queues_a_vendor_facing_notification_to_the_vendors_own_number()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var submission = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Ring", CategoryId = category.Id, WholesalePrice = 500, VendorProductCode = "R-1",
        });

        await ctx.AdminSubmissionService.ApproveAsync(Actor, submission.Id);

        var notification = await ctx.Db.Context.VendorNotifications.SingleAsync();
        Assert.Equal(VendorNotificationEvent.SubmissionApproved, notification.Event);
        Assert.Equal(vendor.Phone, notification.WhatsAppNumber);
        Assert.Equal(vendor.Id, notification.VendorId);
    }

    [Fact]
    public async Task Rejecting_and_returning_for_correction_each_queue_their_own_vendor_facing_event()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);

        var rejected = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Ring A", CategoryId = category.Id, WholesalePrice = 500, VendorProductCode = "R-A",
        });
        await ctx.AdminSubmissionService.RejectAsync(Actor, rejected.Id, new AdminRejectSubmissionRequest { Reason = "no" });

        var returned = await ctx.CatalogService.CreateSubmissionAsync(vendor.Id, new VendorSubmissionRequest
        {
            ProductName = "Ring B", CategoryId = category.Id, WholesalePrice = 500, VendorProductCode = "R-B",
        });
        await ctx.AdminSubmissionService.ReturnForCorrectionAsync(Actor, returned.Id, new AdminReturnSubmissionForCorrectionRequest { FieldsRequested = "name" });

        var events = (await ctx.Db.Context.VendorNotifications.ToListAsync()).Select(n => n.Event).OrderBy(e => e).ToList();
        Assert.Equal([VendorNotificationEvent.SubmissionRejected, VendorNotificationEvent.ReturnedForCorrection], events);
    }
}
