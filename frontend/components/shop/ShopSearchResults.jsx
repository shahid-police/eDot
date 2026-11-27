"use client";
import { useEffect, useState } from "react";
import NaturalSearchBox from "./NaturalSearchBox";
import Products1 from "./Products1";
import ShoppingPreferenceChips from "../common/ShoppingPreferenceChips";

export default function ShopSearchResults({ initialProducts, activePreference = "" }) {
  const [products, setProducts] = useState(initialProducts);

  // Resync whenever the server hands us a new initialProducts array - e.g. the `preference` (or
  // `category`) query param changed and the page re-fetched, but this client component stays
  // mounted across that navigation, so its own state would otherwise go stale.
  useEffect(() => {
    setProducts(initialProducts);
  }, [initialProducts]);

  return (
    <>
      <div className="container">
        <ShoppingPreferenceChips active={activePreference} className="mb-4" />
        <NaturalSearchBox onResults={setProducts} onClear={() => setProducts(initialProducts)} />
      </div>
      <Products1 products={products} />
    </>
  );
}
