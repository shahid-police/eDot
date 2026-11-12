"use client";

import { useState } from "react";
import QuickView from "./QuickView";
import { useContextElement } from "@/context/Context";
import { addCartItem } from "@/lib/api/cart";

const NEEDS_VARIANT = /choose an option/i;

// The classic WhatsApp glyph (Font Awesome's brand path) - fill="currentColor" so it inherits the
// same hover color-inversion the icon-font glyphs around it already get from .box-icon.
function WhatsAppGlyph() {
  return (
    <svg className="icon" width="16" height="16" viewBox="0 0 448 512" fill="currentColor" aria-hidden="true">
      <path d="M380.9 97.1C339 55.1 283.2 32 223.9 32c-122.4 0-222 99.6-222 222 0 39.1 10.2 77.3 29.6 111L0 480l117.7-30.9c32.4 17.7 68.9 27 106.1 27h.1c122.3 0 224.1-99.6 224.1-222 0-59.3-25.2-115-67.1-157zm-157 341.6c-33.2 0-65.7-8.9-94-25.7l-6.7-4-69.8 18.3L72 359.2l-4.4-7c-18.5-29.4-28.2-63.3-28.2-98.2 0-101.7 82.8-184.5 184.6-184.5 49.3 0 95.6 19.2 130.4 54.1 34.8 34.9 56.2 81.2 56.1 130.5-.1 101.8-84.9 184.6-186.6 184.6zm101.2-138.2c-5.5-2.8-32.8-16.2-37.9-18-5.1-1.9-8.8-2.8-12.5 2.8-3.7 5.6-14.3 18-17.6 21.8-3.2 3.7-6.5 4.2-12 1.4-32.4-16.2-53.7-28.9-75.1-65.5-5.7-9.8 5.7-9.1 16.3-30.3 1.8-3.7.9-6.9-.5-9.7-1.4-2.8-12.5-30.1-17.1-41.2-4.5-10.8-9.1-9.3-12.5-9.5-3.2-.2-6.9-.2-10.6-.2-3.7 0-9.7 1.4-14.8 6.9-5.1 5.6-19.4 19-19.4 46.3 0 27.3 19.9 53.7 22.6 57.4 2.8 3.7 39.1 59.7 94.8 83.8 35.2 15.2 49 16.5 66.6 13.9 10.7-1.6 32.8-13.4 37.4-26.4 4.6-13 4.6-24.1 3.2-26.4-1.3-2.5-5-3.9-10.5-6.6z" />
    </svg>
  );
}

function buildWhatsAppDiscussUrl(product, storeNumber) {
  if (!storeNumber || !product) return null;
  const origin = typeof window !== "undefined" ? window.location.origin : "";
  const productUrl = `${origin}/product-default/${product.slug ?? product.id}`;
  const message = `Hi! I'm interested in this product:\n${product.title}\n${productUrl}`;
  return `https://wa.me/${storeNumber.replace(/^\+/, "")}?text=${encodeURIComponent(message)}`;
}

// The heart/cart/WhatsApp/eye icon row shown on hover over a product card. Wishlist, Add to Cart,
// and Discuss on WhatsApp are only meaningful for real catalog products (product.source === "api")
// - static template demo cards keep just the QuickView icon, matching how QuickView itself already
// branches.
//
// isSaved and onToggleWishlist are controlled from the parent grid via useSavedProducts(), which
// fetches the wishlist list once per grid rather than once per card.
export default function ProductQuickActions({
  product,
  alwaysVisible = false,
  isSaved = false,
  onToggleWishlist,
}) {
  const isApiProduct = product?.source === "api";
  const liStyle = alwaysVisible ? { opacity: 1, visibility: "visible" } : undefined;
  const [wishlistBusy, setWishlistBusy] = useState(false);
  const [cartBusy, setCartBusy] = useState(false);
  const [justAdded, setJustAdded] = useState(false);
  const { setQuickViewItem, storeWhatsAppNumber } = useContextElement();
  const whatsAppUrl = buildWhatsAppDiscussUrl(product, storeWhatsAppNumber);

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
          {whatsAppUrl && (
            <li style={liStyle}>
              {/* Named target so the browser reuses one tab across different products where
                  possible. WhatsApp's own web entry point still opens its own fresh tab on every
                  click regardless (it closes the tab we open and reopens an opener-less one,
                  which also defeats holding our own window handle) - verified, not a bug here. */}
              <a
                href={whatsAppUrl}
                target="whatsapp-chat"
                rel="noopener noreferrer"
                className="hover-tooltip tooltip-left box-icon"
                aria-label="Discuss on WhatsApp"
              >
                <WhatsAppGlyph />
                <span className="tooltip">Discuss on WhatsApp</span>
              </a>
            </li>
          )}
        </>
      )}
      <li style={liStyle}>
        <QuickView product={product} />
      </li>
    </>
  );
}
