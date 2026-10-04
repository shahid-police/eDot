"use client";
import { useCallback, useEffect, useState } from "react";
import { getCart, removeCartItem, updateCartItem } from "@/lib/api/cart";

export function useServerCart(offcanvasId) {
  const [cart, setCart] = useState(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  const reload = useCallback(async () => {
    try {
      setError("");
      setCart(await getCart());
    } catch (err) {
      setError(err.message || "We could not load your cart.");
    }
  }, []);

  useEffect(() => {
    reload();
    const element = offcanvasId ? document.getElementById(offcanvasId) : null;
    if (!element) {
      return undefined;
    }
    element.addEventListener("show.bs.offcanvas", reload);
    return () => element.removeEventListener("show.bs.offcanvas", reload);
  }, [offcanvasId, reload]);

  const change = useCallback(async (itemId, quantity) => {
    setBusy(true);
    setError("");
    try {
      setCart(quantity === null ? await removeCartItem(itemId) : await updateCartItem(itemId, quantity));
    } catch (err) {
      setError(err.message || "We could not update your cart.");
    } finally {
      setBusy(false);
    }
  }, []);

  const clear = useCallback(async () => {
    setBusy(true);
    setError("");
    try {
      let latest = null;
      for (const item of cart?.items ?? []) {
        latest = await removeCartItem(item.id);
      }
      if (latest) {
        setCart(latest);
      }
    } catch (err) {
      setError(err.message || "We could not empty your cart.");
    } finally {
      setBusy(false);
    }
  }, [cart]);

  return {
    cart,
    items: cart?.items ?? [],
    error,
    busy,
    change,
    clear,
  };
}
