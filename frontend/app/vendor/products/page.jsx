"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { vendorRequest, formatDateTime, SUBMISSION_STATUSES } from "@/lib/vendor/api";

const PAGE_SIZE = 25;

export default function VendorProductsPage() {
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    vendorRequest("/products", { query: { status, page, pageSize: PAGE_SIZE } })
      .then((data) => active && setResult(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [status, page]);

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h1 className="h4 mb-0">My Products</h1>
        <Link href="/vendor/products/new" className="btn btn-dark btn-sm">Add new product</Link>
      </div>

      <div className="mb-3">
        <select className="form-select form-select-sm w-auto" value={status} onChange={(e) => { setPage(1); setStatus(e.target.value); }}>
          <option value="">All statuses</option>
          {SUBMISSION_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
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
                  <th>Category</th>
                  <th>Your code</th>
                  <th>SKU</th>
                  <th>Submitted</th>
                  <th>Status</th>
                  <th>Images</th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((s) => (
                  <tr key={s.id}>
                    <td><Link href={`/vendor/products/${s.id}`}>{s.productName}</Link></td>
                    <td>{s.categoryName}</td>
                    <td>{s.vendorProductCode}</td>
                    <td>{s.productSku ?? "-"}</td>
                    <td>{formatDateTime(s.submittedAt)}</td>
                    <td>{s.status}</td>
                    <td>{s.images.length}</td>
                  </tr>
                ))}
                {result.items.length === 0 && (
                  <tr><td colSpan={7} className="text-muted">No submissions yet.</td></tr>
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
