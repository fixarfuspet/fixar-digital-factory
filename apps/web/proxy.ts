import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "@/app/lib/auth/constants";
import { isJwtValid } from "@/app/lib/auth/jwt";
import { refreshSession } from "@/app/lib/auth/refresh";

// Routes that require an authenticated session.
const protectedRoutes = ["/home", "/dashboard"];
// Routes an already-authenticated user should be bounced away from.
const publicOnlyRoutes = ["/"];

function isProtectedPath(pathname: string): boolean {
  return protectedRoutes.some((route) => pathname === route || pathname.startsWith(`${route}/`));
}

/**
 * Optimistic, cookie-only auth gate (see Next.js "Optimistic checks with
 * Proxy" guidance). This never talks to the database and is not the real
 * security boundary — every protected page independently re-verifies the
 * session against the real backend (`app/lib/auth/session.ts`). This
 * layer exists purely to redirect quickly and to transparently refresh a
 * near-expired access token using the refresh token, so users aren't
 * logged out every 15 minutes.
 */
export async function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const isProtected = isProtectedPath(pathname);
  const isPublicOnly = publicOnlyRoutes.includes(pathname);

  if (!isProtected && !isPublicOnly) {
    return NextResponse.next();
  }

  let accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value;
  const refreshToken = request.cookies.get(REFRESH_TOKEN_COOKIE)?.value;
  let refreshed: Awaited<ReturnType<typeof refreshSession>> = null;

  if (!isJwtValid(accessToken) && refreshToken) {
    refreshed = await refreshSession(refreshToken);
    if (refreshed) {
      accessToken = refreshed.accessToken;
    }
  }

  const isAuthenticated = isJwtValid(accessToken);

  if (isProtected && !isAuthenticated) {
    const response = NextResponse.redirect(new URL("/", request.url));
    response.cookies.delete(ACCESS_TOKEN_COOKIE);
    response.cookies.delete(REFRESH_TOKEN_COOKIE);
    return response;
  }

  if (isPublicOnly && isAuthenticated) {
    return NextResponse.redirect(new URL("/home", request.url));
  }

  const response = NextResponse.next();

  if (refreshed) {
    const isProduction = process.env.NODE_ENV === "production";
    response.cookies.set(ACCESS_TOKEN_COOKIE, refreshed.accessToken, {
      httpOnly: true,
      secure: isProduction,
      sameSite: "lax",
      path: "/",
      expires: new Date(refreshed.accessTokenExpiresAtUtc),
    });
    response.cookies.set(REFRESH_TOKEN_COOKIE, refreshed.refreshToken, {
      httpOnly: true,
      secure: isProduction,
      sameSite: "lax",
      path: "/",
      maxAge: 60 * 60 * 24 * 7,
    });
  }

  return response;
}

export const config = {
  matcher: ["/", "/home", "/home/:path*", "/dashboard", "/dashboard/:path*"],
};
