"use client";
import React, { useEffect, useState } from "react";
import Link from "next/link";
import { getOrder, getOrders } from "@/lib/api/orders";
import { useSession } from "@/lib/auth/session";

const money = (value) => `$${value.toFixed(2)}`;

export default function Orders() {
  const session = useSession();
  const [orders, setOrders] = useState(null);
  const [openId, setOpenId] = useState(null);
  const [details, setDetails] = useState({});
  const [error, setError] = useState("");

  useEffect(() => {
    if (!session) {
      setOrders(null);
      return;
    }
    getOrders()
      .then(setOrders)
      .catch((err) => setError(err.message || "We could not load your orders."));
  }, [session]);

  const toggle = async (orderId) => {
    if (openId === orderId) {
      setOpenId(null);
      return;
    }
    setOpenId(orderId);
    if (!details[orderId]) {
      try {
        const detail = await getOrder(orderId);
        setDetails((current) => ({ ...current, [orderId]: detail }));
      } catch (err) {
        setError(err.message || "We could not load that order.");
      }
    }
  };

  if (!session) {
    return (
      <div className="my-acount-content account-order">
        <h4 className="heading fw-normal">Order History</h4>
        <p>
          <Link href="/shop-default" className="link">Sign in</Link> to see your orders.
        </p>
      </div>
    );
  }

  return (
    <div className="my-acount-content account-order">
      <h4 className="heading fw-normal">Order History</h4>
      {error && <p className="text-danger">{error}</p>}
      {orders === null ? (
        <p>Loading your orders...</p>
      ) : orders.length === 0 ? (
        <p>You have not placed any orders yet.</p>
      ) : (
        <div className="table-order-history">
          <table>
            <thead>
              <tr>
                <th className="title-case order_code">ORDER ID</th>
                <th className="title-case order_date">ORDER DATE</th>
                <th className="title-case order_status">STATUS</th>
                <th className="title-case order_total">TOTAL</th>
                <th className="title-case order_action">ACTION</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => (
                <React.Fragment key={order.id}>
                  <tr>
                    <td>{order.orderNumber}</td>
                    <td>{new Date(`${order.createdAt}Z`).toLocaleDateString()}</td>
                    <td>{order.status}</td>
                    <td>{money(order.grandTotal)}</td>
                    <td>
                      <button type="button" className="tf-btn-line" onClick={() => toggle(order.id)}>
                        {openId === order.id ? "Hide" : "View"}
                      </button>
                    </td>
                  </tr>
                  {openId === order.id && details[order.id] && (
                    <tr>
                      <td colSpan={5}>
                        <ul className="list-order-product">
                          {details[order.id].items.map((item) => (
                            <li key={`${item.productId}-${item.variantId ?? "base"}`}>
                              {item.productName}
                              {item.variantName ? ` (${item.variantName})` : ""} × {item.quantity}
                            </li>
                          ))}
                        </ul>
                        {details[order.id].payments.map((payment) => (
                          <p key={payment.method} className="text-caption">
                            {payment.instructions.title}: {payment.instructions.message}
                          </p>
                        ))}
                        <p className="text-caption">
                          Status history:{" "}
                          {details[order.id].history
                            .map((entry) => `${entry.status}${entry.note ? ` (${entry.note})` : ""}`)
                            .join(" → ")}
                        </p>
                      </td>
                    </tr>
                  )}
                </React.Fragment>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
