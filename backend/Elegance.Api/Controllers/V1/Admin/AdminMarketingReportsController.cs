using Elegance.Api.Auth;
using Elegance.Application.Admin;
using Elegance.Infrastructure.Marketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elegance.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.Marketing)]
[Route("api/v1/admin/marketing")]
public class AdminMarketingReportsController(IAdminMarketingReportService reportService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminMarketingDashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        return Ok(await reportService.GetDashboardAsync(cancellationToken));
    }

    [HttpGet("reports/discounts")]
    public async Task<ActionResult<AdminDiscountReportDto>> DiscountReport([FromQuery] AdminMarketingReportQuery query, CancellationToken cancellationToken)
    {
        return Ok(await reportService.GetDiscountReportAsync(query, cancellationToken));
    }

    [HttpGet("reports/discounts/export")]
    public async Task<IActionResult> DiscountReportExport([FromQuery] AdminMarketingReportQuery query, CancellationToken cancellationToken)
    {
        var report = await reportService.GetDiscountReportAsync(query, cancellationToken);
        var csv = CsvExporter.Write(report.Rows,
            ("Product", r => r.ProductName),
            ("Category", r => r.CategoryName),
            ("Orders", r => r.OrderCount),
            ("Units sold", r => r.UnitsSold),
            ("Discount value given", r => r.DiscountValueGiven));
        return File(csv, "text/csv", "product-discount-report.csv");
    }

    [HttpGet("reports/promo-codes")]
    public async Task<ActionResult<AdminPromoCodeReportDto>> PromoCodeReport([FromQuery] AdminMarketingReportQuery query, CancellationToken cancellationToken)
    {
        return Ok(await reportService.GetPromoCodeReportAsync(query, cancellationToken));
    }

    [HttpGet("reports/promo-codes/export")]
    public async Task<IActionResult> PromoCodeReportExport([FromQuery] AdminMarketingReportQuery query, CancellationToken cancellationToken)
    {
        var report = await reportService.GetPromoCodeReportAsync(query, cancellationToken);
        var csv = CsvExporter.Write(report.MostUsed.Concat(report.ExpiringSoon).DistinctBy(r => r.PromoCodeId),
            ("Code", r => r.Code),
            ("Promoter", r => r.PromoterName),
            ("Status", r => r.EffectiveStatus),
            ("Total uses (all time)", r => r.TotalUsesAllTime),
            ("Uses in range", r => r.UsesInRange),
            ("Discount given in range", r => r.DiscountGivenInRange),
            ("Expiry date", r => r.ExpiryDate));
        return File(csv, "text/csv", "promo-code-report.csv");
    }

    [HttpGet("reports/commissions")]
    public async Task<ActionResult<AdminCommissionReportDto>> CommissionReport([FromQuery] AdminMarketingReportQuery query, CancellationToken cancellationToken)
    {
        return Ok(await reportService.GetCommissionReportAsync(query, cancellationToken));
    }

    [HttpGet("reports/commissions/export")]
    public async Task<IActionResult> CommissionReportExport([FromQuery] AdminMarketingReportQuery query, CancellationToken cancellationToken)
    {
        var report = await reportService.GetCommissionReportAsync(query, cancellationToken);
        var csv = CsvExporter.Write(report.ByPromoter,
            ("Promoter", r => r.PromoterName),
            ("Attributed orders", r => r.AttributedOrders),
            ("Total eligible sales", r => r.TotalEligibleSales),
            ("Pending", r => r.PendingAmount),
            ("Approved", r => r.ApprovedAmount),
            ("Payable", r => r.PayableAmount),
            ("Paid", r => r.PaidAmount),
            ("Reversed/Cancelled", r => r.ReversedAmount));
        return File(csv, "text/csv", "promoter-commission-report.csv");
    }

    [HttpGet("reports/payments")]
    public async Task<ActionResult<AdminPaymentReportDto>> PaymentReport([FromQuery] AdminMarketingReportQuery query, CancellationToken cancellationToken)
    {
        return Ok(await reportService.GetPaymentReportAsync(query, cancellationToken));
    }

    [HttpGet("reports/payments/export")]
    public async Task<IActionResult> PaymentReportExport([FromQuery] AdminMarketingReportQuery query, CancellationToken cancellationToken)
    {
        var report = await reportService.GetPaymentReportAsync(query, cancellationToken);
        var csv = CsvExporter.Write(report.ByPromoter,
            ("Promoter", r => r.PromoterName),
            ("Payment count", r => r.PaymentCount),
            ("Total paid", r => r.TotalPaid),
            ("Outstanding balance", r => r.OutstandingBalance));
        return File(csv, "text/csv", "promoter-payment-report.csv");
    }
}
