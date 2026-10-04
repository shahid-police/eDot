import { apiRequest } from "./client";
import { getSession } from "@/lib/auth/session";
import { ensureFreshSession } from "./auth";

// Wishlist and compare accept guests (by cookie) and customers (by bearer token).
// Refresh first so a signed-in customer is never treated as a guest.
export async function savedRequest(path, options = {}) {
  await ensureFreshSession();
  return apiRequest(path, { ...options, token: getSession()?.accessToken });
}
