import { savedRequest } from "./saved";

export function getOrders() {
  return savedRequest("/api/v1/orders");
}

export function getOrder(orderId) {
  return savedRequest(`/api/v1/orders/${orderId}`);
}

export function getOrderByNumber(orderNumber) {
  return savedRequest(`/api/v1/orders/by-number/${encodeURIComponent(orderNumber)}`);
}
