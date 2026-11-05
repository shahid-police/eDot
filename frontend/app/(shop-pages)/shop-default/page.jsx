import Features from "@/components/common/Features";
import Footer1 from "@/components/footers/Footer1";
import Header1 from "@/components/headers/Header1";
import Topbar1 from "@/components/headers/Topbar1";
import Categories from "@/components/shop/Categories";

import ShopSearchResults from "@/components/shop/ShopSearchResults";
import React from "react";
import { getProducts, toTemplateCardProduct } from "@/lib/api/products";
export const metadata = {
  title: "Shop || E.",
  description: "The Choice of Elegant Women, Every Day",
};

async function loadProducts(category) {
  try {
    const result = await getProducts({ pageSize: 100, category });
    return result.items.map(toTemplateCardProduct);
  } catch (error) {
    console.error("Product API unavailable, rendering template data:", error);
    return undefined;
  }
}

export default async function page({ searchParams }) {
  const { category } = await searchParams;
  const products = await loadProducts(category);

  return (
    <>
      <Topbar1 parentClass="tf-topbar bg-dark-olive" />
      <Header1 parentClass="tf-header" />
      <Categories />
      <ShopSearchResults initialProducts={products} />
      <Features
        parentClass="flat-spacing-8 bg-main"
        iconColorClass="text-primary"
      />
      <Footer1 />
    </>
  );
}
