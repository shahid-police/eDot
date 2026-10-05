"use client";
import React, { useEffect, useState } from "react";
import Link from "next/link";
import OrderPageTestimonials from "./OrderPageTestimonials";

const money = (value) => `$${value.toFixed(2)}`;

export default function Order() {
  const [order, setOrder] = useState(null);

  useEffect(() => {
    const requested = new URLSearchParams(window.location.search).get("order");
    try {
      const stored = JSON.parse(sessionStorage.getItem("lastOrder") ?? "null");
      if (stored && (!requested || stored.orderNumber === requested)) {
        setOrder(stored);
      }
    } catch {
      setOrder(null);
    }
  }, []);

  return (
    <section className="flat-spacing">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-lg-7">
            {order ? (
              <div className="tf-page-cart-sidebar">
                <h3 className="checkout-title">Thank you for your order</h3>
                <p>
                  Order <strong>{order.orderNumber}</strong> has been placed and is{" "}
                  {order.status.toLowerCase()}.
                </p>
                <ul className="list-order-product">
                  {order.items.map((item) => (
                    <li key={`${item.productId}-${item.variantId ?? "base"}`} className="order-item">
                      <div className="content">
                        <div className="info">
                          <p className="name">{item.productName}</p>
                          {item.variantName && <span className="variant">{item.variantName}</span>}
                          <span className="quantity"> × {item.quantity}</span>
                        </div>
                      </div>
                      <h6 className="price">{money(item.lineTotal)}</h6>
                    </li>
                  ))}
                </ul>
                <span className="br-line" />
                <ul className="list-total">
                  <li className="total-item">
                    <span>SUBTOTAL:</span>
                    <span>{money(order.subtotal)}</span>
                  </li>
                  <li className="total-item">
                    <span>SHIPPING:</span>
                    <span>{order.shippingTotal === 0 ? "Free" : money(order.shippingTotal)}</span>
                  </li>
                  <li className="total-item">
                    <span>TAX:</span>
                    <span>{money(order.taxTotal)}</span>
                  </li>
                </ul>
                <span className="br-line" />
                <h4 className="last-total d-flex justify-content-between">
                  <span>TOTAL:</span>
                  <span className="total-price-order">{money(order.grandTotal)}</span>
                </h4>
                {order.payments.map((payment) => (
                  <div key={payment.method} className="mt-3">
                    <p className="fw-medium mb-1">{payment.instructions.title}</p>
                    <p className="text-caption">{payment.instructions.message}</p>
                  </div>
                ))}
                <p className="mt-3">
                  Delivering to {order.shippingAddress.fullName}, {order.shippingAddress.addressLine1},{" "}
                  {order.shippingAddress.city}.
                </p>
              </div>
            ) : (
              <div className="text-center">
                <h3 className="checkout-title">Thank you</h3>
                <p>Your order details are available in your account once you sign in.</p>
                <Link href="/account-orders" className="tf-btn btn-fill animate-btn">
                  View orders
                </Link>
              </div>
            )}
          </div>
        </div>
      </div>
      <OrderPageTestimonials />
    </section>
  );
}
