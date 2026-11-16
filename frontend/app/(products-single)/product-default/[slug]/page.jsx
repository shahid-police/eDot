import Features from "@/components/common/Features";
import Footer2 from "@/components/footers/Footer2";
import Header2 from "@/components/headers/Header2";
import ApiProductDetail from "@/components/product-details/ApiProductDetail";
import Descriptions1 from "@/components/product-details/Descriptions1";
import Details1 from "@/components/product-details/Details1";
import RecentProducts from "@/components/product-details/RecentProducts";
import RelatedProducts from "@/components/product-details/RelatedProducts";
import React from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { allProducts } from "@/data/products";
import TextSlider from "@/components/common/TextSlider3";
import { loadApiProductBySlug, getProducts, toTemplateCardProduct } from "@/lib/api/products";

const DEFAULT_METADATA = {
  title: "Product Details || E.",
  description: "The Choice of Elegant Women, Every Day",
};

async function loadRelatedProducts(excludeSlug) {
  try {
    const result = await getProducts({ pageSize: 20 });
    return result.items
      .filter((item) => item.inStock && item.slug !== excludeSlug)
      .map(toTemplateCardProduct)
      .sort(() => Math.random() - 0.5);
  } catch (error) {
    console.error("Product API unavailable, rendering template data:", error);
    return undefined;
  }
}

export async function generateMetadata({ params }) {
  const { slug } = await params;
  const product = await loadApiProductBySlug(slug);
  if (!product) {
    return DEFAULT_METADATA;
  }
  return {
    title: `${product.name} || E.`,
    description: product.shortDescription ?? DEFAULT_METADATA.description,
  };
}

export default async function ProductDetailPage({ params }) {
  const { slug } = await params;

  const apiProduct = await loadApiProductBySlug(slug);
  const templateProduct = apiProduct
    ? null
    : allProducts.find((p) => String(p.id) === slug);

  if (!apiProduct && !templateProduct) {
    notFound();
  }

  const relatedProducts = await loadRelatedProducts(apiProduct?.slug ?? slug);

  return (
    <>
      <Header2 parentClass="tf-header style-2" />
      <div className="flat-spacing-16 pb-0">
        <div className="container">
          <div className="page-title border-0">
            <div className="breadcrumbs">
              <ul className="bread-wrap mb-0">
                <li>
                  <Link href={`/`} className="text-main-4 link">
                    Home
                  </Link>
                </li>
                <li className="br-line w-12 bg-main" />
                <li>
                  <Link href={`/shop-default`} className="text-main-4 link">
                    Shop
                  </Link>
                </li>
                {apiProduct && (
                  <>
                    <li className="br-line w-12 bg-main" />
                    <li>
                      <p>{apiProduct.categoryName}</p>
                    </li>
                  </>
                )}
              </ul>
            </div>
          </div>
        </div>
      </div>
      {apiProduct ? (
        <ApiProductDetail product={apiProduct} />
      ) : (
        <Details1 product={templateProduct} />
      )}
      {!apiProduct && <Descriptions1 />}
      <TextSlider />
      <RelatedProducts products={relatedProducts} />
      <RecentProducts />
      <Features
        parentClass="flat-spacing-8 bg-main"
        iconColorClass="text-primary"
      />
      <Footer2 />
    </>
  );
}
