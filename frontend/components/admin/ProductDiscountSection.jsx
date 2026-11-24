"use client";

import { useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

function toDateInputValue(date) {
  return date.toISOString().slice(0, 10);
}

const EMPTY_FORM = {
  productVariantId: "",
  discountPercentage: "",
  startDate: toDateInputValue(new Date()),
  endDate: "",
};

export default function ProductDiscountSection({ productId, productType, variants }) {
  const [discounts, setDiscounts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [form, setForm] = useState(EMPTY_FORM);
  const [busy, setBusy] = useState(false);

  const isVariantProduct = productType === "Variant";

  function load() {
    setLoading(true);
    adminRequest(`/products/${productId}/discounts`)
      .then(setDiscounts)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [productId]);

  async function submit() {
    setError("");
    setBusy(true);
    try {
      await adminRequest(`/products/${productId}/discounts`, {
        method: "POST",
        body: {
          productVariantId: isVariantProduct ? Number(form.productVariantId) : null,
          discountPercentage: Number(form.discountPercentage),
          startDate: new Date(form.startDate).toISOString(),
          endDate: new Date(form.endDate).toISOString(),
        },
      });
      setForm(EMPTY_FORM);
      load();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function cancelDiscount(discountId) {
    setError("");
    try {
      await adminRequest(`/products/${productId}/discounts/${discountId}/cancel`, { method: "PATCH" });
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  const activeVariants = (variants ?? []).filter((v) => v.id);

  return (
    <section>
      <h2 className="h6 mb-2">Discount</h2>
      {error && <div className="alert alert-danger py-2 mb-2">{error}</div>}

      {/* A plain div, not a <form> - this section lives inside ProductForm's own outer <form>,
          and nested <form> elements are invalid HTML and trigger a hydration error. */}
      <div className="row g-2 align-items-end mb-3">
        {isVariantProduct && (
          <div className="col-md-3">
            <label className="form-label small">Option</label>
            <select
              className="form-select form-select-sm"
              value={form.productVariantId}
              onChange={(e) => setForm((f) => ({ ...f, productVariantId: e.target.value }))}
              required
            >
              <option value="">Choose an option...</option>
              {activeVariants.map((v) => (
                <option key={v.id} value={v.id}>{v.variantName}</option>
              ))}
            </select>
          </div>
        )}
        <div className="col-md-2">
          <label className="form-label small">Discount %</label>
          <input
            type="number" min="0.01" max="100" step="0.01"
            className="form-control form-control-sm"
            value={form.discountPercentage}
            onChange={(e) => setForm((f) => ({ ...f, discountPercentage: e.target.value }))}
            required
          />
        </div>
        <div className="col-md-3">
          <label className="form-label small">Start date</label>
          <input
            type="date"
            className="form-control form-control-sm"
            value={form.startDate}
            onChange={(e) => setForm((f) => ({ ...f, startDate: e.target.value }))}
            required
          />
        </div>
        <div className="col-md-3">
          <label className="form-label small">End date</label>
          <input
            type="date"
            className="form-control form-control-sm"
            value={form.endDate}
            onChange={(e) => setForm((f) => ({ ...f, endDate: e.target.value }))}
            required
          />
        </div>
        <div className="col-md-auto">
          <button type="button" onClick={submit} className="btn btn-outline-dark btn-sm" disabled={busy || (isVariantProduct && activeVariants.length === 0)}>
            {busy ? "Saving..." : "Add discount"}
          </button>
        </div>
        {isVariantProduct && activeVariants.length === 0 && (
          <div className="col-12"><p className="small text-muted mb-0">Add and save at least one option first.</p></div>
        )}
      </div>

      {loading && <p className="small text-muted">Loading...</p>}

      {!loading && discounts.length > 0 && (
        <div className="table-responsive">
          <table className="table table-sm align-middle">
            <thead>
              <tr>
                <th>Option</th>
                <th className="text-end">Original</th>
                <th className="text-end">%</th>
                <th className="text-end">Final</th>
                <th>Start</th>
                <th>End</th>
                <th>Status</th>
                <th>By</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {discounts.map((d) => (
                <tr key={d.id}>
                  <td>{d.variantName ?? "Whole product"}</td>
                  <td className="text-end">{d.originalPriceSnapshot}</td>
                  <td className="text-end">{d.discountPercentage}%</td>
                  <td className="text-end">{d.finalPriceSnapshot}</td>
                  <td>{formatDateTime(d.startDate)}</td>
                  <td>{formatDateTime(d.endDate)}</td>
                  <td>{d.effectiveStatus}</td>
                  <td>{d.createdByEmail ?? "-"}</td>
                  <td className="text-end">
                    {(d.effectiveStatus === "Active" || d.effectiveStatus === "Scheduled") && (
                      <button type="button" className="btn btn-outline-danger btn-sm" onClick={() => cancelDiscount(d.id)}>
                        Cancel
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {!loading && discounts.length === 0 && <p className="small text-muted mb-0">No discounts yet.</p>}
    </section>
  );
}
