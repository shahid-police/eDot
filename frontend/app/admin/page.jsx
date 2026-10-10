"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";
import { formatPrice } from "@/lib/format/price";

export default function AdminDashboardPage() {
  const [data, setData] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    adminRequest("/dashboard")
      .then(setData)
      .catch((err) => setError(err.message));
  }, []);

  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!data) return <p>Loading...</p>;

  const stats = [
    ["Orders today (UTC)", data.ordersToday],
    ["Pending orders", data.pendingOrders],
    ["Revenue (excl. cancelled/refunded)", formatPrice(data.revenue)],
    ["Total orders", data.totalOrders],
    ["Customers", data.customerCount],
    ["Active products", data.activeProductCount],
    ["Low stock items", data.lowStockCount],
  ];

  return (
    <div>
      <h1 className="h4 mb-4">Dashboard</h1>
      <div className="row g-3 mb-4">
        {stats.map(([label, value]) => (
          <div key={label} className="col-6 col-md-4 col-xl-3">
            <div className="card h-100">
              <div className="card-body">
                <div className="small text-muted">{label}</div>
                <div className="fs-5 fw-semibold">{value}</div>
              </div>
            </div>
          </div>
        ))}
      </div>

      <h2 className="h6 mb-2">Recent orders</h2>
      <div className="table-responsive">
        <table className="table table-sm align-middle">
          <thead>
            <tr>
              <th>Order</th>
              <th>Placed</th>
              <th>Customer</th>
              <th>Status</th>
              <th>Payment</th>
              <th className="text-end">Total</th>
            </tr>
          </thead>
          <tbody>
            {data.recentOrders.map((order) => (
              <tr key={order.id}>
                <td><Link href={`/admin/orders/${order.id}`}>{order.orderNumber}</Link></td>
                <td>{formatDateTime(order.createdAt)}</td>
                <td>{order.email}</td>
                <td>{order.status}</td>
                <td>{order.paymentStatus}</td>
                <td className="text-end">{formatPrice(order.grandTotal)}</td>
              </tr>
            ))}
            {data.recentOrders.length === 0 && (
              <tr><td colSpan={6} className="text-muted">No orders yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
