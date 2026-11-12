import { apiGet } from "./client";

export function getNoShopCategories() {
  return apiGet("/api/v1/noshop/categories");
}

export function getNoShopVideos(query = {}) {
  return apiGet("/api/v1/noshop/videos", { query });
}

export function getFeaturedNoShopVideos(take = 5) {
  return apiGet("/api/v1/noshop/videos/featured", { query: { take } });
}

export function getNoShopVideoBySlug(slug) {
  return apiGet(`/api/v1/noshop/videos/${encodeURIComponent(slug)}`);
}

export async function loadNoShopCategories() {
  try {
    return (await getNoShopCategories()) ?? [];
  } catch (error) {
    console.error("No Shop category API unavailable:", error);
    return [];
  }
}

export async function loadNoShopVideos(query = {}) {
  try {
    const result = await getNoShopVideos(query);
    return result ?? { items: [], page: 1, pageSize: query.pageSize ?? 24, totalCount: 0 };
  } catch (error) {
    console.error("No Shop video API unavailable:", error);
    return { items: [], page: 1, pageSize: query.pageSize ?? 24, totalCount: 0 };
  }
}

export async function loadFeaturedNoShopVideos(take = 5) {
  try {
    return (await getFeaturedNoShopVideos(take)) ?? [];
  } catch (error) {
    console.error("No Shop featured video API unavailable:", error);
    return [];
  }
}

export async function loadNoShopVideoBySlug(slug) {
  try {
    return await getNoShopVideoBySlug(slug);
  } catch (error) {
    console.error(`No Shop video API unavailable for "${slug}":`, error);
    return null;
  }
}
