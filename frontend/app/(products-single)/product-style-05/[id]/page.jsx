import Features from "@/components/common/Features";
import Footer1 from "@/components/footers/Footer1";
import Header2 from "@/components/headers/Header2";

import RecentProducts from "@/components/product-details/RecentProducts";
import RelatedProducts from "@/components/product-details/RelatedProducts";
import React from "react";

import { allProducts } from "@/data/products";
import TextSlider from "@/components/common/TextSlider3";

import Details5 from "@/components/product-details/Details5";
export const metadata = {
  title: "Product Details || E.",
  description: "The Choice of Elegant Women, Every Day",
};
export default async function ProductDetailPage({ params }) {
  const { id } = await params;

  const product = allProducts.filter((p) => p.id == id)[0] || allProducts[0];
  return (
    <>
      <Header2 parentClass="tf-header style-2" />

      <Details5 product={product} />

      <TextSlider />
      <RelatedProducts containerFull />
      <RecentProducts containerFull />

      <Footer1 />
    </>
  );
}
