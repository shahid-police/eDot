const STORAGE_KEY = "elegance_recently_viewed";
const MAX_ITEMS = 20;

// Browser-local view history - there is no backend endpoint for this, it's purely a per-device
// convenience, so localStorage is the right home for it (not useSavedProducts' API-backed pattern).
export function recordRecentlyViewed(product) {
  if (typeof window === "undefined" || !product?.id) return;
  try {
    const existing = JSON.parse(window.localStorage.getItem(STORAGE_KEY) ?? "[]");
    const next = [product, ...existing.filter((item) => item.id !== product.id)].slice(
      0,
      MAX_ITEMS
    );
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  } catch {
    // Private browsing / storage disabled - recently viewed is a nice-to-have, fail silently.
  }
}

export function getRecentlyViewed(excludeId) {
  if (typeof window === "undefined") return [];
  try {
    const items = JSON.parse(window.localStorage.getItem(STORAGE_KEY) ?? "[]");
    return items.filter((item) => item.id !== excludeId);
  } catch {
    return [];
  }
}
