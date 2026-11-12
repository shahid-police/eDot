using System.ComponentModel.DataAnnotations;
using Elegance.Application.Common;
using Elegance.Application.Customers;
using Elegance.Application.Orders;

namespace Elegance.Application.Admin;

public sealed record AdminActor(long Id, string Email);

public class AdminLoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}

public class AdminProfileDto
{
    public required long Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}

public class AdminSession
{
    public required string AccessToken { get; init; }
    public required DateTime AccessTokenExpiresAt { get; init; }
    public required AdminProfileDto Admin { get; init; }
}

// Login returns exactly one of these. When TwoFactor is set, Session is null and the caller must
// submit a TOTP code via IAdminAuthService.VerifyTwoFactorAsync before getting a real session -
// the PendingToken alone cannot authorize any admin endpoint.
public class AdminLoginResult
{
    public AdminSession? Session { get; init; }
    public AdminTwoFactorChallenge? TwoFactor { get; init; }
}

public class AdminTwoFactorChallenge
{
    public required string PendingToken { get; init; }
    public required DateTime PendingTokenExpiresAt { get; init; }
}

public class VerifyAdminTwoFactorRequest
{
    [Required]
    public string PendingToken { get; init; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; init; } = string.Empty;
}

public interface IAdminAuthService
{
    Task<AdminLoginResult> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken = default);
    Task<AdminSession> VerifyTwoFactorAsync(VerifyAdminTwoFactorRequest request, CancellationToken cancellationToken = default);
}

public class StartTwoFactorEnrollmentRequest
{
    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}

public class TwoFactorEnrollmentDto
{
    public required string ManualEntryKey { get; init; }
    public required string QrCodePngBase64 { get; init; }
}

public class VerifyTwoFactorEnrollmentRequest
{
    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; init; } = string.Empty;
}

public class DisableTwoFactorRequest
{
    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; init; } = string.Empty;
}

public class TwoFactorStatusDto
{
    public required bool Enabled { get; init; }
}

public interface IAdminTwoFactorService
{
    Task<TwoFactorEnrollmentDto> StartEnrollmentAsync(long adminId, StartTwoFactorEnrollmentRequest request, CancellationToken cancellationToken = default);
    Task ConfirmEnrollmentAsync(long adminId, VerifyTwoFactorEnrollmentRequest request, CancellationToken cancellationToken = default);
    Task DisableAsync(long adminId, DisableTwoFactorRequest request, CancellationToken cancellationToken = default);
    Task<TwoFactorStatusDto> GetStatusAsync(long adminId, CancellationToken cancellationToken = default);
}

