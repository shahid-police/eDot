"use client";

import { useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

const PAGE_SIZE = 25;
const STATUSES = ["Active", "Inactive"];

function emptyForm(promoterId) {
  return {
    promoterId: promoterId ? String(promoterId) : "",
    grantsCustomerDiscount: false,
    customerDiscountPercentage: "",
    grantsCommission: true,
    commissionPercentageOverride: "",
    minimumOrderAmount: "",
    maximumDiscountAmount: "",
    maximumTotalUses: "",
    maximumUsesPerCustomer: "",
    startDate: new Date().toISOString().slice(0, 10),
    expiryDate: "",
    productIds: [],
    categoryIds: [],
  };
}

function toNumberOrNull(value) {
  return value === "" || value === null || value === undefined ? null : Number(value);
}

export default function AdminPromoCodesPage() {
  const searchParams = useSearchParams();
  const initialPromoterId = searchParams.get("promoterId");

  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [reload, setReload] = useState(0);

  const [promoters, setPromoters] = useState([]);
  const [categories, setCategories] = useState([]);
  const [products, setProducts] = useState([]);

  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(emptyForm(initialPromoterId));
  const [formError, setFormError] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    adminRequest("/promoters", { query: { pageSize: 200 } }).then((d) => setPromoters(d.items)).catch(() => {});
    adminRequest("/categories").then(setCategories).catch(() => {});
    adminRequest("/products", { query: { pageSize: 200 } }).then((d) => setProducts(d.items)).catch(() => {});
  }, []);

  useEffect(() => {
    let active = true;
    adminRequest("/promo-codes", { query: { search: query, status, promoterId: initialPromoterId, page, pageSize: PAGE_SIZE } })
      .then((data) => active && setResult(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query, status, page, reload, initialPromoterId]);

  function applySearch(event) {
    event.preventDefault();
    setPage(1);
    setQuery(search.trim());
  }

  function startCreate() {
    setEditingId(null);
    setForm(emptyForm(initialPromoterId));
    setFormError("");
    setShowForm(true);
  }

  function startEdit(c) {
    setEditingId(c.id);
    setForm({
      promoterId: String(c.promoterId),
      grantsCustomerDiscount: c.grantsCustomerDiscount,
      customerDiscountPercentage: String(c.customerDiscountPercentage),
      grantsCommission: c.grantsCommission,
      commissionPercentageOverride: c.commissionPercentageOverride == null ? "" : String(c.commissionPercentageOverride),
      minimumOrderAmount: c.minimumOrderAmount == null ? "" : String(c.minimumOrderAmount),
      maximumDiscountAmount: c.maximumDiscountAmount == null ? "" : String(c.maximumDiscountAmount),
      maximumTotalUses: c.maximumTotalUses == null ? "" : String(c.maximumTotalUses),
      maximumUsesPerCustomer: c.maximumUsesPerCustomer == null ? "" : String(c.maximumUsesPerCustomer),
      startDate: c.startDate.slice(0, 10),
      expiryDate: c.expiryDate ? c.expiryDate.slice(0, 10) : "",
      productIds: c.productIds.map(String),
      categoryIds: c.categoryIds.map(String),
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
        promoterId: Number(form.promoterId),
        grantsCustomerDiscount: form.grantsCustomerDiscount,
        customerDiscountPercentage: toNumberOrNull(form.customerDiscountPercentage) ?? 0,
        grantsCommission: form.grantsCommission,
        commissionPercentageOverride: toNumberOrNull(form.commissionPercentageOverride),
        minimumOrderAmount: toNumberOrNull(form.minimumOrderAmount),
        maximumDiscountAmount: toNumberOrNull(form.maximumDiscountAmount),
        maximumTotalUses: toNumberOrNull(form.maximumTotalUses),
        maximumUsesPerCustomer: toNumberOrNull(form.maximumUsesPerCustomer),
        startDate: new Date(form.startDate).toISOString(),
        expiryDate: form.expiryDate ? new Date(form.expiryDate).toISOString() : null,
        productIds: form.productIds.map(Number),
        categoryIds: form.categoryIds.map(Number),
      };
      if (editingId) {
        await adminRequest(`/promo-codes/${editingId}`, { method: "PUT", body: payload });
      } else {
        await adminRequest("/promo-codes", { method: "POST", body: payload });
      }
      setShowForm(false);
      setReload((n) => n + 1);
    } catch (err) {
      setFormError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function changeStatus(code, newStatus) {
    setError("");
    try {
      await adminRequest(`/promo-codes/${code.id}/status`, { method: "PUT", body: { status: newStatus } });
      setReload((n) => n + 1);
    } catch (err) {
      setError(err.message);
    }
  }

  async function regenerate(code) {
    setError("");
    try {
      await adminRequest(`/promo-codes/${code.id}/regenerate`, { method: "POST" });
      setReload((n) => n + 1);
    } catch (err) {
      setError(err.message);
    }
  }

  function toggleMulti(field, value) {
    setForm((f) => {
      const set = new Set(f[field]);
      if (set.has(value)) set.delete(value); else set.add(value);
      return { ...f, [field]: Array.from(set) };
    });
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <div className="d-flex align-items-center justify-content-between mb-3">
        <h1 className="h4 mb-0">Promo Codes{initialPromoterId ? " - filtered by promoter" : ""}</h1>
        <button type="button" className="btn btn-dark btn-sm" onClick={startCreate}>Create promo code</button>
      </div>

      {showForm && (
        <form onSubmit={submit} className="border rounded p-3 mb-4">
          <h2 className="h6 mb-3">{editingId ? "Edit promo code" : "Create promo code"}</h2>
          {formError && <div className="alert alert-danger py-2">{formError}</div>}

          <div className="row g-2 mb-2">
            <div className="col-md-4">
              <label className="form-label small">Promoter</label>
              <select className="form-select form-select-sm" value={form.promoterId} onChange={(e) => setForm((f) => ({ ...f, promoterId: e.target.value }))} required>
                <option value="">Choose...</option>
                {promoters.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
              </select>
            </div>
            <div className="col-md-4">
              <label className="form-label small">Start date</label>
              <input type="date" className="form-control form-control-sm" value={form.startDate} onChange={(e) => setForm((f) => ({ ...f, startDate: e.target.value }))} required />
            </div>
            <div className="col-md-4">
              <label className="form-label small">Expiry date (optional)</label>
              <input type="date" className="form-control form-control-sm" value={form.expiryDate} onChange={(e) => setForm((f) => ({ ...f, expiryDate: e.target.value }))} />
            </div>
          </div>

          <div className="row g-2 mb-2 align-items-end">
            <div className="col-md-2">
              <div className="form-check">
                <input id="grantsDiscount" type="checkbox" className="form-check-input" checked={form.grantsCustomerDiscount} onChange={(e) => setForm((f) => ({ ...f, grantsCustomerDiscount: e.target.checked }))} />
                <label htmlFor="grantsDiscount" className="form-check-label small">Customer discount</label>
              </div>
            </div>
            <div className="col-md-2">
              <label className="form-label small">Discount %</label>
              <input type="number" min="0" max="100" step="0.01" className="form-control form-control-sm" value={form.customerDiscountPercentage} onChange={(e) => setForm((f) => ({ ...f, customerDiscountPercentage: e.target.value }))} disabled={!form.grantsCustomerDiscount} />
            </div>
            <div className="col-md-2">
              <div className="form-check">
                <input id="grantsCommission" type="checkbox" className="form-check-input" checked={form.grantsCommission} onChange={(e) => setForm((f) => ({ ...f, grantsCommission: e.target.checked }))} />
                <label htmlFor="grantsCommission" className="form-check-label small">Commission</label>
              </div>
            </div>
            <div className="col-md-3">
              <label className="form-label small">Commission % override (optional)</label>
              <input type="number" min="0" max="100" step="0.01" className="form-control form-control-sm" value={form.commissionPercentageOverride} onChange={(e) => setForm((f) => ({ ...f, commissionPercentageOverride: e.target.value }))} disabled={!form.grantsCommission} placeholder="Uses promoter's rate" />
            </div>
          </div>

          <div className="row g-2 mb-2">
            <div className="col-md-3">
              <label className="form-label small">Minimum order (optional)</label>
              <input type="number" min="0" step="0.01" className="form-control form-control-sm" value={form.minimumOrderAmount} onChange={(e) => setForm((f) => ({ ...f, minimumOrderAmount: e.target.value }))} />
            </div>
            <div className="col-md-3">
              <label className="form-label small">Max discount amount (optional)</label>
              <input type="number" min="0" step="0.01" className="form-control form-control-sm" value={form.maximumDiscountAmount} onChange={(e) => setForm((f) => ({ ...f, maximumDiscountAmount: e.target.value }))} />
            </div>
            <div className="col-md-3">
              <label className="form-label small">Max total uses (optional)</label>
              <input type="number" min="1" className="form-control form-control-sm" value={form.maximumTotalUses} onChange={(e) => setForm((f) => ({ ...f, maximumTotalUses: e.target.value }))} placeholder="Unlimited" />
            </div>
            <div className="col-md-3">
              <label className="form-label small">Max uses per customer (optional)</label>
              <input type="number" min="1" className="form-control form-control-sm" value={form.maximumUsesPerCustomer} onChange={(e) => setForm((f) => ({ ...f, maximumUsesPerCustomer: e.target.value }))} placeholder="Unlimited" />
            </div>
          </div>

          <div className="row g-2 mb-3">
            <div className="col-md-6">
              <label className="form-label small">Restrict to products (optional, blank = all)</label>
              <select multiple className="form-select form-select-sm" style={{ height: 120 }} value={form.productIds} onChange={() => {}}>
                {products.map((p) => (
                  <option key={p.id} value={p.id} onClick={() => toggleMulti("productIds", String(p.id))} style={{ fontWeight: form.productIds.includes(String(p.id)) ? "bold" : "normal" }}>
                    {p.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-md-6">
              <label className="form-label small">Restrict to categories (optional, blank = all)</label>
              <select multiple className="form-select form-select-sm" style={{ height: 120 }} value={form.categoryIds} onChange={() => {}}>
                {categories.map((c) => (
                  <option key={c.id} value={c.id} onClick={() => toggleMulti("categoryIds", String(c.id))} style={{ fontWeight: form.categoryIds.includes(String(c.id)) ? "bold" : "normal" }}>
                    {c.name}
                  </option>
                ))}
              </select>
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
          <input className="form-control form-control-sm" placeholder="Code or promoter name" value={search} onChange={(e) => setSearch(e.target.value)} />
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
                  <th>Code</th>
                  <th>Promoter</th>
                  <th>Discount</th>
                  <th>Commission</th>
                  <th className="text-end">Uses</th>
                  <th>Start</th>
                  <th>Expiry</th>
                  <th>Status</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {result.items.map((c) => (
                  <tr key={c.id}>
                    <td className="font-monospace">{c.code}</td>
                    <td>{c.promoterName}</td>
                    <td>{c.grantsCustomerDiscount ? `${c.customerDiscountPercentage}%` : "-"}</td>
                    <td>{c.grantsCommission ? (c.commissionPercentageOverride != null ? `${c.commissionPercentageOverride}% (override)` : "promoter rate") : "-"}</td>
                    <td className="text-end">{c.usedCount}{c.maximumTotalUses != null ? ` / ${c.maximumTotalUses}` : ""}</td>
                    <td>{formatDateTime(c.startDate)}</td>
                    <td>{c.expiryDate ? formatDateTime(c.expiryDate) : "Never"}</td>
                    <td>{c.effectiveStatus}</td>
                    <td className="text-end">
                      <div className="d-flex gap-1 justify-content-end">
                        <button type="button" className="btn btn-outline-dark btn-sm" onClick={() => startEdit(c)}>Edit</button>
                        <button type="button" className="btn btn-outline-secondary btn-sm" onClick={() => regenerate(c)}>Regenerate</button>
                        <button type="button" className="btn btn-outline-secondary btn-sm" onClick={() => changeStatus(c, c.status === "Active" ? "Inactive" : "Active")}>
                          {c.status === "Active" ? "Deactivate" : "Activate"}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {result.items.length === 0 && <tr><td colSpan={9} className="text-muted">No promo codes match.</td></tr>}
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
