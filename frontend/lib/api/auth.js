import { apiRequest } from "./client";
import { getSession, setSession } from "@/lib/auth/session";

function toSession(response) {
  return {
    accessToken: response.accessToken,
    accessTokenExpiresAt: response.accessTokenExpiresAt,
    customer: response.customer,
  };
}

export async function register(details) {
  const response = await apiRequest("/api/v1/auth/register", {
    method: "POST",
    body: details,
  });
  setSession(toSession(response));
  return getSession();
}

export async function login(email, password) {
  const response = await apiRequest("/api/v1/auth/login", {
    method: "POST",
    body: { email, password },
  });
  setSession(toSession(response));
  return getSession();
}

export async function logout() {
  try {
    await apiRequest("/api/v1/auth/logout", { method: "POST" });
  } finally {
    setSession(null);
  }
}

let pendingRestore = null;

// Refresh tokens rotate on use, so concurrent refreshes from one page would
// present the same token twice and trip reuse detection. Share one request.
export function restoreSession() {
  pendingRestore ??= apiRequest("/api/v1/auth/refresh", { method: "POST" })
    .then((response) => setSession(toSession(response)))
    .catch(() => setSession(null))
    .finally(() => {
      pendingRestore = null;
    });
  return pendingRestore;
}
