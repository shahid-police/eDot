using System.Text.Json;
using Elegance.Application.Notifications;
using Elegance.Domain.Notifications.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Elegance.Infrastructure.Notifications;

// Turns Meta's "messages" webhook payload (statuses: sent/delivered/read/failed) into updates on
// the WhatsAppNotification row that matches the message id. This is the ONLY source of truth for
// Delivered/Read - the worker that sends the message only ever knows "the API accepted it".
public class WhatsAppWebhookService(EleganceDbContext dbContext, ILogger<WhatsAppWebhookService> logger) : IWhatsAppWebhookService
{
    // Pending/Queued/Failed aren't part of this forward progression - Failed is a separate
    // terminal outcome (always applied below, never compared against this), and Pending/Queued
    // are states only the send worker ever sets.
    private static readonly Dictionary<WhatsAppNotificationStatus, int> DeliveryProgress = new()
    {
        [WhatsAppNotificationStatus.Sent] = 1,
        [WhatsAppNotificationStatus.Delivered] = 2,
        [WhatsAppNotificationStatus.Read] = 3,
    };

    public async Task ProcessStatusUpdatesAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        if (!payload.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value) ||
                    !value.TryGetProperty("statuses", out var statuses) ||
                    statuses.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var status in statuses.EnumerateArray())
                {
                    await ApplyStatusAsync(status, cancellationToken);
                }
            }
        }
    }

    private async Task ApplyStatusAsync(JsonElement status, CancellationToken cancellationToken)
    {
        if (!status.TryGetProperty("id", out var idProp) || idProp.GetString() is not { } messageId)
        {
            return;
        }

        var rawStatus = status.TryGetProperty("status", out var s) ? s.GetString() : null;
        var newStatus = rawStatus?.ToLowerInvariant() switch
        {
            "sent" => WhatsAppNotificationStatus.Sent,
            "delivered" => WhatsAppNotificationStatus.Delivered,
            "read" => WhatsAppNotificationStatus.Read,
            "failed" => WhatsAppNotificationStatus.Failed,
            _ => (WhatsAppNotificationStatus?)null,
        };
        if (newStatus is null)
        {
            logger.LogWarning("Unrecognized WhatsApp status webhook value '{Status}' for message {MessageId}", rawStatus, messageId);
            return;
        }

        var notification = await dbContext.WhatsAppNotifications
            .FirstOrDefaultAsync(n => n.WhatsAppMessageId == messageId, cancellationToken);
        if (notification is null)
        {
            // Either a status for a message this app didn't send, or it arrived before our own
            // Sent update committed - safe to ignore either way, nothing to attach it to yet.
            return;
        }

        if (newStatus.Value != WhatsAppNotificationStatus.Failed &&
            DeliveryProgress.GetValueOrDefault(newStatus.Value) <= DeliveryProgress.GetValueOrDefault(notification.Status))
        {
            // A webhook can arrive out of order - never let a late "sent" overwrite an
            // already-processed "delivered" or "read".
            return;
        }

        notification.Status = newStatus.Value;
        notification.UpdatedAt = DateTime.UtcNow;

        if (newStatus.Value == WhatsAppNotificationStatus.Delivered)
        {
            notification.DeliveredAt = DateTime.UtcNow;
        }
        else if (newStatus.Value == WhatsAppNotificationStatus.Failed)
        {
            notification.FailureReason = ExtractErrorMessage(status) ?? notification.FailureReason ?? "Reported failed by WhatsApp.";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? ExtractErrorMessage(JsonElement status)
    {
        if (status.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0 &&
            errors[0].TryGetProperty("title", out var title))
        {
            return title.GetString();
        }

        return null;
    }
}
