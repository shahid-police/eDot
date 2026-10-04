"use client";
import React, { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { getCart, removeCartItem, updateCartItem } from "@/lib/api/cart";

export default function ApiShopCart() {
  const [cart, setCart] = useState(null);
  const [error, setError] = useState("");
  const [busyItemId, setBusyItemId] = useState(null);

  const load = useCallback(async () => {
    try {
      setCart(await getCart());
    } catch (err) {
      setError(err.message || "We could not load your cart.");
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const change = async (item, action) => {
    setError("");
    setBusyItemId(item.id);
    try {
      if (action === "remove") {
        setCart(await removeCartItem(item.id));
      } else {
        setCart(await updateCartItem(item.id, item.quantity + action));
      }
    } catch (err) {
      setError(err.message || "We could not update your cart.");
    } finally {
      setBusyItemId(null);
    }
  };

  const items = cart?.items ?? [];

  return (
    <section className="flat-spacing s-shop-cart each-list-prd">
      <div className="container">
        <div className="row">
          <div className="col-xl-8">
            <div className="left mb-xl-0">
              {error && <p className="text-danger">{error}</p>}
              <div className="table-shop-cart table-order-detail">
                {cart === null ? (
                  <div>Loading your cart...</div>
                ) : items.length === 0 ? (
                  <>
                    <div>Your Cart is Empty</div>
                    <Link href="/shop-default" className="tf-btn btn-fill mt-3">
                      Continue shopping
                    </Link>
                  </>
                ) : (
                  <table>
                    <thead>
                      <tr>
                        <th className="order_product h6 fw-normal">Product</th>
                        <th className="order_price h6 fw-normal">price</th>
                        <th className="order_quantity h6 fw-normal">quantity</th>
                        <th className="order_subtotal h6 fw-normal">subtotal</th>
                      </tr>
                    </thead>
                    <tbody>
                      {items.map((item) => (
                        <tr key={item.id} className="file-delete order_item each-prd">
                          <td>
                            <div className="order_product">
                              <span className="image">
                                {item.imageUrl && (
                                  <img alt={item.productName} src={item.imageUrl} width={714} height={900} />
                                )}
                              </span>
                              <div className="infor">
                                <Link
                                  href={`/product-default/${item.productSlug}`}
                                  className="prd-name h6 fw-normal link"
                                >
                                  {item.productName}
                                </Link>
                                {item.variantName && <p className="prd-type">{item.variantName}</p>}
                                {!item.isAvailable && (
                                  <p className="text-danger">
                                    Only {item.availableQuantity} left
                                  </p>
                                )}
                              </div>
                            </div>
                          </td>
                          <td className="order_price each-price">${item.unitPrice.toFixed(2)}</td>
                          <td>
                            <div className="order_quantity">
                              <div className="wg-quantity style-2">
                                <button
                                  className="btn-quantity minus-quantity"
                                  disabled={busyItemId === item.id || item.quantity <= 1}
                                  onClick={() => change(item, -1)}
                                >
                                  <i className="icon-minus" />
                                </button>
                                <input
                                  className="quantity-product"
                                  type="text"
                                  readOnly
                                  value={item.quantity}
                                />
                                <button
                                  className="btn-quantity plus-quantity"
                                  disabled={busyItemId === item.id || item.quantity >= 99}
                                  onClick={() => change(item, 1)}
                                >
                                  <i className="icon-plus" />
                                </button>
                              </div>
                              <span
                                onClick={() => change(item, "remove")}
                                className="remove tf-btn-line style-line-2 fw-normal"
                                role="button"
                              >
                                Remove
                              </span>
                            </div>
                          </td>
                          <td className="order_subtotal each-subtotal-price">
                            ${item.lineTotal.toFixed(2)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            </div>
          </div>
          <div className="col-xl-4">
            <div className="right">
              <div className="checkout-sidebar">
                <form className="form-checkout-sidebar" onSubmit={(e) => e.preventDefault()}>
                  <div className="form-content">
                    <h4 className="title-total">
                      TOTAL: ${(cart?.subtotal ?? 0).toFixed(2)}
                    </h4>
                    <p className="tax-text">Taxes and shipping calculated at checkout</p>
                  </div>
                  <Link href="/checkout" className="tf-btn btn-fill fw-medium animate-btn w-100">
                    CHECKOUT
                  </Link>
                </form>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
