using Elegance.Application.Common;
using Elegance.Application.Notifications;
using Elegance.Domain.Notifications;
using Elegance.Domain.Notifications.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Notifications;

public class BackInStockService(EleganceDbContext dbContext) : IBackInStockService
{
    public async Task<BackInStockSubscriptionDto> SubscribeAsync(BackInStockSubscribeRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.ConsentGiven)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Please agree to receive a WhatsApp notification to continue.");
        }

        var customerName = request.CustomerName.Trim();
        if (customerName.Length == 0)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Enter your name.");
        }

        if (!PhoneNumberNormalizer.TryNormalize(request.WhatsAppNumber, out var normalizedNumber))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Enter a valid WhatsApp number, including the country code if it isn't a Pakistani number.");
        }

        var productActive = await dbContext.Products.AsNoTracking()
            .AnyAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);
        if (!productActive)
        {
            throw new ApplicationFailure(FailureKind.NotFound, "That product could not be found.");
        }

        if (request.ProductVariantId is { } variantId)
        {
            var variantBelongs = await dbContext.ProductVariants.AsNoTracking()
                .AnyAsync(v => v.Id == variantId && v.ProductId == request.ProductId && v.IsActive, cancellationToken);
            if (!variantBelongs)
            {
                throw new ApplicationFailure(FailureKind.NotFound, "That option could not be found.");
            }
        }

        var now = DateTime.UtcNow;

        // Reuse an existing Active subscription for this exact product/variant/number rather than
        // creating a duplicate - a prior Notified or Unsubscribed row for the same key is left
        // alone (it's history from an earlier subscribe cycle) and a fresh Active row is only
        // ever created when none is currently active.
        var existing = await dbContext.BackInStockSubscriptions
            .FirstOrDefaultAsync(
                s => s.ProductId == request.ProductId
                    && s.ProductVariantId == request.ProductVariantId
                    && s.NormalizedWhatsAppNumber == normalizedNumber
                    && s.Status == SubscriptionStatus.Active,
                cancellationToken);

        if (existing is not null)
        {
            existing.CustomerName = customerName;
            existing.WhatsAppNumber = request.WhatsAppNumber.Trim();
            existing.ConsentGiven = true;
            existing.ConsentTimestamp = now;
            existing.SubscribedAt = now;
            existing.UpdatedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);

            return ToDto(existing);
        }

        var subscription = new BackInStockSubscription
        {
            ProductId = request.ProductId,
            ProductVariantId = request.ProductVariantId,
            CustomerName = customerName,
            WhatsAppNumber = request.WhatsAppNumber.Trim(),
            NormalizedWhatsAppNumber = normalizedNumber,
            ConsentGiven = true,
            ConsentTimestamp = now,
            Status = SubscriptionStatus.Active,
            SubscribedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.BackInStockSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(subscription);
    }

    private static BackInStockSubscriptionDto ToDto(BackInStockSubscription s) => new()
    {
        Id = s.Id,
        ProductId = s.ProductId,
        ProductVariantId = s.ProductVariantId,
        Status = s.Status.ToString(),
    };
}
