import { savedRequest } from "./saved";

export function getCheckoutSummary() {
  return savedRequest("/api/v1/checkout/summary");
}

export function placeOrder(payload) {
  return savedRequest("/api/v1/orders", { method: "POST", body: payload });
}
