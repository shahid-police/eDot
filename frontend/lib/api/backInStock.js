import { apiRequest } from "./client";

export function subscribeBackInStock({ productId, productVariantId, customerName, whatsAppNumber, consentGiven }) {
  return apiRequest("/api/v1/back-in-stock/subscribe", {
    method: "POST",
    body: {
      productId,
      productVariantId: productVariantId ?? null,
      customerName,
      whatsAppNumber,
      consentGiven,
    },
  });
}
