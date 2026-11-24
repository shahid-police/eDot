using Elegance.Domain.Notifications;
using Elegance.Domain.Notifications.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Elegance.Infrastructure.Notifications;

// Stages a VendorNotification row on the shared DbContext change tracker - it does NOT call
// SaveChangesAsync itself, so the row commits atomically together with whatever status change
// the caller is already about to save (the notification only ever exists if that change did).
// VendorNotificationWorker picks up and sends anything left Pending.
public class VendorNotificationQueuer(EleganceDbContext dbContext, IOptions<VendorNotificationOptions> options)
{
    public Task QueueVendorSubmittedAsync(long submissionId, CancellationToken cancellationToken) =>
        QueueAdminFacingAsync(VendorNotificationEvent.VendorSubmitted, submissionId, cancellationToken);

    public Task QueueVendorResubmittedAsync(long submissionId, CancellationToken cancellationToken) =>
        QueueAdminFacingAsync(VendorNotificationEvent.VendorResubmitted, submissionId, cancellationToken);

    public Task QueueSubmissionApprovedAsync(long vendorId, long submissionId, CancellationToken cancellationToken) =>
        QueueVendorFacingAsync(VendorNotificationEvent.SubmissionApproved, vendorId, submissionId, cancellationToken);

    public Task QueueSubmissionRejectedAsync(long vendorId, long submissionId, CancellationToken cancellationToken) =>
        QueueVendorFacingAsync(VendorNotificationEvent.SubmissionRejected, vendorId, submissionId, cancellationToken);

    public Task QueueReturnedForCorrectionAsync(long vendorId, long submissionId, CancellationToken cancellationToken) =>
        QueueVendorFacingAsync(VendorNotificationEvent.ReturnedForCorrection, vendorId, submissionId, cancellationToken);

    private Task QueueAdminFacingAsync(VendorNotificationEvent evt, long submissionId, CancellationToken cancellationToken)
    {
        var adminNumber = options.Value.AdminWhatsAppNumber;
        if (string.IsNullOrWhiteSpace(adminNumber))
        {
            // No admin number configured - a clear no-op, not an error, until one is set.
            return Task.CompletedTask;
        }

        Add(evt, vendorId: null, submissionId, adminNumber);
        return Task.CompletedTask;
    }

    private async Task QueueVendorFacingAsync(VendorNotificationEvent evt, long vendorId, long submissionId, CancellationToken cancellationToken)
    {
        var phone = await dbContext.Vendors.AsNoTracking()
            .Where(v => v.Id == vendorId)
            .Select(v => v.Phone)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(phone))
        {
            return;
        }

        Add(evt, vendorId, submissionId, phone);
    }

    private void Add(VendorNotificationEvent evt, long? vendorId, long submissionId, string whatsAppNumber)
    {
        var now = DateTime.UtcNow;
        dbContext.VendorNotifications.Add(new VendorNotification
        {
            Event = evt,
            VendorId = vendorId,
            SubmissionId = submissionId,
            WhatsAppNumber = whatsAppNumber,
            MessageTemplateName = TemplateFor(evt),
            Status = WhatsAppNotificationStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    private string TemplateFor(VendorNotificationEvent evt) => evt switch
    {
        VendorNotificationEvent.VendorSubmitted => options.Value.SubmittedTemplateName,
        VendorNotificationEvent.VendorResubmitted => options.Value.ResubmittedTemplateName,
        VendorNotificationEvent.SubmissionApproved => options.Value.ApprovedTemplateName,
        VendorNotificationEvent.SubmissionRejected => options.Value.RejectedTemplateName,
        VendorNotificationEvent.ReturnedForCorrection => options.Value.ReturnedForCorrectionTemplateName,
        _ => throw new ArgumentOutOfRangeException(nameof(evt)),
    };
}
