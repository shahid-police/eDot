import { apiRequest } from "./client";
import { getSession } from "@/lib/auth/session";
import { ensureFreshSession } from "./auth";

// Cart endpoints accept anonymous requests, so an expired access token would
// silently fall back to the guest cart. Refresh first to keep the right cart.
async function cartRequest(path, options = {}) {
  await ensureFreshSession();
  return apiRequest(path, { ...options, token: getSession()?.accessToken });
}

export function getCart() {
  return cartRequest("/api/v1/cart");
}

export function addCartItem({ productId, variantId = null, quantity = 1 }) {
  return cartRequest("/api/v1/cart/items", {
    method: "POST",
    body: { productId, variantId, quantity },
  });
}

export function updateCartItem(itemId, quantity) {
  return cartRequest(`/api/v1/cart/items/${itemId}`, {
    method: "PUT",
    body: { quantity },
  });
}

export function removeCartItem(itemId) {
  return cartRequest(`/api/v1/cart/items/${itemId}`, { method: "DELETE" });
}
