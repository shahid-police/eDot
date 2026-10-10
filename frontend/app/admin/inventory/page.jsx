"use client";

import { useCallback, useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

export default function AdminInventoryPage() {
  const [lowStock, setLowStock] = useState(null);
  const [movements, setMovements] = useState(null);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [productId, setProductId] = useState("");
  const [variantId, setVariantId] = useState("");
  const [delta, setDelta] = useState("");
  const [note, setNote] = useState("");

  const load = useCallback(() => {
    adminRequest("/inventory/low-stock").then(setLowStock).catch((err) => setError(err.message));
    adminRequest("/inventory/movements", { query: { pageSize: 25 } }).then(setMovements).catch((err) => setError(err.message));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function adjust(event) {
    event.preventDefault();
    setError("");
    setNotice("");
    try {
      await adminRequest("/inventory/adjustments", {
        method: "POST",
        body: {
          productId: Number(productId),
          variantId: variantId ? Number(variantId) : null,
          quantityDelta: Number(delta),
          note,
        },
      });
      setNotice("Stock adjusted.");
      setDelta("");
      setNote("");
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div className="vstack gap-4">
      <h1 className="h4 mb-0">Inventory</h1>

      {error && <div className="alert alert-danger py-2 mb-0">{error}</div>}
      {notice && <div className="alert alert-success py-2 mb-0">{notice}</div>}

      <form onSubmit={adjust} className="row g-2 align-items-end">
        <div className="col-6 col-md-2">
          <label className="form-label small">Product ID</label>
          <input type="number" min="1" className="form-control form-control-sm" value={productId} onChange={(e) => setProductId(e.target.value)} required />
        </div>
        <div className="col-6 col-md-2">
          <label className="form-label small">Option ID (optional)</label>
          <input type="number" min="1" className="form-control form-control-sm" value={variantId} onChange={(e) => setVariantId(e.target.value)} />
        </div>
        <div className="col-6 col-md-2">
          <label className="form-label small">Change (+/-)</label>
          <input type="number" className="form-control form-control-sm" value={delta} onChange={(e) => setDelta(e.target.value)} required />
        </div>
        <div className="col-6 col-md-4">
          <label className="form-label small">Reason</label>
          <input className="form-control form-control-sm" value={note} onChange={(e) => setNote(e.target.value)} required maxLength={500} />
        </div>
        <div className="col-12 col-md-2">
          <button type="submit" className="btn btn-dark btn-sm w-100">Adjust stock</button>
        </div>
      </form>

      <section>
        <h2 className="h6">Low stock</h2>
        {!lowStock ? <p>Loading...</p> : (
          <table className="table table-sm">
            <thead>
              <tr><th>SKU</th><th>Item</th><th className="text-end">Stock</th><th className="text-end">Threshold</th><th>IDs</th></tr>
            </thead>
            <tbody>
              {lowStock.map((item) => (
                <tr key={`${item.productId}-${item.variantId ?? "p"}`}>
                  <td>{item.sku}</td>
                  <td>{item.name}</td>
                  <td className="text-end">{item.stockQuantity}</td>
                  <td className="text-end">{item.threshold}</td>
                  <td className="small text-muted">product {item.productId}{item.variantId ? `, option ${item.variantId}` : ""}</td>
                </tr>
              ))}
              {lowStock.length === 0 && <tr><td colSpan={5} className="text-muted">Nothing is low on stock.</td></tr>}
            </tbody>
          </table>
        )}
      </section>

      <section>
        <h2 className="h6">Recent movements</h2>
        {!movements ? <p>Loading...</p> : (
          <table className="table table-sm">
            <thead>
              <tr><th>When</th><th>Product</th><th>Option</th><th>Change</th><th>Reason</th><th>Order</th><th>By</th><th>Note</th></tr>
            </thead>
            <tbody>
              {movements.items.map((m) => (
                <tr key={m.id}>
                  <td>{formatDateTime(m.createdAt)}</td>
                  <td>{m.productId}</td>
                  <td>{m.variantId ?? "-"}</td>
                  <td className={m.quantityDelta < 0 ? "text-danger" : "text-success"}>{m.quantityDelta > 0 ? `+${m.quantityDelta}` : m.quantityDelta}</td>
                  <td>{m.reason}</td>
                  <td>{m.orderId ?? "-"}</td>
                  <td>{m.adminEmail ?? "system"}</td>
                  <td className="small">{m.note ?? ""}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  );
}
