"use client";

import { useEffect, useState } from "react";
import { adminRequest, adminDownload } from "@/lib/admin/api";

const TABS = ["Discounts", "Promo Codes", "Commissions", "Payments"];

function useReport(path, filters) {
  const [data, setData] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    adminRequest(path, { query: filters })
      .then((d) => active && setData(d))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [path, JSON.stringify(filters)]);

  return { data, error };
}

function FilterBar({ filters, setFilters, promoters, showProductCategory }) {
  return (
    <div className="d-flex flex-wrap gap-2 mb-3">
      <div>
        <label className="form-label small mb-0">From</label>
        <input type="date" className="form-control form-control-sm" value={filters.startDate} onChange={(e) => setFilters((f) => ({ ...f, startDate: e.target.value }))} />
      </div>
      <div>
        <label className="form-label small mb-0">To</label>
        <input type="date" className="form-control form-control-sm" value={filters.endDate} onChange={(e) => setFilters((f) => ({ ...f, endDate: e.target.value }))} />
      </div>
      {promoters && (
        <div>
          <label className="form-label small mb-0">Promoter</label>
          <select className="form-select form-select-sm" value={filters.promoterId} onChange={(e) => setFilters((f) => ({ ...f, promoterId: e.target.value }))}>
            <option value="">All promoters</option>
            {promoters.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
        </div>
      )}
      {showProductCategory && (
        <div>
          <label className="form-label small mb-0">Product ID</label>
          <input type="number" min="1" className="form-control form-control-sm" style={{ width: 110 }} value={filters.productId} onChange={(e) => setFilters((f) => ({ ...f, productId: e.target.value }))} />
        </div>
      )}
    </div>
  );
}

function toQuery(filters) {
  return {
    startDate: filters.startDate ? new Date(filters.startDate).toISOString() : undefined,
    endDate: filters.endDate ? new Date(filters.endDate).toISOString() : undefined,
    promoterId: filters.promoterId || undefined,
    productId: filters.productId || undefined,
  };
}

export default function AdminMarketingDashboardPage() {
  const [dashboard, setDashboard] = useState(null);
  const [dashboardError, setDashboardError] = useState("");
  const [promoters, setPromoters] = useState([]);
  const [tab, setTab] = useState("Discounts");

  const [discountFilters, setDiscountFilters] = useState({ startDate: "", endDate: "", productId: "" });
  const [codeFilters, setCodeFilters] = useState({ startDate: "", endDate: "", promoterId: "" });
  const [commissionFilters, setCommissionFilters] = useState({ startDate: "", endDate: "", promoterId: "" });
  const [paymentFilters, setPaymentFilters] = useState({ startDate: "", endDate: "", promoterId: "" });

  useEffect(() => {
    adminRequest("/marketing/dashboard").then(setDashboard).catch((err) => setDashboardError(err.message));
    adminRequest("/promoters", { query: { pageSize: 200 } }).then((d) => setPromoters(d.items)).catch(() => {});
  }, []);

  const discountReport = useReport("/marketing/reports/discounts", toQuery(discountFilters));
  const codeReport = useReport("/marketing/reports/promo-codes", toQuery(codeFilters));
  const commissionReport = useReport("/marketing/reports/commissions", toQuery(commissionFilters));
  const paymentReport = useReport("/marketing/reports/payments", toQuery(paymentFilters));

  function exportCsv(path, filters, filename) {
    adminDownload(path, { query: toQuery(filters), filename }).catch((err) => alert(err.message));
  }

  const dashboardStats = dashboard && [
    ["Active discounts", dashboard.activeDiscountCount],
    ["Scheduled discounts", dashboard.scheduledDiscountCount],
    ["Expired discounts", dashboard.expiredDiscountCount],
    ["Promo codes (active/total)", `${dashboard.activePromoCodes}/${dashboard.totalPromoCodes}`],
    ["Promo code uses", dashboard.totalPromoCodeUses],
    ["Customer discount given", `${dashboard.totalCustomerDiscountGiven.toLocaleString()} PKR`],
    ["Promoters (active/total)", `${dashboard.activePromoters}/${dashboard.totalPromoters}`],
    ["Attributed orders", dashboard.attributedOrders],
    ["Total eligible sales", `${dashboard.totalEligibleSales.toLocaleString()} PKR`],
    ["Commission pending", `${dashboard.pendingCommissionAmount.toLocaleString()} PKR`],
    ["Commission payable", `${dashboard.payableCommissionAmount.toLocaleString()} PKR`],
    ["Commission paid", `${dashboard.paidCommissionAmount.toLocaleString()} PKR`],
    ["Commission reversed", `${dashboard.reversedCommissionAmount.toLocaleString()} PKR`],
    ["Payments recorded", dashboard.totalPayments],
    ["Total paid out", `${dashboard.totalPaymentsAmount.toLocaleString()} PKR`],
    ["Outstanding payable balance", `${dashboard.outstandingPayableBalance.toLocaleString()} PKR`],
  ];

  return (
    <div>
      <h1 className="h4 mb-3">Marketing Dashboard</h1>

      {dashboardError && <div className="alert alert-danger">{dashboardError}</div>}
      {dashboardStats && (
        <div className="row g-3 mb-4">
          {dashboardStats.map(([label, value]) => (
            <div key={label} className="col-6 col-md-4 col-xl-3">
              <div className="card h-100">
                <div className="card-body">
                  <div className="small text-muted">{label}</div>
                  <div className="fs-6 fw-semibold">{value}</div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <ul className="nav nav-tabs mb-3">
        {TABS.map((t) => (
          <li key={t} className="nav-item">
            <button type="button" className={`nav-link ${tab === t ? "active" : ""}`} onClick={() => setTab(t)}>{t}</button>
          </li>
        ))}
      </ul>

      {tab === "Discounts" && (
        <section>
          <FilterBar filters={discountFilters} setFilters={setDiscountFilters} showProductCategory />
          <div className="d-flex justify-content-between align-items-center mb-2">
            <div className="d-flex gap-3">
              {discountReport.data && (
                <>
                  <span className="small">Active: <strong>{discountReport.data.activeCount}</strong></span>
                  <span className="small">Scheduled: <strong>{discountReport.data.scheduledCount}</strong></span>
                  <span className="small">Expired: <strong>{discountReport.data.expiredCount}</strong></span>
                  <span className="small">Total discount given: <strong>{discountReport.data.totalDiscountValueGiven.toLocaleString()} PKR</strong></span>
                </>
              )}
            </div>
            <button type="button" className="btn btn-outline-dark btn-sm" onClick={() => exportCsv("/marketing/reports/discounts/export", discountFilters, "product-discount-report.csv")}>
              Export CSV
            </button>
          </div>
          {discountReport.error && <div className="alert alert-danger">{discountReport.error}</div>}
          {discountReport.data && (
            <table className="table table-sm align-middle">
              <thead><tr><th>Product</th><th>Category</th><th className="text-end">Orders</th><th className="text-end">Units</th><th className="text-end">Discount given</th></tr></thead>
              <tbody>
                {discountReport.data.rows.map((r) => (
                  <tr key={r.productId}>
                    <td>{r.productName}</td>
                    <td>{r.categoryName}</td>
                    <td className="text-end">{r.orderCount}</td>
                    <td className="text-end">{r.unitsSold}</td>
                    <td className="text-end">{r.discountValueGiven.toLocaleString()}</td>
                  </tr>
                ))}
                {discountReport.data.rows.length === 0 && <tr><td colSpan={5} className="text-muted">No data in range.</td></tr>}
              </tbody>
            </table>
          )}
        </section>
      )}

      {tab === "Promo Codes" && (
        <section>
          <FilterBar filters={codeFilters} setFilters={setCodeFilters} promoters={promoters} />
          <div className="d-flex justify-content-between align-items-center mb-2">
            <div className="d-flex gap-3">
              {codeReport.data && (
                <>
                  <span className="small">Total: <strong>{codeReport.data.totalCodes}</strong></span>
                  <span className="small">Active: <strong>{codeReport.data.activeCodes}</strong></span>
                  <span className="small">Inactive: <strong>{codeReport.data.inactiveCodes}</strong></span>
                  <span className="small">Expired: <strong>{codeReport.data.expiredCodes}</strong></span>
                  <span className="small">Discount given in range: <strong>{codeReport.data.totalDiscountGivenInRange.toLocaleString()} PKR</strong></span>
                </>
              )}
            </div>
            <button type="button" className="btn btn-outline-dark btn-sm" onClick={() => exportCsv("/marketing/reports/promo-codes/export", codeFilters, "promo-code-report.csv")}>
              Export CSV
            </button>
          </div>
          {codeReport.error && <div className="alert alert-danger">{codeReport.error}</div>}
          {codeReport.data && (
            <>
              <h3 className="h6">Most used</h3>
              <table className="table table-sm align-middle mb-4">
                <thead><tr><th>Code</th><th>Promoter</th><th>Status</th><th className="text-end">Uses (all time)</th><th className="text-end">Uses in range</th><th className="text-end">Discount in range</th></tr></thead>
                <tbody>
                  {codeReport.data.mostUsed.map((r) => (
                    <tr key={r.promoCodeId}>
                      <td className="font-monospace">{r.code}</td>
                      <td>{r.promoterName}</td>
                      <td>{r.effectiveStatus}</td>
                      <td className="text-end">{r.totalUsesAllTime}</td>
                      <td className="text-end">{r.usesInRange}</td>
                      <td className="text-end">{r.discountGivenInRange.toLocaleString()}</td>
                    </tr>
                  ))}
                  {codeReport.data.mostUsed.length === 0 && <tr><td colSpan={6} className="text-muted">No codes.</td></tr>}
                </tbody>
              </table>
              <h3 className="h6">Expiring within 14 days</h3>
              <table className="table table-sm align-middle">
                <thead><tr><th>Code</th><th>Promoter</th><th>Expiry</th></tr></thead>
                <tbody>
                  {codeReport.data.expiringSoon.map((r) => (
                    <tr key={r.promoCodeId}>
                      <td className="font-monospace">{r.code}</td>
                      <td>{r.promoterName}</td>
                      <td>{r.expiryDate ? new Date(r.expiryDate).toLocaleDateString() : "-"}</td>
                    </tr>
                  ))}
                  {codeReport.data.expiringSoon.length === 0 && <tr><td colSpan={3} className="text-muted">None expiring soon.</td></tr>}
                </tbody>
              </table>
            </>
          )}
        </section>
      )}

      {tab === "Commissions" && (
        <section>
          <FilterBar filters={commissionFilters} setFilters={setCommissionFilters} promoters={promoters} />
          <div className="d-flex justify-content-between align-items-center mb-2">
            <div className="d-flex gap-3 flex-wrap">
              {commissionReport.data && (
                <>
                  <span className="small">Promoters (active/total): <strong>{commissionReport.data.activePromoters}/{commissionReport.data.totalPromoters}</strong></span>
                  <span className="small">Attributed orders: <strong>{commissionReport.data.totalAttributedOrders}</strong></span>
                  <span className="small">Eligible sales: <strong>{commissionReport.data.totalEligibleSales.toLocaleString()}</strong></span>
                </>
              )}
            </div>
            <button type="button" className="btn btn-outline-dark btn-sm" onClick={() => exportCsv("/marketing/reports/commissions/export", commissionFilters, "promoter-commission-report.csv")}>
              Export CSV
            </button>
          </div>
          {commissionReport.error && <div className="alert alert-danger">{commissionReport.error}</div>}
          {commissionReport.data && (
            <table className="table table-sm align-middle">
              <thead><tr><th>Promoter</th><th className="text-end">Orders</th><th className="text-end">Eligible sales</th><th className="text-end">Pending</th><th className="text-end">Approved</th><th className="text-end">Payable</th><th className="text-end">Paid</th><th className="text-end">Reversed</th></tr></thead>
              <tbody>
                {commissionReport.data.byPromoter.map((r) => (
                  <tr key={r.promoterId}>
                    <td>{r.promoterName}</td>
                    <td className="text-end">{r.attributedOrders}</td>
                    <td className="text-end">{r.totalEligibleSales.toLocaleString()}</td>
                    <td className="text-end">{r.pendingAmount.toLocaleString()}</td>
                    <td className="text-end">{r.approvedAmount.toLocaleString()}</td>
                    <td className="text-end">{r.payableAmount.toLocaleString()}</td>
                    <td className="text-end">{r.paidAmount.toLocaleString()}</td>
                    <td className="text-end">{r.reversedAmount.toLocaleString()}</td>
                  </tr>
                ))}
                {commissionReport.data.byPromoter.length === 0 && <tr><td colSpan={8} className="text-muted">No data in range.</td></tr>}
              </tbody>
            </table>
          )}
        </section>
      )}

      {tab === "Payments" && (
        <section>
          <FilterBar filters={paymentFilters} setFilters={setPaymentFilters} promoters={promoters} />
          <div className="d-flex justify-content-between align-items-center mb-2">
            <div className="d-flex gap-3 flex-wrap">
              {paymentReport.data && (
                <>
                  <span className="small">Payments: <strong>{paymentReport.data.totalPayments}</strong></span>
                  <span className="small">Total paid: <strong>{paymentReport.data.totalPaidAmount.toLocaleString()}</strong></span>
                  <span className="small">Outstanding: <strong>{paymentReport.data.totalOutstandingBalance.toLocaleString()}</strong></span>
                  <span className="small">Partial settlements: <strong>{paymentReport.data.partialSettlementCount}</strong></span>
                </>
              )}
            </div>
            <button type="button" className="btn btn-outline-dark btn-sm" onClick={() => exportCsv("/marketing/reports/payments/export", paymentFilters, "promoter-payment-report.csv")}>
              Export CSV
            </button>
          </div>
          {paymentReport.error && <div className="alert alert-danger">{paymentReport.error}</div>}
          {paymentReport.data && (
            <table className="table table-sm align-middle">
              <thead><tr><th>Promoter</th><th className="text-end">Payments</th><th className="text-end">Total paid</th><th className="text-end">Outstanding</th></tr></thead>
              <tbody>
                {paymentReport.data.byPromoter.map((r) => (
                  <tr key={r.promoterId}>
                    <td>{r.promoterName}</td>
                    <td className="text-end">{r.paymentCount}</td>
                    <td className="text-end">{r.totalPaid.toLocaleString()}</td>
                    <td className="text-end">{r.outstandingBalance.toLocaleString()}</td>
                  </tr>
                ))}
                {paymentReport.data.byPromoter.length === 0 && <tr><td colSpan={4} className="text-muted">No data in range.</td></tr>}
              </tbody>
            </table>
          )}
        </section>
      )}
    </div>
  );
}
