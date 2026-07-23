import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "@/app/lib/auth/constants";
import { refreshSession } from "@/app/lib/auth/refresh";
import { isSameOriginRequest } from "@/app/lib/security/origin";

export async function POST(request: Request) {
  if (!isSameOriginRequest(request)) {
    return NextResponse.json({ success: false, message: "İstek kaynağı doğrulanamadı." }, { status: 403 });
  }

  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(REFRESH_TOKEN_COOKIE)?.value;
  if (!refreshToken) {
    return clearSession(NextResponse.json({ success: false, message: "Oturum yenilenemedi." }, { status: 401 }));
  }

  const refreshed = await refreshSession(refreshToken);
  if (!refreshed) {
    return clearSession(NextResponse.json({ success: false, message: "Oturum yenilenemedi." }, { status: 401 }));
  }

  const response = NextResponse.json({ success: true });
  const secure = process.env.NODE_ENV === "production";
  response.cookies.set(ACCESS_TOKEN_COOKIE, refreshed.accessToken, {
    httpOnly: true,
    secure,
    sameSite: "lax",
    path: "/",
    expires: new Date(refreshed.accessTokenExpiresAtUtc),
  });
  response.cookies.set(REFRESH_TOKEN_COOKIE, refreshed.refreshToken, {
    httpOnly: true,
    secure,
    sameSite: "lax",
    path: "/",
    maxAge: 60 * 60 * 24 * 7,
  });
  return response;
}

function clearSession(response: NextResponse): NextResponse {
  response.cookies.delete(ACCESS_TOKEN_COOKIE);
  response.cookies.delete(REFRESH_TOKEN_COOKIE);
  return response;
}
