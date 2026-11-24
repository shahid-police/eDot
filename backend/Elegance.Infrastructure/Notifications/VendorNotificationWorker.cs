using Elegance.Application.Notifications;
using Elegance.Domain.Notifications;
using Elegance.Domain.Notifications.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elegance.Infrastructure.Notifications;

// Mirrors BackInStockNotificationWorker's send-with-backoff loop exactly, but needs no separate
// detection/fan-out step first - vendor notification rows are queued directly and synchronously
// by VendorCatalogService/AdminVendorSubmissionService at the moment the triggering action
// happens (submission created/resubmitted/approved/rejected/returned).
public class VendorNotificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<VendorNotificationOptions> options,
    ILogger<VendorNotificationWorker> logger) : BackgroundService
{
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, options.Value.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EleganceDbContext>();
                var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppNotificationService>();

                await SendDueNotificationsAsync(dbContext, whatsApp, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Vendor notification worker tick failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task SendDueNotificationsAsync(EleganceDbContext dbContext, IWhatsAppNotificationService whatsApp, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var due = await dbContext.VendorNotifications
            .Where(n => (n.Status == WhatsAppNotificationStatus.Pending || n.Status == WhatsAppNotificationStatus.Queued)
                && (n.NextAttemptAt == null || n.NextAttemptAt <= now))
            .OrderBy(n => n.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return;
        }

        foreach (var notification in due)
        {
            notification.Status = WhatsAppNotificationStatus.Queued;
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var notification in due)
        {
            await SendOneAsync(dbContext, whatsApp, notification, cancellationToken);
        }
    }

    private async Task SendOneAsync(EleganceDbContext dbContext, IWhatsAppNotificationService whatsApp, VendorNotification notification, CancellationToken cancellationToken)
    {
        // Fetched fresh at send time (not persisted at queue time) so a backlog never sends a
        // message with stale text - same reasoning as BackInStockNotificationWorker.SendOneAsync.
        string? productName = null;
        if (notification.SubmissionId is { } submissionId)
        {
            productName = await dbContext.VendorProductSubmissions.AsNoTracking()
                .Where(s => s.Id == submissionId)
                .Select(s => s.ProductName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        string? shopName = null;
        if (notification.VendorId is { } vendorId)
        {
            shopName = await dbContext.Vendors.AsNoTracking()
                .Where(v => v.Id == vendorId)
                .Select(v => v.ShopName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (productName is null || (notification.VendorId is not null && shopName is null))
        {
            // The submission or vendor is gone since queueing - not retryable.
            notification.Status = WhatsAppNotificationStatus.Failed;
            notification.FailureReason = "The related submission or vendor no longer exists.";
            notification.NextAttemptAt = null;
            notification.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var parameters = notification.Event switch
        {
            VendorNotificationEvent.VendorSubmitted or VendorNotificationEvent.VendorResubmitted =>
                new[] { shopName ?? "A vendor", productName },
            _ => new[] { productName },
        };

        var templateName = notification.MessageTemplateName ?? notification.Event.ToString();
        var result = await whatsApp.SendTemplateMessageAsync(notification.WhatsAppNumber, templateName, parameters, cancellationToken);

        if (result.Accepted)
        {
            notification.Status = WhatsAppNotificationStatus.Sent;
            notification.WhatsAppMessageId = result.MessageId;
            notification.SentAt = DateTime.UtcNow;
            notification.NextAttemptAt = null;
            notification.FailureReason = null;
        }
        else
        {
            notification.RetryCount++;
            notification.FailureReason = result.ErrorReason;

            if (notification.RetryCount >= options.Value.MaxRetries)
            {
                notification.Status = WhatsAppNotificationStatus.Failed;
                notification.NextAttemptAt = null;
            }
            else
            {
                notification.Status = WhatsAppNotificationStatus.Pending;
                var backoffSeconds = options.Value.RetryBaseDelaySeconds * Math.Pow(2, notification.RetryCount - 1);
                notification.NextAttemptAt = DateTime.UtcNow.AddSeconds(backoffSeconds);
            }
        }

        notification.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
