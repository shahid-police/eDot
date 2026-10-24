import Footer2 from "@/components/footers/Footer2";
import Header2 from "@/components/headers/Header2";
import Topbar2 from "@/components/headers/Topbar2";
import Banner from "@/components/homes/home-2/Banner";
import Blogs from "@/components/homes/home-2/Blogs";
import Categories from "@/components/homes/home-2/Categories";
import Collections from "@/components/homes/home-2/Collections";
import Cta from "@/components/homes/home-2/Cta";
import Features from "@/components/homes/home-2/Features";
import Hero from "@/components/homes/home-2/Hero";
import Products1 from "@/components/homes/home-2/Products1";
import Products2 from "@/components/homes/home-2/Products2";
import Testimonials from "@/components/homes/home-2/Testimonials";
import BeforeLeave from "@/components/modals/BeforeLeave";
import React from "react";
import { getProducts, toTemplateCardProduct } from "@/lib/api/products";

export const metadata = {
  title: "Home 02 || E.",
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

async function loadRecommended() {
  try {
    const result = await getProducts({ isNew: true, pageSize: 4 });
    return result.items.map(toTemplateCardProduct);
  } catch (error) {
    console.error("Product API unavailable, rendering template data:", error);
    return undefined;
  }
}

export default async function page() {
  const [bestSellers, trendyCollection, recommended] = await Promise.all([
    loadBestSellers(),
    loadTrendyCollection(),
    loadRecommended(),
  ]);

  return (
    <>
      <Topbar2 />
      <Header2 />
      <Hero />
      <Categories />
      <Products1 products={bestSellers} />
      <Collections />
      <Products2 products={trendyCollection} />
      <Banner />
      <Features />
      <Testimonials />
      <Blogs />
      <Cta />
      <Footer2 />
      <BeforeLeave products={recommended} />
    </>
  );
}
