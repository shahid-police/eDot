import Footer2 from "@/components/footers/Footer2";
import Header2 from "@/components/headers/Header2";
import Banner from "@/components/homes/home-2/Banner";
import Blogs from "@/components/homes/home-2/Blogs";
import Categories from "@/components/homes/home-2/Categories";
import Collections from "@/components/homes/home-2/Collections";
import Cta from "@/components/homes/home-2/Cta";
import Features from "@/components/homes/home-2/Features";
import Hero from "@/components/homes/home-2/Hero";
import ShoppingPreferenceChips from "@/components/common/ShoppingPreferenceChips";
import Products1 from "@/components/homes/home-2/Products1";
import Products2 from "@/components/homes/home-2/Products2";
import Testimonials from "@/components/homes/home-2/Testimonials";
import React from "react";
import { getProducts, toTemplateCardProduct } from "@/lib/api/products";
import { loadTopLevelCategories } from "@/lib/api/categories";

export const metadata = {
  title: "Empress.pk || E.",
  description: "The Choice of Elegant Women, Every Day",
};

async function loadBestSellers() {
  try {
    const result = await getProducts({ isBestSeller: true, pageSize: 8 });
    return result.items.map(toTemplateCardProduct);
  } catch (error) {
    console.error("Product API unavailable, rendering template data:", error);
    return undefined;
  }
}

async function loadTrendyCollection() {
  try {
    const result = await getProducts({ isFeatured: true, pageSize: 8 });
    return result.items.map(toTemplateCardProduct);
  } catch (error) {
    console.error("Product API unavailable, rendering template data:", error);
    return undefined;
  }
}

export default async function page() {
  const [bestSellers, trendyCollection, topLevelCategories] = await Promise.all([
    loadBestSellers(),
    loadTrendyCollection(),
    loadTopLevelCategories(),
  ]);

  return (
    <>
      <Header2 />
      <div className="position-relative">
        <Hero />
        <div
          className="container"
          style={{ position: "absolute", left: 0, right: 0, bottom: 0, transform: "translateY(50%)", zIndex: 5 }}
        >
          <ShoppingPreferenceChips elevated />
        </div>
      </div>
      <div style={{ marginTop: 56 }}>
        <Products2 products={trendyCollection} />
      </div>

      <Products1 products={bestSellers} />
      <Collections />
      <Categories categories={topLevelCategories} />
      <Banner />
      <Features />
      
      <Cta />
      <Footer2 />
    </>
  );
}
