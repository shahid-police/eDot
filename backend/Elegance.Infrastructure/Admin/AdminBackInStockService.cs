using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Notifications.Enums;
using Elegance.Infrastructure.Notifications;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Elegance.Infrastructure.Admin;

public class AdminBackInStockService(EleganceDbContext dbContext, IOptions<WhatsAppOptions> whatsAppOptions) : IAdminBackInStockService
{
    public async Task<AdminBackInStockSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var subscriptionCounts = await dbContext.BackInStockSubscriptions
            .AsNoTracking()
            .GroupBy(s => s.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

        var notificationCounts = await dbContext.WhatsAppNotifications
            .AsNoTracking()
            .GroupBy(n => n.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

        return new AdminBackInStockSummaryDto
        {
            ActiveSubscriptions = subscriptionCounts.GetValueOrDefault(SubscriptionStatus.Active),
            NotifiedSubscriptions = subscriptionCounts.GetValueOrDefault(SubscriptionStatus.Notified),
            UnsubscribedSubscriptions = subscriptionCounts.GetValueOrDefault(SubscriptionStatus.Unsubscribed),
            PendingNotifications = notificationCounts.GetValueOrDefault(WhatsAppNotificationStatus.Pending),
            QueuedNotifications = notificationCounts.GetValueOrDefault(WhatsAppNotificationStatus.Queued),
            SentNotifications = notificationCounts.GetValueOrDefault(WhatsAppNotificationStatus.Sent),
            DeliveredNotifications = notificationCounts.GetValueOrDefault(WhatsAppNotificationStatus.Delivered),
            ReadNotifications = notificationCounts.GetValueOrDefault(WhatsAppNotificationStatus.Read),
            FailedNotifications = notificationCounts.GetValueOrDefault(WhatsAppNotificationStatus.Failed),
        };
    }

    public async Task<PagedResult<AdminBackInStockSubscriptionDto>> ListSubscriptionsAsync(
        AdminBackInStockSubscriptionQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var subscriptions = dbContext.BackInStockSubscriptions.AsNoTracking().AsQueryable();

        if (Enum.TryParse<SubscriptionStatus>(query.Status, true, out var status))
        {
            subscriptions = subscriptions.Where(s => s.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            subscriptions = subscriptions.Where(s =>
                s.CustomerName.Contains(term) ||
                s.NormalizedWhatsAppNumber.Contains(term) ||
                s.Product.Name.Contains(term));
        }

        var total = await subscriptions.CountAsync(cancellationToken);
        var rows = await subscriptions
            .OrderByDescending(s => s.SubscribedAt)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.ProductId,
                ProductName = s.Product.Name,
                ProductSlug = s.Product.Slug,
                ProductPrice = s.Product.Price,
                ProductSalePrice = s.Product.SalePrice,
                s.ProductVariantId,
                VariantName = s.ProductVariant != null ? s.ProductVariant.VariantName : null,
                VariantPrice = s.ProductVariant != null ? s.ProductVariant.Price : null,
                VariantSalePrice = s.ProductVariant != null ? s.ProductVariant.SalePrice : null,
                s.CustomerName,
                s.NormalizedWhatsAppNumber,
                s.Status,
                s.SubscribedAt,
                s.NotifiedAt,
            })
            .ToListAsync(cancellationToken);

        // The chat link's text calls Uri.EscapeDataString, which EF Core cannot translate to SQL -
        // built here, after materialization, from the raw columns fetched above.
        var items = rows.Select(r =>
        {
            var effectivePrice = r.ProductVariantId is not null
                ? r.VariantSalePrice ?? r.VariantPrice ?? r.ProductPrice
                : r.ProductSalePrice ?? r.ProductPrice;
            var productUrl = $"{whatsAppOptions.Value.FrontendBaseUrl.TrimEnd('/')}/product-default/{r.ProductSlug}";
            var message = WhatsAppMessageTemplates.BuildBackInStockMessage(r.CustomerName, r.ProductName, $"{effectivePrice:N0} PKR", productUrl);

            return new AdminBackInStockSubscriptionDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.ProductName,
                ProductVariantId = r.ProductVariantId,
                VariantName = r.VariantName,
                CustomerName = r.CustomerName,
                WhatsAppNumber = r.NormalizedWhatsAppNumber,
                Status = r.Status.ToString(),
                SubscribedAt = r.SubscribedAt,
                NotifiedAt = r.NotifiedAt,
                WhatsAppChatUrl = WhatsAppMessageTemplates.BuildChatUrl(r.NormalizedWhatsAppNumber, message),
            };
        }).ToList();

        return new PagedResult<AdminBackInStockSubscriptionDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<PagedResult<AdminWhatsAppNotificationDto>> ListNotificationsAsync(
        AdminWhatsAppNotificationQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var notifications = dbContext.WhatsAppNotifications.AsNoTracking().AsQueryable();

        if (Enum.TryParse<WhatsAppNotificationStatus>(query.Status, true, out var status))
        {
            notifications = notifications.Where(n => n.Status == status);
        }

        if (query.ProductId is { } productId)
        {
            notifications = notifications.Where(n => n.ProductId == productId);
        }

        var total = await notifications.CountAsync(cancellationToken);
        var rows = await notifications
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new AdminWhatsAppNotificationDto
            {
                Id = n.Id,
                ProductId = n.ProductId,
                ProductName = n.Product.Name,
                WhatsAppNumber = n.WhatsAppNumber,
                Status = n.Status.ToString(),
                RetryCount = n.RetryCount,
                FailureReason = n.FailureReason,
                SentAt = n.SentAt,
                DeliveredAt = n.DeliveredAt,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminWhatsAppNotificationDto>
        {
            Items = rows,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }
}
