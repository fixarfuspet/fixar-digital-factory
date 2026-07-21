import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { ACCESS_TOKEN_COOKIE, getApiBaseUrl } from "./constants";
import { parseJsonResponse, type BackendApiResponse, type MeDto } from "./backend";

export type SessionUser = MeDto;

/**
 * The authoritative session check. Calls the real backend's
 * `/api/v1/auth/me`, which validates the JWT signature and expiry
 * server-side — this is the actual security boundary, not `proxy.ts`
 * (which only does a cheap, non-authoritative pre-check for UX/redirect
 * purposes). Returns null for any missing/invalid/expired session.
 */
export async function getSession(): Promise<SessionUser | null> {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get(ACCESS_TOKEN_COOKIE)?.value;
  if (!accessToken) return null;

  try {
    const response = await fetch(`${getApiBaseUrl()}/api/v1/auth/me`, {
      headers: { Authorization: `Bearer ${accessToken}` },
      cache: "no-store",
    });

    if (!response.ok) return null;

    const payload = await parseJsonResponse<BackendApiResponse<MeDto>>(response);
    if (!payload?.success || !payload.data) return null;

    return payload.data;
  } catch {
    return null;
  }
}

/** Use in protected Server Components/pages. Redirects to /login if unauthenticated. */
export async function requireSession(): Promise<SessionUser> {
  const session = await getSession();
  if (!session) {
    redirect("/");
  }
  return session;
}
