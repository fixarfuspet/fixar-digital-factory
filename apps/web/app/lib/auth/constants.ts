export const ACCESS_TOKEN_COOKIE = "fixar_access_token";
export const REFRESH_TOKEN_COOKIE = "fixar_refresh_token";

/**
 * Base URL of the FIXAR OS backend (server-side only — never read this
 * on the client, the browser never talks to the backend directly).
 */
export function getApiBaseUrl(): string {
  return process.env.API_BASE_URL ?? "http://localhost:5000";
}
