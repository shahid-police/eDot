import React from "react";
import Image from "next/image";
import Link from "next/link";
import Topbar2 from "./Topbar2";

// transparent=true gives the "float over a hero banner" look (used on home-2, which has a
// full-bleed Hero slideshow right below it): no background, pulled up via header-absolute's
// negative margin, and a white logo variant that only shows before the page scrolls. Every other
// page passes transparent=false (the default) to get a normal solid header instead, since they
// don't have a hero banner to overlay and the negative margin would otherwise hide their content
// under the header.
//
// Topbar2 (the "Discover Yourself..." announcement strip) is rendered here, as a normal sibling
// before <header>, rather than left for each page to import - Header2 is used on every page, so
// this is what makes the strip sitewide instead of home-2-only.
export default function Header2({ parentClass = "tf-header style-2", transparent = false }) {
  return (
    <>
      <Topbar2 />
      <header className={transparent ? `${parentClass} header-absolute` : parentClass}>
      {/* Hidden below xl (phone and tablet both), by request - every column inside it (menu
          button, logo, search/wishlist/cart icons) was already hidden there, so this collapses
          the row's own vertical space too rather than leaving an empty bar. The bottom tab bar
          (Shop/Search/Account/Wishlist/Cart) already covers these on phone/tablet anyway. */}
      <div className="header-top d-none d-xl-block">
        <div className="container">
          <div className="row align-items-center">
            {/* The menu-open button used to show below xl (phone/tablet); it's now hidden at
                every width by request, since the desktop header-bottom nav (xl+) never used it
                either. Left out of the markup entirely rather than kept as permanently d-none.
                The "E." text mark that used to sit here was removed by request, replaced with
                Shop/Search icons mirroring the mobile toolbar's left-hand tabs. */}
            <div className="col-xl-3 d-none d-xl-flex align-items-center">
              <ul className="nav-icon">
                <li className="d-inline-flex">
                  <Link
                    href="/shop-default"
                    className="nav-icon-item text-primary link"
                  >
                    <i className="icon icon-menu-home" />
                  </Link>
                </li>
                <li className="br-line d-none d-xl-flex" />
                <li className="d-inline-flex">
                  <Link
                    href="/no-shop"
                    className="nav-icon-item text-primary link"
                  >
                    <span className="icon-crossed">
                      <i className="icon icon-shop-cart" />
                    </span>
                  </Link>
                </li>
              </ul>
            </div>
            {/* Hidden below xl (phone and tablet both) - the decorative logo image only shows
                at desktop width now, by request. */}
            <div className="col-xl-6 d-none d-xl-flex justify-content-center">
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
            {/* col-12 below xl so this is the only thing in the row (hamburger and logo are both
                hidden there, at phone and tablet widths) - it fills the row and sits flush right. */}
            <div className="col-12 col-xl-3 d-flex justify-content-end">
              <ul className="nav-icon">
                <li className="d-none d-md-inline-flex">
                  <Link
                    href={`/wishlist`}
                    className="nav-icon-item text-primary link"
                  >
                    <i className="icon icon-hearth" />
                  </Link>
                </li>
                <li className="br-line d-none d-xl-flex" />
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
      </header>
    </>
  );
}
