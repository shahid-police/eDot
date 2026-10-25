"use client";

import { useCallback, useEffect, useState } from "react";
import { addWishlistItem, removeWishlistItem, getWishlist } from "@/lib/api/wishlist";
import { addCompareItem, removeCompareItem, getCompare } from "@/lib/api/compare";

// Fetches the wishlist/compare lists once per grid (not once per card - a shop grid can render
// dozens of ProductQuickActions instances, and each firing its own GET would flood the page with
// duplicate requests) and exposes productId -> item lookups plus toggle handlers.
export default function useSavedProducts() {
  const [wishlistMap, setWishlistMap] = useState(new Map());
  const [compareMap, setCompareMap] = useState(new Map());

  useEffect(() => {
    let active = true;
    getWishlist()
      .then((data) => {
        if (active) setWishlistMap(new Map(data.items.map((item) => [item.productId, item.id])));
      })
      .catch(() => {});
    getCompare()
      .then((data) => {
        if (active) setCompareMap(new Map(data.items.map((item) => [item.productId, item.id])));
      })
      .catch(() => {});
    return () => {
      active = false;
    };
  }, []);

  const toggleWishlist = useCallback(
    async (product) => {
      const existingItemId = wishlistMap.get(product.id);
      if (existingItemId) {
        await removeWishlistItem(existingItemId);
        setWishlistMap((prev) => {
          const next = new Map(prev);
          next.delete(product.id);
          return next;
        });
      } else {
        const data = await addWishlistItem({ productId: product.id });
        setWishlistMap(new Map(data.items.map((item) => [item.productId, item.id])));
      }
    },
    [wishlistMap]
  );

  const toggleCompare = useCallback(
    async (product) => {
      const existingItemId = compareMap.get(product.id);
      if (existingItemId) {
        await removeCompareItem(existingItemId);
        setCompareMap((prev) => {
          const next = new Map(prev);
          next.delete(product.id);
          return next;
        });
      } else {
        const data = await addCompareItem(product.id);
        setCompareMap(new Map(data.items.map((item) => [item.productId, item.id])));
      }
    },
    [compareMap]
  );

  return { wishlistMap, compareMap, toggleWishlist, toggleCompare };
}
