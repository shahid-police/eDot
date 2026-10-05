"use client";
import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { getCart } from "@/lib/api/cart";
import { getCheckoutSummary, placeOrder } from "@/lib/api/checkout";
import { getAddresses } from "@/lib/api/customers";
import { useSession } from "@/lib/auth/session";
import { formatPrice } from "@/lib/format/price";

const NEW_ADDRESS = "new";
const EMPTY_ADDRESS = {
  fullName: "",
  phone: "",
  addressLine1: "",
  addressLine2: "",
  city: "",
  province: "",
  postalCode: "",
};

const money = (value) => `${formatPrice(value)}`;

export default function Checkout() {
  const router = useRouter();
  const session = useSession();
  const [items, setItems] = useState(null);
  const [summary, setSummary] = useState(null);
  const [addresses, setAddresses] = useState([]);
  const [selectedAddress, setSelectedAddress] = useState(NEW_ADDRESS);
  const [address, setAddress] = useState(EMPTY_ADDRESS);
  const [email, setEmail] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("CashOnDelivery");
  const [notes, setNotes] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    Promise.all([getCart(), getCheckoutSummary()])
      .then(([cart, checkout]) => {
        setItems(cart.items);
        setSummary(checkout);
      })
      .catch((err) => setError(err.message || "We could not load your checkout."));
  }, [session]);

  useEffect(() => {
    if (!session) {
      setAddresses([]);
      return;
    }
    setEmail(session.customer.email);
    getAddresses()
      .then((list) => {
        setAddresses(list);
        const preferred = list.find((item) => item.isDefaultShipping) ?? list[0];
        setSelectedAddress(preferred ? String(preferred.id) : NEW_ADDRESS);
      })
      .catch(() => setAddresses([]));
  }, [session]);

  const updateAddress = (field) => (event) =>
    setAddress((current) => ({ ...current, [field]: event.target.value }));

  const submit = async (event) => {
    event.preventDefault();
    setError("");
    setSubmitting(true);
    const payload = {
      email: session ? undefined : email,
      paymentMethod,
      notes: notes.trim() || undefined,
      ...(selectedAddress === NEW_ADDRESS
        ? { shippingAddress: address }
        : { shippingAddressId: Number(selectedAddress) }),
    };
    try {
      const order = await placeOrder(payload);
      router.push(`/thank-you?order=${encodeURIComponent(order.orderNumber)}`);
    } catch (err) {
      setError(err.message || "We could not place your order. Please try again.");
      setSubmitting(false);
    }
  };

  if (items === null) {
    return (
      <section className="flat-spacing">
        <div className="container">
          <p>{error || "Loading your checkout..."}</p>
        </div>
      </section>
    );
  }

  if (items.length === 0) {
    return (
      <section className="flat-spacing">
        <div className="container text-center">
          <p>Your cart is empty.</p>
          <Link href="/shop-default" className="tf-btn btn-fill animate-btn">
            Explore products
          </Link>
        </div>
      </section>
    );
  }

  const addressFields = [
    ["fullName", "Full name *", "text", "name", true],
    ["phone", "Mobile number *", "tel", "tel", true],
    ["addressLine1", "Address *", "text", "address-line1", true],
    ["addressLine2", "Apartment, building (optional)", "text", "address-line2", false],
    ["city", "City *", "text", "address-level2", true],
    ["province", "Province *", "text", "address-level1", true],
    ["postalCode", "Postal code", "text", "postal-code", false],
  ];

  return (
    <section className="flat-spacing">
      <div className="container">
        <div className="row">
          <div className="col-lg-7">
            <form onSubmit={submit} className="form-checkout">
              {!session && (
                <fieldset className="tf-field">
                  <input
                    className="tf-input"
                    type="email"
                    autoComplete="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                  />
                  <label className="tf-lable">Email *</label>
                </fieldset>
              )}

              <h4 className="checkout-title mt-4">Shipping address</h4>
              {addresses.length > 0 && (
                <fieldset className="tf-field">
                  <select
                    className="tf-input"
                    value={selectedAddress}
                    onChange={(e) => setSelectedAddress(e.target.value)}
                  >
                    {addresses.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.label ? `${item.label}: ` : ""}
                        {item.fullName}, {item.addressLine1}, {item.city}
                      </option>
                    ))}
                    <option value={NEW_ADDRESS}>Use a new address</option>
                  </select>
                  <label className="tf-lable">Deliver to</label>
                </fieldset>
              )}

              {selectedAddress === NEW_ADDRESS &&
                addressFields.map(([field, label, type, autoComplete, required]) => (
                  <fieldset key={field} className="tf-field">
                    <input
                      className="tf-input"
                      type={type}
                      autoComplete={autoComplete}
                      value={address[field]}
                      onChange={updateAddress(field)}
                      required={required}
                    />
                    <label className="tf-lable">{label}</label>
                  </fieldset>
                ))}

              <h4 className="checkout-title mt-4">Payment</h4>
              <div className="checkbox-wrap">
                <input
                  id="pay-cod"
                  type="radio"
                  name="payment"
                  checked={paymentMethod === "CashOnDelivery"}
                  onChange={() => setPaymentMethod("CashOnDelivery")}
                />
                <label htmlFor="pay-cod">Cash on delivery</label>
              </div>
              <div className="checkbox-wrap">
                <input
                  id="pay-bank"
                  type="radio"
                  name="payment"
                  checked={paymentMethod === "BankTransfer"}
                  onChange={() => setPaymentMethod("BankTransfer")}
                />
                <label htmlFor="pay-bank">Bank transfer</label>
              </div>

              <fieldset className="tf-field mt-4">
                <textarea
                  className="tf-input"
                  rows={3}
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                />
                <label className="tf-lable">Order notes (optional)</label>
              </fieldset>

              {error && <p className="text-danger">{error}</p>}
              <button
                type="submit"
                className="tf-btn btn-fill animate-btn w-100 mt-3"
                disabled={submitting}
              >
                {submitting ? "PLACING ORDER..." : "PLACE ORDER"}
              </button>
            </form>
          </div>

          <div className="col-lg-5">
            <div className="tf-page-cart-sidebar">
              <h4 className="checkout-title">YOUR CART</h4>
              <ul className="list-order-product">
                {items.map((item) => (
                  <li key={item.id} className="order-item">
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
                  <span>{money(summary?.subtotal ?? 0)}</span>
                </li>
                <li className="total-item">
                  <span>SHIPPING:</span>
                  <span>
                    {summary?.shippingTotal === 0 ? "Free" : money(summary?.shippingTotal ?? 0)}
                  </span>
                </li>
                <li className="total-item">
                  <span>TAX:</span>
                  <span>{money(summary?.taxTotal ?? 0)}</span>
                </li>
              </ul>
              <span className="br-line" />
              <h4 className="last-total d-flex justify-content-between">
                <span>TOTAL:</span>
                <span className="total-price-order">{money(summary?.grandTotal ?? 0)}</span>
              </h4>
              {summary && summary.subtotal < summary.freeShippingThreshold && (
                <p className="text-caption">
                  Free shipping on orders of {money(summary.freeShippingThreshold)} or more.
                </p>
              )}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
