using Elegance.Application.Admin;
using Elegance.Domain.Discounts.Enums;
using Elegance.Domain.Marketing.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminMarketingReportService(EleganceDbContext dbContext) : IAdminMarketingReportService
{
    private sealed record DiscountStatusRow(ProductDiscountStatus Status, DateTime StartDate, DateTime EndDate);

    public async Task<AdminMarketingDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var discountRows = await dbContext.ProductDiscounts.AsNoTracking()
            .Select(d => new DiscountStatusRow(d.Status, d.StartDate, d.EndDate))
            .ToListAsync(cancellationToken);
        var (activeDiscounts, scheduledDiscounts, expiredDiscounts, _) = CountDiscountBuckets(discountRows, now);

        var codeRows = await dbContext.PromoCodes.AsNoTracking()
            .Select(c => new { c.Status, c.StartDate, c.ExpiryDate, c.UsedCount })
            .ToListAsync(cancellationToken);
        var totalPromoCodes = codeRows.Count;
        var activePromoCodes = codeRows.Count(c => ComputePromoCodeEffectiveStatus(c.Status, c.StartDate, c.ExpiryDate, now) == "Active");
        var totalPromoCodeUses = codeRows.Sum(c => c.UsedCount);

        var totalCustomerDiscountGiven = await dbContext.Orders.AsNoTracking().SumAsync(o => o.PromoCodeDiscountAmount, cancellationToken);

        var totalPromoters = await dbContext.Promoters.AsNoTracking().CountAsync(cancellationToken);
        var activePromoters = await dbContext.Promoters.AsNoTracking().CountAsync(p => p.Status == PromoterStatus.Active, cancellationToken);

        var attributedOrders = await dbContext.Orders.AsNoTracking().CountAsync(o => o.PromoCodeId != null, cancellationToken);
        var totalEligibleSales = await dbContext.Orders.AsNoTracking()
            .Where(o => o.CommissionEligibleAmount != null)
            .SumAsync(o => o.CommissionEligibleAmount!.Value, cancellationToken);

        var commissionAmountsByStatus = await dbContext.PromoterCommissions.AsNoTracking()
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Amount = g.Sum(c => c.CommissionAmount) })
            .ToDictionaryAsync(g => g.Status, g => g.Amount, cancellationToken);

        var totalPayments = await dbContext.PromoterPayments.AsNoTracking().CountAsync(p => p.Status != PromoterPaymentStatus.Voided, cancellationToken);
        var totalPaymentsAmount = await dbContext.PromoterPayments.AsNoTracking()
            .Where(p => p.Status != PromoterPaymentStatus.Voided)
            .SumAsync(p => p.TotalPaymentAmount, cancellationToken);

        var outstandingPayableBalance = await dbContext.PromoterCommissions.AsNoTracking()
            .Where(c => c.Status == CommissionStatus.Payable || c.Status == CommissionStatus.PartiallyPaid)
            .Select(c => c.CommissionAmount - (dbContext.PromoterPaymentAllocations
                .Where(a => a.PromoterCommissionId == c.Id && a.PromoterPayment.Status != PromoterPaymentStatus.Voided)
                .Sum(a => (decimal?)a.AllocatedAmount) ?? 0m))
            .SumAsync(cancellationToken);

        return new AdminMarketingDashboardDto
        {
            ActiveDiscountCount = activeDiscounts,
            ScheduledDiscountCount = scheduledDiscounts,
            ExpiredDiscountCount = expiredDiscounts,
            TotalPromoCodes = totalPromoCodes,
            ActivePromoCodes = activePromoCodes,
            TotalPromoCodeUses = totalPromoCodeUses,
            TotalCustomerDiscountGiven = totalCustomerDiscountGiven,
            TotalPromoters = totalPromoters,
            ActivePromoters = activePromoters,
            AttributedOrders = attributedOrders,
            TotalEligibleSales = totalEligibleSales,
            PendingCommissionAmount = commissionAmountsByStatus.GetValueOrDefault(CommissionStatus.Pending),
            ApprovedCommissionAmount = commissionAmountsByStatus.GetValueOrDefault(CommissionStatus.Approved),
            PayableCommissionAmount = commissionAmountsByStatus.GetValueOrDefault(CommissionStatus.Payable)
                + commissionAmountsByStatus.GetValueOrDefault(CommissionStatus.PartiallyPaid),
            PaidCommissionAmount = commissionAmountsByStatus.GetValueOrDefault(CommissionStatus.Paid),
            ReversedCommissionAmount = commissionAmountsByStatus.GetValueOrDefault(CommissionStatus.Reversed)
                + commissionAmountsByStatus.GetValueOrDefault(CommissionStatus.Cancelled),
            TotalPayments = totalPayments,
            TotalPaymentsAmount = totalPaymentsAmount,
            OutstandingPayableBalance = outstandingPayableBalance,
        };
    }

    public async Task<AdminDiscountReportDto> GetDiscountReportAsync(AdminMarketingReportQuery query, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var discountRows = await dbContext.ProductDiscounts.AsNoTracking()
            .Select(d => new DiscountStatusRow(d.Status, d.StartDate, d.EndDate))
            .ToListAsync(cancellationToken);
        var (active, scheduled, expired, cancelled) = CountDiscountBuckets(discountRows, now);

        var items = dbContext.OrderItems.AsNoTracking()
            .Where(i => i.OriginalUnitPrice > i.UnitPrice);

        if (query.StartDate is { } start) items = items.Where(i => i.Order.CreatedAt >= start);
        if (query.EndDate is { } end) items = items.Where(i => i.Order.CreatedAt <= end);
        if (query.ProductId is { } productId) items = items.Where(i => i.ProductId == productId);
        if (query.CategoryId is { } categoryId) items = items.Where(i => i.Product.CategoryId == categoryId);

        var rows = await items
            .GroupBy(i => new { i.ProductId, ProductName = i.Product.Name, CategoryName = i.Product.Category.Name })
            .Select(g => new AdminDiscountReportRowDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                CategoryName = g.Key.CategoryName,
                OrderCount = g.Select(x => x.OrderId).Distinct().Count(),
                UnitsSold = g.Sum(x => x.Quantity),
                DiscountValueGiven = g.Sum(x => (x.OriginalUnitPrice - x.UnitPrice) * x.Quantity),
            })
            .OrderByDescending(r => r.DiscountValueGiven)
            .ToListAsync(cancellationToken);

        return new AdminDiscountReportDto
        {
            ActiveCount = active,
            ScheduledCount = scheduled,
            ExpiredCount = expired,
            CancelledCount = cancelled,
            TotalDiscountValueGiven = rows.Sum(r => r.DiscountValueGiven),
            Rows = rows,
        };
    }

    public async Task<AdminPromoCodeReportDto> GetPromoCodeReportAsync(AdminMarketingReportQuery query, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var allCodes = await dbContext.PromoCodes.AsNoTracking()
            .Select(c => new { c.Id, c.Code, c.PromoterId, PromoterName = c.Promoter.Name, c.Status, c.StartDate, c.ExpiryDate, c.UsedCount })
            .ToListAsync(cancellationToken);

        var codes = query.PromoterId is { } pid ? allCodes.Where(c => c.PromoterId == pid).ToList() : allCodes;

        var usageQuery = dbContext.PromoCodeUsages.AsNoTracking().AsQueryable();
        if (query.StartDate is { } start) usageQuery = usageQuery.Where(u => u.UsedAt >= start);
        if (query.EndDate is { } end) usageQuery = usageQuery.Where(u => u.UsedAt <= end);
        if (query.PromoterId is { } promoterId) usageQuery = usageQuery.Where(u => u.PromoterId == promoterId);

        var usageByCode = await usageQuery
            .GroupBy(u => u.PromoCodeId)
            .Select(g => new { PromoCodeId = g.Key, Count = g.Count(), Discount = g.Sum(u => u.DiscountAmount) })
            .ToDictionaryAsync(g => g.PromoCodeId, cancellationToken);

        var rows = codes.Select(c =>
        {
            var usage = usageByCode.GetValueOrDefault(c.Id);
            return new AdminPromoCodeReportRowDto
            {
                PromoCodeId = c.Id,
                Code = c.Code,
                PromoterId = c.PromoterId,
                PromoterName = c.PromoterName,
                EffectiveStatus = ComputePromoCodeEffectiveStatus(c.Status, c.StartDate, c.ExpiryDate, now),
                TotalUsesAllTime = c.UsedCount,
                UsesInRange = usage?.Count ?? 0,
                DiscountGivenInRange = usage?.Discount ?? 0,
                ExpiryDate = c.ExpiryDate,
            };
        }).ToList();

        return new AdminPromoCodeReportDto
        {
            TotalCodes = rows.Count,
            ActiveCodes = rows.Count(r => r.EffectiveStatus == "Active"),
            InactiveCodes = rows.Count(r => r.EffectiveStatus == "Inactive"),
            ExpiredCodes = rows.Count(r => r.EffectiveStatus == "Expired"),
            TotalUsesAllTime = rows.Sum(r => r.TotalUsesAllTime),
            TotalDiscountGivenInRange = rows.Sum(r => r.DiscountGivenInRange),
            MostUsed = rows.OrderByDescending(r => r.TotalUsesAllTime).Take(10).ToList(),
            ExpiringSoon = rows
                .Where(r => r.EffectiveStatus == "Active" && r.ExpiryDate is { } expiry && expiry <= now.AddDays(14))
                .OrderBy(r => r.ExpiryDate)
                .ToList(),
        };
    }

    public async Task<AdminCommissionReportDto> GetCommissionReportAsync(AdminMarketingReportQuery query, CancellationToken cancellationToken = default)
    {
        var totalPromoters = await dbContext.Promoters.AsNoTracking().CountAsync(cancellationToken);
        var activePromoters = await dbContext.Promoters.AsNoTracking().CountAsync(p => p.Status == PromoterStatus.Active, cancellationToken);

        var commissions = dbContext.PromoterCommissions.AsNoTracking().AsQueryable();
        if (query.StartDate is { } start) commissions = commissions.Where(c => c.CreatedAt >= start);
        if (query.EndDate is { } end) commissions = commissions.Where(c => c.CreatedAt <= end);
        if (query.PromoterId is { } promoterId) commissions = commissions.Where(c => c.PromoterId == promoterId);

        var rows = await commissions
            .GroupBy(c => new { c.PromoterId, PromoterName = c.Promoter.Name })
            .Select(g => new AdminCommissionReportRowDto
            {
                PromoterId = g.Key.PromoterId,
                PromoterName = g.Key.PromoterName,
                AttributedOrders = g.Select(x => x.OrderId).Distinct().Count(),
                TotalEligibleSales = g.Sum(x => x.EligibleSaleAmount),
                PendingAmount = g.Where(x => x.Status == CommissionStatus.Pending).Sum(x => x.CommissionAmount),
                ApprovedAmount = g.Where(x => x.Status == CommissionStatus.Approved).Sum(x => x.CommissionAmount),
                PayableAmount = g.Where(x => x.Status == CommissionStatus.Payable || x.Status == CommissionStatus.PartiallyPaid).Sum(x => x.CommissionAmount),
                PaidAmount = g.Where(x => x.Status == CommissionStatus.Paid).Sum(x => x.CommissionAmount),
                ReversedAmount = g.Where(x => x.Status == CommissionStatus.Reversed || x.Status == CommissionStatus.Cancelled).Sum(x => x.CommissionAmount),
            })
            .OrderByDescending(r => r.TotalEligibleSales)
            .ToListAsync(cancellationToken);

        return new AdminCommissionReportDto
        {
            TotalPromoters = totalPromoters,
            ActivePromoters = activePromoters,
            TotalAttributedOrders = rows.Sum(r => r.AttributedOrders),
            TotalEligibleSales = rows.Sum(r => r.TotalEligibleSales),
            PendingAmount = rows.Sum(r => r.PendingAmount),
            ApprovedAmount = rows.Sum(r => r.ApprovedAmount),
            PayableAmount = rows.Sum(r => r.PayableAmount),
            PaidAmount = rows.Sum(r => r.PaidAmount),
            ReversedAmount = rows.Sum(r => r.ReversedAmount),
            ByPromoter = rows,
        };
    }

    public async Task<AdminPaymentReportDto> GetPaymentReportAsync(AdminMarketingReportQuery query, CancellationToken cancellationToken = default)
    {
        var payments = dbContext.PromoterPayments.AsNoTracking().Where(p => p.Status != PromoterPaymentStatus.Voided);
        if (query.StartDate is { } start) payments = payments.Where(p => p.PaymentDate >= start);
        if (query.EndDate is { } end) payments = payments.Where(p => p.PaymentDate <= end);
        if (query.PromoterId is { } promoterId) payments = payments.Where(p => p.PromoterId == promoterId);

        var byPromoterPayments = await payments
            .GroupBy(p => p.PromoterId)
            .Select(g => new { PromoterId = g.Key, Count = g.Count(), Total = g.Sum(p => p.TotalPaymentAmount) })
            .ToListAsync(cancellationToken);

        var outstanding = dbContext.PromoterCommissions.AsNoTracking()
            .Where(c => c.Status == CommissionStatus.Payable || c.Status == CommissionStatus.PartiallyPaid);
        if (query.PromoterId is { } pid) outstanding = outstanding.Where(c => c.PromoterId == pid);

        var outstandingByPromoter = await outstanding
            .Select(c => new
            {
                c.PromoterId,
                Remaining = c.CommissionAmount - (dbContext.PromoterPaymentAllocations
                    .Where(a => a.PromoterCommissionId == c.Id && a.PromoterPayment.Status != PromoterPaymentStatus.Voided)
                    .Sum(a => (decimal?)a.AllocatedAmount) ?? 0m),
            })
            .GroupBy(x => x.PromoterId)
            .Select(g => new { PromoterId = g.Key, Outstanding = g.Sum(x => x.Remaining) })
            .ToDictionaryAsync(g => g.PromoterId, g => g.Outstanding, cancellationToken);

        var promoterIds = byPromoterPayments.Select(p => p.PromoterId).Union(outstandingByPromoter.Keys).Distinct().ToList();
        var promoterNames = await dbContext.Promoters.AsNoTracking()
            .Where(p => promoterIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var rows = promoterIds.Select(id =>
        {
            var paymentInfo = byPromoterPayments.FirstOrDefault(p => p.PromoterId == id);
            return new AdminPaymentReportRowDto
            {
                PromoterId = id,
                PromoterName = promoterNames.GetValueOrDefault(id, "Unknown"),
                PaymentCount = paymentInfo?.Count ?? 0,
                TotalPaid = paymentInfo?.Total ?? 0,
                OutstandingBalance = outstandingByPromoter.GetValueOrDefault(id, 0),
            };
        }).OrderByDescending(r => r.TotalPaid).ToList();

        var partialSettlementCount = await dbContext.PromoterCommissions.AsNoTracking()
            .CountAsync(c => c.Status == CommissionStatus.PartiallyPaid, cancellationToken);

        return new AdminPaymentReportDto
        {
            TotalPayments = rows.Sum(r => r.PaymentCount),
            TotalPaidAmount = rows.Sum(r => r.TotalPaid),
            TotalOutstandingBalance = rows.Sum(r => r.OutstandingBalance),
            PartialSettlementCount = partialSettlementCount,
            ByPromoter = rows,
        };
    }

    private static (int Active, int Scheduled, int Expired, int Cancelled) CountDiscountBuckets(
        IEnumerable<DiscountStatusRow> rows, DateTime now)
    {
        int active = 0, scheduled = 0, expired = 0, cancelled = 0;
        foreach (var d in rows)
        {
            if (d.Status == ProductDiscountStatus.Cancelled) { cancelled++; }
            else if (now < d.StartDate) { scheduled++; }
            else if (now > d.EndDate) { expired++; }
            else { active++; }
        }
        return (active, scheduled, expired, cancelled);
    }

    private static string ComputePromoCodeEffectiveStatus(PromoCodeStatus status, DateTime start, DateTime? expiry, DateTime now)
    {
        if (status == PromoCodeStatus.Inactive) return "Inactive";
        if (now < start) return "Scheduled";
        if (expiry is { } e && now > e) return "Expired";
        return "Active";
    }
}
