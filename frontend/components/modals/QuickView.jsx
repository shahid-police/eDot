"use client";
import React, { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import Image from "next/image";
import { Swiper, SwiperSlide } from "swiper/react";
import { useContextElement } from "@/context/Context";
import { Navigation } from "swiper/modules";
import ColorSelect from "../product-details/ColorSelect";
import SizePicker from "../product-details/SizeSelect";
import QuantitySelect from "../common/QuantitySelect";
import { getProductBySlug } from "@/lib/api/products";
import { addCartItem } from "@/lib/api/cart";
import { addWishlistItem, getWishlist, removeWishlistItem } from "@/lib/api/wishlist";
import { addCompareItem, getCompare, removeCompareItem } from "@/lib/api/compare";
import { formatPrice } from "@/lib/format/price";

const imageData = [
  { color: "gold", src: "/images/products/detail/prd-detail-1.jpg" },
  { color: "rose", src: "/images/products/detail/prd-detail-4.jpg" },
  { color: "titanium", src: "/images/products/detail/prd-detail-7.jpg" },
];

export default function QuickView() {
  const { quickViewItem } = useContextElement();
  const isApiProduct = quickViewItem?.source === "api";

  return (
    <div className="modal modalCentered fade modal-quick-view" id="quickView">
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <span
            className="icon-close icon-close-popup"
            data-bs-dismiss="modal"
          />
          {isApiProduct ? (
            <ApiQuickViewBody key={quickViewItem.slug} item={quickViewItem} />
          ) : (
            <LegacyQuickViewBody item={quickViewItem} />
          )}
        </div>
      </div>
    </div>
  );
}

// Real product, fetched by slug when the modal opens so it has the full catalogue data
// (images, variants, description) the card alone doesn't carry.
function ApiQuickViewBody({ item }) {
  const router = useRouter();
  const [detail, setDetail] = useState(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [selectedVariantId, setSelectedVariantId] = useState(null);
  const [quantity, setQuantity] = useState(1);
  const [adding, setAdding] = useState(false);
  const [cartError, setCartError] = useState("");
  const [wishlistItems, setWishlistItems] = useState([]);
  const [compareItems, setCompareItems] = useState([]);
  const [saveBusy, setSaveBusy] = useState(false);
  const [saveError, setSaveError] = useState("");

  useEffect(() => {
    let active = true;
    setLoading(true);
    setLoadError("");
    getProductBySlug(item.slug)
      .then((data) => {
        if (!active) return;
        setDetail(data);
        const initialVariant = data.variants.find((v) => v.inStock) ?? data.variants[0] ?? null;
        setSelectedVariantId(initialVariant?.id ?? null);
      })
      .catch((err) => active && setLoadError(err.message || "We could not load this product."))
      .finally(() => active && setLoading(false));
    return () => {
      active = false;
    };
  }, [item.slug]);

  useEffect(() => {
    getWishlist().then((list) => setWishlistItems(list.items)).catch(() => setWishlistItems([]));
    getCompare().then((list) => setCompareItems(list.items)).catch(() => setCompareItems([]));
  }, []);

  if (loading) {
    return (
      <div className="tf-product-info-wrap">
        <p className="text-center py-5 mb-0">Loading...</p>
      </div>
    );
  }

  if (loadError || !detail) {
    return (
      <div className="tf-product-info-wrap">
        <p className="text-center text-danger py-5 mb-0">{loadError || "This product is unavailable."}</p>
      </div>
    );
  }

  const selectedVariant = detail.variants.find((v) => v.id === selectedVariantId) ?? null;
  const variantId = selectedVariant?.id ?? null;
  const price = selectedVariant ? selectedVariant.price : detail.price;
  const salePrice = selectedVariant ? selectedVariant.salePrice : detail.salePrice;
  const inStock = selectedVariant ? selectedVariant.inStock : detail.inStock;
  const percentOff =
    salePrice != null && price > 0 ? Math.round((1 - salePrice / price) * 100) : null;

  const savedToWishlist = wishlistItems.find(
    (i) => i.productId === detail.id && i.variantId === variantId
  );
  const savedToCompare = compareItems.find((i) => i.productId === detail.id);
  const detailHref = `/${inStock ? "product-default" : "product-notify-avaiable"}/${detail.slug}`;

  const run = async (action, setItems) => {
    setSaveError("");
    setSaveBusy(true);
    try {
      const list = await action();
      setItems(list.items);
    } catch (err) {
      setSaveError(err.message || "We could not update your saved items.");
    } finally {
      setSaveBusy(false);
    }
  };

  const toggleWishlist = () =>
    run(
      () =>
        savedToWishlist
          ? removeWishlistItem(savedToWishlist.id)
          : addWishlistItem({ productId: detail.id, variantId }),
      setWishlistItems
    );

  const toggleCompare = () =>
    run(
      () => (savedToCompare ? removeCompareItem(savedToCompare.id) : addCompareItem(detail.id)),
      setCompareItems
    );

  const addToBag = async () => {
    setCartError("");
    setAdding(true);
    try {
      await addCartItem({ productId: detail.id, variantId, quantity });
      router.push("/shop-cart");
    } catch (err) {
      setCartError(err.message || "We could not add this item to your bag.");
      setAdding(false);
    }
  };

  const images = detail.images.length > 0 ? detail.images : [{ url: item.imgSrc, altText: detail.name }];

  return (
    <>
      <div className="tf-product-media-wrap tf-btn-swiper-item">
        <Swiper
          dir="ltr"
          modules={[Navigation]}
          navigation={{ prevEl: ".snbqvp1", nextEl: ".snbqvn1" }}
          className="swiper tf-single-slide"
        >
          {images.map((image, index) => (
            <SwiperSlide className="swiper-slide" key={index}>
              <div className="item">
                <Image
                  className="lazyload"
                  alt={image.altText || detail.name}
                  width={1000}
                  height={1000}
                  src={image.url}
                />
              </div>
            </SwiperSlide>
          ))}
          {images.length > 1 && (
            <div className="nav-swiper-group style-2">
              <div className="nav-thumbs thumbs-prev single-slide-prev snbqvp1">
                <span className="fw-normal">PRE</span>
              </div>
              <span className="text-main">/</span>
              <div className="nav-thumbs thumbs-next single-slide-next snbqvn1">
                <span className="fw-normal">NEXT</span>
              </div>
            </div>
          )}
        </Swiper>
      </div>
      <div className="tf-product-info-wrap">
        <div className="tf-product-info-inner tf-product-info-list">
          <div className="tf-product-info-heading">
            <Link href={detailHref} className="product-info-name h4 fw-normal text-uppercase link">
              {detail.name}
            </Link>
            <div className="product-info-price">
              <div className="price-wrap">
                <span className="price-new price-on-sale h4">
                  {formatPrice(salePrice ?? price)}
                </span>
                {salePrice != null && (
                  <span className="price-old compare-at-price fw-normal h6">
                    {formatPrice(price)}
                  </span>
                )}
                {percentOff != null && percentOff > 0 && (
                  <p className="badges-on-sale">
                    <i className="icon-tag" />
                    <span className="number-sale">{percentOff}% OFF</span>
                  </p>
                )}
              </div>
            </div>
            {detail.shortDescription && (
              <p className="product-infor-sub text-main-4">{detail.shortDescription}</p>
            )}
            <p className="h6 fw-normal text-main-4">
              {inStock ? "In stock" : "Currently unavailable"}
            </p>
          </div>
          {detail.variants.length > 0 && (
            <div className="tf-product-info-variant">
              <div className="variant-picker-item">
                <div className="variant-picker-label h6 fw-normal">
                  Option: <span className="variant-picker-label-value">{selectedVariant?.variantName}</span>
                </div>
                <div className="variant-picker-values">
                  <div className="btn-group">
                    {detail.variants.map((variant) => (
                      <span
                        key={variant.id}
                        className={`size-btn ${variant.id === selectedVariantId ? "active" : ""} ${
                          variant.inStock ? "" : "disabled"
                        }`}
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
              disabled={saveBusy}
              onClick={toggleWishlist}
            >
              {savedToWishlist ? "remove from wishlist" : "add to wishlist"}
            </button>
            <button
              type="button"
              className="tf-btn style-2 w-100 mt-2 fw-medium"
              disabled={saveBusy}
              onClick={toggleCompare}
            >
              {savedToCompare ? "remove from compare" : "add to compare"}
            </button>
            {saveError && <p className="text-danger mt-2 mb-0">{saveError}</p>}
          </div>
          <Link href={detailHref} className="tf-btn-line">
            <span className="text-body">View full details</span>
            <i className="icon icon-arrow-top-right" />
          </Link>
        </div>
      </div>
    </>
  );
}

// Unchanged from before: the template's own demo product data (used by the ~8 pages not yet
// wired to the API). Kept exactly as it was so none of those pages regress.
function LegacyQuickViewBody({ item }) {
  const [slides, setSlides] = useState([...imageData]);
  const [quantity, setQuantity] = useState(1);
  const [activeColor, setActiveColor] = useState("gold");

  const { isAddedToCartProducts, cartProducts, updateQuantity } = useContextElement();
  useEffect(() => {
    setSlides((pre) => {
      const prevSlides = [...pre];
      if (item?.imgSrc) {
        prevSlides[0].src = item.imgSrc;
      }
      return prevSlides;
    });
  }, [item]);

  if (!item) return null;

  return (
    <>
      <div className="tf-product-media-wrap tf-btn-swiper-item">
        <Swiper
          dir="ltr"
          modules={[Navigation]}
          navigation={{ prevEl: ".snbqvp1", nextEl: ".snbqvn1" }}
          className="swiper tf-single-slide"
        >
          {imageData.map((data, index) => (
            <SwiperSlide className="swiper-slide" data-color={data.color} key={index}>
              <div className="item">
                <Image
                  className="lazyload"
                  data-src={data.src}
                  alt=""
                  width={1000}
                  height={1000}
                  src={data.src}
                />
              </div>
            </SwiperSlide>
          ))}
          <div className="nav-swiper-group style-2">
            <div className="nav-thumbs thumbs-prev single-slide-prev snbqvp1">
              <span className="fw-normal">PRE</span>
            </div>
            <span className="text-main">/</span>
            <div className="nav-thumbs thumbs-next single-slide-next snbqvn1">
              <span className="fw-normal">NEXT</span>
            </div>
          </div>
        </Swiper>
      </div>
      <div className="tf-product-info-wrap">
        <div className="tf-product-info-inner tf-product-info-list">
          <div className="tf-product-info-heading">
            <Link
              href={`/product-default/${item.id}`}
              className="product-info-name h4 fw-normal text-uppercase link"
            >
              {item.title}
            </Link>
            <div className="product-info-price">
              <div className="price-wrap">
                <span className="price-new price-on-sale h4">${item.price.toFixed(2)}</span>
                {item.oldPrice && (
                  <span className="price-old compare-at-price fw-normal h6">
                    ${item.oldPrice.toFixed(2)}
                  </span>
                )}
                <p className="badges-on-sale">
                  <i className="icon-tag" />
                  <span className="number-sale" data-person-sale={30}>
                    30% OFF
                  </span>
                </p>
              </div>
            </div>
            <p className="product-infor-sub text-main-4">
              Lorem ipsum dolor sit amet nec etiamconsectetur. Egestas cursus a maecenas massa
              facilisi adipiscing dolor iaculis. In mattis nec morbi.
            </p>
          </div>
          <div className="tf-product-info-variant">
            <div className="variant-picker-item variant-color">
              <div className="variant-picker-label h6 fw-normal">
                Material:
                <span className="variant-picker-label-value value-currentColor">{activeColor}</span>
              </div>
              <div className="variant-picker-values">
                <ColorSelect activeColor={activeColor} setActiveColor={setActiveColor} />
              </div>
            </div>
            <SizePicker />
            <div className="variant-picker-item">
              <div className="variant-picker-label h6 fw-normal">Quantity</div>
              <div className="variant-picker-values">
                <QuantitySelect
                  quantity={
                    isAddedToCartProducts(item.id)
                      ? cartProducts.filter((elm) => elm.id == item.id)[0].quantity
                      : quantity
                  }
                  setQuantity={(qty) => {
                    if (isAddedToCartProducts(item.id)) {
                      updateQuantity(item.id, qty);
                    } else {
                      setQuantity(qty);
                    }
                  }}
                />
              </div>
            </div>
          </div>
          <div className="tf-product-total-quantity">
            <Link href={`/checkout`} className="tf-btn w-100 text-uppercase fw-medium">
              buy it now
            </Link>
          </div>
          <Link href={`/product-default/${item.id}`} className="tf-btn-line">
            <span className="text-body">View full details</span>
            <i className="icon icon-arrow-top-right" />
          </Link>
        </div>
      </div>
    </>
  );
}
