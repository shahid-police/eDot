"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { vendorRequest, formatDateTime } from "@/lib/vendor/api";

export default function VendorOrderDetailPage() {
  const { id } = useParams();
  const [order, setOrder] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    vendorRequest(`/orders/${id}`).then(setOrder).catch((err) => setError(err.message));
  }, [id]);

  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!order) return <p>Loading...</p>;

  return (
    <div style={{ maxWidth: 640 }}>
      <h1 className="h4 mb-1">Order {order.orderNumber}</h1>
      <p className="text-muted mb-3">
        Placed {formatDateTime(order.orderDate)} &middot; Status: <strong>{order.orderStatus}</strong> &middot; Payment: {order.paymentStatus}
        {(order.city || order.province) && <> &middot; Ships to: {[order.city, order.province].filter(Boolean).join(", ")}</>}
      </p>

      <h2 className="h6">Your items in this order</h2>
      <table className="table table-sm align-middle">
        <thead><tr><th>Product</th><th>SKU</th><th>Qty</th><th>Unit price</th><th>Line total</th></tr></thead>
        <tbody>
          {order.items.map((item, i) => (
            <tr key={i}>
              <td>{item.productName}</td>
              <td>{item.sku}</td>
              <td>{item.quantity}</td>
              <td>{item.unitPrice}</td>
              <td>{item.lineTotal}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
