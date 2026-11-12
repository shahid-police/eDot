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

// Runs continuously in the API process - no separate worker deployment, no Hangfire/RabbitMQ,
// per the spec's own preference for the simplest infrastructure that satisfies the requirements.
// Each tick does two things, both transactionally safe to re-run if the process restarts
// mid-batch (nothing here assumes a previous tick finished cleanly):
//
//   1. Fan out unprocessed RestockEvents into Pending WhatsAppNotification rows (one per eligible
//      Active subscription for that exact product/variant), then mark the event processed.
//   2. Send any notification whose NextAttemptAt is due, with exponential backoff on failure.
public class BackInStockNotificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WhatsAppOptions> options,
    ILogger<BackInStockNotificationWorker> logger) : BackgroundService
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

                await QueuePendingRestockEventsAsync(dbContext, stoppingToken);
                await SendDueNotificationsAsync(dbContext, whatsApp, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A single bad tick (e.g. a transient DB hiccup) should never kill the worker -
                // the next tick just picks up wherever the data actually is.
                logger.LogError(ex, "Back-in-stock notification worker tick failed");
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

    private async Task QueuePendingRestockEventsAsync(EleganceDbContext dbContext, CancellationToken cancellationToken)
    {
        var events = await dbContext.RestockEvents
            .Where(e => e.ProcessedAt == null)
            .OrderBy(e => e.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
        {
            return;
        }

        foreach (var restockEvent in events)
        {
            // Scenario 6: never queue notifications for an unpublished product, even though it
            // technically just restocked.
            var productIsActive = await dbContext.Products.AsNoTracking()
                .Where(p => p.Id == restockEvent.ProductId)
                .Select(p => (bool?)p.IsActive)
                .FirstOrDefaultAsync(cancellationToken) ?? false;

            if (productIsActive)
            {
                var subscriptions = await dbContext.BackInStockSubscriptions
                    .Where(s => s.ProductId == restockEvent.ProductId
                        && s.ProductVariantId == restockEvent.VariantId
                        && s.Status == SubscriptionStatus.Active)
                    .ToListAsync(cancellationToken);

                var now = DateTime.UtcNow;
                foreach (var subscription in subscriptions)
                {
                    dbContext.WhatsAppNotifications.Add(new WhatsAppNotification
                    {
                        SubscriptionId = subscription.Id,
                        RestockEventId = restockEvent.Id,
                        ProductId = restockEvent.ProductId,
                        WhatsAppNumber = subscription.NormalizedWhatsAppNumber,
                        MessageTemplateName = options.Value.TemplateName,
                        Status = WhatsAppNotificationStatus.Pending,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });

                    // Terminal for this subscribe cycle - the customer must subscribe again to be
                    // notified of a future restock after the item sells out again (spec scenario 4).
                    subscription.Status = SubscriptionStatus.Notified;
                    subscription.NotifiedAt = now;
                    subscription.UpdatedAt = now;
                }
            }

            restockEvent.ProcessedAt = DateTime.UtcNow;
        }

        // The unique index on (SubscriptionId, RestockEventId) is the real idempotency guarantee
        // if this method is ever somehow re-entered for the same event; this save either commits
        // cleanly or (in that edge case) throws and leaves the event unprocessed for a clean retry.
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SendDueNotificationsAsync(EleganceDbContext dbContext, IWhatsAppNotificationService whatsApp, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var due = await dbContext.WhatsAppNotifications
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

    private async Task SendOneAsync(EleganceDbContext dbContext, IWhatsAppNotificationService whatsApp, WhatsAppNotification notification, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.BackInStockSubscriptions.AsNoTracking()
            .Where(s => s.Id == notification.SubscriptionId)
            .Select(s => new { s.CustomerName })
            .FirstOrDefaultAsync(cancellationToken);

        var product = await dbContext.Products.AsNoTracking()
            .Where(p => p.Id == notification.ProductId)
            .Select(p => new { p.Name, p.Slug, p.Price, p.SalePrice, p.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null || product is null || !product.IsActive)
        {
            // Unpublished (or otherwise gone) since queueing - not retryable, record why and stop.
            notification.Status = WhatsAppNotificationStatus.Failed;
            notification.FailureReason = "Product is no longer available.";
            notification.NextAttemptAt = null;
            notification.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var price = product.SalePrice ?? product.Price;
        var productUrl = $"{options.Value.FrontendBaseUrl.TrimEnd('/')}/product-default/{product.Slug}";
        var parameters = new[]
        {
            subscription.CustomerName,
            product.Name,
            $"{price:N0} PKR",
            productUrl,
        };

        var result = await whatsApp.SendTemplateMessageAsync(
            notification.WhatsAppNumber,
            notification.MessageTemplateName ?? options.Value.TemplateName,
            parameters,
            cancellationToken);

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
