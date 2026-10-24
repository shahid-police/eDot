"use client";

import Link from "next/link";
import { otherPages } from "@/data/menu";

export default function MobileNav() {
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
          href="https://themeforest.net/user/themesflat"
          className="mb-menu-link"
        >
          Buy Theme
        </a>
      </li>
    </>
  );
}
