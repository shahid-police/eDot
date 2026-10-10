"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { adminRequest, formatDateTime, ORDER_STATUSES, PAYMENT_STATUSES } from "@/lib/admin/api";
import { formatPrice } from "@/lib/format/price";

export default function AdminOrderDetailPage() {
  const { id } = useParams();
  const [order, setOrder] = useState(null);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [nextStatus, setNextStatus] = useState("");
  const [statusNote, setStatusNote] = useState("");
  const [nextPayment, setNextPayment] = useState("");

  const load = useCallback(() => {
    return adminRequest(`/orders/${id}`)
      .then((data) => {
        setOrder(data);
        setError("");
      })
      .catch((err) => setError(err.message));
  }, [id]);

  useEffect(() => {
    load();
  }, [load]);

  async function changeStatus(event) {
    event.preventDefault();
    setError("");
    setNotice("");
    try {
      const updated = await adminRequest(`/orders/${id}/status`, { method: "PUT", body: { status: nextStatus, note: statusNote || null } });
      setOrder(updated);
      setStatusNote("");
      setNotice(`Order moved to ${nextStatus}.`);
    } catch (err) {
      setError(err.message);
    }
  }

  async function changePayment(event) {
    event.preventDefault();
    setError("");
    setNotice("");
    try {
      const updated = await adminRequest(`/orders/${id}/payment-status`, { method: "PUT", body: { status: nextPayment } });
      setOrder(updated);
      setNotice(`Payment set to ${nextPayment}.`);
    } catch (err) {
      setError(err.message);
    }
  }

  if (!order && error) return <div className="alert alert-danger">{error}</div>;
  if (!order) return <p>Loading...</p>;

  const address = order.shippingAddress;

  return (
    <div className="vstack gap-4">
      <div className="d-flex align-items-center gap-3">
        <h1 className="h4 mb-0">Order {order.orderNumber}</h1>
        <span className="badge text-bg-secondary">{order.status}</span>
        <Link href="/admin/orders" className="small">Back to orders</Link>
      </div>

      {error && <div className="alert alert-danger py-2 mb-0">{error}</div>}
      {notice && <div className="alert alert-success py-2 mb-0">{notice}</div>}

      <div className="row g-4">
        <div className="col-lg-7">
          <h2 className="h6">Items</h2>
          <table className="table table-sm">
            <thead>
              <tr><th>Item</th><th className="text-end">Qty</th><th className="text-end">Unit</th><th className="text-end">Line</th></tr>
            </thead>
            <tbody>
              {order.items.map((item, index) => (
                <tr key={index}>
                  <td>
                    {item.productName}{item.variantName ? ` (${item.variantName})` : ""}
                    <div className="small text-muted">{item.sku}</div>
                  </td>
                  <td className="text-end">{item.quantity}</td>
                  <td className="text-end">{formatPrice(item.unitPrice)}</td>
                  <td className="text-end">{formatPrice(item.lineTotal)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr><td colSpan={3} className="text-end">Subtotal</td><td className="text-end">{formatPrice(order.subtotal)}</td></tr>
              <tr><td colSpan={3} className="text-end">Shipping</td><td className="text-end">{formatPrice(order.shippingTotal)}</td></tr>
              <tr><th colSpan={3} className="text-end">Total</th><th className="text-end">{formatPrice(order.grandTotal)}</th></tr>
            </tfoot>
          </table>

          <h2 className="h6 mt-4">Payments</h2>
          {order.payments.map((p, index) => (
            <div key={index} className="small mb-2">
              {p.method} · {p.status} · {formatPrice(p.amount)}
            </div>
          ))}

          <h2 className="h6 mt-4">History</h2>
          <ul className="list-unstyled small">
            {order.history.map((h, index) => (
              <li key={index}>{formatDateTime(h.createdAt)} · {h.status}{h.note ? ` · ${h.note}` : ""}</li>
            ))}
          </ul>
        </div>

        <div className="col-lg-5">
          <h2 className="h6">Customer</h2>
          <div className="small mb-3">
            {order.email}<br />{order.phone}<br />
            {address.fullName}, {address.addressLine1}{address.addressLine2 ? `, ${address.addressLine2}` : ""}, {address.city}, {address.province} {address.postalCode ?? ""}
          </div>

          <form onSubmit={changeStatus} className="card card-body vstack gap-2 mb-3">
            <label className="form-label mb-0">Change order status</label>
            <select className="form-select" value={nextStatus} onChange={(e) => setNextStatus(e.target.value)} required>
              <option value="">Choose...</option>
              {ORDER_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
            <input className="form-control" placeholder="Note (optional)" value={statusNote} onChange={(e) => setStatusNote(e.target.value)} maxLength={500} />
            <button className="btn btn-dark btn-sm" type="submit" disabled={!nextStatus}>Update status</button>
            <div className="small text-muted">Cancelling or returning an order puts its stock back.</div>
          </form>

          <form onSubmit={changePayment} className="card card-body vstack gap-2">
            <label className="form-label mb-0">Change payment status</label>
            <select className="form-select" value={nextPayment} onChange={(e) => setNextPayment(e.target.value)} required>
              <option value="">Choose...</option>
              {PAYMENT_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
            <button className="btn btn-dark btn-sm" type="submit" disabled={!nextPayment}>Update payment</button>
          </form>
        </div>
      </div>
    </div>
  );
}
