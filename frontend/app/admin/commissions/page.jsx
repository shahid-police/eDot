"use client";

import { useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

const PAGE_SIZE = 25;
const STATUSES = ["Pending", "Approved", "Payable", "PartiallyPaid", "Paid", "Reversed", "Cancelled"];

function StatusBadge({ status }) {
  const tone = {
    Pending: "text-bg-secondary",
    Approved: "text-bg-info",
    Payable: "text-bg-primary",
    PartiallyPaid: "text-bg-warning",
    Paid: "text-bg-success",
    Reversed: "text-bg-danger",
    Cancelled: "text-bg-dark",
  }[status] || "text-bg-secondary";

  return <span className={`badge ${tone}`}>{status}</span>;
}

export default function AdminCommissionsPage() {
  const [summary, setSummary] = useState(null);
  const [summaryError, setSummaryError] = useState("");

  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [reload, setReload] = useState(0);
  const [busyId, setBusyId] = useState(null);

  useEffect(() => {
    adminRequest("/commissions/summary").then(setSummary).catch((err) => setSummaryError(err.message));
  }, [reload]);

  useEffect(() => {
    let active = true;
    adminRequest("/commissions", { query: { status, page, pageSize: PAGE_SIZE } })
      .then((data) => active && setResult(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [status, page, reload]);

  async function approve(commission) {
    setBusyId(commission.id);
    setError("");
    try {
      await adminRequest(`/commissions/${commission.id}/approve`, { method: "POST" });
      setReload((n) => n + 1);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusyId(null);
    }
  }

  async function markPayable(commission) {
    setBusyId(commission.id);
    setError("");
    try {
      await adminRequest(`/commissions/${commission.id}/mark-payable`, { method: "POST" });
      setReload((n) => n + 1);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusyId(null);
    }
  }

  async function reverse(commission) {
    const remarks = window.prompt("Reason for reversing this commission:");
    if (!remarks || !remarks.trim()) return;
    setBusyId(commission.id);
    setError("");
    try {
      await adminRequest(`/commissions/${commission.id}/reverse`, { method: "POST", body: { remarks: remarks.trim() } });
      setReload((n) => n + 1);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusyId(null);
    }
  }

  async function cancel(commission) {
    const remarks = window.prompt("Reason for cancelling this commission:");
    if (!remarks || !remarks.trim()) return;
    setBusyId(commission.id);
    setError("");
    try {
      await adminRequest(`/commissions/${commission.id}/cancel`, { method: "POST", body: { remarks: remarks.trim() } });
      setReload((n) => n + 1);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusyId(null);
    }
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  const stats = summary && [
    ["Pending", summary.pendingCount, summary.pendingAmount],
    ["Approved", summary.approvedCount, null],
    ["Payable", summary.payableCount, summary.payableAmount],
    ["Paid", summary.paidCount, summary.paidAmount],
    ["Reversed", summary.reversedCount, null],
    ["Cancelled", summary.cancelledCount, null],
  ];

  return (
    <div>
      <h1 className="h4 mb-3">Promoter Commissions</h1>

      {summaryError && <div className="alert alert-danger">{summaryError}</div>}
      {stats && (
        <div className="row g-3 mb-4">
          {stats.map(([label, count, amount]) => (
            <div key={label} className="col-6 col-md-4 col-xl-2">
              <div className="card h-100">
                <div className="card-body">
                  <div className="small text-muted">{label}</div>
                  <div className="fs-5 fw-semibold">{count}</div>
                  {amount != null && <div className="small text-muted">{amount.toLocaleString()} PKR</div>}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <div className="d-flex flex-wrap gap-2 mb-3">
        <select className="form-select form-select-sm w-auto" value={status} onChange={(e) => { setPage(1); setStatus(e.target.value); }}>
          <option value="">All statuses</option>
          {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}
      {!result && !error && <p>Loading...</p>}

      {result && (
        <>
          <div className="table-responsive mb-2">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>Order</th>
                  <th>Promoter</th>
                  <th>Code</th>
                  <th className="text-end">Eligible amount</th>
                  <th className="text-end">%</th>
                  <th className="text-end">Commission</th>
                  <th>Status</th>
                  <th>Created</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {result.items.map((c) => (
                  <tr key={c.id}>
                    <td>{c.orderNumber} <span className="small text-muted">({c.orderStatus})</span></td>
                    <td>{c.promoterName}</td>
                    <td className="font-monospace">{c.promoCode}</td>
                    <td className="text-end">{c.eligibleSaleAmount.toLocaleString()}</td>
                    <td className="text-end">{c.commissionPercentageSnapshot}%</td>
                    <td className="text-end">{c.commissionAmount.toLocaleString()}</td>
                    <td>
                      <StatusBadge status={c.status} />
                      {c.remarks && <div className="small text-muted">{c.remarks}</div>}
                    </td>
                    <td>{formatDateTime(c.createdAt)}</td>
                    <td className="text-end">
                      <div className="d-flex gap-1 justify-content-end flex-wrap">
                        {c.status === "Pending" && (
                          <button type="button" className="btn btn-outline-dark btn-sm" disabled={busyId === c.id} onClick={() => approve(c)}>Approve</button>
                        )}
                        {(c.status === "Pending" || c.status === "Approved") && (
                          <button type="button" className="btn btn-outline-primary btn-sm" disabled={busyId === c.id} onClick={() => markPayable(c)}>Mark payable</button>
                        )}
                        {["Pending", "Approved", "Payable", "PartiallyPaid"].includes(c.status) && (
                          <>
                            <button type="button" className="btn btn-outline-danger btn-sm" disabled={busyId === c.id} onClick={() => reverse(c)}>Reverse</button>
                            <button type="button" className="btn btn-outline-secondary btn-sm" disabled={busyId === c.id} onClick={() => cancel(c)}>Cancel</button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
                {result.items.length === 0 && <tr><td colSpan={9} className="text-muted">No commissions match.</td></tr>}
              </tbody>
            </table>
          </div>
          <div className="d-flex align-items-center gap-2">
            <button className="btn btn-outline-dark btn-sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</button>
            <span className="small">Page {page} of {totalPages}</span>
            <button className="btn btn-outline-dark btn-sm" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Next</button>
          </div>
        </>
      )}
    </div>
  );
}
