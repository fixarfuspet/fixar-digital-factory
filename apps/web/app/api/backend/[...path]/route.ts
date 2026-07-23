import { cookies } from "next/headers";
import { NextRequest, NextResponse } from "next/server";
import { ACCESS_TOKEN_COOKIE, getApiBaseUrl } from "@/app/lib/auth/constants";
import { isSameOriginRequest } from "@/app/lib/security/origin";

type Context = { params: Promise<{ path: string[] }> };

async function proxy(request: NextRequest, context: Context) {
  if (!["GET", "HEAD", "OPTIONS"].includes(request.method) && !isSameOriginRequest(request)) {
    console.error("Çapraz kaynaklı değişiklik isteği engellendi.", { url: request.nextUrl.pathname });
    return NextResponse.json({ message: "İstek kaynağı doğrulanamadı." }, { status: 403 });
  }
  const { path } = await context.params;
  const accessToken = (await cookies()).get(ACCESS_TOKEN_COOKIE)?.value;
  if (!accessToken) return NextResponse.json({ message: "Oturum gerekli." }, { status: 401 });

  const target = new URL(path.join("/"), `${getApiBaseUrl()}/`);
  target.search = request.nextUrl.search;
  const headers = new Headers(request.headers);
  headers.set("Authorization", `Bearer ${accessToken}`);
  headers.delete("host");
  headers.delete("cookie");
  const method = request.method;
  const body = method === "GET" || method === "HEAD" ? undefined : await request.arrayBuffer();
  const response = await fetch(target, { method, headers, body, cache: "no-store" });
  return new NextResponse(response.body, {
    status: response.status,
    headers: { "content-type": response.headers.get("content-type") ?? "application/json" },
  });
}

export const GET = proxy;
export const POST = proxy;
export const PUT = proxy;
export const PATCH = proxy;
export const DELETE = proxy;
