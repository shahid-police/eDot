import React from "react";
import Image from "next/image";
import Link from "next/link";
import Nav from "./Nav";

// transparent=true gives the "float over a hero banner" look (used on home-2, which has a
// full-bleed Hero slideshow right below it): no background, pulled up via header-absolute's
// negative margin, and a white logo variant that only shows before the page scrolls. Every other
// page passes transparent=false (the default) to get a normal solid header instead, since they
// don't have a hero banner to overlay and the negative margin would otherwise hide their content
// under the header.
export default function Header2({ parentClass = "tf-header style-2", transparent = false }) {
  return (
    <header className={transparent ? `${parentClass} header-absolute` : parentClass}>
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
            <div className="col-xl-3 d-none d-xl-flex align-items-center">
              <Link
                href={`/`}
                className="header-e-mark font-2 fst-italic"
                style={{
                  fontSize: "32px",
                  letterSpacing: "0.04em",
                  color: "#7a6530",
                  whiteSpace: "nowrap",
                }}
              >
                E.
              </Link>
            </div>
            <div className="col-xl-6 col-md-4 col-6 d-flex justify-content-center">
              <Link href={`/`} className="logo-site">
                {transparent ? (
                  <>
                    <span className="logo-shine d-inline-block">
                      <Image
                        alt="E."
                        className="logo-black"
                        src="/images/logo/logo-transparent.png"
                        width={495}
                        height={383}
                        style={{ width: "90px", height: "auto" }}
                      />
                    </span>
                    <span className="logo-shine d-inline-block">
                      <Image
                        alt="E."
                        className="logo-white"
                        src="/images/logo/logo-transparent.png"
                        width={495}
                        height={383}
                        style={{ width: "90px", height: "auto" }}
                      />
                    </span>
                  </>
                ) : (
                  <span className="logo-shine d-inline-block">
                    <Image
                      alt="E."
                      src="/images/logo/logo-transparent.png"
                      width={495}
                      height={383}
                      style={{ width: "90px", height: "auto" }}
                    />
                  </span>
                )}
              </Link>
            </div>
            <div className="col-xl-3 col-md-4 col-3">
              <ul className="nav-icon">
                <li className="d-inline-flex">
                  <Link
                    href="/shop-default"
                    className="nav-icon-item text-primary link"
                  >
                    <i className="icon icon-search" />
                  </Link>
                </li>
                <li className="br-line d-none d-xl-flex" />
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
