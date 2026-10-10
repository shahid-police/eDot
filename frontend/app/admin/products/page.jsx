"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { adminRequest } from "@/lib/admin/api";
import { formatPrice } from "@/lib/format/price";

const PAGE_SIZE = 25;

export default function AdminProductsPage() {
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    adminRequest("/products", { query: { search: query, page, pageSize: PAGE_SIZE } })
      .then((data) => active && setResult(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [query, page]);

  function applySearch(event) {
    event.preventDefault();
    setPage(1);
    setQuery(search.trim());
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <h1 className="h4 mb-0">Products</h1>
        <Link href="/admin/products/new" className="btn btn-dark btn-sm">New product</Link>
      </div>

      <form onSubmit={applySearch} className="d-flex gap-2 mb-3">
        <input className="form-control form-control-sm" placeholder="Search name, SKU or slug" value={search} onChange={(e) => setSearch(e.target.value)} />
        <button className="btn btn-outline-dark btn-sm" type="submit">Search</button>
      </form>

      {error && <div className="alert alert-danger">{error}</div>}
      {!result && !error && <p>Loading...</p>}

      {result && (
        <>
          <div className="table-responsive">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>SKU</th>
                  <th>Name</th>
                  <th>Category</th>
                  <th>Type</th>
                  <th className="text-end">Price</th>
                  <th className="text-end">Stock</th>
                  <th>Active</th>
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
                  </tr>
                ))}
                {result.items.length === 0 && (
                  <tr><td colSpan={7} className="text-muted">No products found.</td></tr>
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
