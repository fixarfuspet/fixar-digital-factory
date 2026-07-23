export const API_PROXY = "/api/backend/api/v1";

type ApiEnvelope<T> = { data?: T; message?: string };
let refreshPromise: Promise<boolean> | null = null;
let redirectStarted = false;

async function refreshBrowserSession(): Promise<boolean> {
  if (!refreshPromise) {
    refreshPromise = fetch("/api/auth/refresh", {
      method: "POST",
      cache: "no-store",
      credentials: "same-origin",
    })
      .then((response) => response.ok)
      .catch(() => false)
      .finally(() => {
        refreshPromise = null;
      });
  }

  return refreshPromise;
}

function redirectToLogin(): void {
  if (typeof window === "undefined" || redirectStarted) return;
  redirectStarted = true;
  window.location.assign("/");
}

export async function authenticatedFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const requestInit: RequestInit = { ...init, credentials: "same-origin" };
  let response = await fetch(input, requestInit);
  if (response.status !== 401) return response;

  if (await refreshBrowserSession()) {
    response = await fetch(input, requestInit);
    if (response.status !== 401) return response;
  }

  redirectToLogin();
  return response;
}

// A default is required for legacy callers whose response contracts are still untyped.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export async function safeResponseJson<T = any>(response: Response): Promise<ApiEnvelope<T>> {
  if (response.status === 204 || response.headers.get("content-length") === "0") return {};
  const contentType = response.headers.get("content-type")?.toLowerCase() ?? "";
  try {
    const body = await response.text();
    if (!body.trim()) return {};
    if (!contentType.includes("json")) {
      console.error("API JSON olmayan yanıt döndürdü.", { url: response.url, status: response.status, contentType });
      return {};
    }
    return JSON.parse(body) as ApiEnvelope<T>;
  } catch (error) {
    console.error("API yanıtı ayrıştırılamadı.", { url: response.url, status: response.status, error });
    return {};
  }
}

export type ApiResult<T> = {
  ok: boolean;
  status: number;
  data: T | null;
  message?: string;
  empty: boolean;
};

export async function apiRequest<T>(url: string, init?: RequestInit): Promise<ApiResult<T>> {
  try {
    const response = await authenticatedFetch(url, init);

    const contentType = response.headers.get("content-type")?.toLowerCase() ?? "";
    const contentLength = response.headers.get("content-length");
    if (response.status === 204 || contentLength === "0") {
      return { ok: response.ok, status: response.status, data: null, empty: true };
    }

    const body = await response.text();
    if (!body.trim()) return { ok: response.ok, status: response.status, data: null, empty: true };
    if (!contentType.includes("json")) {
      console.error("API JSON olmayan yanıt döndürdü.", { url, status: response.status, contentType });
      return { ok: false, status: response.status, data: null, empty: false };
    }

    try {
      const payload = JSON.parse(body) as ApiEnvelope<T>;
      return { ok: response.ok, status: response.status, data: payload.data ?? null, message: payload.message, empty: false };
    } catch (error) {
      console.error("API yanıtı ayrıştırılamadı.", { url, status: response.status, error });
      return { ok: false, status: response.status, data: null, empty: false };
    }
  } catch (error) {
    console.error("API isteği tamamlanamadı.", { url, error });
    return { ok: false, status: 0, data: null, empty: true };
  }
}
