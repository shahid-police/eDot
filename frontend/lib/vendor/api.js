import { ApiError } from "@/lib/api/client";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5071";
const TOKEN_KEY = "elegance.vendor.token";

// Mirrors lib/admin/api.js's token handling, but the vendor portal also has a refresh cookie
// (elegance_vendor_refresh, set by VendorAuthController) so a 401 first tries a silent refresh
// before sending the vendor back to the login screen.
export function getVendorToken() {
  if (typeof window === "undefined") return null;
  try {
    return window.sessionStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
}

export function setVendorToken(token) {
  try {
    window.sessionStorage.setItem(TOKEN_KEY, token);
  } catch {
    // Storage can be blocked - the vendor will simply be asked to sign in again.
  }
}

export function clearVendorToken() {
  try {
    window.sessionStorage.removeItem(TOKEN_KEY);
  } catch {
    // Nothing to clear when storage is unavailable.
  }
}

async function tryRefresh() {
  try {
    const response = await fetch(new URL("/api/v1/vendor/auth/refresh", API_BASE_URL), {
      method: "POST",
      credentials: "include",
      cache: "no-store",
    });
    if (!response.ok) return false;
    const data = await response.json();
    setVendorToken(data.accessToken);
    return true;
  } catch {
    return false;
  }
}

export async function vendorRequest(path, { method = "GET", body, query, _retried = false } = {}) {
  const url = new URL(`/api/v1/vendor${path}`, API_BASE_URL);
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
  const token = getVendorToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    cache: "no-store",
    // The auth endpoints (login/2fa enroll/verify/logout) set or read the elegance_vendor_refresh
    // HttpOnly cookie - every vendor request includes credentials so that cookie is always sent
    // and any Set-Cookie response header is always honored, not just on the dedicated auth calls.
    credentials: "include",
  });

  if (response.status === 401 && !_retried) {
    const refreshed = await tryRefresh();
    if (refreshed) {
      return vendorRequest(path, { method, body, query, _retried: true });
    }
    clearVendorToken();
    window.location.assign("/vendor/login");
    return null;
  }

  if (response.status === 204) {
    return null;
  }

  const data = await response.json().catch(() => null);

  if (!response.ok) {
    const message = data?.detail ?? Object.values(data?.errors ?? {}).flat()[0];
    throw new ApiError(response.status, path, message);
  }

  return data;
}

export async function vendorUpload(path, formData) {
  const url = new URL(`/api/v1/vendor${path}`, API_BASE_URL);
  const headers = { Accept: "application/json" };
  const token = getVendorToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(url, { method: "POST", headers, body: formData, cache: "no-store" });
  const data = await response.json().catch(() => null);

  if (response.status === 401) {
    clearVendorToken();
    window.location.assign("/vendor/login");
    return null;
  }

  if (!response.ok) {
    const message = data?.detail ?? Object.values(data?.errors ?? {}).flat()[0];
    throw new ApiError(response.status, path, message);
  }

  return data;
}

export function formatDateTime(value) {
  return value ? new Date(value).toLocaleString() : "-";
}

export const SUBMISSION_STATUSES = ["PendingApproval", "Approved", "Rejected", "ReturnedForCorrection"];
