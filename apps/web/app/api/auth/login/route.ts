import { NextResponse } from "next/server";
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE, getApiBaseUrl } from "@/app/lib/auth/constants";
import { parseJsonResponse, type AuthResultDto, type BackendApiResponse } from "@/app/lib/auth/backend";
import { isSameOriginRequest } from "@/app/lib/security/origin";

/**
 * Backend-for-frontend proxy for login. The browser never talks to the
 * FIXAR OS API directly and never sees the JWT — this route calls the
 * real backend, then stores the access/refresh tokens as httpOnly
 * cookies so they're inaccessible to client-side JavaScript (XSS-safe).
 */
export async function POST(request: Request) {
  if (!isSameOriginRequest(request)) {
    return NextResponse.json({ success: false, message: "İstek kaynağı doğrulanamadı." }, { status: 403 });
  }
  let body: { email?: unknown; password?: unknown };
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ success: false, message: "Geçersiz istek." }, { status: 400 });
  }

  const email = typeof body.email === "string" ? body.email.trim() : "";
  const password = typeof body.password === "string" ? body.password : "";

  if (!email || !password) {
    return NextResponse.json(
      { success: false, message: "E-posta ve şifre zorunludur." },
      { status: 400 }
    );
  }

  let backendResponse: Response;
  try {
    backendResponse = await fetch(`${getApiBaseUrl()}/api/v1/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
      cache: "no-store",
    });
  } catch {
    return NextResponse.json(
      { success: false, message: "Sunucuya bağlanılamadı." },
      { status: 502 }
    );
  }

  const payload = await parseJsonResponse<BackendApiResponse<AuthResultDto>>(backendResponse);
  if (!payload) {
    return NextResponse.json(
      { success: false, message: "Sunucuya bağlanılamadı." },
      { status: 502 }
    );
  }

  if (!backendResponse.ok || !payload.success || !payload.data) {
    return NextResponse.json(
      { success: false, message: "E-posta veya şifre hatalı." },
      { status: backendResponse.status >= 400 ? backendResponse.status : 401 }
    );
  }

  const { accessToken, refreshToken, accessTokenExpiresAtUtc, email: userEmail, fullName, roles } =
    payload.data;

  const response = NextResponse.json({
    success: true,
    message: "Login successful.",
    data: { email: userEmail, fullName, roles },
  });

  const isProduction = process.env.NODE_ENV === "production";

  response.cookies.set(ACCESS_TOKEN_COOKIE, accessToken, {
    httpOnly: true,
    secure: isProduction,
    sameSite: "lax",
    path: "/",
    expires: new Date(accessTokenExpiresAtUtc),
  });

  response.cookies.set(REFRESH_TOKEN_COOKIE, refreshToken, {
    httpOnly: true,
    secure: isProduction,
    sameSite: "lax",
    path: "/",
    maxAge: 60 * 60 * 24 * 7, // 7 days — matches backend Jwt:RefreshTokenExpirationDays default
  });

  return response;
}
