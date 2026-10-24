import { ApiError } from "@/lib/api/client";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5071";
const TOKEN_KEY = "elegance.admin.token";

// The admin token lives in sessionStorage for this tab only. It expires after the access
// token lifetime (15 minutes) and there is no admin refresh yet, so sign-in is required again.
export function getAdminToken() {
  if (typeof window === "undefined") return null;
  try {
    return window.sessionStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
}

export function setAdminToken(token) {
  try {
    window.sessionStorage.setItem(TOKEN_KEY, token);
  } catch {
    // Storage can be blocked. The admin will simply be asked to sign in again.
  }
}

export function clearAdminToken() {
  try {
    window.sessionStorage.removeItem(TOKEN_KEY);
  } catch {
    // Nothing to clear when storage is unavailable.
  }
}

export async function adminRequest(path, { method = "GET", body, query } = {}) {
  const url = new URL(`/api/v1/admin${path}`, API_BASE_URL);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== "") {
        url.searchParams.set(key, String(value));
      }
    }
  }

  const headers = { Accept: "application/json" };
  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
  }
  const token = getAdminToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    cache: "no-store",
  });

  if (response.status === 204) {
    return null;
  }

  const data = await response.json().catch(() => null);

  // These can 401 for reasons that have nothing to do with the session token itself (a wrong
  // password or code re-entered to confirm a sensitive action) - redirecting to login on those
  // would hide the real error and sign the admin out of an otherwise perfectly valid session.
  const isReauthPath = path === "/2fa/enroll/start" || path === "/2fa/enroll/verify" || path === "/2fa/disable";
  if (response.status === 401 && !path.startsWith("/auth/") && !isReauthPath) {
    clearAdminToken();
    window.location.assign("/admin/login");
  }

  if (!response.ok) {
    const message = data?.detail ?? Object.values(data?.errors ?? {}).flat()[0];
    throw new ApiError(response.status, path, message);
  }

  return data;
}

// For file uploads. Unlike adminRequest, this never sets Content-Type itself - the browser
// fills in the multipart boundary when the body is a FormData.
export async function adminUpload(path, formData) {
  const url = new URL(`/api/v1/admin${path}`, API_BASE_URL);
  const headers = { Accept: "application/json" };
  const token = getAdminToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    method: "POST",
    headers,
    body: formData,
    cache: "no-store",
  });

  const data = await response.json().catch(() => null);

  if (response.status === 401) {
    clearAdminToken();
    window.location.assign("/admin/login");
  }

  if (!response.ok) {
    const message = data?.detail ?? Object.values(data?.errors ?? {}).flat()[0];
    throw new ApiError(response.status, path, message);
  }

  return data;
}

export const ORDER_STATUSES = [
  "Pending",
  "Confirmed",
  "Processing",
  "Packed",
  "Shipped",
  "OutForDelivery",
  "Delivered",
  "Cancelled",
  "Returned",
  "Refunded",
];

export const PAYMENT_STATUSES = ["Pending", "Captured", "Failed", "Refunded"];

export function formatDateTime(value) {
  return value ? new Date(value).toLocaleString() : "-";
}
