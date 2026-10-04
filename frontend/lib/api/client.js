const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5071";

export class ApiError extends Error {
  constructor(status, path, detail) {
    super(detail ?? `API request failed with status ${status} for ${path}`);
    this.name = "ApiError";
    this.status = status;
  }
}

function buildUrl(path, query) {
  const url = new URL(path, API_BASE_URL);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== "") {
        url.searchParams.set(key, String(value));
      }
    }
  }
  return url;
}

export async function apiGet(path, { query, revalidate = 60, signal } = {}) {
  const response = await fetch(buildUrl(path, query), {
    headers: { Accept: "application/json" },
    next: { revalidate },
    signal,
  });

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new ApiError(response.status, path);
  }

  return response.json();
}

export async function apiRequest(path, { method = "GET", body, token } = {}) {
  const headers = { Accept: "application/json" };
  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
  }
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(buildUrl(path), {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    credentials: "include",
    cache: "no-store",
  });

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
