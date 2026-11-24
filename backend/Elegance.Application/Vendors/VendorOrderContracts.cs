using Elegance.Application.Common;

namespace Elegance.Application.Vendors;

// All figures here are derived by querying the EXISTING Order/OrderItem tables filtered to
// Product.VendorId - there is no parallel order system, and no vendor settlement/commission is
// ever computed (only gross sales figures), per the spec's explicit restriction.

public class VendorOrderSummaryDto
{
    public required int TotalOrders { get; init; }
    public required int TotalUnits { get; init; }
    public required int DeliveredUnits { get; init; }
    public required int PendingOrders { get; init; }
    public required int ConfirmedOrders { get; init; }
    public required int ProcessingOrders { get; init; }
    public required int DispatchedOrders { get; init; } // Packed + Shipped + OutForDelivery
    public required int DeliveredOrders { get; init; }
    public required int CancelledOrders { get; init; }
    public required int ReturnedOrders { get; init; }
}

public class VendorOrderItemDto
{
    public required long OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required DateTime OrderDate { get; init; }
    public required string ProductName { get; init; }
    public required string Sku { get; init; }
    public required int Quantity { get; init; }
    public required decimal ItemAmount { get; init; }
    public required string OrderStatus { get; init; }
    public required string PaymentStatus { get; init; }
    public required DateTime LastUpdated { get; init; }
}

public class VendorOrderQuery
{
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class VendorOrderDetailItemDto
{
    public required string ProductName { get; init; }
    public required string Sku { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal LineTotal { get; init; }
}

// Deliberately omits customer name/phone/email and the full delivery address - only city/province
// are included, per the spec's privacy restrictions on vendor-visible order data.
public class VendorOrderDetailDto
{
    public required long OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required DateTime OrderDate { get; init; }
    public required string OrderStatus { get; init; }
    public required string PaymentStatus { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public required IReadOnlyList<VendorOrderDetailItemDto> Items { get; init; }
}

public class VendorBestSellingProductDto
{
    public required string ProductName { get; init; }
    public required string Sku { get; init; }
    public required int UnitsSold { get; init; }
}

public class VendorMonthlySalesDto
{
    public required string Month { get; init; } // "2026-11"
    public required int UnitsSold { get; init; }
    public required decimal GrossSalesValue { get; init; }
}

public class VendorSalesAnalyticsDto
{
    public required int TotalUnitsSold { get; init; }
    public required decimal TotalGrossSalesValue { get; init; }
    public required int CancelledUnits { get; init; }
    public required int ReturnedUnits { get; init; }
    public required IReadOnlyList<VendorBestSellingProductDto> BestSellingProducts { get; init; }
    public required IReadOnlyList<VendorMonthlySalesDto> MonthlySales { get; init; }
}

public interface IVendorOrderService
{
    Task<VendorOrderSummaryDto> GetSummaryAsync(long vendorId, CancellationToken cancellationToken = default);
    Task<PagedResult<VendorOrderItemDto>> ListOrderItemsAsync(long vendorId, VendorOrderQuery query, CancellationToken cancellationToken = default);
    Task<VendorOrderDetailDto> GetOrderDetailAsync(long vendorId, long orderId, CancellationToken cancellationToken = default);
    Task<VendorSalesAnalyticsDto> GetSalesAnalyticsAsync(long vendorId, CancellationToken cancellationToken = default);
}
