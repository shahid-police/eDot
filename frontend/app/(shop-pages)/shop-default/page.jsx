import Features from "@/components/common/Features";
import Footer2 from "@/components/footers/Footer2";
import Header2 from "@/components/headers/Header2";

import ShopSearchResults from "@/components/shop/ShopSearchResults";
import React from "react";
import { getProducts, toTemplateCardProduct } from "@/lib/api/products";
export const metadata = {
  title: "Shop || E.",
  description: "The Choice of Elegant Women, Every Day",
};

async function loadProducts(category, shoppingPreference) {
  try {
    const result = await getProducts({ pageSize: 100, category, shoppingPreference });
    return result.items.map(toTemplateCardProduct);
  } catch (error) {
    console.error("Product API unavailable, rendering template data:", error);
    return undefined;
  }
}

export default async function page({ searchParams }) {
  const { category, preference } = await searchParams;
  const products = await loadProducts(category, preference);

  return (
    <>
      <Header2 parentClass="tf-header style-2" />
      <ShopSearchResults initialProducts={products} activePreference={preference ?? ""} />
      <Features
        parentClass="flat-spacing-8 bg-main"
        iconColorClass="text-primary"
      />
      <Footer2 />
    </>
  );
}
