// Single source of truth for the site's public base URL - used for metadataBase, canonical
// links, Open Graph URLs, and the sitemap/robots routes. Falls back to localhost for dev since
// NEXT_PUBLIC_SITE_URL isn't set anywhere in this project yet.
export const SITE_URL = (process.env.NEXT_PUBLIC_SITE_URL || "http://localhost:3000").replace(/\/+$/, "");

export function absoluteUrl(path = "/") {
  return new URL(path, SITE_URL).toString();
}
