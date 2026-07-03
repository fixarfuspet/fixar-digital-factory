/**
 * Decodes the `exp` claim out of a JWT payload without verifying its
 * signature. This is only used for optimistic, non-authoritative checks
 * (e.g. "is it worth attempting a refresh before this request?") in
 * `proxy.ts`. The backend is always the source of truth: every protected
 * page independently calls `/api/v1/auth/me`, which verifies the token
 * signature for real (see `app/lib/auth/session.ts`).
 */
export function decodeJwtExpiry(token: string): number | null {
  try {
    const payloadSegment = token.split(".")[1];
    if (!payloadSegment) return null;

    const base64 = payloadSegment.replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
    const json = Buffer.from(padded, "base64").toString("utf-8");
    const payload = JSON.parse(json) as { exp?: unknown };

    return typeof payload.exp === "number" ? payload.exp : null;
  } catch {
    return null;
  }
}

export function isJwtValid(token: string | undefined, clockSkewMs = 5000): boolean {
  if (!token) return false;
  const exp = decodeJwtExpiry(token);
  if (exp === null) return false;
  return Date.now() < exp * 1000 - clockSkewMs;
}
