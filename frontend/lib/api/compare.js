import { savedRequest } from "./saved";

export function getCompare() {
  return savedRequest("/api/v1/compare");
}

export function addCompareItem(productId) {
  return savedRequest("/api/v1/compare/items", {
    method: "POST",
    body: { productId },
  });
}

export function removeCompareItem(itemId) {
  return savedRequest(`/api/v1/compare/items/${itemId}`, { method: "DELETE" });
}
