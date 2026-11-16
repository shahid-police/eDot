"use client";
import { products14 } from "@/data/products";
import React from "react";
import { Swiper, SwiperSlide } from "swiper/react";
import Link from "next/link";
import Image from "next/image";
import QuickView from "../common/QuickView";
import { Navigation, Pagination } from "swiper/modules";
import { formatPrice } from "@/lib/format/price";
export default function RelatedProducts({ containerFull = false, products }) {
  const source = products?.length ? products : products14;
  return (
    <section className="flat-spacing-3">
      <div className={`container${containerFull ? "-full" : ""}`}>
        <div className="sect-top wow fadeInUp">
          <h3 className="s-title">YOU MAY ALSO LIKE</h3>
          <div className="group-btn-slider">
            <div className="nav-prev-swiper tf-sw-nav snbp46">
              <i className="icon-arrow-left" />
            </div>
            <div className="nav-next-swiper tf-sw-nav snbn46">
              <i className="icon-arrow-right" />
            </div>
          </div>
        </div>
        <Swiper
          dir="ltr"
          className="swiper tf-swiper wow fadeInUp"
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
            el: ".spd46",
          }}
          navigation={{
            prevEl: ".snbp46",
            nextEl: ".snbn46",
          }}
        >
          {source.map((product) => (
            <SwiperSlide className="swiper-slide" key={product.id}>
              <div className="card_product--V01">
                <div className="card_product-wrapper">
                  <Link
                    href={`/product-default/${product.slug ?? product.id}`}
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
                  <ul className="list-product-btn">
                                                            <li>
                      <QuickView product={product} />
                    </li>
                                      </ul>
                </div>
                <div className="card_product-info">
                  <Link
                    href={`/product-default/${product.slug ?? product.id}`}
                    className="name-product h5 fw-normal link text-line-clamp-2"
                  >
                    {product.title}
                  </Link>
                  <div className="price-wrap">
                    <span className="price-new h5">
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

          <div className="sw-dot-default tf-sw-pagination d-xl-none spd46" />
        </Swiper>
      </div>
    </section>
  );
}
