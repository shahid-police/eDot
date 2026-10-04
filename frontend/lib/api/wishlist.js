import { savedRequest } from "./saved";

export function getWishlist() {
  return savedRequest("/api/v1/wishlist");
}

export function addWishlistItem({ productId, variantId = null }) {
  return savedRequest("/api/v1/wishlist/items", {
    method: "POST",
    body: { productId, variantId },
  });
}

export function removeWishlistItem(itemId) {
  return savedRequest(`/api/v1/wishlist/items/${itemId}`, { method: "DELETE" });
}
