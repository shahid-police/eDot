"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

const PAGE_SIZE = 25;
const STATUSES = ["PendingApproval", "Approved", "Rejected", "ReturnedForCorrection"];

export default function AdminVendorSubmissionsPage() {
  const [status, setStatus] = useState("PendingApproval");
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    adminRequest("/vendor-submissions", { query: { status, search: query, page, pageSize: PAGE_SIZE } })
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
      <h1 className="h4 mb-3">Vendor Product Approvals</h1>

      <div className="d-flex flex-wrap gap-2 mb-3">
        <select className="form-select form-select-sm w-auto" value={status} onChange={(e) => { setPage(1); setStatus(e.target.value); }}>
          <option value="">All statuses</option>
          {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
        <form onSubmit={applySearch} className="d-flex gap-2">
          <input className="form-control form-control-sm" placeholder="Product name or vendor code" value={search} onChange={(e) => setSearch(e.target.value)} />
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
                  <th>Product</th>
                  <th>Vendor</th>
                  <th>Category</th>
                  <th>Wholesale</th>
                  <th>Images</th>
                  <th>Submitted</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((s) => (
                  <tr key={s.id}>
                    <td><Link href={`/admin/vendor-submissions/${s.id}`}>{s.productName}</Link></td>
                    <td>{s.shopName} ({s.vendorCode})</td>
                    <td>{s.categoryName}</td>
                    <td>{s.wholesalePrice}</td>
                    <td>{s.images.length}</td>
                    <td>{formatDateTime(s.submittedAt)}</td>
                    <td>{s.status}</td>
                  </tr>
                ))}
                {result.items.length === 0 && (
                  <tr><td colSpan={7} className="text-muted">No submissions match.</td></tr>
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
