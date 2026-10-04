import { ApiError, apiRequest } from "./client";
import { getSession } from "@/lib/auth/session";
import { ensureFreshSession } from "./auth";

async function wishlistRequest(path, options = {}) {
  await ensureFreshSession();
  const token = getSession()?.accessToken;
  if (!token) {
    throw new ApiError(401, path, "Please log in to use your wishlist.");
  }
  return apiRequest(path, { ...options, token });
}

export function getWishlist() {
  return wishlistRequest("/api/v1/wishlist");
}

export function addWishlistItem(productId) {
  return wishlistRequest("/api/v1/wishlist/items", {
    method: "POST",
    body: { productId },
  });
}

export function removeWishlistItem(itemId) {
  return wishlistRequest(`/api/v1/wishlist/items/${itemId}`, { method: "DELETE" });
}
