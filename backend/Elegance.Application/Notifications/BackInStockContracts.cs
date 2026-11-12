using System.ComponentModel.DataAnnotations;

namespace Elegance.Application.Notifications;

public class BackInStockSubscribeRequest
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; init; }

    public long? ProductVariantId { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string CustomerName { get; init; } = string.Empty;

    // Validated server-side and normalized to E.164 - see PhoneNumberNormalizer. Raw user input,
    // so only a generous length cap here; the real validation happens in the service.
    [Required, StringLength(32, MinimumLength = 1)]
    public string WhatsAppNumber { get; init; } = string.Empty;

    // Must be true - enforced again in the service, not just by this attribute, since a client
    // could omit the field entirely rather than send false.
    [Required]
    public bool ConsentGiven { get; init; }
}

public class BackInStockSubscriptionDto
{
    public required long Id { get; init; }
    public required long ProductId { get; init; }
    public long? ProductVariantId { get; init; }
    public required string Status { get; init; }
}

public interface IBackInStockService
{
    Task<BackInStockSubscriptionDto> SubscribeAsync(BackInStockSubscribeRequest request, CancellationToken cancellationToken = default);
}
