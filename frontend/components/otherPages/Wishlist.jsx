"use client";
import React, { useEffect, useState } from "react";
import Link from "next/link";
import { getWishlist, removeWishlistItem } from "@/lib/api/wishlist";
import { useSession } from "@/lib/auth/session";

export default function Wishlist() {
  const session = useSession();
  const [wishlist, setWishlist] = useState(null);
  const [error, setError] = useState("");
  const [busyItemId, setBusyItemId] = useState(null);

  useEffect(() => {
    getWishlist()
      .then(setWishlist)
      .catch((err) => setError(err.message || "We could not load your wishlist."));
  }, [session]);

  const remove = async (itemId) => {
    setError("");
    setBusyItemId(itemId);
    try {
      setWishlist(await removeWishlistItem(itemId));
    } catch (err) {
      setError(err.message || "We could not update your wishlist.");
    } finally {
      setBusyItemId(null);
    }
  };

  const items = wishlist?.items ?? [];

  return (
    <div className="flat-spacing">
      <div className="container">
        {error && <p className="text-danger">{error}</p>}
        {wishlist === null ? (
          <p>Loading your wishlist...</p>
        ) : items.length === 0 ? (
          <div className="text-center">
            <p>Your wishlist is empty.</p>
            <Link href="/shop-default" className="tf-btn btn-fill animate-btn">
              Explore products
            </Link>
          </div>
        ) : (
          <div className="wrapper-wishlist tf-grid-layout tf-col-2 md-col-3 xl-col-4">
            {items.map((item) => (
              <div key={item.id} className="card_product--V01">
                <div className="card_product-wrapper">
                  <span
                    className="remove icon-close"
                    role="button"
                    onClick={() => remove(item.id)}
                    style={busyItemId === item.id ? { opacity: 0.5 } : undefined}
                  />
                  <Link href={`/product-default/${item.slug}`} className="product-img">
                    {item.imageUrl && (
                      <img
                        src={item.imageUrl}
                        alt={item.name}
                        className="lazyload img-product"
                        width={714}
                        height={900}
                      />
                    )}
                  </Link>
                  {!item.inStock && (
                    <div className="variant-box">
                      <p className="size-box text-center text-caption">Out of stock</p>
                    </div>
                  )}
                </div>
                <div className="card_product-info">
                  <Link href={`/product-default/${item.slug}`} className="name-product h5 fw-normal link">
                    {item.name}
                  </Link>
                  {item.variantName && <p className="text-caption text-main-4">{item.variantName}</p>}
                  <div className="price-wrap">
                    <span className="price-new h5">
                      ${(item.salePrice ?? item.price).toFixed(2)}
                    </span>
                    {item.salePrice !== null && item.salePrice !== undefined && (
                      <span className="price-old fw-normal">${item.price.toFixed(2)}</span>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
