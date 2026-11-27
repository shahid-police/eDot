"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { adminRequest } from "@/lib/admin/api";
import { formatPrice } from "@/lib/format/price";

const PAGE_SIZE = 25;

const PREFERENCE_BADGE_STYLES = {
  Budget: { background: "#e6f4ea", color: "#1e7e34" },
  Premium: { background: "#e3eefc", color: "#1a5fb4" },
  Luxury: { background: "#f8ecd4", color: "#8a6d1d" },
};

function ShoppingPreferenceBadge({ value }) {
  const style = value ? PREFERENCE_BADGE_STYLES[value] : { background: "#f1f1f1", color: "#777" };
  return (
    <span
      className="badge rounded-pill"
      style={{ ...style, fontWeight: 500 }}
    >
      {value || "Unassigned"}
    </span>
  );
}

export default function AdminProductsPage() {
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [productType, setProductType] = useState("");
  const [isActive, setIsActive] = useState("");
  const [shoppingPreference, setShoppingPreference] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [categories, setCategories] = useState([]);
  const [error, setError] = useState("");
  const [sortBy, setSortBy] = useState("");
  const [sortDescending, setSortDescending] = useState(false);

  useEffect(() => {
    adminRequest("/categories").then(setCategories).catch(() => {});
  }, []);

  useEffect(() => {
    let active = true;
    adminRequest("/products", {
      query: { search: query, categoryId, productType, isActive, shoppingPreference, sortBy, sortDescending, page, pageSize: PAGE_SIZE },
    })
      .then((data) => active && setResult(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [query, categoryId, productType, isActive, shoppingPreference, sortBy, sortDescending, page]);

  // Clicking the active column flips direction; clicking a different column starts ascending.
  function toggleSort(column) {
    setPage(1);
    if (sortBy === column) {
      setSortDescending((d) => !d);
    } else {
      setSortBy(column);
      setSortDescending(false);
    }
  }

  function SortableHeader({ column, children, className = "" }) {
    const active = sortBy === column;
    const arrow = active ? (sortDescending ? " ↓" : " ↑") : "";
    return (
      <th className={className} role="button" onClick={() => toggleSort(column)} style={{ cursor: "pointer", userSelect: "none" }}>
        {children}{arrow}
      </th>
    );
  }

  function applySearch(event) {
    event.preventDefault();
    setPage(1);
    setQuery(search.trim());
  }

  function resetFilterPage(setter) {
    return (value) => {
      setPage(1);
      setter(value);
    };
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <h1 className="h4 mb-0">Products</h1>
        <Link href="/admin/products/new" className="btn btn-dark btn-sm">New product</Link>
      </div>

      <form onSubmit={applySearch} className="d-flex flex-wrap gap-2 mb-3">
        <input
          className="form-control form-control-sm"
          style={{ maxWidth: 320 }}
          placeholder="Search name, SKU or slug"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <button className="btn btn-outline-dark btn-sm" type="submit">Search</button>

        <select
          className="form-select form-select-sm w-auto"
          value={categoryId}
          onChange={(e) => resetFilterPage(setCategoryId)(e.target.value)}
        >
          <option value="">All categories</option>
          {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>

        <select
          className="form-select form-select-sm w-auto"
          value={productType}
          onChange={(e) => resetFilterPage(setProductType)(e.target.value)}
        >
          <option value="">All types</option>
          <option value="Simple">Simple</option>
          <option value="Variant">Variant</option>
        </select>

        <select
          className="form-select form-select-sm w-auto"
          value={isActive}
          onChange={(e) => resetFilterPage(setIsActive)(e.target.value)}
        >
          <option value="">Active + Inactive</option>
          <option value="true">Active only</option>
          <option value="false">Inactive only</option>
        </select>

        <select
          className="form-select form-select-sm w-auto"
          value={shoppingPreference}
          onChange={(e) => resetFilterPage(setShoppingPreference)(e.target.value)}
        >
          <option value="">All preferences</option>
          <option value="Budget">Budget</option>
          <option value="Premium">Premium</option>
          <option value="Luxury">Luxury</option>
          <option value="Unassigned">Unassigned</option>
        </select>

        {(categoryId || productType || isActive || shoppingPreference || query || sortBy) && (
          <button
            type="button"
            className="btn btn-outline-secondary btn-sm"
            onClick={() => {
              setSearch(""); setQuery(""); setCategoryId(""); setProductType(""); setIsActive("");
              setShoppingPreference(""); setSortBy(""); setSortDescending(false); setPage(1);
            }}
          >
            Clear filters
          </button>
        )}
      </form>

      {error && <div className="alert alert-danger">{error}</div>}
      {!result && !error && <p>Loading...</p>}

      {result && (
        <>
          <div className="table-responsive">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <SortableHeader column="sku">SKU</SortableHeader>
                  <SortableHeader column="name">Name</SortableHeader>
                  <SortableHeader column="category">Category</SortableHeader>
                  <SortableHeader column="type">Type</SortableHeader>
                  <SortableHeader column="price" className="text-end">Price</SortableHeader>
                  <SortableHeader column="stock" className="text-end">Stock</SortableHeader>
                  <SortableHeader column="active">Active</SortableHeader>
                  <th>Preference</th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((p) => (
                  <tr key={p.id}>
                    <td><Link href={`/admin/products/${p.id}`}>{p.sku}</Link></td>
                    <td>{p.name}</td>
                    <td>{p.categoryName}</td>
                    <td>{p.productType}</td>
                    <td className="text-end">{formatPrice(p.salePrice ?? p.price)}</td>
                    <td className="text-end">{p.productType === "Variant" ? "per option" : p.stockQuantity}</td>
                    <td>{p.isActive ? "Yes" : "No"}</td>
                    <td><ShoppingPreferenceBadge value={p.shoppingPreference} /></td>
                  </tr>
                ))}
                {result.items.length === 0 && (
                  <tr><td colSpan={8} className="text-muted">No products found.</td></tr>
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
