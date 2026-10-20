"use client";
import { products5 } from "@/data/products";
import React from "react";
import { Swiper, SwiperSlide } from "swiper/react";
import Link from "next/link";
import Image from "next/image";
import QuickView from "@/components/common/QuickView";
import { Navigation, Pagination } from "swiper/modules";
import { formatPrice } from "@/lib/format/price";
export default function Products2({ products }) {
  const source = products?.length ? products : products5;
  return (
    <section className="flat-spacing-6 pt-0">
      <div className="container">
        <div className="sect-top center text-center wow fadeInUp">
          <h2 className="s-title font-2">
            <span className="fst-italic">Trendy</span> COLLECTION
          </h2>
          <p className="s-sub-title">
            Discover the pieces our customers love most – from everyday
            essentials to statement styles that stand out. These are the
            <br className="d-none d-xl-block" />
            tried-and-true picks you’ll want in your wardrobe."
          </p>
        </div>
        <div className="tf-btn-swiper-item wow fadeInUp">
          <Swiper
            dir="ltr"
            className="swiper tf-swiper"
            breakpoints={{
              0: { slidesPerView: 2 },
              575: {
                slidesPerView: 2,
              },
              768: {
                slidesPerView: 3,
                spaceBetween: 20,
              },
              1200: {
                slidesPerView: 4,
                spaceBetween: 30,
              },
            }}
            spaceBetween={15}
            modules={[Navigation, Pagination]}
            pagination={{
              clickable: true,
              el: ".spd10",
            }}
            navigation={{
              prevEl: ".snbp10",
              nextEl: ".snbn10",
            }}
          >
            {source.map((product) => (
              <SwiperSlide className="swiper-slide" key={product.id}>
                <div
                  className={`card_product--V01 type-space-35 ${
                    product.outOfStock ? "out-of-stock" : ""
                  }`}
                >
                  <div className="card_product-wrapper">
                    <Link
                      href={`/${
                        product.outOfStock
                          ? "product-notify-avaiable"
                          : "product-default"
                      }/${product.slug ?? product.id}`}
                      className="product-img"
                    >
                      <Image
                        src={product.imgSrc}
                        alt="Image Product"
                        className="lazyload img-product"
                        width={714}
                        height={900}
                      />
                      <Image
                        src={product.hoverImgSrc}
                        alt="Image Product"
                        className="lazyload img-hover"
                        width={714}
                        height={900}
                      />
                    </Link>

                    {!product.outOfStock && product.source !== "api" && (
                      <ul className="list-product-btn">
                                                                  <li>
                        <QuickView product={product} />
                      </li>
                                          </ul>
                    )}

                    {product.badge && (
                      <div className="badge-box">
                        <span className={`badge-item ${product.badgeType}`}>
                          {product.badge}
                        </span>
                      </div>
                    )}

                    {product.outOfStock && (
                      <a
                        href="#unavailable"
                        data-bs-toggle="modal"
                        className="variant-box stock bg-main link text-white"
                      >
                        <p className="text-center d-none d-md-block">
                          {product.variantText ?? "Notify me when available"}
                        </p>
                        <p className="text-center d-md-none">Notify Me</p>
                      </a>
                    )}
                  </div>

                  <div className="card_product-info">
                    <Link
                      href={`/${
                        product.outOfStock
                          ? "product-notify-avaiable"
                          : "product-default"
                      }/${product.slug ?? product.id}`}
                      className="name-product h5 fw-normal link text-line-clamp-2"
                    >
                      {product.title}
                    </Link>
                    <div className="price-wrap">
                      <span className={`price-new h5 ${product.textColor ?? ""}`}>
                        {formatPrice(product.price)}
                      </span>
                      {product.oldPrice && (
                        <span className="price-old fw-normal">
                          {formatPrice(product.oldPrice)}
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              </SwiperSlide>
            ))}
            <div className="sw-dot-default tf-sw-pagination d-xl-none spd10" />
          </Swiper>
          <div className="nav-swiper style-2 nav-prev-swiper d-xl-flex d-none snbp10">
            <i className="icon-arrow-left" />
          </div>
          <div className="nav-swiper style-2 nav-next-swiper d-xl-flex d-none snbn10">
            <i className="icon-arrow-right" />
          </div>
        </div>
      </div>
    </section>
  );
}
