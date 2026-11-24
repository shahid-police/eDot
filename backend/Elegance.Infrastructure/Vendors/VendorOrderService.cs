using Elegance.Application.Common;
using Elegance.Application.Vendors;
using Elegance.Domain.Orders.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Vendors;

public class VendorOrderService(EleganceDbContext dbContext) : IVendorOrderService
{
    private static readonly OrderStatus[] DispatchedStatuses = [OrderStatus.Packed, OrderStatus.Shipped, OrderStatus.OutForDelivery];

    public async Task<VendorOrderSummaryDto> GetSummaryAsync(long vendorId, CancellationToken cancellationToken = default)
    {
        // One row per distinct order containing at least one of this vendor's items, with that
        // order's status and this vendor's unit count within it - orders, not items, are what the
        // summary counts per status.
        var orders = await dbContext.OrderItems.AsNoTracking()
            .Where(i => i.Product.VendorId == vendorId)
            .GroupBy(i => new { i.OrderId, i.Order.Status })
            .Select(g => new { g.Key.OrderId, g.Key.Status, Units = g.Sum(i => i.Quantity) })
            .ToListAsync(cancellationToken);

        var totalUnits = await dbContext.OrderItems.AsNoTracking()
            .Where(i => i.Product.VendorId == vendorId)
            .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;

        int CountOrders(params OrderStatus[] statuses) => orders.Count(o => statuses.Contains(o.Status));
        int SumUnits(params OrderStatus[] statuses) => orders.Where(o => statuses.Contains(o.Status)).Sum(o => o.Units);

        return new VendorOrderSummaryDto
        {
            TotalOrders = orders.Select(o => o.OrderId).Distinct().Count(),
            TotalUnits = totalUnits,
            DeliveredUnits = SumUnits(OrderStatus.Delivered),
            PendingOrders = CountOrders(OrderStatus.Pending),
            ConfirmedOrders = CountOrders(OrderStatus.Confirmed),
            ProcessingOrders = CountOrders(OrderStatus.Processing),
            DispatchedOrders = CountOrders(DispatchedStatuses),
            DeliveredOrders = CountOrders(OrderStatus.Delivered),
            CancelledOrders = CountOrders(OrderStatus.Cancelled),
            ReturnedOrders = CountOrders(OrderStatus.Returned),
        };
    }

    public async Task<PagedResult<VendorOrderItemDto>> ListOrderItemsAsync(long vendorId, VendorOrderQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = dbContext.OrderItems.AsNoTracking().Where(i => i.Product.VendorId == vendorId);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<OrderStatus>(query.Status, true, out var status))
        {
            items = items.Where(i => i.Order.Status == status);
        }

