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
    public string? ProductType { get; init; }

    // One of: budget, premium, luxury, unassigned. Unrecognized or omitted applies no filter.
    public string? ShoppingPreference { get; init; }

    // One of: sku, name, category, type, price, stock, active. Unrecognized or omitted falls back
    // to the original default (newest first).
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }

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
    public string? ShoppingPreference { get; init; }
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
    public string? ShoppingPreference { get; init; }
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

    // One of: Budget, Premium, Luxury. Leave blank/null to leave the product Unassigned - never
    // forced, never derived from price.
    [StringLength(20)]
    public string? ShoppingPreference { get; init; }

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

public class AdminProductDiscountRequest
{
    // Null applies the discount to the simple product itself. Set it to one of the product's
    // own variant IDs for a Variant-type product - the service rejects a variant that doesn't
    // belong to the product being discounted.
    public long? ProductVariantId { get; init; }

    [Range(0.01, 100)]
    public decimal DiscountPercentage { get; init; }

    [Required]
    public DateTime StartDate { get; init; }

    [Required]
    public DateTime EndDate { get; init; }
}

public class AdminProductDiscountDto
{
    public required long Id { get; init; }
    public required long ProductId { get; init; }
    public long? ProductVariantId { get; init; }
    public string? VariantName { get; init; }
    public required decimal OriginalPriceSnapshot { get; init; }
    public required decimal DiscountPercentage { get; init; }
    public required decimal DiscountAmount { get; init; }
    public required decimal FinalPriceSnapshot { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }

    // Persisted admin-controlled state: "Active" or "Cancelled".
    public required string Status { get; init; }

    // Derived from Status plus StartDate/EndDate against the current time: "Scheduled", "Active",
    // "Expired", or "Cancelled" - what the admin UI and reports should actually display/filter on.
    public required string EffectiveStatus { get; init; }

