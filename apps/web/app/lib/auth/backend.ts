// Shapes mirroring the FIXAR OS backend contracts (apps/api).
// Kept intentionally minimal — only the fields this app actually uses.

export interface BackendApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  errorCode: string | null;
}

export interface AuthResultDto {
  succeeded: boolean;
  errors: string[];
  userId: string;
  email: string;
  fullName: string;
  roles: string[];
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
}

export interface MeDto {
  userId: string;
  userName?: string;
  email: string;
  roles: string[];
}

export async function parseJsonResponse<T>(response: Response): Promise<T | null> {
  if (response.status === 204 || response.headers.get("content-length") === "0") return null;

  const contentType = response.headers.get("content-type")?.toLowerCase() ?? "";
  const body = await response.text();
  if (!body.trim()) return null;
  if (!contentType.includes("json")) {
    console.error("API JSON olmayan yanıt döndürdü.", {
      url: response.url,
      status: response.status,
      contentType,
    });
    return null;
  }

  try {
    return JSON.parse(body) as T;
  } catch (error) {
    console.error("API yanıtı ayrıştırılamadı.", { url: response.url, status: response.status, error });
    return null;
  }
}
