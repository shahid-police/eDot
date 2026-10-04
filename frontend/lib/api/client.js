const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5071";

export class ApiError extends Error {
  constructor(status, path) {
    super(`API request failed with status ${status} for ${path}`);
    this.name = "ApiError";
    this.status = status;
  }
}

export async function apiGet(path, { query, revalidate = 60, signal } = {}) {
  const url = new URL(path, API_BASE_URL);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== "") {
        url.searchParams.set(key, String(value));
      }
    }
  }

  const response = await fetch(url, {
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