    public string? CreatedByEmail { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public interface IAdminProductDiscountService
{
    Task<IReadOnlyList<AdminProductDiscountDto>> ListProductDiscountsAsync(long productId, CancellationToken cancellationToken = default);
    Task<AdminProductDiscountDto> CreateProductDiscountAsync(AdminActor actor, long productId, AdminProductDiscountRequest request, CancellationToken cancellationToken = default);
    Task CancelProductDiscountAsync(AdminActor actor, long productId, long discountId, CancellationToken cancellationToken = default);
}

public class AdminPromoterQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminPromoterRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    // Validated server-side and normalized to E.164 - see PhoneNumberNormalizer.
    [Required, StringLength(32)]
    public string WhatsAppNumber { get; init; } = string.Empty;

    [EmailAddress, StringLength(254)]
    public string? Email { get; init; }

    [StringLength(500)]
    public string? Address { get; init; }

    [Required, StringLength(30)]
    public string PromoterType { get; init; } = "Individual";

    [Range(0, 100)]
    public decimal CommissionPercentage { get; init; }

    [StringLength(1000)]
    public string? Notes { get; init; }
}

public class AdminPromoterStatusRequest
{
    [Required, StringLength(20)]
    public string Status { get; init; } = string.Empty;
}

public class AdminPromoterDto
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required string WhatsAppNumber { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public required string PromoterType { get; init; }
    public required decimal CommissionPercentage { get; init; }
    public required string Status { get; init; }
    public string? Notes { get; init; }
    public required int PromoCodeCount { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public interface IAdminPromoterService
{
    Task<PagedResult<AdminPromoterDto>> ListPromotersAsync(AdminPromoterQuery query, CancellationToken cancellationToken = default);
    Task<AdminPromoterDto> GetPromoterAsync(long promoterId, CancellationToken cancellationToken = default);
    Task<AdminPromoterDto> CreatePromoterAsync(AdminActor actor, AdminPromoterRequest request, CancellationToken cancellationToken = default);
    Task<AdminPromoterDto> UpdatePromoterAsync(AdminActor actor, long promoterId, AdminPromoterRequest request, CancellationToken cancellationToken = default);
    Task<AdminPromoterDto> SetPromoterStatusAsync(AdminActor actor, long promoterId, string status, CancellationToken cancellationToken = default);
}

public class AdminPromoCodeQuery
{
    public string? Search { get; init; }
    public long? PromoterId { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminPromoCodeRequest
{
    [Range(1, long.MaxValue)]
    public long PromoterId { get; init; }

    public bool GrantsCustomerDiscount { get; init; }

    [Range(0, 100)]
    public decimal CustomerDiscountPercentage { get; init; }

    public bool GrantsCommission { get; init; } = true;

    [Range(0, 100)]
    public decimal? CommissionPercentageOverride { get; init; }

    [Range(0, 1000000)]
    public decimal? MinimumOrderAmount { get; init; }

    [Range(0, 1000000)]
    public decimal? MaximumDiscountAmount { get; init; }

    [Range(1, int.MaxValue)]
    public int? MaximumTotalUses { get; init; }

    [Range(1, int.MaxValue)]
    public int? MaximumUsesPerCustomer { get; init; }

    [Required]
    public DateTime StartDate { get; init; }

    public DateTime? ExpiryDate { get; init; }

    public List<long> ProductIds { get; init; } = [];
    public List<long> CategoryIds { get; init; } = [];
}

public class AdminPromoCodeStatusRequest
{
    [Required, StringLength(20)]
    public string Status { get; init; } = string.Empty;
}

public class AdminPromoCodeDto
{
    public required long Id { get; init; }
    public required string Code { get; init; }
    public required long PromoterId { get; init; }
    public required string PromoterName { get; init; }
    public required bool GrantsCustomerDiscount { get; init; }
    public required decimal CustomerDiscountPercentage { get; init; }
    public required bool GrantsCommission { get; init; }
    public decimal? CommissionPercentageOverride { get; init; }
    public decimal? MinimumOrderAmount { get; init; }
    public decimal? MaximumDiscountAmount { get; init; }
    public int? MaximumTotalUses { get; init; }
    public int? MaximumUsesPerCustomer { get; init; }
    public required int UsedCount { get; init; }
    public required DateTime StartDate { get; init; }
    public DateTime? ExpiryDate { get; init; }

    // Persisted admin-controlled state: "Active" or "Inactive".
    public required string Status { get; init; }

    // Derived from Status plus StartDate/ExpiryDate against the current time: "Scheduled",
    // "Active", "Expired", or "Inactive".
    public required string EffectiveStatus { get; init; }

    public required IReadOnlyList<long> ProductIds { get; init; }
    public required IReadOnlyList<long> CategoryIds { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public interface IAdminPromoCodeService
{
    Task<PagedResult<AdminPromoCodeDto>> ListPromoCodesAsync(AdminPromoCodeQuery query, CancellationToken cancellationToken = default);
    Task<AdminPromoCodeDto> GetPromoCodeAsync(long promoCodeId, CancellationToken cancellationToken = default);
    Task<AdminPromoCodeDto> CreatePromoCodeAsync(AdminActor actor, AdminPromoCodeRequest request, CancellationToken cancellationToken = default);
    Task<AdminPromoCodeDto> UpdatePromoCodeAsync(AdminActor actor, long promoCodeId, AdminPromoCodeRequest request, CancellationToken cancellationToken = default);
    Task<AdminPromoCodeDto> SetPromoCodeStatusAsync(AdminActor actor, long promoCodeId, string status, CancellationToken cancellationToken = default);
    Task<AdminPromoCodeDto> RegeneratePromoCodeAsync(AdminActor actor, long promoCodeId, CancellationToken cancellationToken = default);
}

public class AdminCommissionQuery
{
    public long? PromoterId { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminCommissionDto
{
    public required long Id { get; init; }
    public required long PromoterId { get; init; }
    public required string PromoterName { get; init; }
    public required long PromoCodeId { get; init; }
    public required string PromoCode { get; init; }
    public required long OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required string OrderStatus { get; init; }
    public required decimal CommissionPercentageSnapshot { get; init; }
    public required decimal EligibleSaleAmount { get; init; }
    public required decimal CommissionAmount { get; init; }
    public required string Currency { get; init; }
    public required string Status { get; init; }
    public string? Remarks { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? PayableAt { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime? ReversedAt { get; init; }
}

public class AdminCommissionRemarksRequest
{
    [Required, StringLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public class AdminCommissionSummaryDto
{
    public required int PendingCount { get; init; }
    public required int ApprovedCount { get; init; }
    public required int PayableCount { get; init; }
    public required int PaidCount { get; init; }
    public required int ReversedCount { get; init; }
    public required int CancelledCount { get; init; }
    public required decimal PendingAmount { get; init; }
    public required decimal PayableAmount { get; init; }
    public required decimal PaidAmount { get; init; }
}

public interface IAdminCommissionService
{
    Task<AdminCommissionSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminCommissionDto>> ListCommissionsAsync(AdminCommissionQuery query, CancellationToken cancellationToken = default);
    Task<AdminCommissionDto> ApproveCommissionAsync(AdminActor actor, long commissionId, CancellationToken cancellationToken = default);
    Task<AdminCommissionDto> MarkPayableAsync(AdminActor actor, long commissionId, CancellationToken cancellationToken = default);
    Task<AdminCommissionDto> ReverseCommissionAsync(AdminActor actor, long commissionId, AdminCommissionRemarksRequest request, CancellationToken cancellationToken = default);
    Task<AdminCommissionDto> CancelCommissionAsync(AdminActor actor, long commissionId, AdminCommissionRemarksRequest request, CancellationToken cancellationToken = default);
}

public class AdminPayableCommissionDto
{
    public required long CommissionId { get; init; }
    public required long OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required decimal CommissionAmount { get; init; }
    public required decimal AllocatedAmount { get; init; }
    public required decimal RemainingAmount { get; init; }
    public required string Status { get; init; }
}

public class AdminPromoterBalanceDto
{
    public required long PromoterId { get; init; }
    public required string PromoterName { get; init; }
    public required decimal OutstandingBalance { get; init; }
    public required IReadOnlyList<AdminPayableCommissionDto> PayableCommissions { get; init; }
}

public class AdminPromoterPaymentAllocationRequest
{
    [Range(1, long.MaxValue)]
    public long CommissionId { get; init; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; init; }
}

public class AdminPromoterPaymentRequest
{
    [Range(1, long.MaxValue)]
    public long PromoterId { get; init; }

    [Required]
    public DateTime PaymentDate { get; init; }

    [Required, StringLength(50)]
    public string PaymentMethod { get; init; } = string.Empty;

    [StringLength(100)]
    public string? TransactionReference { get; init; }

    [StringLength(1000)]
    public string? Remarks { get; init; }

    public List<AdminPromoterPaymentAllocationRequest> Allocations { get; init; } = [];
}

public class AdminPromoterPaymentAllocationDto
{
    public required long CommissionId { get; init; }
    public required long OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required decimal Amount { get; init; }
}

public class AdminPromoterPaymentDto
{
    public required long Id { get; init; }
    public required string PaymentReference { get; init; }
    public required long PromoterId { get; init; }
    public required string PromoterName { get; init; }
    public required decimal TotalPaymentAmount { get; init; }
    public required DateTime PaymentDate { get; init; }
    public required string PaymentMethod { get; init; }
    public string? TransactionReference { get; init; }
    public required string Status { get; init; }
    public string? Remarks { get; init; }
    public required IReadOnlyList<AdminPromoterPaymentAllocationDto> Allocations { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public class AdminPromoterPaymentQuery
{
    public long? PromoterId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class AdminVoidPaymentRequest
{
    [Required, StringLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public interface IAdminPromoterPaymentService
{
    Task<AdminPromoterBalanceDto> GetPromoterBalanceAsync(long promoterId, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminPromoterPaymentDto>> ListPaymentsAsync(AdminPromoterPaymentQuery query, CancellationToken cancellationToken = default);
    Task<AdminPromoterPaymentDto> GetPaymentAsync(long paymentId, CancellationToken cancellationToken = default);
    Task<AdminPromoterPaymentDto> CreatePaymentAsync(AdminActor actor, AdminPromoterPaymentRequest request, CancellationToken cancellationToken = default);
    Task<AdminPromoterPaymentDto> VoidPaymentAsync(AdminActor actor, long paymentId, AdminVoidPaymentRequest request, CancellationToken cancellationToken = default);
}

public class AdminMarketingDashboardDto
{
    public required int ActiveDiscountCount { get; init; }
    public required int ScheduledDiscountCount { get; init; }
    public required int ExpiredDiscountCount { get; init; }
    public required int TotalPromoCodes { get; init; }
    public required int ActivePromoCodes { get; init; }
    public required int TotalPromoCodeUses { get; init; }
    public required decimal TotalCustomerDiscountGiven { get; init; }
    public required int TotalPromoters { get; init; }
    public required int ActivePromoters { get; init; }
    public required int AttributedOrders { get; init; }
    public required decimal TotalEligibleSales { get; init; }
    public required decimal PendingCommissionAmount { get; init; }
    public required decimal ApprovedCommissionAmount { get; init; }
    public required decimal PayableCommissionAmount { get; init; }
    public required decimal PaidCommissionAmount { get; init; }
    public required decimal ReversedCommissionAmount { get; init; }
    public required int TotalPayments { get; init; }
    public required decimal TotalPaymentsAmount { get; init; }
    public required decimal OutstandingPayableBalance { get; init; }
}

// Date filters apply to the row-level, period-bound figures (what happened in that window).
// Current-state counts (how many discounts are Active right now, etc.) are never date-filtered,
// since "how many are Active today" wouldn't mean anything scoped to a past window.
public class AdminMarketingReportQuery
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public long? PromoterId { get; init; }
    public long? ProductId { get; init; }
    public long? CategoryId { get; init; }
}

public class AdminDiscountReportRowDto
{
    public required long ProductId { get; init; }
    public required string ProductName { get; init; }
    public required string CategoryName { get; init; }
    public required int OrderCount { get; init; }
    public required int UnitsSold { get; init; }
    public required decimal DiscountValueGiven { get; init; }
}

public class AdminDiscountReportDto
{
    public required int ActiveCount { get; init; }
    public required int ScheduledCount { get; init; }
    public required int ExpiredCount { get; init; }
    public required int CancelledCount { get; init; }
    public required decimal TotalDiscountValueGiven { get; init; }
    public required IReadOnlyList<AdminDiscountReportRowDto> Rows { get; init; }
}

public class AdminPromoCodeReportRowDto
{
    public required long PromoCodeId { get; init; }
    public required string Code { get; init; }
    public required long PromoterId { get; init; }
    public required string PromoterName { get; init; }
    public required string EffectiveStatus { get; init; }
    public required int TotalUsesAllTime { get; init; }
    public required int UsesInRange { get; init; }
    public required decimal DiscountGivenInRange { get; init; }
    public DateTime? ExpiryDate { get; init; }
}

public class AdminPromoCodeReportDto
{
    public required int TotalCodes { get; init; }
    public required int ActiveCodes { get; init; }
    public required int InactiveCodes { get; init; }
    public required int ExpiredCodes { get; init; }
    public required int TotalUsesAllTime { get; init; }
    public required decimal TotalDiscountGivenInRange { get; init; }
    public required IReadOnlyList<AdminPromoCodeReportRowDto> MostUsed { get; init; }
    public required IReadOnlyList<AdminPromoCodeReportRowDto> ExpiringSoon { get; init; }
}

public class AdminCommissionReportRowDto
{
    public required long PromoterId { get; init; }
    public required string PromoterName { get; init; }
    public required int AttributedOrders { get; init; }
    public required decimal TotalEligibleSales { get; init; }
    public required decimal PendingAmount { get; init; }
    public required decimal ApprovedAmount { get; init; }
    public required decimal PayableAmount { get; init; }
    public required decimal PaidAmount { get; init; }
    public required decimal ReversedAmount { get; init; }
}

public class AdminCommissionReportDto
{
    public required int TotalPromoters { get; init; }
    public required int ActivePromoters { get; init; }
    public required int TotalAttributedOrders { get; init; }
    public required decimal TotalEligibleSales { get; init; }
    public required decimal PendingAmount { get; init; }
    public required decimal ApprovedAmount { get; init; }
    public required decimal PayableAmount { get; init; }
    public required decimal PaidAmount { get; init; }
    public required decimal ReversedAmount { get; init; }
    public required IReadOnlyList<AdminCommissionReportRowDto> ByPromoter { get; init; }
}

public class AdminPaymentReportRowDto
{
    public required long PromoterId { get; init; }
    public required string PromoterName { get; init; }
    public required int PaymentCount { get; init; }
    public required decimal TotalPaid { get; init; }
    public required decimal OutstandingBalance { get; init; }
}

public class AdminPaymentReportDto
{
    public required int TotalPayments { get; init; }
    public required decimal TotalPaidAmount { get; init; }
    public required decimal TotalOutstandingBalance { get; init; }
    public required int PartialSettlementCount { get; init; }
    public required IReadOnlyList<AdminPaymentReportRowDto> ByPromoter { get; init; }
}

public interface IAdminMarketingReportService
{
    Task<AdminMarketingDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<AdminDiscountReportDto> GetDiscountReportAsync(AdminMarketingReportQuery query, CancellationToken cancellationToken = default);
    Task<AdminPromoCodeReportDto> GetPromoCodeReportAsync(AdminMarketingReportQuery query, CancellationToken cancellationToken = default);
    Task<AdminCommissionReportDto> GetCommissionReportAsync(AdminMarketingReportQuery query, CancellationToken cancellationToken = default);
    Task<AdminPaymentReportDto> GetPaymentReportAsync(AdminMarketingReportQuery query, CancellationToken cancellationToken = default);
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
    public required int PendingVendorSubmissionCount { get; init; }
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
