"use client";

import { useEffect, useState } from "react";
import { adminRequest } from "@/lib/admin/api";

const EMPTY_CATEGORY = { name: "", slug: "", description: "", sortOrder: 0, isActive: true };

function slugify(value) {
  return value.toLowerCase().trim().replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
}

export default function AdminNoShopCategoriesPage() {
  const [categories, setCategories] = useState(null);
  const [error, setError] = useState("");
  const [formError, setFormError] = useState("");
  const [busy, setBusy] = useState(false);
  const [editingId, setEditingId] = useState(null); // null = creating a new one
  const [form, setForm] = useState(EMPTY_CATEGORY);
  const [slugTouched, setSlugTouched] = useState(false);

  function load() {
    adminRequest("/noshop/categories").then(setCategories).catch((err) => setError(err.message));
  }

  useEffect(load, []);

  function update(field, value) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function updateName(value) {
    setForm((current) => ({
      ...current,
      name: value,
      slug: slugTouched ? current.slug : slugify(value),
    }));
  }

  function startEdit(category) {
    setEditingId(category.id);
    setSlugTouched(true);
    setFormError("");
    setForm({
      name: category.name,
      slug: category.slug,
      description: category.description ?? "",
      sortOrder: category.sortOrder,
      isActive: category.isActive,
    });
  }

  function startCreate() {
    setEditingId(null);
    setSlugTouched(false);
    setFormError("");
    setForm(EMPTY_CATEGORY);
  }

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setFormError("");
    try {
      const payload = {
        name: form.name.trim(),
        slug: form.slug.trim(),
        description: form.description || null,
        sortOrder: Number(form.sortOrder) || 0,
        isActive: form.isActive,
      };
      if (editingId === null) {
        await adminRequest("/noshop/categories", { method: "POST", body: payload });
      } else {
        await adminRequest(`/noshop/categories/${editingId}`, { method: "PUT", body: payload });
      }
      startCreate();
      load();
    } catch (err) {
      setFormError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="row g-4">
      <div className="col-lg-7">
        <h1 className="h4 mb-3">No Shop categories</h1>
        {error && <div className="alert alert-danger">{error}</div>}
        {!categories && !error && <p>Loading...</p>}
        {categories && (
          <div className="table-responsive">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Slug</th>
                  <th className="text-end">Videos</th>
                  <th className="text-end">Order</th>
                  <th>Active</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {categories.map((c) => (
                  <tr key={c.id}>
                    <td>{c.name}</td>
                    <td>{c.slug}</td>
                    <td className="text-end">{c.videoCount}</td>
                    <td className="text-end">{c.sortOrder}</td>
                    <td>{c.isActive ? "Yes" : "No"}</td>
                    <td className="text-end">
                      <button type="button" className="btn btn-outline-dark btn-sm" onClick={() => startEdit(c)}>Edit</button>
                    </td>
                  </tr>
                ))}
                {categories.length === 0 && (
                  <tr><td colSpan={6} className="text-muted">No categories yet.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div className="col-lg-5">
        <div className="d-flex align-items-center justify-content-between mb-2">
          <h2 className="h6 mb-0">{editingId === null ? "New category" : "Edit category"}</h2>
          {editingId !== null && (
            <button type="button" className="btn btn-link btn-sm p-0" onClick={startCreate}>Cancel edit</button>
          )}
        </div>
        <form onSubmit={submit} className="vstack gap-3 border rounded p-3">
          {formError && <div className="alert alert-danger py-2 mb-0">{formError}</div>}
          <div>
            <label className="form-label">Name</label>
            <input className="form-control" value={form.name} onChange={(e) => updateName(e.target.value)} required maxLength={200} />
          </div>
          <div>
            <label className="form-label">Slug</label>
            <input
              className="form-control"
              value={form.slug}
              onChange={(e) => { setSlugTouched(true); update("slug", e.target.value); }}
              required
              maxLength={220}
            />
          </div>
          <div>
            <label className="form-label">Description</label>
            <textarea className="form-control" rows={2} value={form.description} onChange={(e) => update("description", e.target.value)} maxLength={500} />
          </div>
          <div className="row g-2">
            <div className="col-6">
              <label className="form-label">Sort order</label>
              <input type="number" className="form-control" value={form.sortOrder} onChange={(e) => update("sortOrder", e.target.value)} />
            </div>
            <div className="col-6 d-flex align-items-end">
              <div className="form-check mb-2">
                <input id="isActive" type="checkbox" className="form-check-input" checked={form.isActive} onChange={(e) => update("isActive", e.target.checked)} />
                <label htmlFor="isActive" className="form-check-label">Active</label>
              </div>
            </div>
          </div>
          <div>
            <button type="submit" className="btn btn-dark" disabled={busy}>
              {busy ? "Saving..." : editingId === null ? "Create category" : "Save changes"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
