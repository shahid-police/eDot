"use client";

import { useEffect, useState } from "react";
import { vendorRequest } from "@/lib/vendor/api";

export default function VendorSalesPage() {
  const [data, setData] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    vendorRequest("/orders/analytics").then(setData).catch((err) => setError(err.message));
  }, []);

  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!data) return <p>Loading...</p>;

  return (
    <div>
      <h1 className="h4 mb-3">Sales Overview</h1>
      <p className="text-muted small mb-4">
        Gross figures only (before any returns/cancellations are excluded) - these are not your payable settlement amount.
      </p>

      <div className="row g-3 mb-4">
        {[
          ["Total units sold", data.totalUnitsSold],
          ["Total gross sales value", data.totalGrossSalesValue],
          ["Cancelled units", data.cancelledUnits],
          ["Returned units", data.returnedUnits],
        ].map(([label, value]) => (
          <div className="col-6 col-md-3" key={label}>
            <div className="border rounded p-3 h-100">
              <div className="h4 mb-0">{value}</div>
              <div className="small text-muted">{label}</div>
            </div>
          </div>
        ))}
      </div>

      <div className="row g-4">
        <div className="col-md-6">
          <h2 className="h6">Best-selling products</h2>
          <table className="table table-sm align-middle">
            <thead><tr><th>Product</th><th>SKU</th><th>Units sold</th></tr></thead>
            <tbody>
              {data.bestSellingProducts.map((p, i) => (
                <tr key={i}><td>{p.productName}</td><td>{p.sku}</td><td>{p.unitsSold}</td></tr>
              ))}
              {data.bestSellingProducts.length === 0 && (
                <tr><td colSpan={3} className="text-muted">No sales yet.</td></tr>
              )}
            </tbody>
          </table>
        </div>
        <div className="col-md-6">
          <h2 className="h6">Monthly sales</h2>
          <table className="table table-sm align-middle">
            <thead><tr><th>Month</th><th>Units</th><th>Gross value</th></tr></thead>
            <tbody>
              {data.monthlySales.map((m, i) => (
                <tr key={i}><td>{m.month}</td><td>{m.unitsSold}</td><td>{m.grossSalesValue}</td></tr>
              ))}
              {data.monthlySales.length === 0 && (
                <tr><td colSpan={3} className="text-muted">No sales yet.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
