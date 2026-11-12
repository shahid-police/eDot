using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Marketing.Enums;
using Elegance.Domain.Orders.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminCommissionService(EleganceDbContext dbContext, IAuditLogService auditLog) : IAdminCommissionService
{
    private sealed record CommissionRow(
        long Id, long PromoterId, string PromoterName, long PromoCodeId, string PromoCode,
        long OrderId, string OrderNumber, OrderStatus OrderStatus,
        decimal CommissionPercentageSnapshot, decimal EligibleSaleAmount, decimal CommissionAmount, string Currency,
        CommissionStatus Status, string? Remarks, DateTime CreatedAt,
        DateTime? ApprovedAt, DateTime? PayableAt, DateTime? PaidAt, DateTime? ReversedAt);

    public async Task<AdminCommissionSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var counts = await dbContext.PromoterCommissions
            .AsNoTracking()
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(c => c.CommissionAmount) })
            .ToDictionaryAsync(g => g.Status, cancellationToken);

        return new AdminCommissionSummaryDto
        {
            PendingCount = counts.GetValueOrDefault(CommissionStatus.Pending)?.Count ?? 0,
            ApprovedCount = counts.GetValueOrDefault(CommissionStatus.Approved)?.Count ?? 0,
            PayableCount = counts.GetValueOrDefault(CommissionStatus.Payable)?.Count ?? 0,
            PaidCount = counts.GetValueOrDefault(CommissionStatus.Paid)?.Count ?? 0,
            ReversedCount = counts.GetValueOrDefault(CommissionStatus.Reversed)?.Count ?? 0,
            CancelledCount = counts.GetValueOrDefault(CommissionStatus.Cancelled)?.Count ?? 0,
            PendingAmount = counts.GetValueOrDefault(CommissionStatus.Pending)?.Amount ?? 0,
            PayableAmount = counts.GetValueOrDefault(CommissionStatus.Payable)?.Amount ?? 0,
            PaidAmount = counts.GetValueOrDefault(CommissionStatus.Paid)?.Amount ?? 0,
        };
    }

    public async Task<PagedResult<AdminCommissionDto>> ListCommissionsAsync(AdminCommissionQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var commissions = dbContext.PromoterCommissions.AsNoTracking().AsQueryable();

        if (query.PromoterId is { } promoterId)
        {
            commissions = commissions.Where(c => c.PromoterId == promoterId);
        }

        if (Enum.TryParse<CommissionStatus>(query.Status, true, out var status))
        {
            commissions = commissions.Where(c => c.Status == status);
        }

        var total = await commissions.CountAsync(cancellationToken);
        var rows = await commissions
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CommissionRow(
                c.Id, c.PromoterId, c.Promoter.Name, c.PromoCodeId, c.PromoCode.Code,
                c.OrderId, c.Order.OrderNumber, c.Order.Status,
                c.CommissionPercentageSnapshot, c.EligibleSaleAmount, c.CommissionAmount, c.Currency,
                c.Status, c.Remarks, c.CreatedAt,
                c.ApprovedAt, c.PayableAt, c.PaidAt, c.ReversedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminCommissionDto> { Items = rows.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<AdminCommissionDto> ApproveCommissionAsync(AdminActor actor, long commissionId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var taken = await dbContext.PromoterCommissions
            .Where(c => c.Id == commissionId && c.Status == CommissionStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, CommissionStatus.Approved)
                .SetProperty(c => c.ApprovedAt, now)
                .SetProperty(c => c.UpdatedAt, now), cancellationToken);

        if (taken == 0)
        {
            throw ConflictOrNotFound(await dbContext.PromoterCommissions.AnyAsync(c => c.Id == commissionId, cancellationToken),
                "Only a Pending commission can be approved.");
        }

        await auditLog.RecordAsync(actor, "commission.approve", "PromoterCommission", commissionId.ToString(), $"Approved commission {commissionId}", cancellationToken);
        return await GetAsync(commissionId, cancellationToken);
    }

    public async Task<AdminCommissionDto> MarkPayableAsync(AdminActor actor, long commissionId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var taken = await dbContext.PromoterCommissions
            .Where(c => c.Id == commissionId && (c.Status == CommissionStatus.Pending || c.Status == CommissionStatus.Approved))
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, CommissionStatus.Payable)
                .SetProperty(c => c.PayableAt, now)
                .SetProperty(c => c.UpdatedAt, now), cancellationToken);

        if (taken == 0)
        {
            throw ConflictOrNotFound(await dbContext.PromoterCommissions.AnyAsync(c => c.Id == commissionId, cancellationToken),
                "Only a Pending or Approved commission can be marked payable.");
        }

        await auditLog.RecordAsync(actor, "commission.mark_payable", "PromoterCommission", commissionId.ToString(), $"Marked commission {commissionId} payable", cancellationToken);
        return await GetAsync(commissionId, cancellationToken);
    }

    public async Task<AdminCommissionDto> ReverseCommissionAsync(AdminActor actor, long commissionId, AdminCommissionRemarksRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var taken = await dbContext.PromoterCommissions
            .Where(c => c.Id == commissionId
                && c.Status != CommissionStatus.Paid && c.Status != CommissionStatus.Reversed && c.Status != CommissionStatus.Cancelled)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, CommissionStatus.Reversed)
                .SetProperty(c => c.ReversedAt, now)
                .SetProperty(c => c.Remarks, request.Remarks.Trim())
                .SetProperty(c => c.UpdatedAt, now), cancellationToken);

        if (taken == 0)
        {
            throw ConflictOrNotFound(await dbContext.PromoterCommissions.AnyAsync(c => c.Id == commissionId, cancellationToken),
                "A Paid, Reversed, or Cancelled commission cannot be reversed.");
        }

        await auditLog.RecordAsync(actor, "commission.reverse", "PromoterCommission", commissionId.ToString(), $"Reversed commission {commissionId}: {request.Remarks}", cancellationToken);
        return await GetAsync(commissionId, cancellationToken);
    }

    public async Task<AdminCommissionDto> CancelCommissionAsync(AdminActor actor, long commissionId, AdminCommissionRemarksRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var taken = await dbContext.PromoterCommissions
            .Where(c => c.Id == commissionId
                && c.Status != CommissionStatus.Paid && c.Status != CommissionStatus.Reversed && c.Status != CommissionStatus.Cancelled)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, CommissionStatus.Cancelled)
                .SetProperty(c => c.Remarks, request.Remarks.Trim())
                .SetProperty(c => c.UpdatedAt, now), cancellationToken);

        if (taken == 0)
        {
            throw ConflictOrNotFound(await dbContext.PromoterCommissions.AnyAsync(c => c.Id == commissionId, cancellationToken),
                "A Paid, Reversed, or Cancelled commission cannot be cancelled.");
        }

        await auditLog.RecordAsync(actor, "commission.cancel", "PromoterCommission", commissionId.ToString(), $"Cancelled commission {commissionId}: {request.Remarks}", cancellationToken);
        return await GetAsync(commissionId, cancellationToken);
    }

    private async Task<AdminCommissionDto> GetAsync(long commissionId, CancellationToken cancellationToken)
    {
        var row = await dbContext.PromoterCommissions
            .AsNoTracking()
            .Where(c => c.Id == commissionId)
            .Select(c => new CommissionRow(
                c.Id, c.PromoterId, c.Promoter.Name, c.PromoCodeId, c.PromoCode.Code,
                c.OrderId, c.Order.OrderNumber, c.Order.Status,
                c.CommissionPercentageSnapshot, c.EligibleSaleAmount, c.CommissionAmount, c.Currency,
                c.Status, c.Remarks, c.CreatedAt,
                c.ApprovedAt, c.PayableAt, c.PaidAt, c.ReversedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested commission was not found.");

        return ToDto(row);
    }

    private static AdminCommissionDto ToDto(CommissionRow r) => new()
    {
        Id = r.Id,
        PromoterId = r.PromoterId,
        PromoterName = r.PromoterName,
        PromoCodeId = r.PromoCodeId,
        PromoCode = r.PromoCode,
        OrderId = r.OrderId,
        OrderNumber = r.OrderNumber,
        OrderStatus = r.OrderStatus.ToString(),
        CommissionPercentageSnapshot = r.CommissionPercentageSnapshot,
        EligibleSaleAmount = r.EligibleSaleAmount,
        CommissionAmount = r.CommissionAmount,
        Currency = r.Currency,
        Status = r.Status.ToString(),
        Remarks = r.Remarks,
        CreatedAt = r.CreatedAt,
        ApprovedAt = r.ApprovedAt,
        PayableAt = r.PayableAt,
        PaidAt = r.PaidAt,
        ReversedAt = r.ReversedAt,
    };

    private static ApplicationFailure ConflictOrNotFound(bool exists, string conflictMessage) =>
        exists
            ? new ApplicationFailure(FailureKind.Conflict, conflictMessage)
            : new ApplicationFailure(FailureKind.NotFound, "The requested commission was not found.");
}
