import { SITE_URL } from "@/lib/siteUrl";
import { loadNoShopVideos } from "@/lib/api/noshop";

// Scoped to what this project actually has dedicated, slug-based detail pages for today (the
// homepage, shop, and No Shop videos) rather than attempting a full product/category sitemap
// that hasn't been asked for.
export default async function sitemap() {
  const staticEntries = [
    { url: `${SITE_URL}/`, changeFrequency: "daily", priority: 1 },
    { url: `${SITE_URL}/shop-default`, changeFrequency: "daily", priority: 0.8 },
    { url: `${SITE_URL}/no-shop`, changeFrequency: "daily", priority: 0.7 },
  ];

  const videos = await loadNoShopVideos({ pageSize: 200 });
  const videoEntries = videos.items.map((v) => ({
    url: `${SITE_URL}/no-shop/${v.slug}`,
    lastModified: v.publishedAt ?? undefined,
    changeFrequency: "weekly",
    priority: 0.6,
  }));

  return [...staticEntries, ...videoEntries];
}
