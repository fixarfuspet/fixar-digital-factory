import { getApiBaseUrl } from "./constants";
import type { AuthResultDto, BackendApiResponse } from "./backend";

export interface RefreshedTokens {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
}

/**
 * Calls the real backend refresh-token endpoint. Used by `proxy.ts` to
 * transparently renew an expired access token using the refresh token,
 * so a logged-in user isn't bounced to /login every 15 minutes.
 */
export async function refreshSession(refreshToken: string): Promise<RefreshedTokens | null> {
  try {
    const response = await fetch(`${getApiBaseUrl()}/api/v1/auth/refresh-token`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
      cache: "no-store",
    });

    if (!response.ok) return null;

    const payload: BackendApiResponse<AuthResultDto> = await response.json();
    if (!payload.success || !payload.data) return null;

    return {
      accessToken: payload.data.accessToken,
      refreshToken: payload.data.refreshToken,
      accessTokenExpiresAtUtc: payload.data.accessTokenExpiresAtUtc,
    };
  } catch {
    return null;
  }
}
