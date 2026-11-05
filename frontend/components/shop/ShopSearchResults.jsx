"use client";
import { useState } from "react";
import NaturalSearchBox from "./NaturalSearchBox";
import Products1 from "./Products1";

export default function ShopSearchResults({ initialProducts }) {
  const [products, setProducts] = useState(initialProducts);

  return (
    <>
      <div className="container">
        <NaturalSearchBox onResults={setProducts} onClear={() => setProducts(initialProducts)} />
      </div>
      <Products1 products={products} />
    </>
  );
}
