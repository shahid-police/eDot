"use client";
import React, { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Slider1 from "./sliders/Slider1";
import QuantitySelect from "../common/QuantitySelect";
import { addCartItem } from "@/lib/api/cart";
import { addWishlistItem, getWishlist, removeWishlistItem } from "@/lib/api/wishlist";
import { useSession } from "@/lib/auth/session";
import { openModal } from "@/lib/ui/modal";

const GALLERY_KEY = "api";

export default function ApiProductDetail({ product }) {
  const router = useRouter();
  const initialVariant =
    product.variants.find((variant) => variant.inStock) ?? product.variants[0] ?? null;
  const [selectedVariantId, setSelectedVariantId] = useState(initialVariant?.id ?? null);
  const [quantity, setQuantity] = useState(1);
  const [adding, setAdding] = useState(false);
  const [cartError, setCartError] = useState("");
  const session = useSession();
  const [wishlistItemId, setWishlistItemId] = useState(null);
  const [wishlistBusy, setWishlistBusy] = useState(false);
  const [wishlistError, setWishlistError] = useState("");

  useEffect(() => {
    if (!session) {
      setWishlistItemId(null);
      return;
    }
    getWishlist()
      .then((wishlist) => {
        const saved = wishlist.items.find((item) => item.productId === product.id);
        setWishlistItemId(saved?.id ?? null);
      })
      .catch(() => setWishlistItemId(null));
  }, [session, product.id]);

  const toggleWishlist = async () => {
    if (!session) {
      openModal("log");
      return;
    }
    setWishlistError("");
    setWishlistBusy(true);
    try {
      if (wishlistItemId) {
        await removeWishlistItem(wishlistItemId);
        setWishlistItemId(null);
      } else {
        const wishlist = await addWishlistItem(product.id);
        setWishlistItemId(wishlist.items.find((item) => item.productId === product.id)?.id ?? null);
      }
    } catch (err) {
      setWishlistError(err.message || "We could not update your wishlist.");
    } finally {
      setWishlistBusy(false);
    }
  };

  const selectedVariant =
    product.variants.find((variant) => variant.id === selectedVariantId) ?? null;

  const price = selectedVariant ? selectedVariant.price : product.price;
  const salePrice = selectedVariant ? selectedVariant.salePrice : product.salePrice;
  const inStock = selectedVariant ? selectedVariant.inStock : product.inStock;

  const addToBag = async () => {
    setCartError("");
    setAdding(true);
    try {
      await addCartItem({
        productId: product.id,
        variantId: selectedVariant?.id ?? null,
        quantity,
      });
      router.push("/shop-cart");
    } catch (err) {
      setCartError(err.message || "We could not add this item to your bag.");
      setAdding(false);
    }
  };

  const slideItems = product.images.map((image, index) => ({
    id: index + 1,
    color: GALLERY_KEY,
    size: 0,
    imgSrc: image.url,
  }));

  return (
    <section className="themesFlat">
      <div className="tf-main-product section-image-zoom">
        <div className="container">
          <div className="row">
            <div className="col-md-6">
              {slideItems.length > 0 && (
                <div className="tf-product-media-wrap sticky-top">
                  <div className="thumbs-slider">
                    <Slider1
                      slideItems={slideItems}
                      activeColor={GALLERY_KEY}
                      setActiveColor={() => {}}
                    />
                  </div>
                </div>
              )}
            </div>
            <div className="col-md-6">
              <div className="tf-product-info-wrap">
                <div className="tf-product-info-list other-image-zoom">
                  <div className="tf-product-info-heading">
                    <h3 className="product-info-name fw-normal">{product.name}</h3>
                    <div className="product-info-price">
                      <div className="price-wrap">
                        <span className="price-new price-on-sale h4">
                          ${(salePrice ?? price).toFixed(2)}
                        </span>
                        {salePrice !== null && salePrice !== undefined && (
                          <span className="price-old compare-at-price fw-normal h6">
                            ${price.toFixed(2)}
                          </span>
                        )}
                      </div>
                    </div>
                    {product.shortDescription && (
                      <p className="product-infor-sub h6 fw-normal text-main-4">
                        {product.shortDescription}
                      </p>
                    )}
                    <p className="h6 fw-normal text-main-4">
                      SKU: {selectedVariant ? selectedVariant.sku : product.sku}
                    </p>
                    <p className="h6 fw-normal">
                      {inStock ? "In stock" : "Currently unavailable"}
                    </p>
                  </div>
                  {product.variants.length > 0 && (
                    <div className="tf-product-info-variant">
                      <div className="variant-picker-item">
                        <div className="variant-picker-label h6 fw-normal">
                          Option:{" "}
                          <span className="variant-picker-label-value">
                            {selectedVariant?.variantName}
                          </span>
                        </div>
                        <div className="variant-picker-values">
                          <div className="btn-group">
                            {product.variants.map((variant) => (
                              <span
                                key={variant.id}
                                className={`size-btn ${
                                  variant.id === selectedVariantId ? "active" : ""
                                } ${variant.inStock ? "" : "disabled"}`}
                                onClick={() => setSelectedVariantId(variant.id)}
                              >
                                {variant.variantName}
                              </span>
                            ))}
                          </div>
                        </div>
                      </div>
                    </div>
                  )}
                  <div className="tf-product-info-variant">
                    <div className="variant-picker-item">
                      <div className="variant-picker-label h6 fw-normal">Quantity</div>
                      <div className="variant-picker-values">
                        <QuantitySelect quantity={quantity} setQuantity={setQuantity} />
                      </div>
                    </div>
                  </div>
                  <div className="tf-product-total-quantity">
                    <button
                      type="button"
                      className="tf-btn btn-fill-2 text-uppercase fw-medium animate-btn w-100"
                      disabled={!inStock || adding}
                      onClick={addToBag}
                    >
                      {adding ? "adding..." : inStock ? "add to bag" : "currently unavailable"}
                    </button>
                    {cartError && <p className="text-danger mt-2 mb-0">{cartError}</p>}
                    <button
                      type="button"
                      className="tf-btn style-2 w-100 mt-2 fw-medium"
                      disabled={wishlistBusy}
                      onClick={toggleWishlist}
                    >
                      {wishlistItemId ? "remove from wishlist" : "add to wishlist"}
                    </button>
                    {wishlistError && <p className="text-danger mt-2 mb-0">{wishlistError}</p>}
                  </div>
                </div>
              </div>
            </div>
          </div>
          {product.description && (
            <div className="row">
              <div className="col-12 flat-spacing-8 pb-0">
                <p className="h6 fw-normal text-main-4">{product.description}</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </section>
  );
}
