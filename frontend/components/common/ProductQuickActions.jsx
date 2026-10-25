"use client";

import { useState } from "react";
import QuickView from "./QuickView";
import { useContextElement } from "@/context/Context";
import { addCartItem } from "@/lib/api/cart";

const NEEDS_VARIANT = /choose an option/i;

// The heart/cart/compare/eye icon row shown on hover over a product card. Wishlist, Add to Cart,
// and Compare are only meaningful for real catalog products (product.source === "api") - static
// template demo cards keep just the QuickView icon, matching how QuickView itself already branches.
//
// isSaved/isCompared and onToggle* are controlled from the parent grid via useSavedProducts(),
// which fetches the wishlist/compare lists once per grid rather than once per card.
export default function ProductQuickActions({
  product,
  alwaysVisible = false,
  isSaved = false,
  isCompared = false,
  onToggleWishlist,
  onToggleCompare,
}) {
  const isApiProduct = product?.source === "api";
  const liStyle = alwaysVisible ? { opacity: 1, visibility: "visible" } : undefined;
  const [wishlistBusy, setWishlistBusy] = useState(false);
  const [compareBusy, setCompareBusy] = useState(false);
  const [cartBusy, setCartBusy] = useState(false);
  const [justAdded, setJustAdded] = useState(false);
  const { setQuickViewItem } = useContextElement();

  // A Variant product (one with colour/size options) needs a specific option chosen before the
  // server will add it to the wishlist or cart, and a grid card has no variant picker - open
  // Quick View instead, where one exists, rather than leaving the click looking like it did
  // nothing. Returns true if it handled the error this way.
  async function openQuickViewIfVariantRequired(err) {
    if (err?.status === 400 && NEEDS_VARIANT.test(err?.message ?? "")) {
      setQuickViewItem(product);
      const { Modal } = await import("bootstrap");
      Modal.getOrCreateInstance(document.getElementById("quickView")).show();
      return true;
    }
    return false;
  }

  async function handleWishlistClick(event) {
    event.preventDefault();
    if (wishlistBusy) return;
    setWishlistBusy(true);
    try {
      await onToggleWishlist?.(product);
    } catch (err) {
      await openQuickViewIfVariantRequired(err);
    } finally {
      setWishlistBusy(false);
    }
  }

  async function handleCompareClick(event) {
    event.preventDefault();
    if (compareBusy) return;
    setCompareBusy(true);
    try {
      await onToggleCompare?.(product);
    } catch {
    } finally {
      setCompareBusy(false);
    }
  }

  async function handleAddToCartClick(event) {
    event.preventDefault();
    if (cartBusy) return;
    setCartBusy(true);
    try {
      await addCartItem({ productId: product.id, quantity: 1 });
      // The shopping cart offcanvas reloads itself on this exact "about to show" event (see
      // useServerCart), so opening it is all that's needed for the new item to appear in it.
      const { Offcanvas } = await import("bootstrap");
      Offcanvas.getOrCreateInstance(document.getElementById("shoppingCart")).show();
      setJustAdded(true);
      setTimeout(() => setJustAdded(false), 1500);
    } catch (err) {
      await openQuickViewIfVariantRequired(err);
    } finally {
      setCartBusy(false);
    }
  }

  return (
    <>
      {isApiProduct && (
        <>
          <li style={liStyle}>
            <a
              href="#wishlist"
              onClick={handleWishlistClick}
              className={`hover-tooltip tooltip-left box-icon${isSaved ? " text-primary" : ""}`}
              aria-label={isSaved ? "Remove from wishlist" : "Add to wishlist"}
            >
              <span className="icon icon-hearth" />
              <span className="tooltip">{isSaved ? "Remove from Wishlist" : "Add to Wishlist"}</span>
            </a>
          </li>
          <li style={liStyle}>
            <a
              href="#shoppingCart"
              onClick={handleAddToCartClick}
              className={`hover-tooltip tooltip-left box-icon${justAdded ? " text-primary" : ""}`}
              aria-label="Add to cart"
            >
              <span className="icon icon-cart" />
              <span className="tooltip">{justAdded ? "Added!" : "Add to Cart"}</span>
            </a>
          </li>
          <li style={liStyle}>
            <a
              href="#compare"
              onClick={handleCompareClick}
              className={`hover-tooltip tooltip-left box-icon${isCompared ? " text-primary" : ""}`}
              aria-label={isCompared ? "Remove from compare" : "Add to compare"}
            >
              <span className="icon icon-compare" />
              <span className="tooltip">{isCompared ? "Remove from Compare" : "Add to Compare"}</span>
            </a>
          </li>
        </>
      )}
      <li style={liStyle}>
        <QuickView product={product} />
      </li>
    </>
  );
}
