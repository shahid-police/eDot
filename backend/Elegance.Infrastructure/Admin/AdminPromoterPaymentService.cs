using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Marketing;
using Elegance.Domain.Marketing.Enums;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminPromoterPaymentService(EleganceDbContext dbContext, IAuditLogService auditLog) : IAdminPromoterPaymentService
{
    private sealed record PaymentRow(
        long Id, string PaymentReference, long PromoterId, string PromoterName,
        decimal TotalPaymentAmount, DateTime PaymentDate, string PaymentMethod, string? TransactionReference,
        PromoterPaymentStatus Status, string? Remarks, DateTime CreatedAt);

    public async Task<AdminPromoterBalanceDto> GetPromoterBalanceAsync(long promoterId, CancellationToken cancellationToken = default)
    {
        var promoter = await dbContext.Promoters
            .AsNoTracking()
            .Where(p => p.Id == promoterId)
            .Select(p => new { p.Id, p.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();

        var rows = await dbContext.PromoterCommissions
            .AsNoTracking()
            .Where(c => c.PromoterId == promoterId && (c.Status == CommissionStatus.Payable || c.Status == CommissionStatus.PartiallyPaid))
            .Select(c => new
            {
                c.Id,
                c.OrderId,
                OrderNumber = c.Order.OrderNumber,
                c.CommissionAmount,
                c.Status,
                Allocated = dbContext.PromoterPaymentAllocations
                    .Where(a => a.PromoterCommissionId == c.Id && a.PromoterPayment.Status != PromoterPaymentStatus.Voided)
                    .Sum(a => (decimal?)a.AllocatedAmount) ?? 0m,
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new AdminPayableCommissionDto
        {
            CommissionId = r.Id,
            OrderId = r.OrderId,
            OrderNumber = r.OrderNumber,
            CommissionAmount = r.CommissionAmount,
            AllocatedAmount = r.Allocated,
            RemainingAmount = r.CommissionAmount - r.Allocated,
            Status = r.Status.ToString(),
        }).ToList();

        return new AdminPromoterBalanceDto
        {
            PromoterId = promoter.Id,
            PromoterName = promoter.Name,
            OutstandingBalance = items.Sum(i => i.RemainingAmount),
            PayableCommissions = items,
        };
    }

    public async Task<PagedResult<AdminPromoterPaymentDto>> ListPaymentsAsync(AdminPromoterPaymentQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var payments = dbContext.PromoterPayments.AsNoTracking().AsQueryable();
        if (query.PromoterId is { } promoterId)
        {
            payments = payments.Where(p => p.PromoterId == promoterId);
        }

        var total = await payments.CountAsync(cancellationToken);
        var rows = await payments
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentRow(
                p.Id, p.PaymentReference, p.PromoterId, p.Promoter.Name,
                p.TotalPaymentAmount, p.PaymentDate, p.PaymentMethod, p.TransactionReference,
                p.Status, p.Remarks, p.CreatedAt))
            .ToListAsync(cancellationToken);

        var allocationsByPayment = await LoadAllocationsAsync(rows.Select(r => r.Id).ToList(), cancellationToken);

        return new PagedResult<AdminPromoterPaymentDto>
        {
            Items = rows.Select(r => ToDto(r, allocationsByPayment.GetValueOrDefault(r.Id, []))).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<AdminPromoterPaymentDto> GetPaymentAsync(long paymentId, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.PromoterPayments
            .AsNoTracking()
            .Where(p => p.Id == paymentId)
            .Select(p => new PaymentRow(
                p.Id, p.PaymentReference, p.PromoterId, p.Promoter.Name,
                p.TotalPaymentAmount, p.PaymentDate, p.PaymentMethod, p.TransactionReference,
                p.Status, p.Remarks, p.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();

        var allocations = await LoadAllocationsAsync([row.Id], cancellationToken);
        return ToDto(row, allocations.GetValueOrDefault(row.Id, []));
    }

    public async Task<AdminPromoterPaymentDto> CreatePaymentAsync(AdminActor actor, AdminPromoterPaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Allocations.Count == 0)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Select at least one commission to pay.");
        }

        var commissionIds = request.Allocations.Select(a => a.CommissionId).ToList();
        if (commissionIds.Distinct().Count() != commissionIds.Count)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Each commission can only be allocated once per payment.");
        }

        var promoterExists = await dbContext.Promoters.AsNoTracking().AnyAsync(p => p.Id == request.PromoterId, cancellationToken);
        if (!promoterExists)
        {
            throw new ApplicationFailure(FailureKind.NotFound, "That promoter could not be found.");
        }

        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var payment = new PromoterPayment
        {
            // Placeholder until the real Id exists - same pattern as Order.OrderNumber.
            PaymentReference = Guid.NewGuid().ToString("N")[..20],
            PromoterId = request.PromoterId,
            TotalPaymentAmount = 0,
            PaymentDate = request.PaymentDate,
            PaymentMethod = request.PaymentMethod.Trim(),
            TransactionReference = string.IsNullOrWhiteSpace(request.TransactionReference) ? null : request.TransactionReference.Trim(),
            Status = PromoterPaymentStatus.Recorded,
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
            CreatedByAdminUserId = actor.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var totalAmount = 0m;
        foreach (var allocationRequest in request.Allocations)
        {
            var commission = await dbContext.PromoterCommissions
                .FirstOrDefaultAsync(c => c.Id == allocationRequest.CommissionId && c.PromoterId == request.PromoterId, cancellationToken)
                ?? throw new ApplicationFailure(FailureKind.NotFound, $"Commission {allocationRequest.CommissionId} could not be found for this promoter.");

            if (commission.Status != CommissionStatus.Payable && commission.Status != CommissionStatus.PartiallyPaid)
            {
                throw new ApplicationFailure(FailureKind.Conflict, $"The commission for order {commission.OrderId} is not currently payable.");
            }

            var alreadyAllocated = await dbContext.PromoterPaymentAllocations
                .Where(a => a.PromoterCommissionId == commission.Id && a.PromoterPayment.Status != PromoterPaymentStatus.Voided)
                .SumAsync(a => (decimal?)a.AllocatedAmount, cancellationToken) ?? 0m;

            var remaining = commission.CommissionAmount - alreadyAllocated;
            if (allocationRequest.Amount > remaining)
            {
                throw new ApplicationFailure(FailureKind.Validation,
                    $"Cannot allocate {allocationRequest.Amount:N2} to the commission for order {commission.OrderId} - only {remaining:N2} remains outstanding.");
            }

            payment.Allocations.Add(new PromoterPaymentAllocation
            {
                PromoterCommission = commission,
                AllocatedAmount = allocationRequest.Amount,
                CreatedAt = now,
            });

            totalAmount += allocationRequest.Amount;

            var newAllocated = alreadyAllocated + allocationRequest.Amount;
            commission.Status = newAllocated >= commission.CommissionAmount ? CommissionStatus.Paid : CommissionStatus.PartiallyPaid;
            if (commission.Status == CommissionStatus.Paid)
            {
                commission.PaidAt = now;
            }
            commission.UpdatedAt = now;
        }

        payment.TotalPaymentAmount = totalAmount;
        dbContext.PromoterPayments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        payment.PaymentReference = $"PMT-{now:yyyy}-{payment.Id:D6}";
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "promoter_payment.create", "PromoterPayment", payment.Id.ToString(),
            $"Recorded payment {payment.PaymentReference} of {totalAmount:N2} to promoter {request.PromoterId}", cancellationToken);

        return await GetPaymentAsync(payment.Id, cancellationToken);
    }

    public async Task<AdminPromoterPaymentDto> VoidPaymentAsync(AdminActor actor, long paymentId, AdminVoidPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.PromoterPayments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw NotFound();

        if (payment.Status == PromoterPaymentStatus.Voided)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "This payment has already been voided.");
        }

        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        payment.Status = PromoterPaymentStatus.Voided;
        payment.Remarks = string.IsNullOrWhiteSpace(payment.Remarks)
            ? $"Voided: {request.Remarks.Trim()}"
            : $"{payment.Remarks}; Voided: {request.Remarks.Trim()}";
        payment.UpdatedAt = now;

        foreach (var allocation in payment.Allocations)
        {
            var commission = await dbContext.PromoterCommissions.FirstAsync(c => c.Id == allocation.PromoterCommissionId, cancellationToken);
            if (commission.Status != CommissionStatus.Paid && commission.Status != CommissionStatus.PartiallyPaid)
            {
                continue;
            }

            var remainingAllocated = await dbContext.PromoterPaymentAllocations
                .Where(a => a.PromoterCommissionId == commission.Id && a.PromoterPaymentId != paymentId && a.PromoterPayment.Status != PromoterPaymentStatus.Voided)
                .SumAsync(a => (decimal?)a.AllocatedAmount, cancellationToken) ?? 0m;

            commission.Status = remainingAllocated <= 0 ? CommissionStatus.Payable
                : remainingAllocated >= commission.CommissionAmount ? CommissionStatus.Paid
                : CommissionStatus.PartiallyPaid;
            commission.PaidAt = commission.Status == CommissionStatus.Paid ? commission.PaidAt : null;
            commission.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "promoter_payment.void", "PromoterPayment", paymentId.ToString(),
            $"Voided payment {payment.PaymentReference}: {request.Remarks}", cancellationToken);

        return await GetPaymentAsync(paymentId, cancellationToken);
    }

    private async Task<Dictionary<long, List<AdminPromoterPaymentAllocationDto>>> LoadAllocationsAsync(List<long> paymentIds, CancellationToken cancellationToken)
    {
        if (paymentIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.PromoterPaymentAllocations
            .AsNoTracking()
            .Where(a => paymentIds.Contains(a.PromoterPaymentId))
            .Select(a => new
            {
                a.PromoterPaymentId,
                CommissionId = a.PromoterCommissionId,
                OrderId = a.PromoterCommission.OrderId,
                OrderNumber = a.PromoterCommission.Order.OrderNumber,
                Amount = a.AllocatedAmount,
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.PromoterPaymentId)
            .ToDictionary(g => g.Key, g => g.Select(r => new AdminPromoterPaymentAllocationDto
            {
                CommissionId = r.CommissionId,
                OrderId = r.OrderId,
                OrderNumber = r.OrderNumber,
                Amount = r.Amount,
            }).ToList());
    }

    private static AdminPromoterPaymentDto ToDto(PaymentRow r, List<AdminPromoterPaymentAllocationDto> allocations) => new()
    {
        Id = r.Id,
        PaymentReference = r.PaymentReference,
        PromoterId = r.PromoterId,
        PromoterName = r.PromoterName,
        TotalPaymentAmount = r.TotalPaymentAmount,
        PaymentDate = r.PaymentDate,
        PaymentMethod = r.PaymentMethod,
        TransactionReference = r.TransactionReference,
        Status = r.Status.ToString(),
        Remarks = r.Remarks,
        Allocations = allocations,
        CreatedAt = r.CreatedAt,
    };

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested payment was not found.");
}
