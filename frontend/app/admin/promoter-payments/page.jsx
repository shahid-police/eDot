"use client";

import { useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

const PAGE_SIZE = 25;
const PAYMENT_METHODS = ["BankTransfer", "EasyPaisa", "JazzCash", "Cash", "Other"];

export default function AdminPromoterPaymentsPage() {
  const [promoters, setPromoters] = useState([]);
  const [promoterId, setPromoterId] = useState("");
  const [balance, setBalance] = useState(null);
  const [balanceError, setBalanceError] = useState("");

  const [selected, setSelected] = useState({}); // commissionId -> amount string
  const [paymentDate, setPaymentDate] = useState(new Date().toISOString().slice(0, 10));
  const [paymentMethod, setPaymentMethod] = useState("BankTransfer");
  const [transactionReference, setTransactionReference] = useState("");
  const [remarks, setRemarks] = useState("");
  const [formError, setFormError] = useState("");
  const [busy, setBusy] = useState(false);

  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [listError, setListError] = useState("");
  const [reload, setReload] = useState(0);

  useEffect(() => {
    adminRequest("/promoters", { query: { pageSize: 200 } }).then((d) => setPromoters(d.items)).catch(() => {});
  }, []);

  function loadBalance(id) {
    setBalance(null);
    setBalanceError("");
    setSelected({});
    if (!id) return;
    adminRequest(`/promoter-payments/balance/${id}`).then(setBalance).catch((err) => setBalanceError(err.message));
  }

  useEffect(() => {
    let active = true;
    adminRequest("/promoter-payments", { query: { promoterId: promoterId || undefined, page, pageSize: PAGE_SIZE } })
      .then((data) => active && setResult(data))
      .catch((err) => active && setListError(err.message));
    return () => { active = false; };
  }, [promoterId, page, reload]);

  function toggleCommission(commission) {
    setSelected((current) => {
      const next = { ...current };
      if (commission.commissionId in next) {
        delete next[commission.commissionId];
      } else {
        next[commission.commissionId] = String(commission.remainingAmount);
      }
      return next;
    });
  }

  function updateAmount(commissionId, value) {
    setSelected((current) => ({ ...current, [commissionId]: value }));
  }

  const selectedTotal = Object.values(selected).reduce((sum, v) => sum + (Number(v) || 0), 0);

  async function submitPayment(event) {
    event.preventDefault();
    setFormError("");
    const allocations = Object.entries(selected)
      .map(([commissionId, amount]) => ({ commissionId: Number(commissionId), amount: Number(amount) }))
      .filter((a) => a.amount > 0);
    if (allocations.length === 0) {
      setFormError("Select at least one commission and amount to pay.");
      return;
    }
    setBusy(true);
    try {
      await adminRequest("/promoter-payments", {
        method: "POST",
        body: {
          promoterId: Number(promoterId),
          paymentDate: new Date(paymentDate).toISOString(),
          paymentMethod,
          transactionReference: transactionReference.trim() || null,
          remarks: remarks.trim() || null,
          allocations,
        },
      });
      setTransactionReference("");
      setRemarks("");
      loadBalance(promoterId);
      setReload((n) => n + 1);
    } catch (err) {
      setFormError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function voidPayment(payment) {
    const voidRemarks = window.prompt("Reason for voiding this payment:");
    if (!voidRemarks || !voidRemarks.trim()) return;
    try {
      await adminRequest(`/promoter-payments/${payment.id}/void`, { method: "POST", body: { remarks: voidRemarks.trim() } });
      setReload((n) => n + 1);
      if (promoterId) loadBalance(promoterId);
    } catch (err) {
      setListError(err.message);
    }
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <h1 className="h4 mb-3">Promoter Payments</h1>

      <div className="mb-4">
        <label className="form-label small">Promoter</label>
        <select
          className="form-select form-select-sm w-auto"
          value={promoterId}
          onChange={(e) => { setPromoterId(e.target.value); setPage(1); loadBalance(e.target.value); }}
        >
          <option value="">Choose a promoter...</option>
          {promoters.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
        </select>
      </div>

      {balanceError && <div className="alert alert-danger">{balanceError}</div>}

      {balance && (
        <div className="border rounded p-3 mb-4">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <h2 className="h6 mb-0">{balance.promoterName} - outstanding balance</h2>
            <span className="fs-5 fw-semibold">{balance.outstandingBalance.toLocaleString()} PKR</span>
          </div>

          {balance.payableCommissions.length === 0 && <p className="small text-muted mb-0">Nothing payable right now.</p>}

          {balance.payableCommissions.length > 0 && (
            <>
              <table className="table table-sm align-middle mb-3">
                <thead>
                  <tr>
                    <th />
                    <th>Order</th>
                    <th className="text-end">Commission</th>
                    <th className="text-end">Already paid</th>
                    <th className="text-end">Remaining</th>
                    <th className="text-end">Pay amount</th>
                  </tr>
                </thead>
                <tbody>
                  {balance.payableCommissions.map((c) => (
                    <tr key={c.commissionId}>
                      <td>
                        <input type="checkbox" checked={c.commissionId in selected} onChange={() => toggleCommission(c)} />
                      </td>
                      <td>{c.orderNumber}</td>
                      <td className="text-end">{c.commissionAmount.toLocaleString()}</td>
                      <td className="text-end">{c.allocatedAmount.toLocaleString()}</td>
                      <td className="text-end">{c.remainingAmount.toLocaleString()}</td>
                      <td className="text-end" style={{ width: 120 }}>
                        <input
                          type="number" min="0.01" max={c.remainingAmount} step="0.01"
                          className="form-control form-control-sm"
                          disabled={!(c.commissionId in selected)}
                          value={selected[c.commissionId] ?? ""}
                          onChange={(e) => updateAmount(c.commissionId, e.target.value)}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              <form onSubmit={submitPayment} className="row g-2 align-items-end">
                {formError && <div className="col-12"><div className="alert alert-danger py-2">{formError}</div></div>}
                <div className="col-md-2">
                  <label className="form-label small">Payment date</label>
                  <input type="date" className="form-control form-control-sm" value={paymentDate} onChange={(e) => setPaymentDate(e.target.value)} required />
                </div>
                <div className="col-md-2">
                  <label className="form-label small">Method</label>
                  <select className="form-select form-select-sm" value={paymentMethod} onChange={(e) => setPaymentMethod(e.target.value)}>
                    {PAYMENT_METHODS.map((m) => <option key={m} value={m}>{m}</option>)}
                  </select>
                </div>
                <div className="col-md-3">
                  <label className="form-label small">Transaction reference (optional)</label>
                  <input className="form-control form-control-sm" value={transactionReference} onChange={(e) => setTransactionReference(e.target.value)} maxLength={100} />
                </div>
                <div className="col-md-3">
                  <label className="form-label small">Remarks (optional)</label>
                  <input className="form-control form-control-sm" value={remarks} onChange={(e) => setRemarks(e.target.value)} maxLength={1000} />
                </div>
                <div className="col-md-2">
                  <button type="submit" className="btn btn-dark btn-sm w-100" disabled={busy || selectedTotal <= 0}>
                    {busy ? "Saving..." : `Pay ${selectedTotal.toLocaleString()}`}
                  </button>
                </div>
              </form>
            </>
          )}
        </div>
      )}

      <h2 className="h6 mb-2">Payment history</h2>
      {listError && <div className="alert alert-danger">{listError}</div>}
      {!result && !listError && <p>Loading...</p>}

      {result && (
        <>
          <div className="table-responsive mb-2">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>Reference</th>
                  <th>Promoter</th>
                  <th className="text-end">Amount</th>
                  <th>Method</th>
                  <th>Transaction ref</th>
                  <th>Date</th>
                  <th>Status</th>
                  <th>Allocations</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {result.items.map((p) => (
                  <tr key={p.id}>
                    <td className="font-monospace">{p.paymentReference}</td>
                    <td>{p.promoterName}</td>
                    <td className="text-end">{p.totalPaymentAmount.toLocaleString()}</td>
                    <td>{p.paymentMethod}</td>
                    <td>{p.transactionReference ?? "-"}</td>
                    <td>{formatDateTime(p.paymentDate)}</td>
                    <td>
                      <span className={`badge ${p.status === "Voided" ? "text-bg-danger" : "text-bg-success"}`}>{p.status}</span>
                      {p.remarks && <div className="small text-muted">{p.remarks}</div>}
                    </td>
                    <td className="small">
                      {p.allocations.map((a) => (
                        <div key={a.commissionId}>{a.orderNumber}: {a.amount.toLocaleString()}</div>
                      ))}
                    </td>
                    <td className="text-end">
                      {p.status !== "Voided" && (
                        <button type="button" className="btn btn-outline-danger btn-sm" onClick={() => voidPayment(p)}>Void</button>
                      )}
                    </td>
                  </tr>
                ))}
                {result.items.length === 0 && <tr><td colSpan={9} className="text-muted">No payments recorded yet.</td></tr>}
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
