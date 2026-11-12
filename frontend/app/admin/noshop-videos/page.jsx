"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { adminRequest } from "@/lib/admin/api";

const PAGE_SIZE = 25;
const STATUSES = ["Draft", "Published", "Unpublished"];

export default function AdminNoShopVideosPage() {
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [categories, setCategories] = useState([]);
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [actionError, setActionError] = useState("");

  useEffect(() => {
    adminRequest("/noshop/categories").then(setCategories).catch(() => {});
  }, []);

  useEffect(() => {
    let active = true;
    adminRequest("/noshop/videos", { query: { search: query, status, categoryId, page, pageSize: PAGE_SIZE } })
      .then((data) => active && setResult(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [query, status, categoryId, page]);

  function applySearch(event) {
    event.preventDefault();
    setPage(1);
    setQuery(search.trim());
  }

  function reload() {
    adminRequest("/noshop/videos", { query: { search: query, status, categoryId, page, pageSize: PAGE_SIZE } })
      .then(setResult)
      .catch((err) => setActionError(err.message));
  }

  async function toggleStatus(video, nextStatus) {
    setActionError("");
    try {
      await adminRequest(`/noshop/videos/${video.id}/status`, { method: "PATCH", body: { status: nextStatus } });
      reload();
    } catch (err) {
      setActionError(err.message);
    }
  }

  async function deleteVideo(video) {
    if (!window.confirm(`Delete "${video.title}"? This cannot be undone.`)) return;
    setActionError("");
    try {
      await adminRequest(`/noshop/videos/${video.id}`, { method: "DELETE" });
      reload();
    } catch (err) {
      setActionError(err.message);
    }
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <h1 className="h4 mb-0">No Shop videos</h1>
        <Link href="/admin/noshop-videos/new" className="btn btn-dark btn-sm">New video</Link>
      </div>

      <form onSubmit={applySearch} className="d-flex flex-wrap gap-2 mb-3">
        <input className="form-control form-control-sm" style={{ maxWidth: 260 }} placeholder="Search title or slug" value={search} onChange={(e) => setSearch(e.target.value)} />
        <select className="form-select form-select-sm" style={{ maxWidth: 160 }} value={status} onChange={(e) => { setPage(1); setStatus(e.target.value); }}>
          <option value="">All statuses</option>
          {STATUSES.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
        <select className="form-select form-select-sm" style={{ maxWidth: 200 }} value={categoryId} onChange={(e) => { setPage(1); setCategoryId(e.target.value); }}>
          <option value="">All categories</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>
        <button className="btn btn-outline-dark btn-sm" type="submit">Search</button>
      </form>

      {error && <div className="alert alert-danger">{error}</div>}
      {actionError && <div className="alert alert-danger py-2">{actionError}</div>}
      {!result && !error && <p>Loading...</p>}

      {result && (
        <>
          <div className="table-responsive">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>Thumbnail</th>
                  <th>Title</th>
                  <th>Category</th>
                  <th>Type</th>
                  <th>Featured</th>
                  <th>Status</th>
                  <th className="text-end">Order</th>
                  <th>Created</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {result.items.map((v) => (
                  <tr key={v.id}>
                    <td><img src={v.thumbnailUrl} alt="" width={64} className="rounded" /></td>
                    <td><Link href={`/admin/noshop-videos/${v.id}`}>{v.title}</Link></td>
                    <td>{v.categoryName}</td>
                    <td>{v.videoType}</td>
                    <td>{v.isFeatured ? "Yes" : "No"}</td>
                    <td>{v.status}</td>
                    <td className="text-end">{v.displayOrder}</td>
                    <td>{new Date(v.createdAt).toLocaleDateString()}</td>
                    <td className="text-end">
                      <div className="btn-group btn-group-sm">
                        {v.status !== "Published" && (
                          <button type="button" className="btn btn-outline-success" onClick={() => toggleStatus(v, "Published")}>Publish</button>
                        )}
                        {v.status === "Published" && (
                          <button type="button" className="btn btn-outline-secondary" onClick={() => toggleStatus(v, "Unpublished")}>Unpublish</button>
                        )}
                        <button type="button" className="btn btn-outline-danger" onClick={() => deleteVideo(v)}>Delete</button>
                      </div>
                    </td>
                  </tr>
                ))}
                {result.items.length === 0 && (
                  <tr><td colSpan={9} className="text-muted">No videos found.</td></tr>
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