public class AdminProductQuery
{
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public long? CategoryId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminProductSummaryDto
{
    public required long Id { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required string CategoryName { get; init; }
    public required string ProductType { get; init; }
    public required decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public required int StockQuantity { get; init; }
    public required bool IsActive { get; init; }
}

public class AdminVariantDto
{
    public long? Id { get; init; }
    public required string Sku { get; init; }
    public required string VariantName { get; init; }
    public decimal? Price { get; init; }
    public decimal? SalePrice { get; init; }
    public required int StockQuantity { get; init; }
    public decimal? WeightGrams { get; init; }
    public string? Size { get; init; }
    public string? Color { get; init; }
    public required bool IsActive { get; init; }
    public string? ImageUrl { get; init; }
}

public class AdminProductImageDto
{
    public required long Id { get; init; }
    public required string Url { get; init; }
    public required string ImageType { get; init; }
    public required int SortOrder { get; init; }
}

public class AdminProductDto
{
    public required long Id { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public required decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal? CostPrice { get; init; }
    public required string Currency { get; init; }
    public required long CategoryId { get; init; }
    public required string ProductType { get; init; }
    public string? Material { get; init; }
    public string? Color { get; init; }
    public string? Occasion { get; init; }
    public decimal? WeightGrams { get; init; }
    public required int StockQuantity { get; init; }
    public int? LowStockThreshold { get; init; }
    public required bool IsFeatured { get; init; }
    public required bool IsNew { get; init; }
    public required bool IsBestSeller { get; init; }
    public required bool IsActive { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public string? SeoKeywords { get; init; }
    public string? VideoUrl { get; init; }
    public required IReadOnlyList<AdminVariantDto> Variants { get; init; }
    public required IReadOnlyList<AdminProductImageDto> Images { get; init; }
}

public class AdminVariantRequest
{
    public long? Id { get; init; }

    [Required, StringLength(64)]
    public string Sku { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string VariantName { get; init; } = string.Empty;

    [Range(0, 1000000)]
    public decimal? Price { get; init; }

    [Range(0, 1000000)]
    public decimal? SalePrice { get; init; }

    [Range(0, 1000000)]
    public int StockQuantity { get; init; }

    public decimal? WeightGrams { get; init; }

    [StringLength(50)]
    public string? Size { get; init; }

    [StringLength(50)]
    public string? Color { get; init; }

    public bool IsActive { get; init; } = true;
}

public class AdminProductRequest
{
    // Leave blank to auto-generate JEW-{CategoryCode}-NNN from the chosen category. Provide a
    // value to set the SKU explicitly instead - it is then validated and must be unique, exactly
    // like today.
    [StringLength(64)]
    public string? Sku { get; init; }

    [Required, StringLength(300)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(320)]
    public string Slug { get; init; } = string.Empty;

    [StringLength(500)]
    public string? ShortDescription { get; init; }

    public string? Description { get; init; }

    [Range(0, 1000000)]
    public decimal Price { get; init; }

    [Range(0, 1000000)]
    public decimal? SalePrice { get; init; }

    [Range(0, 1000000)]
    public decimal? CostPrice { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = "PKR";

    [Range(1, long.MaxValue)]
    public long CategoryId { get; init; }

    [Required, StringLength(20)]
    public string ProductType { get; init; } = "Simple";

    [StringLength(100)]
    public string? Material { get; init; }

    [StringLength(50)]
    public string? Color { get; init; }

    [StringLength(100)]
    public string? Occasion { get; init; }

    public decimal? WeightGrams { get; init; }

    [Range(0, 1000000)]
    public int StockQuantity { get; init; }

    [Range(0, 1000000)]
    public int? LowStockThreshold { get; init; }

    public bool IsFeatured { get; init; }
    public bool IsNew { get; init; }
    public bool IsBestSeller { get; init; }
    public bool IsActive { get; init; } = true;

    [StringLength(200)]
    public string? SeoTitle { get; init; }

    [StringLength(500)]
    public string? SeoDescription { get; init; }

    [StringLength(500)]
    public string? SeoKeywords { get; init; }

    [StringLength(500)]
    [Url]
    public string? VideoUrl { get; init; }

    public List<AdminVariantRequest> Variants { get; init; } = [];
}

public class AdminCategoryDto
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public long? ParentCategoryId { get; init; }
    public required int SortOrder { get; init; }
    public required bool IsActive { get; init; }
    public required int ProductCount { get; init; }

    // Short uppercase code used to build this category's product SKUs (JEW-{Code}-NNN). Null if
    // not configured yet - see AdminCatalogService.PreviewNextSkuAsync for how that is handled.
    public string? Code { get; init; }
}

public class AdminCategoryRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(220)]
    public string Slug { get; init; } = string.Empty;

    public string? Description { get; init; }
    public long? ParentCategoryId { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;

    [StringLength(10)]
    public string? Code { get; init; }
}

public class NextSkuDto
{
    public required string Sku { get; init; }
}

public interface IAdminCatalogService
{
    Task<PagedResult<AdminProductSummaryDto>> ListProductsAsync(AdminProductQuery query, CancellationToken cancellationToken = default);
    Task<AdminProductDto> GetProductAsync(long productId, CancellationToken cancellationToken = default);
    Task<AdminProductDto> CreateProductAsync(AdminActor actor, AdminProductRequest request, CancellationToken cancellationToken = default);
    Task<AdminProductDto> UpdateProductAsync(AdminActor actor, long productId, AdminProductRequest request, CancellationToken cancellationToken = default);
    Task<AdminProductDto> SetProductActiveAsync(AdminActor actor, long productId, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminCategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default);
    Task<AdminCategoryDto> CreateCategoryAsync(AdminActor actor, AdminCategoryRequest request, CancellationToken cancellationToken = default);
    Task<AdminCategoryDto> UpdateCategoryAsync(AdminActor actor, long categoryId, AdminCategoryRequest request, CancellationToken cancellationToken = default);
    Task<NextSkuDto> PreviewNextSkuAsync(long categoryId, CancellationToken cancellationToken = default);
}

public interface IAdminMediaService
{
    Task<AdminProductImageDto> UploadProductImageAsync(
        AdminActor actor, long productId, string imageType, Stream content, string contentType, long contentLength, CancellationToken cancellationToken = default);

    Task DeleteProductImageAsync(AdminActor actor, long productId, long imageId, CancellationToken cancellationToken = default);

    Task<AdminProductImageDto> ReorderProductImageAsync(AdminActor actor, long productId, long imageId, int sortOrder, CancellationToken cancellationToken = default);

    Task<string> UploadVariantImageAsync(
        AdminActor actor, long productId, long variantId, Stream content, string contentType, long contentLength, CancellationToken cancellationToken = default);

    Task RemoveVariantImageAsync(AdminActor actor, long productId, long variantId, CancellationToken cancellationToken = default);

    // Custom thumbnail override for a No Shop video - optional, falls back to YouTube's own
    // thumbnail when never set (or after RemoveNoShopVideoThumbnailAsync is called).
    Task<string> UploadNoShopVideoThumbnailAsync(
        AdminActor actor, long videoId, Stream content, string contentType, long contentLength, CancellationToken cancellationToken = default);

    Task RemoveNoShopVideoThumbnailAsync(AdminActor actor, long videoId, CancellationToken cancellationToken = default);
}

public class AdminOrderQuery
{
    public string? Status { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminOrderSummaryDto
{
    public required long Id { get; init; }
    public required string OrderNumber { get; init; }
    public required string Status { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required decimal GrandTotal { get; init; }
    public required string Currency { get; init; }
    public required string PaymentStatus { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public class OrderStatusRequest
{
    [Required, StringLength(30)]
    public string Status { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; init; }
}

public class PaymentStatusRequest
{
    [Required, StringLength(20)]
    public string Status { get; init; } = string.Empty;
}

public interface IAdminOrderService
{
    Task<PagedResult<AdminOrderSummaryDto>> ListOrdersAsync(AdminOrderQuery query, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> GetOrderAsync(long orderId, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> ChangeStatusAsync(AdminActor actor, long orderId, OrderStatusRequest request, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> SetPaymentStatusAsync(AdminActor actor, long orderId, PaymentStatusRequest request, CancellationToken cancellationToken = default);
}

public class LowStockItemDto
{
    public required long ProductId { get; init; }
    public long? VariantId { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required int StockQuantity { get; init; }
    public required int Threshold { get; init; }
}

public class StockAdjustmentRequest
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; init; }

    public long? VariantId { get; init; }

    [Range(-100000, 100000)]
    public int QuantityDelta { get; init; }

    [Required, StringLength(500)]
    public string Note { get; init; } = string.Empty;
}

public class StockMovementDto
{
    public required long Id { get; init; }
    public required long ProductId { get; init; }
    public long? VariantId { get; init; }
    public required int QuantityDelta { get; init; }
    public required string Reason { get; init; }
    public long? OrderId { get; init; }
    public string? Note { get; init; }
    public string? AdminEmail { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public interface IAdminInventoryService
{
    Task<IReadOnlyList<LowStockItemDto>> GetLowStockAsync(CancellationToken cancellationToken = default);
    Task<StockMovementDto> AdjustStockAsync(AdminActor actor, StockAdjustmentRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<StockMovementDto>> ListMovementsAsync(long? productId, int page, int pageSize, CancellationToken cancellationToken = default);
}

public class AdminCustomerQuery
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminCustomerSummaryDto
{
    public required long Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required bool IsActive { get; init; }
    public required int OrderCount { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public class AdminCustomerDetailDto
{
    public required AdminCustomerSummaryDto Customer { get; init; }
    public required IReadOnlyList<AddressDto> Addresses { get; init; }
    public required IReadOnlyList<OrderSummaryDto> Orders { get; init; }
}

public class ActiveStatusRequest
{
    public bool IsActive { get; init; }
}

public interface IAdminCustomerService
{
    Task<PagedResult<AdminCustomerSummaryDto>> ListCustomersAsync(AdminCustomerQuery query, CancellationToken cancellationToken = default);
    Task<AdminCustomerDetailDto> GetCustomerAsync(long customerId, CancellationToken cancellationToken = default);
    Task<AdminCustomerSummaryDto> SetCustomerActiveAsync(AdminActor actor, long customerId, bool isActive, CancellationToken cancellationToken = default);
}

public class AdminDashboardDto
{
    public required int OrdersToday { get; init; }
    public required int PendingOrders { get; init; }
    public required decimal Revenue { get; init; }
    public required int TotalOrders { get; init; }
    public required int CustomerCount { get; init; }
    public required int ActiveProductCount { get; init; }
    public required int LowStockCount { get; init; }
    public required IReadOnlyList<AdminOrderSummaryDto> RecentOrders { get; init; }
}

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public class AuditLogDto
{
    public required long Id { get; init; }
    public required string AdminEmail { get; init; }
    public required string Action { get; init; }
    public required string EntityType { get; init; }
    public string? EntityId { get; init; }
    public required string Summary { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public interface IAuditLogService
{
    Task RecordAsync(AdminActor actor, string action, string entityType, string? entityId, string summary, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLogDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

public class AdminNoShopVideoQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public long? CategoryId { get; init; }
    public string? VideoType { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminNoShopVideoSummaryDto
{
    public required long Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string ThumbnailUrl { get; init; }
    public required string CategoryName { get; init; }
    public required string VideoType { get; init; }
    public required bool IsFeatured { get; init; }
    public required string Status { get; init; }
    public required int DisplayOrder { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public class AdminNoShopVideoDto
{
    public required long Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public required string YouTubeUrl { get; init; }
    public required string YouTubeVideoId { get; init; }
    public required string ThumbnailUrl { get; init; }
    public required long CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required string VideoType { get; init; }
    public required bool IsFeatured { get; init; }
    public required int DisplayOrder { get; init; }
    public required string Status { get; init; }
    public DateTime? PublishedAt { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

public class AdminNoShopVideoRequest
{
    [Required, StringLength(300)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(320)]
    public string Slug { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    // Accepts a full YouTube URL (watch/shorts/youtu.be/embed) or a bare 11-character video ID -
    // see Elegance.Infrastructure.Content.YouTubeUrlParser for what is extracted from it.
    [Required, StringLength(500)]
    public string YouTubeUrl { get; init; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long CategoryId { get; init; }

    [Required, StringLength(20)]
    public string VideoType { get; init; } = "Entertainment";

    public bool IsFeatured { get; init; }
    public int DisplayOrder { get; init; }

    [Required, StringLength(20)]
    public string Status { get; init; } = "Draft";

    // Leave blank while publishing to publish immediately; set a future date/time to schedule.
    public DateTime? PublishedAt { get; init; }

    [StringLength(200)]
    public string? SeoTitle { get; init; }

    [StringLength(500)]
    public string? SeoDescription { get; init; }
}

public class AdminNoShopVideoStatusRequest
{
    [Required, StringLength(20)]
    public string Status { get; init; } = string.Empty;
}

public class AdminNoShopCategoryDto
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public required int SortOrder { get; init; }
    public required bool IsActive { get; init; }
    public required int VideoCount { get; init; }
}

public class AdminNoShopCategoryRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(220)]
    public string Slug { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public interface IAdminNoShopService
{
    Task<PagedResult<AdminNoShopVideoSummaryDto>> ListVideosAsync(AdminNoShopVideoQuery query, CancellationToken cancellationToken = default);
    Task<AdminNoShopVideoDto> GetVideoAsync(long videoId, CancellationToken cancellationToken = default);
    Task<AdminNoShopVideoDto> CreateVideoAsync(AdminActor actor, AdminNoShopVideoRequest request, CancellationToken cancellationToken = default);
    Task<AdminNoShopVideoDto> UpdateVideoAsync(AdminActor actor, long videoId, AdminNoShopVideoRequest request, CancellationToken cancellationToken = default);
    Task<AdminNoShopVideoDto> SetVideoStatusAsync(AdminActor actor, long videoId, string status, CancellationToken cancellationToken = default);
    Task DeleteVideoAsync(AdminActor actor, long videoId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminNoShopCategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default);
    Task<AdminNoShopCategoryDto> CreateCategoryAsync(AdminActor actor, AdminNoShopCategoryRequest request, CancellationToken cancellationToken = default);
    Task<AdminNoShopCategoryDto> UpdateCategoryAsync(AdminActor actor, long categoryId, AdminNoShopCategoryRequest request, CancellationToken cancellationToken = default);
}

public class AdminBackInStockSummaryDto
{
    public required int ActiveSubscriptions { get; init; }
    public required int NotifiedSubscriptions { get; init; }
    public required int UnsubscribedSubscriptions { get; init; }
    public required int PendingNotifications { get; init; }
    public required int QueuedNotifications { get; init; }
    public required int SentNotifications { get; init; }
    public required int DeliveredNotifications { get; init; }
    public required int ReadNotifications { get; init; }
    public required int FailedNotifications { get; init; }
}

public class AdminBackInStockSubscriptionQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminBackInStockSubscriptionDto
{
    public required long Id { get; init; }
    public required long ProductId { get; init; }
    public required string ProductName { get; init; }
    public long? ProductVariantId { get; init; }
    public string? VariantName { get; init; }
    public required string CustomerName { get; init; }
    public required string WhatsAppNumber { get; init; }
    public required string Status { get; init; }
    public required DateTime SubscribedAt { get; init; }
    public DateTime? NotifiedAt { get; init; }

    // A wa.me "click to chat" link pre-filled with the approved back-in-stock message text, for an
    // admin to send manually from their own WhatsApp while real Cloud API credentials aren't set up.
    public required string WhatsAppChatUrl { get; init; }
}

public class AdminWhatsAppNotificationQuery
{
    public string? Status { get; init; }
    public long? ProductId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminWhatsAppNotificationDto
{
    public required long Id { get; init; }
    public required long ProductId { get; init; }
    public required string ProductName { get; init; }
    public required string WhatsAppNumber { get; init; }
    public required string Status { get; init; }
    public required int RetryCount { get; init; }
    public string? FailureReason { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public interface IAdminBackInStockService
{
    Task<AdminBackInStockSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminBackInStockSubscriptionDto>> ListSubscriptionsAsync(AdminBackInStockSubscriptionQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminWhatsAppNotificationDto>> ListNotificationsAsync(AdminWhatsAppNotificationQuery query, CancellationToken cancellationToken = default);
}
