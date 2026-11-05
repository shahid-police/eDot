import { apiRequest } from "./client";
import { toTemplateCardProduct } from "./products";

export async function searchProductsNatural(query) {
  const data = await apiRequest("/api/v1/search/natural", {
    method: "POST",
    body: { query },
  });

  return {
    appliedFilters: data?.appliedFilters ?? null,
    products: (data?.products?.items ?? []).map(toTemplateCardProduct),
    totalCount: data?.products?.totalCount ?? 0,
  };
}
