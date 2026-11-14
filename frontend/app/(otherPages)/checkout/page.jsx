import Footer2 from "@/components/footers/Footer2";
import Header2 from "@/components/headers/Header2";
import Checkout from "@/components/otherPages/Checkout";
import React from "react";
import Link from "next/link";
export const metadata = {
  title: "Checkout || E.",
  description: "The Choice of Elegant Women, Every Day",
};
export default function page() {
  return (
    <>
      <Header2 parentClass="tf-header style-2" />
      <section className="flat-spacing-2 pb-0">
        <div className="container">
          <div className="page-title">
            <div className="breadcrumbs">
              <ul className="bread-wrap">
                <li>
                  <Link href={`/`} className="text-main-4 link-secondary">
                    Home
                  </Link>
                </li>
                <li className="br-line w-12 bg-main" />
                <li>Cart</li>
              </ul>
              <h1 className="heading fw-normal text-uppercase">checkout</h1>
            </div>
          </div>
        </div>
      </section>
      <Checkout />
      <Footer2 />
    </>
  );
}