        var total = await items.CountAsync(cancellationToken);
        var rows = await items
            .OrderByDescending(i => i.Order.CreatedAt)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.OrderId,
                i.Order.OrderNumber,
                i.Order.CreatedAt,
                i.Order.Status,
                i.Order.UpdatedAt,
                i.ProductName,
                i.Sku,
                i.Quantity,
                i.LineTotal,
            })
            .ToListAsync(cancellationToken);

        var orderIds = rows.Select(r => r.OrderId).Distinct().ToList();
        var paymentStatuses = await LatestPaymentStatusesAsync(orderIds, cancellationToken);

        return new PagedResult<VendorOrderItemDto>
        {
            Items = rows.Select(r => new VendorOrderItemDto
            {
                OrderId = r.OrderId,
                OrderNumber = r.OrderNumber,
                OrderDate = r.CreatedAt,
                ProductName = r.ProductName,
                Sku = r.Sku,
                Quantity = r.Quantity,
                ItemAmount = r.LineTotal,
                OrderStatus = r.Status.ToString(),
                PaymentStatus = paymentStatuses.TryGetValue(r.OrderId, out var p) ? p : "None",
                LastUpdated = r.UpdatedAt,
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<VendorOrderDetailDto> GetOrderDetailAsync(long vendorId, long orderId, CancellationToken cancellationToken = default)
    {
        // Ownership check: the order must contain at least one of this vendor's items, or this
        // vendor has no business seeing any part of it. NotFound (not Forbidden) so existence of
        // an order with no items from this vendor isn't leaked either.
        var order = await dbContext.Orders.AsNoTracking()
            .Where(o => o.Id == orderId && o.Items.Any(i => i.Product.VendorId == vendorId))
            .Select(o => new { o.Id, o.OrderNumber, o.CreatedAt, o.Status })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested resource was not found.");

        var items = await dbContext.OrderItems.AsNoTracking()
            .Where(i => i.OrderId == orderId && i.Product.VendorId == vendorId)
            .Select(i => new VendorOrderDetailItemDto
            {
                ProductName = i.ProductName,
                Sku = i.Sku,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal,
            })
            .ToListAsync(cancellationToken);

        var address = await dbContext.OrderAddresses.AsNoTracking()
            .Where(a => a.OrderId == orderId)
            .Select(a => new { a.City, a.Province })
            .FirstOrDefaultAsync(cancellationToken);

        var paymentStatus = (await LatestPaymentStatusesAsync([orderId], cancellationToken)).GetValueOrDefault(orderId, "None");

        return new VendorOrderDetailDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.CreatedAt,
            OrderStatus = order.Status.ToString(),
            PaymentStatus = paymentStatus,
            City = address?.City,
            Province = address?.Province,
            Items = items,
        };
    }

    public async Task<VendorSalesAnalyticsDto> GetSalesAnalyticsAsync(long vendorId, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.OrderItems.AsNoTracking()
            .Where(i => i.Product.VendorId == vendorId)
            .Select(i => new { i.ProductName, i.Sku, i.Quantity, i.LineTotal, i.Order.Status, i.Order.CreatedAt })
            .ToListAsync(cancellationToken);

        var sellable = items.Where(i => i.Status != OrderStatus.Cancelled && i.Status != OrderStatus.Returned).ToList();

        var bestSellers = sellable
            .GroupBy(i => new { i.ProductName, i.Sku })
            .Select(g => new VendorBestSellingProductDto { ProductName = g.Key.ProductName, Sku = g.Key.Sku, UnitsSold = g.Sum(i => i.Quantity) })
            .OrderByDescending(p => p.UnitsSold)
            .Take(10)
            .ToList();

        var monthly = sellable
            .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
            .Select(g => new VendorMonthlySalesDto
            {
                Month = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                UnitsSold = g.Sum(i => i.Quantity),
                GrossSalesValue = g.Sum(i => i.LineTotal),
            })
            .OrderByDescending(m => m.Month)
            .Take(12)
            .OrderBy(m => m.Month)
            .ToList();

        return new VendorSalesAnalyticsDto
        {
            TotalUnitsSold = sellable.Sum(i => i.Quantity),
            TotalGrossSalesValue = sellable.Sum(i => i.LineTotal),
            CancelledUnits = items.Where(i => i.Status == OrderStatus.Cancelled).Sum(i => i.Quantity),
            ReturnedUnits = items.Where(i => i.Status == OrderStatus.Returned).Sum(i => i.Quantity),
            BestSellingProducts = bestSellers,
            MonthlySales = monthly,
        };
    }

    // Mirrors AdminOrderService's "latest payment row per order" derivation exactly.
    private async Task<Dictionary<long, string>> LatestPaymentStatusesAsync(List<long> orderIds, CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0)
        {
            return [];
        }

        var paymentRows = await dbContext.Payments.AsNoTracking()
            .Where(p => orderIds.Contains(p.OrderId))
            .OrderBy(p => p.Id)
            .Select(p => new { p.OrderId, p.Status })
            .ToListAsync(cancellationToken);

        return paymentRows.GroupBy(p => p.OrderId).ToDictionary(g => g.Key, g => g.Last().Status.ToString());
    }
}
