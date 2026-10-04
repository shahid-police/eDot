import { apiGet } from "./client";

const FALLBACK_IMAGE = "/images/products/product-6.jpg";

export function getProducts(query = {}) {
  return apiGet("/api/v1/products", { query });
}

export function getProductBySlug(slug) {
  return apiGet(`/api/v1/products/${encodeURIComponent(slug)}`);
}

/**
 * Adapts an API ProductListItemDto to the card shape the Vemus shop grid renders.
 * Fields the API does not model yet (size, multi-colour facets) are left empty.
 */
export function toTemplateCardProduct(dto) {
  const onSale = dto.salePrice !== null && dto.salePrice !== undefined;
  const imageUrl = dto.imageUrl ?? FALLBACK_IMAGE;

  return {
    id: dto.id,
    slug: dto.slug,
    source: "api",
    title: dto.name,
    price: onSale ? dto.salePrice : dto.price,
    oldPrice: onSale ? dto.price : null,
    imgSrc: imageUrl,
    hoverImgSrc: dto.hoverImageUrl ?? imageUrl,
    outOfStock: !dto.inStock,
    availability: dto.inStock ? "In stock" : "Out of stock",
    category: dto.categorySlug,
    material: dto.material ?? undefined,
    filterColors: dto.color ? [dto.color.toLowerCase().replace(/\s+/g, "-")] : [],
    badge: dto.isNew ? "NEW IN" : undefined,
    badgeType: dto.isNew ? "new" : undefined,
  };
}
