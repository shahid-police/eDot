"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  blogMenuLinks,
  otherPages,
  productDetailPages,
} from "@/data/menu";

export default function MobileNav() {
  const pathname = usePathname();
  const isMenuActive = (link) => {
    return link.href?.split("/")[1] == pathname.split("/")[1];
  };
  const isMenuParentActive = (menu) => {
    return menu.some((elm) => isMenuActive(elm));
  };
  const isMenuParentActive2 = (menu) => {
    return menu.some((elm) => isMenuParentActive(elm.links));
  };
  return (
    <>
      {" "}
      <li className="nav-mb-item">
        <Link href="/" className="mb-menu-link">
          <span>Home</span>
        </Link>
      </li>
      <li className="nav-mb-item">
        <Link href="/shop-default" className="mb-menu-link">
          <span>Shop</span>
        </Link>
      </li>
      <li className="nav-mb-item">
        <a
          href="#dropdown-menu-product"
          className="collapsed mb-menu-link"
          data-bs-toggle="collapse"
          aria-expanded="true"
          aria-controls="dropdown-menu-product"
        >
          <span>Products</span>
          <span className="btn-open-sub" />
        </a>
        <div id="dropdown-menu-product" className="collapse">
          <ul className="sub-nav-menu">
            {productDetailPages.map((section, sectionIndex) => (
              <li key={sectionIndex}>
                <a
                  href={`#sub-product-layout${sectionIndex}`}
                  className="sub-nav-link collapsed"
                  data-bs-toggle="collapse"
                  aria-expanded="true"
                  aria-controls={`sub-product-layout${sectionIndex}`}
                >
                  <span> {section.heading}</span>
                  <span className="btn-open-sub" />
                </a>
                <div
                  id={`sub-product-layout${sectionIndex}`}
                  className="collapse"
                >
                  <ul className="sub-nav-menu sub-menu-level-2">
                    {section.links.map((link, linkIndex) => (
                      <li key={linkIndex}>
                        <Link
                          href={link.href2 ? link.href2 : link.href}
                          className="sub-nav-link"
                        >
                          {link.label}
                          {link.badge && (
                            <span
                              className={`demo-label ${
                                link.badgeType || ""
                              }`.trim()}
                            >
                              {link.badge}
                            </span>
                          )}
                        </Link>
                      </li>
                    ))}
                  </ul>
                </div>
              </li>
            ))}
          </ul>
        </div>
      </li>
      <li className="nav-mb-item">
        <a
          href="#dropdown-menu-pages"
          className="collapsed mb-menu-link"
          data-bs-toggle="collapse"
          aria-expanded="true"
          aria-controls="dropdown-menu-pages"
        >
          <span>Pages</span>
          <span className="btn-open-sub" />
        </a>
        <div id="dropdown-menu-pages" className="collapse">
          <ul className="sub-nav-menu">
            {otherPages.map((link, index) => (
              <li key={index}>
                <Link href={link.href} className="sub-nav-link">
                  {link.label}
                </Link>
              </li>
            ))}
          </ul>
        </div>
      </li>
      <li className="nav-mb-item">
        <a
          href="#dropdown-menu-blog"
          className="collapsed mb-menu-link"
          data-bs-toggle="collapse"
          aria-expanded="true"
          aria-controls="dropdown-menu-blog"
        >
          <span>Blog</span>
          <span className="btn-open-sub" />
        </a>
        <div id="dropdown-menu-blog" className="collapse">
          <ul className="sub-nav-menu">
            {blogMenuLinks.map((link, index) => (
              <li key={index}>
                <Link href={link.href} className="sub-nav-link">
                  {link.label}
                </Link>
              </li>
            ))}
          </ul>
        </div>
      </li>
      <li className="nav-mb-item">
        <a
          href="https://themeforest.net/user/themesflat"
          className="mb-menu-link"
        >
          Buy Theme
        </a>
      </li>
    </>
  );
}
