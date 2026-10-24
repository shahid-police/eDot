import React from "react";
import Image from "next/image";
import Link from "next/link";
import Nav from "./Nav";
export default function Header2() {
  return (
    <header className="tf-header style-2 header-absolute">
      <div className="header-top">
        <div className="container">
          <div className="row align-items-center">
            <div className="col-md-4 col-3 d-xl-none">
              <a
                href="#mobileMenu"
                data-bs-toggle="offcanvas"
                className="btn-mobile-menu"
              >
                <span />
              </a>
            </div>
            <div className="col-xl-3 col-md-4 col-6">
              <Link href={`/`} className="logo-site">
                <Image
                  alt="E."
                  className="logo-black"
                  src="/images/logo/logo-transparent.png"
                  width={495}
                  height={383}
                  style={{ width: "90px", height: "auto" }}
                />
                <Image
                  alt="E."
                  className="logo-white"
                  src="/images/logo/logo-transparent.png"
                  width={495}
                  height={383}
                  style={{ width: "90px", height: "auto" }}
                />
              </Link>
            </div>
            <div className="col-xl-6 d-none d-xl-flex align-items-center justify-content-center">
              <span
                className="font-2 fst-italic text-center"
                style={{
                  fontSize: "26px",
                  letterSpacing: "0.04em",
                  color: "#7a6530",
                  whiteSpace: "nowrap",
                }}
              >
                The Choice of Elegant Women, Every Day
              </span>
            </div>
            <div className="col-xl-3 col-md-4 col-3">
              <ul className="nav-icon">
                <li className="d-inline-flex">
                  <a
                    href="#search"
                    data-bs-toggle="offcanvas"
                    className="nav-icon-item text-primary link"
                  >
                    <i className="icon icon-search" />
                  </a>
                </li>
                <li className="br-line d-none d-xl-flex" />
                <li className="d-none d-md-inline-flex">
                  <a
                    href="#log"
                    data-bs-toggle="modal"
                    className="nav-icon-item text-primary link"
                  >
                    <i className="icon icon-user" />
                  </a>
                </li>
                <li className="d-none d-md-inline-flex">
                  <Link
                    href={`/wishlist`}
                    className="nav-icon-item text-primary link"
                  >
                    <i className="icon icon-hearth" />
                  </Link>
                </li>
                <li className="d-inline-flex">
                  <a
                    href="#shoppingCart"
                    data-bs-toggle="offcanvas"
                    className="nav-icon-item text-primary link"
                  >
                    <i className="icon icon-cart" />
                    <span className="count-notice" />
                  </a>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>
      <div className="header-bottom d-none d-xl-block">
        <div className="container">
          <nav className="box-navigation">
            <ul className="box-nav-menu justify-content-start">
              <Nav megaMarginRight={false} />
            </ul>
          </nav>
        </div>
      </div>
    </header>
  );
}
