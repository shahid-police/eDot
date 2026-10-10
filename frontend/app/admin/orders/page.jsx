"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { adminRequest, formatDateTime, ORDER_STATUSES } from "@/lib/admin/api";
import { formatPrice } from "@/lib/format/price";

const PAGE_SIZE = 25;

export default function AdminOrdersPage() {
  const [status, setStatus] = useState("");
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    adminRequest("/orders", { query: { status, search: query, page, pageSize: PAGE_SIZE } })
      .then((data) => active && setResult(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [status, query, page]);

  function applySearch(event) {
    event.preventDefault();
    setPage(1);
    setQuery(search.trim());
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <h1 className="h4 mb-3">Orders</h1>

      <div className="d-flex flex-wrap gap-2 mb-3">
        <select className="form-select form-select-sm w-auto" value={status} onChange={(e) => { setPage(1); setStatus(e.target.value); }}>
          <option value="">All statuses</option>
          {ORDER_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
        <form onSubmit={applySearch} className="d-flex gap-2">
          <input className="form-control form-control-sm" placeholder="Order number, email or phone" value={search} onChange={(e) => setSearch(e.target.value)} />
          <button className="btn btn-outline-dark btn-sm" type="submit">Search</button>
        </form>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}
      {!result && !error && <p>Loading...</p>}

      {result && (
        <>
          <div className="table-responsive">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>Order</th>
                  <th>Placed</th>
                  <th>Customer</th>
                  <th>Status</th>
                  <th>Payment</th>
                  <th className="text-end">Total</th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((o) => (
                  <tr key={o.id}>
                    <td><Link href={`/admin/orders/${o.id}`}>{o.orderNumber}</Link></td>
                    <td>{formatDateTime(o.createdAt)}</td>
                    <td>{o.email}</td>
                    <td>{o.status}</td>
                    <td>{o.paymentStatus}</td>
                    <td className="text-end">{formatPrice(o.grandTotal)}</td>
                  </tr>
                ))}
                {result.items.length === 0 && (
                  <tr><td colSpan={6} className="text-muted">No orders match.</td></tr>
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
