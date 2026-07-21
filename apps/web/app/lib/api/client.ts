export const API_PROXY = "/api/backend/api/v1";

type ApiEnvelope<T> = { data?: T; message?: string };

export async function safeResponseJson<T = never>(response: Response): Promise<ApiEnvelope<T>> {
  if (response.status === 401) {
    window.location.assign("/");
    return {};
  }
  if (response.status === 204 || response.headers.get("content-length") === "0") return {};
  const contentType = response.headers.get("content-type")?.toLowerCase() ?? "";
  try {
    const body = await response.text();
    if (!body.trim()) return {};
    if (!contentType.includes("json")) {
      console.error("API JSON olmayan yanıt döndürdü.", { status: response.status, contentType });
      return {};
    }
    return JSON.parse(body) as ApiEnvelope<T>;
  } catch (error) {
    console.error("API yanıtı ayrıştırılamadı.", { status: response.status, error });
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
    const response = await fetch(url, init);
    if (response.status === 401) {
      window.location.assign("/");
      return { ok: false, status: 401, data: null, empty: true };
    }

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
