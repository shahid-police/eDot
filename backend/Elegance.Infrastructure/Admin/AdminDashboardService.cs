using Elegance.Application.Admin;
using Elegance.Application.Orders;
using Elegance.Domain.Orders.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminDashboardService(
    EleganceDbContext dbContext,
    IAdminOrderService orderService,
    IAdminInventoryService inventoryService) : IAdminDashboardService
{
    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        // "Today" is the UTC day, matching how timestamps are stored and returned.
        var startOfTodayUtc = DateTime.UtcNow.Date;

        var ordersToday = await dbContext.Orders.CountAsync(o => o.CreatedAt >= startOfTodayUtc, cancellationToken);
        var pendingOrders = await dbContext.Orders.CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);
        var totalOrders = await dbContext.Orders.CountAsync(cancellationToken);
        var revenue = await dbContext.Orders
            .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
            .SumAsync(o => o.GrandTotal, cancellationToken);
        var customerCount = await dbContext.Customers.CountAsync(cancellationToken);
        var activeProductCount = await dbContext.Products.CountAsync(p => p.IsActive, cancellationToken);
        var lowStock = await inventoryService.GetLowStockAsync(cancellationToken);
        var recent = await orderService.ListOrdersAsync(new AdminOrderQuery { PageSize = 10 }, cancellationToken);

        return new AdminDashboardDto
        {
            OrdersToday = ordersToday,
            PendingOrders = pendingOrders,
            Revenue = revenue,
            TotalOrders = totalOrders,
            CustomerCount = customerCount,
            ActiveProductCount = activeProductCount,
            LowStockCount = lowStock.Count,
            RecentOrders = recent.Items,
        };
    }
}
