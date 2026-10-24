"use client";
import Image from "next/image";
import Link from "next/link";
import React from "react";
import { otherPages } from "@/data/menu";
import { usePathname } from "next/navigation";

export default function Nav({ megaMarginRight = true }) {
  const pathname = usePathname();
  const isMenuActive = (link) => {
    return link.href?.split("/")[1] == pathname.split("/")[1];
  };
  return (
    <>
      <li
        className={`menu-item ${pathname.split("/")[1] === "home-2" || pathname === "/" ? "active" : ""}`}
      >
        <Link href="/" className="item-link">
          Home
        </Link>
      </li>
      <li
        className={`menu-item ${pathname.split("/")[1] === "shop-default" ? "active" : ""}`}
      >
        <Link href="/shop-default" className="item-link">
          Shop
        </Link>
      </li>
      <li
        className={`sub-menu mega-menu mega-menu-page container-layout-right${
          megaMarginRight ? "-3" : ""
        } mega-menu-style-2`}
      >
        <a href="#" className="item-link">
          Pages
          <i className="icon icon-arrow-angle-down" />
        </a>
        <div className="sub-menu mega-menu mega-menu-page container-layout-right-3 mega-menu-style-2">
          <div className="mega-menu-wrap">
            <div className="wrapper-sub-menu">
              <div className="mega-menu-item">
                <p className="text-caption menu-heading">PAGES</p>
                <ul className="menu-list">
                  {otherPages.map((link, index) => (
                    <li key={index}>
                      <Link
                        href={link.href}
                        className={`menu-link-text link ${
                          isMenuActive(link) ? "active" : ""
                        }`}
                      >
                        {link.label}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
            <div className="wrapper-sub-collection gap-0">
              <div className="box_image--V01 h-100 hover-img">
                <div className="image h-100 img-style">
                  <Image
                    src="/images/collections/cls-header-2.jpg"
                    alt=""
                    className="lazyload"
                    width={792}
                    height={914}
                  />
                </div>
                <div className="content">
                  <h5 className="box-text fw-medium text-white">Most Gifted</h5>
                  <div className="box-btn">
                    <Link
                      href={`/shop-default`}
                      className="tf-btn-line style-white text-uppercase"
                    >
                      <span className="text-caption lh-28">Shop Now</span>
                      <i className="icon-arrow-top-right-2 fs-10" />
                    </Link>
                  </div>
                </div>
              </div>
              <div className="box_image--V01 h-100 hover-img">
                <div className="image h-100 img-style">
                  <Image
                    src="/images/collections/cls-header-3.jpg"
                    alt=""
                    className="lazyload"
                    width={792}
                    height={914}
                  />
                </div>
                <div className="content">
                  <h5 className="box-text fw-medium text-white">
                    <span>Flash Sale</span>
                    <span className="br-line w-22 bg-white d-block" />
                    <span>30% OFF</span>
                  </h5>
                  <div className="box-btn">
                    <Link
                      href={`/shop-default`}
                      className="tf-btn-line style-white text-uppercase"
                    >
                      <span className="text-caption lh-28">Shop Now</span>
                      <i className="icon-arrow-top-right-2 fs-10" />
                    </Link>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </li>
    </>
  );
}
