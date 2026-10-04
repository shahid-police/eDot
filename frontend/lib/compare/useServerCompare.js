"use client";
import { useCallback, useEffect, useState } from "react";
import { getCompare, removeCompareItem } from "@/lib/api/compare";

export function useServerCompare(modalId) {
  const [compare, setCompare] = useState(null);
  const [error, setError] = useState("");

  const reload = useCallback(async () => {
    try {
      setError("");
      setCompare(await getCompare());
    } catch (err) {
      setError(err.message || "We could not load your comparison.");
    }
  }, []);

  useEffect(() => {
    reload();
    const element = modalId ? document.getElementById(modalId) : null;
    if (!element) {
      return undefined;
    }
    element.addEventListener("show.bs.modal", reload);
    return () => element.removeEventListener("show.bs.modal", reload);
  }, [modalId, reload]);

  const remove = useCallback(async (itemId) => {
    setError("");
    try {
      setCompare(await removeCompareItem(itemId));
    } catch (err) {
      setError(err.message || "We could not update your comparison.");
    }
  }, []);

  const clear = useCallback(async () => {
    setError("");
    try {
      let latest = compare;
      for (const item of compare?.items ?? []) {
        latest = await removeCompareItem(item.id);
      }
      setCompare(latest);
    } catch (err) {
      setError(err.message || "We could not clear your comparison.");
    }
  }, [compare]);

  return { compare, items: compare?.items ?? [], error, remove, clear, reload };
}
