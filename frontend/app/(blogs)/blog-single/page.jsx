import BlogSingle from "@/components/blogs/BlogSingle";
import Footer2 from "@/components/footers/Footer2";
import Header2 from "@/components/headers/Header2";
import React from "react";
import Link from "next/link";
export const metadata = {
  title: "Blog Single || E.",
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
                <li>
                  <p className="text-main-4">Stories</p>
                </li>
                <li className="br-line w-12 bg-main" />
                <li>
                  <p>Behind the sparkle: Our iconic creations</p>
                </li>
              </ul>
              <h1 className="heading fw-normal text-uppercase">
                behind the sparkle: <br className="d-none d-xxl-block" />
                our iconic creations
              </h1>
            </div>
            <div className="box-text">
              <p className="text-main-4">
                Discover the stories behind our exquisite pieces—timeless
                craftsmanship, meaningful designs,
                <br className="d-none d-xxl-block" />
                and the moments that make them unforgettable.
              </p>
            </div>
          </div>
        </div>
      </section>
      <BlogSingle />
      <Footer2 />
    </>
  );
}
