using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Application.Orders;
using Elegance.Domain.Inventory;
using Elegance.Domain.Orders;
using Elegance.Domain.Orders.Enums;
using Elegance.Infrastructure.Orders;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminOrderService(EleganceDbContext dbContext, OrderDetailBuilder detailBuilder, IAuditLogService auditLog) : IAdminOrderService
{
    // Captured money can only be refunded, and a pending payment is either received or failed.
    private static readonly Dictionary<PaymentStatus, PaymentStatus[]> PaymentTransitions = new()
    {
        [PaymentStatus.Pending] = [PaymentStatus.Captured, PaymentStatus.Failed],
        [PaymentStatus.Captured] = [PaymentStatus.Refunded],
        [PaymentStatus.Failed] = [],
        [PaymentStatus.Refunded] = [],
    };

    public async Task<PagedResult<AdminOrderSummaryDto>> ListOrdersAsync(AdminOrderQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var orders = dbContext.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ParseEnum<OrderStatus>(query.Status, "order status");
            orders = orders.Where(o => o.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            orders = orders.Where(o => o.OrderNumber.Contains(search) || o.Email.Contains(search) || o.Phone.Contains(search));
        }

        var total = await orders.CountAsync(cancellationToken);
        var rows = await orders
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.Email,
                o.Phone,
                o.GrandTotal,
                o.Currency,
                o.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var orderIds = rows.Select(r => r.Id).ToList();
        var paymentStatuses = await dbContext.Payments
            .AsNoTracking()
            .Where(p => orderIds.Contains(p.OrderId))
            .OrderBy(p => p.Id)
            .Select(p => new { p.OrderId, p.Status })
            .ToListAsync(cancellationToken);
        var latestPayment = paymentStatuses.GroupBy(p => p.OrderId).ToDictionary(g => g.Key, g => g.Last().Status);

        return new PagedResult<AdminOrderSummaryDto>
        {
            Items = rows.Select(r => new AdminOrderSummaryDto
            {
                Id = r.Id,
                OrderNumber = r.OrderNumber,
                Status = r.Status.ToString(),
                Email = r.Email,
                Phone = r.Phone,
                GrandTotal = r.GrandTotal,
                Currency = r.Currency,
                PaymentStatus = latestPayment.TryGetValue(r.Id, out var payment) ? payment.ToString() : "None",
                CreatedAt = r.CreatedAt,
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<OrderDetailDto> GetOrderAsync(long orderId, CancellationToken cancellationToken = default)
    {
        await EnsureOrderExistsAsync(orderId, cancellationToken);
        return await detailBuilder.BuildAsync(orderId, cancellationToken);
    }

    public async Task<OrderDetailDto> ChangeStatusAsync(AdminActor actor, long orderId, OrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var target = ParseEnum<OrderStatus>(request.Status, "order status");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var order = await dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw NotFound();

        var from = order.Status;
        if (!OrderStatusRules.CanTransition(from, target))
        {
            throw new ApplicationFailure(FailureKind.Conflict, $"An order cannot move from {from} to {target}.");
        }

        var now = DateTime.UtcNow;

        // Conditional on the current status, so two admins cannot both apply the same transition
        // and restore the same stock twice.
        var moved = await dbContext.Orders
            .Where(o => o.Id == orderId && o.Status == from)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, target).SetProperty(o => o.UpdatedAt, now), cancellationToken);
        if (moved == 0)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "This order was just changed by someone else. Reload and try again.");
        }

        if (OrderStatusRules.RestoresStockOn(target))
        {
            await RestoreStockAsync(order, target, actor, now, cancellationToken);
        }

        if (target == OrderStatus.Refunded)
        {
            await dbContext.Payments
                .Where(p => p.OrderId == orderId && p.Status == PaymentStatus.Captured)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Refunded).SetProperty(p => p.UpdatedAt, now), cancellationToken);
        }

        dbContext.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            Status = target,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "order.status", "Order", orderId.ToString(), $"{order.OrderNumber}: {from} to {target}", cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await detailBuilder.BuildAsync(orderId, cancellationToken);
    }

    public async Task<OrderDetailDto> SetPaymentStatusAsync(AdminActor actor, long orderId, PaymentStatusRequest request, CancellationToken cancellationToken = default)
    {
        var target = ParseEnum<PaymentStatus>(request.Status, "payment status");
        var orderStatus = await dbContext.Orders
            .Where(o => o.Id == orderId)
            .Select(o => (OrderStatus?)o.Status)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();

        if (target == PaymentStatus.Captured && orderStatus is OrderStatus.Cancelled or OrderStatus.Returned or OrderStatus.Refunded)
        {
            throw new ApplicationFailure(FailureKind.Conflict, $"Money cannot be captured on an order that is {orderStatus}.");
        }

        var payment = await dbContext.Payments
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.Conflict, "This order has no payment to update.");

        if (!PaymentTransitions[payment.Status].Contains(target))
        {
            throw new ApplicationFailure(FailureKind.Conflict, $"A payment cannot move from {payment.Status} to {target}.");
        }

        var from = payment.Status;
        var now = DateTime.UtcNow;
        var moved = await dbContext.Payments
            .Where(p => p.Id == payment.Id && p.Status == from)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, target).SetProperty(p => p.UpdatedAt, now), cancellationToken);
        if (moved == 0)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "This payment was just changed by someone else. Reload and try again.");
        }

        await auditLog.RecordAsync(actor, "order.payment", "Payment", payment.Id.ToString(), $"Order {orderId}: {from} to {target}", cancellationToken);
        return await detailBuilder.BuildAsync(orderId, cancellationToken);
    }

    private async Task RestoreStockAsync(Order order, OrderStatus reason, AdminActor actor, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            var quantity = item.Quantity;
            if (item.VariantId is { } variantId)
            {
                await dbContext.ProductVariants
                    .Where(v => v.Id == variantId)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + quantity), cancellationToken);
            }
            else
            {
                await dbContext.Products
                    .Where(p => p.Id == item.ProductId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + quantity), cancellationToken);
            }

            dbContext.StockMovements.Add(new StockMovement
            {
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                QuantityDelta = quantity,
                Reason = reason == OrderStatus.Returned ? StockMovementReason.Return : StockMovementReason.Cancellation,
                OrderId = order.Id,
                AdminUserId = actor.Id,
                CreatedAt = now,
            });
        }
    }

    private async Task EnsureOrderExistsAsync(long orderId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Orders.AnyAsync(o => o.Id == orderId, cancellationToken))
        {
            throw NotFound();
        }
    }

    internal static T ParseEnum<T>(string value, string label) where T : struct, Enum =>
        Enum.TryParse<T>(value.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ApplicationFailure(FailureKind.Validation, $"Unknown {label}.");

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");
}
