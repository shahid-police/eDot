import { savedRequest } from "./saved";

export function getCheckoutSummary() {
  return savedRequest("/api/v1/checkout/summary");
}

export function applyPromoCode(code) {
  return savedRequest("/api/v1/checkout/promo-code", { method: "POST", body: { code } });
}

export function removePromoCode() {
  return savedRequest("/api/v1/checkout/promo-code", { method: "DELETE" });
}

export function placeOrder(payload) {
  return savedRequest("/api/v1/orders", { method: "POST", body: payload });
}
