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

public interface IAdminAuthService
{
    Task<AdminSession> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken = default);
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
    [Required, StringLength(64)]
    public string Sku { get; init; } = string.Empty;

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
