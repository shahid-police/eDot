import Features from "@/components/common/Features";
import Footer1 from "@/components/footers/Footer1";
import Header1 from "@/components/headers/Header1";
import Topbar1 from "@/components/headers/Topbar1";
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
import { loadApiProductBySlug } from "@/lib/api/products";

const DEFAULT_METADATA = {
  title: "Product Details || Vemus - Jewelry Ecommerce React Nextjs Template",
  description: "Vemus - Jewelry Ecommerce React Nextjs Template",
};

export async function generateMetadata({ params }) {
  const { slug } = await params;
  const product = await loadApiProductBySlug(slug);
  if (!product) {
    return DEFAULT_METADATA;
  }
  return {
    title: `${product.name} || Vemus`,
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

  return (
    <>
      <Topbar1 parentClass="tf-topbar bg-dark-olive" />
      <Header1 parentClass="tf-header" />
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
      <RelatedProducts />
      <RecentProducts />
      <Features
        parentClass="flat-spacing-8 bg-main"
        iconColorClass="text-primary"
      />
      <Footer1 />
    </>
  );
}
