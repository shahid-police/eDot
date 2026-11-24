"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { vendorRequest, formatDateTime } from "@/lib/vendor/api";

const PAGE_SIZE = 25;
const STATUSES = ["Pending", "Confirmed", "Processing", "Packed", "Shipped", "OutForDelivery", "Delivered", "Cancelled", "Returned", "Refunded"];

export default function VendorOrdersPage() {
  const [summary, setSummary] = useState(null);
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    vendorRequest("/orders/summary").then(setSummary).catch((err) => setError(err.message));
  }, []);

  useEffect(() => {
    let active = true;
    vendorRequest("/orders", { query: { status, page, pageSize: PAGE_SIZE } })
      .then((data) => active && setResult(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [status, page]);

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <h1 className="h4 mb-3">My Orders</h1>

      {error && <div className="alert alert-danger">{error}</div>}

      {summary && (
        <div className="row g-3 mb-4">
          {[
            ["Orders containing my products", summary.totalOrders],
            ["Total units ordered", summary.totalUnits],
            ["Delivered units", summary.deliveredUnits],
            ["Pending", summary.pendingOrders],
            ["Confirmed", summary.confirmedOrders],
            ["Processing", summary.processingOrders],
            ["Dispatched", summary.dispatchedOrders],
            ["Delivered orders", summary.deliveredOrders],
            ["Cancelled", summary.cancelledOrders],
            ["Returned", summary.returnedOrders],
          ].map(([label, value]) => (
            <div className="col-6 col-md-3" key={label}>
              <div className="border rounded p-3 h-100">
                <div className="h4 mb-0">{value}</div>
                <div className="small text-muted">{label}</div>
              </div>
            </div>
          ))}
        </div>
      )}

      <div className="mb-3">
        <select className="form-select form-select-sm w-auto" value={status} onChange={(e) => { setPage(1); setStatus(e.target.value); }}>
          <option value="">All statuses</option>
          {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>

      {!result && !error && <p>Loading...</p>}

      {result && (
        <>
          <div className="table-responsive">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>Order</th>
                  <th>Date</th>
                  <th>Product</th>
                  <th>SKU</th>
                  <th>Qty</th>
                  <th>Amount</th>
                  <th>Status</th>
                  <th>Payment</th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((item, i) => (
                  <tr key={i}>
                    <td><Link href={`/vendor/orders/${item.orderId}`}>{item.orderNumber}</Link></td>
                    <td>{formatDateTime(item.orderDate)}</td>
                    <td>{item.productName}</td>
                    <td>{item.sku}</td>
                    <td>{item.quantity}</td>
                    <td>{item.itemAmount}</td>
                    <td>{item.orderStatus}</td>
                    <td>{item.paymentStatus}</td>
                  </tr>
                ))}
                {result.items.length === 0 && (
                  <tr><td colSpan={8} className="text-muted">No orders yet.</td></tr>
                )}
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
