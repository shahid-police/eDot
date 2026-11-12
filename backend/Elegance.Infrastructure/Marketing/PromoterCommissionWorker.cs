using Elegance.Domain.Marketing.Enums;
using Elegance.Domain.Orders.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elegance.Infrastructure.Marketing;

// Runs continuously in the API process, same shape as BackInStockNotificationWorker - no separate
// worker deployment needed for something this infrequent. Each tick promotes any Pending/Approved
// commission whose order has been Delivered for at least ReturnWindowDays to Payable. A commission
// never gets here at all if its order was Returned/Refunded in the meantime - AdminOrderService
// reverses it directly at that moment instead (see AdminOrderService.ChangeStatusAsync).
public class PromoterCommissionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CommissionOptions> options,
    ILogger<PromoterCommissionWorker> logger) : BackgroundService
{
    private const int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, options.Value.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EleganceDbContext>();
                await PromoteEligibleCommissionsAsync(dbContext, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Promoter commission worker tick failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task PromoteEligibleCommissionsAsync(EleganceDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var windowCutoff = now.AddDays(-options.Value.ReturnWindowDays);

        var candidates = await dbContext.PromoterCommissions
            .Where(c => c.Status == CommissionStatus.Pending || c.Status == CommissionStatus.Approved)
            .Where(c => c.Order.Status == OrderStatus.Delivered)
            .OrderBy(c => c.Id)
            .Take(BatchSize)
            .Select(c => new { c.Id, c.OrderId })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return;
        }

        var orderIds = candidates.Select(c => c.OrderId).Distinct().ToList();
        var deliveredAtByOrder = await dbContext.OrderStatusHistory
            .Where(h => orderIds.Contains(h.OrderId) && h.Status == OrderStatus.Delivered)
            .GroupBy(h => h.OrderId)
            .Select(g => new { OrderId = g.Key, DeliveredAt = g.Max(h => h.CreatedAt) })
            .ToDictionaryAsync(x => x.OrderId, x => x.DeliveredAt, cancellationToken);

        var toPromoteIds = candidates
            .Where(c => deliveredAtByOrder.TryGetValue(c.OrderId, out var deliveredAt) && deliveredAt <= windowCutoff)
            .Select(c => c.Id)
            .ToList();

        if (toPromoteIds.Count == 0)
        {
            return;
        }

        await dbContext.PromoterCommissions
            .Where(c => toPromoteIds.Contains(c.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, CommissionStatus.Payable)
                .SetProperty(c => c.PayableAt, now)
                .SetProperty(c => c.UpdatedAt, now), cancellationToken);
    }
}
