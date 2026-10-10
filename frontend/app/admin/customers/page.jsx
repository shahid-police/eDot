"use client";

import { useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

const PAGE_SIZE = 25;

export default function AdminCustomersPage() {
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    adminRequest("/customers", { query: { search: query, page, pageSize: PAGE_SIZE } })
      .then((data) => active && setResult(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [query, page, reload]);

  async function toggle(customer) {
    setError("");
    try {
      await adminRequest(`/customers/${customer.id}/status`, { method: "PUT", body: { isActive: !customer.isActive } });
      setReload((n) => n + 1);
    } catch (err) {
      setError(err.message);
    }
  }

  function applySearch(event) {
    event.preventDefault();
    setPage(1);
    setQuery(search.trim());
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <h1 className="h4 mb-3">Customers</h1>

      <form onSubmit={applySearch} className="d-flex gap-2 mb-3">
        <input className="form-control form-control-sm" placeholder="Email or name" value={search} onChange={(e) => setSearch(e.target.value)} />
        <button className="btn btn-outline-dark btn-sm" type="submit">Search</button>
      </form>

      {error && <div className="alert alert-danger">{error}</div>}
      {!result && !error && <p>Loading...</p>}

      {result && (
        <>
          <div className="table-responsive">
            <table className="table table-sm align-middle">
              <thead>
                <tr><th>Email</th><th>Name</th><th className="text-end">Orders</th><th>Joined</th><th>Status</th><th /></tr>
              </thead>
              <tbody>
                {result.items.map((c) => (
                  <tr key={c.id}>
                    <td>{c.email}</td>
                    <td>{c.firstName} {c.lastName}</td>
                    <td className="text-end">{c.orderCount}</td>
                    <td>{formatDateTime(c.createdAt)}</td>
                    <td>{c.isActive ? "Active" : "Deactivated"}</td>
                    <td className="text-end">
                      <button className={`btn btn-sm ${c.isActive ? "btn-outline-danger" : "btn-outline-success"}`} onClick={() => toggle(c)}>
                        {c.isActive ? "Deactivate" : "Activate"}
                      </button>
                    </td>
                  </tr>
                ))}
                {result.items.length === 0 && <tr><td colSpan={6} className="text-muted">No customers match.</td></tr>}
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
