"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

const PAGE_SIZE = 25;
const PROMOTER_TYPES = ["Individual", "SocialMediaInfluencer", "Affiliate", "Other"];
const STATUSES = ["Active", "Inactive", "Suspended"];

const EMPTY_FORM = {
  name: "",
  whatsAppNumber: "+92",
  email: "",
  address: "",
  promoterType: "Individual",
  commissionPercentage: "",
  notes: "",
};

export default function AdminPromotersPage() {
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [reload, setReload] = useState(0);

  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [formError, setFormError] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let active = true;
    adminRequest("/promoters", { query: { search: query, status, page, pageSize: PAGE_SIZE } })
      .then((data) => active && setResult(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [query, status, page, reload]);

  function applySearch(event) {
    event.preventDefault();
    setPage(1);
    setQuery(search.trim());
  }

  function startCreate() {
    setEditingId(null);
    setForm(EMPTY_FORM);
    setFormError("");
    setShowForm(true);
  }

  function startEdit(p) {
    setEditingId(p.id);
    setForm({
      name: p.name,
      whatsAppNumber: p.whatsAppNumber,
      email: p.email ?? "",
      address: p.address ?? "",
      promoterType: p.promoterType,
      commissionPercentage: String(p.commissionPercentage),
      notes: p.notes ?? "",
    });
    setFormError("");
    setShowForm(true);
  }

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setFormError("");
    try {
      const payload = {
        name: form.name.trim(),
        whatsAppNumber: form.whatsAppNumber.trim(),
        email: form.email.trim() || null,
        address: form.address.trim() || null,
        promoterType: form.promoterType,
        commissionPercentage: Number(form.commissionPercentage),
        notes: form.notes.trim() || null,
      };
      if (editingId) {
        await adminRequest(`/promoters/${editingId}`, { method: "PUT", body: payload });
      } else {
        await adminRequest("/promoters", { method: "POST", body: payload });
      }
      setShowForm(false);
      setReload((n) => n + 1);
    } catch (err) {
      setFormError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function changeStatus(promoter, newStatus) {
    setError("");
    try {
      await adminRequest(`/promoters/${promoter.id}/status`, { method: "PUT", body: { status: newStatus } });
      setReload((n) => n + 1);
    } catch (err) {
      setError(err.message);
    }
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <div className="d-flex align-items-center justify-content-between mb-3">
        <h1 className="h4 mb-0">Promoters</h1>
        <button type="button" className="btn btn-dark btn-sm" onClick={startCreate}>Register promoter</button>
      </div>

      {showForm && (
        <form onSubmit={submit} className="border rounded p-3 mb-4">
          <h2 className="h6 mb-3">{editingId ? "Edit promoter" : "Register promoter"}</h2>
          {formError && <div className="alert alert-danger py-2">{formError}</div>}
          <div className="row g-2 mb-2">
            <div className="col-md-4">
              <label className="form-label small">Name</label>
              <input className="form-control form-control-sm" value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} required maxLength={200} />
            </div>
            <div className="col-md-3">
              <label className="form-label small">WhatsApp number</label>
              <input className="form-control form-control-sm" value={form.whatsAppNumber} onChange={(e) => setForm((f) => ({ ...f, whatsAppNumber: e.target.value }))} required maxLength={32} />
            </div>
            <div className="col-md-3">
              <label className="form-label small">Email (optional)</label>
              <input type="email" className="form-control form-control-sm" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} maxLength={254} />
            </div>
            <div className="col-md-2">
              <label className="form-label small">Commission %</label>
              <input type="number" min="0" max="100" step="0.01" className="form-control form-control-sm" value={form.commissionPercentage} onChange={(e) => setForm((f) => ({ ...f, commissionPercentage: e.target.value }))} required />
            </div>
          </div>
          <div className="row g-2 mb-2">
            <div className="col-md-4">
              <label className="form-label small">Address (optional)</label>
              <input className="form-control form-control-sm" value={form.address} onChange={(e) => setForm((f) => ({ ...f, address: e.target.value }))} maxLength={500} />
            </div>
            <div className="col-md-3">
              <label className="form-label small">Promoter type</label>
              <select className="form-select form-select-sm" value={form.promoterType} onChange={(e) => setForm((f) => ({ ...f, promoterType: e.target.value }))}>
                {PROMOTER_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
              </select>
            </div>
            <div className="col-md-5">
              <label className="form-label small">Notes (optional)</label>
              <input className="form-control form-control-sm" value={form.notes} onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))} maxLength={1000} />
            </div>
          </div>
          <div className="d-flex gap-2">
            <button type="submit" className="btn btn-dark btn-sm" disabled={busy}>{busy ? "Saving..." : "Save"}</button>
            <button type="button" className="btn btn-outline-secondary btn-sm" onClick={() => setShowForm(false)}>Cancel</button>
          </div>
        </form>
      )}

      <div className="d-flex flex-wrap gap-2 mb-3">
        <form onSubmit={applySearch} className="d-flex gap-2">
          <input className="form-control form-control-sm" placeholder="Name, number, or email" value={search} onChange={(e) => setSearch(e.target.value)} />
          <button className="btn btn-outline-dark btn-sm" type="submit">Search</button>
        </form>
        <select className="form-select form-select-sm w-auto" value={status} onChange={(e) => { setPage(1); setStatus(e.target.value); }}>
          <option value="">All statuses</option>
          {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
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
                  <th>Name</th>
                  <th>WhatsApp</th>
                  <th>Type</th>
                  <th className="text-end">Commission</th>
                  <th>Status</th>
                  <th className="text-end">Codes</th>
                  <th>Registered</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {result.items.map((p) => (
                  <tr key={p.id}>
                    <td>{p.name}</td>
                    <td>{p.whatsAppNumber}</td>
                    <td>{p.promoterType}</td>
                    <td className="text-end">{p.commissionPercentage}%</td>
                    <td>{p.status}</td>
                    <td className="text-end"><Link href={`/admin/promo-codes?promoterId=${p.id}`}>{p.promoCodeCount}</Link></td>
                    <td>{formatDateTime(p.createdAt)}</td>
                    <td className="text-end">
                      <div className="d-flex gap-1 justify-content-end">
                        <button type="button" className="btn btn-outline-dark btn-sm" onClick={() => startEdit(p)}>Edit</button>
                        {STATUSES.filter((s) => s !== p.status).map((s) => (
                          <button key={s} type="button" className="btn btn-outline-secondary btn-sm" onClick={() => changeStatus(p, s)}>{s}</button>
                        ))}
                      </div>
                    </td>
                  </tr>
                ))}
                {result.items.length === 0 && <tr><td colSpan={8} className="text-muted">No promoters match.</td></tr>}
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
