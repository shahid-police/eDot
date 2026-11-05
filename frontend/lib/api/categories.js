import { apiGet } from "./client";

// Real local jewellery photography available in /public/images/collections.
// Keyed by category slug so each real category gets a visually fitting image.
const CATEGORY_IMAGES = {
  rings: { src: "/images/collections/cls-18.jpg", width: 800, height: 800 },
  necklaces: { src: "/images/collections/cls-22.jpg", width: 800, height: 800 },
  earrings: { src: "/images/collections/cls-20.jpg", width: 800, height: 800 },
  "bridal-jewellery": { src: "/images/collections/cls-19.jpg", width: 800, height: 800 },
  "traditional-jewellery": { src: "/images/collections/jewelry_7.png", width: 800, height: 800 },
  "party-wear-jewellery": { src: "/images/collections/ab.jpg", width: 800, height: 800 },
  "bangles-bracelets": { src: "/images/collections/cls-21.jpg", width: 800, height: 800 },
  "pendants-chains": { src: "/images/collections/cls-22.jpg", width: 800, height: 800 },
  "maang-tikka-mathapatti": { src: "/images/collections/cls-34.png", width: 552, height: 552 },
  "daily-wear-jewellery": { src: "/images/collections/jewelry_9.png", width: 800, height: 800 },
  "gift-collections": { src: "/images/collections/cls-34.png", width: 552, height: 552 },
};

const FALLBACK_IMAGE = { src: "/images/collections/cls-18.jpg", width: 800, height: 800 };

export function getCategories() {
  return apiGet("/api/v1/categories");
}

/**
 * Adapts top-level API CategoryDto records to the shape the Vemus "Our Categories"
 * swiper renders (label + image), falling back to curated local images since
 * seeded categories don't have an ImageUrl set yet.
 */
export function toTemplateCardCategory(dto) {
  const image = CATEGORY_IMAGES[dto.slug] ?? (dto.imageUrl ? { src: dto.imageUrl, width: 800, height: 800 } : FALLBACK_IMAGE);
  return {
    label: dto.name.toUpperCase(),
    slug: dto.slug,
    image: image.src,
    width: image.width,
    height: image.height,
  };
}

export async function loadTopLevelCategories() {
  try {
    const categories = await getCategories();
    if (!categories) return undefined;
    return categories
      .filter((c) => !c.parentCategoryId)
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map(toTemplateCardCategory);
  } catch (error) {
    console.error("Category API unavailable, rendering template data:", error);
    return undefined;
  }
}
