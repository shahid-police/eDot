"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

const PAGE_SIZE = 25;
const STATUSES = ["Active", "Suspended", "Deactivated"];

export default function AdminVendorsPage() {
  const [status, setStatus] = useState("");
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [showCreate, setShowCreate] = useState(false);

  function load() {
    adminRequest("/vendors", { query: { status, search: query, page, pageSize: PAGE_SIZE } })
      .then(setResult)
      .catch((err) => setError(err.message));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status, query, page]);

  function applySearch(event) {
    event.preventDefault();
    setPage(1);
    setQuery(search.trim());
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h1 className="h4 mb-0">Vendors</h1>
        <button className="btn btn-dark btn-sm" onClick={() => setShowCreate(true)}>Create vendor</button>
      </div>

      {showCreate && (
        <CreateVendorForm onClose={() => setShowCreate(false)} onCreated={() => { setShowCreate(false); load(); }} />
      )}

      <div className="d-flex flex-wrap gap-2 mb-3">
        <select className="form-select form-select-sm w-auto" value={status} onChange={(e) => { setPage(1); setStatus(e.target.value); }}>
          <option value="">All statuses</option>
          {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
        <form onSubmit={applySearch} className="d-flex gap-2">
          <input className="form-control form-control-sm" placeholder="Shop name, email or code" value={search} onChange={(e) => setSearch(e.target.value)} />
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
                  <th>Shop</th>
                  <th>Code</th>
                  <th>Email</th>
                  <th>Status</th>
                  <th>Submissions</th>
                  <th>Pending</th>
                  <th>Created</th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((v) => (
                  <tr key={v.id}>
                    <td><Link href={`/admin/vendors/${v.id}`}>{v.shopName}</Link></td>
                    <td>{v.vendorCode}</td>
                    <td>{v.email}</td>
                    <td>{v.status}</td>
                    <td>{v.submissionCount}</td>
                    <td>{v.pendingSubmissionCount}</td>
                    <td>{formatDateTime(v.createdAt)}</td>
                  </tr>
                ))}
                {result.items.length === 0 && (
                  <tr><td colSpan={7} className="text-muted">No vendors yet.</td></tr>
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

function CreateVendorForm({ onClose, onCreated }) {
  const [form, setForm] = useState({ email: "", shopName: "", contactPerson: "", phone: "", address: "", city: "" });
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState(null);

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      const created = await adminRequest("/vendors", { method: "POST", body: form });
      setResult(created);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  if (result) {
    return (
      <div className="alert alert-success">
        <p className="mb-2">Vendor <strong>{result.vendor.shopName}</strong> ({result.vendor.vendorCode}) created.</p>
        <p className="mb-2">
          Temporary password (communicate this to the vendor securely - it will not be shown again):
          {" "}<code>{result.temporaryPassword}</code>
        </p>
        <button className="btn btn-sm btn-dark" onClick={onCreated}>Done</button>
      </div>
    );
  }

  return (
    <form onSubmit={submit} className="border rounded p-3 mb-3 vstack gap-2" style={{ maxWidth: 480 }}>
      <div className="d-flex justify-content-between">
        <strong>Create vendor</strong>
        <button type="button" className="btn-close" aria-label="Close" onClick={onClose} />
      </div>
      <input className="form-control form-control-sm" placeholder="Email" type="email"
        value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required />
      <input className="form-control form-control-sm" placeholder="Shop name"
        value={form.shopName} onChange={(e) => setForm({ ...form, shopName: e.target.value })} required />
      <input className="form-control form-control-sm" placeholder="Contact person"
        value={form.contactPerson} onChange={(e) => setForm({ ...form, contactPerson: e.target.value })} required />
      <input className="form-control form-control-sm" placeholder="Phone"
        value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} required />
      <input className="form-control form-control-sm" placeholder="Address (optional)"
        value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
      <input className="form-control form-control-sm" placeholder="City (optional)"
        value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} />
      {error && <div className="alert alert-danger py-2 mb-0">{error}</div>}
      <button type="submit" className="btn btn-dark btn-sm" disabled={busy}>{busy ? "Creating..." : "Create vendor"}</button>
    </form>
  );
}
