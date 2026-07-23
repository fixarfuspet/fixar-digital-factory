import { cookies } from "next/headers";
import { NextRequest, NextResponse } from "next/server";
import { ACCESS_TOKEN_COOKIE, getApiBaseUrl } from "@/app/lib/auth/constants";
import { isSameOriginRequest } from "@/app/lib/security/origin";

type Context = { params: Promise<{ path: string[] }> };

async function proxy(request: NextRequest, context: Context) {
  if (
    !["GET", "HEAD", "OPTIONS"].includes(request.method) &&
    !isSameOriginRequest(request)
  ) {
    console.error("Çapraz kaynaklı değişiklik isteği engellendi.", {
      url: request.nextUrl.pathname,
    });

    return NextResponse.json(
      { message: "İstek kaynağı doğrulanamadı." },
      { status: 403 }
    );
  }

  const { path } = await context.params;

  const cookieStore = await cookies();
  const accessToken = cookieStore.get(ACCESS_TOKEN_COOKIE)?.value;

  if (!accessToken) {
    return NextResponse.json(
      { message: "Oturum gerekli." },
      { status: 401 }
    );
  }

  const target = new URL(path.join("/"), `${getApiBaseUrl()}/`);
  target.search = request.nextUrl.search;
  // Tarayıcıdan gelen bütün header'ları backend'e taşımıyoruz.
  // Yalnızca backend'in ihtiyaç duyduğu güvenli başlıkları oluşturuyoruz.
  const headers = new Headers();
  headers.set("Authorization", `Bearer ${accessToken}`);
  headers.set("Accept", "application/json");

  const contentType = request.headers.get("content-type");
  if (contentType) {
    headers.set("Content-Type", contentType);
  }

  const idempotencyKey = request.headers.get("idempotency-key");
  if (idempotencyKey) {
    headers.set("Idempotency-Key", idempotencyKey);
  }

  const correlationId = request.headers.get("x-correlation-id");
  if (correlationId) {
    headers.set("X-Correlation-ID", correlationId);
  }

  const method = request.method;
  const body =
    method === "GET" || method === "HEAD"
      ? undefined
      : await request.arrayBuffer();

  const response = await fetch(target, {
    method,
    headers,
    body,
    cache: "no-store",
  });

  const responseHeaders = new Headers();

  const responseContentType = response.headers.get("content-type");
  if (responseContentType) {
    responseHeaders.set("content-type", responseContentType);
  }

  const disposition = response.headers.get("content-disposition");
  if (disposition) {
    responseHeaders.set("content-disposition", disposition);
  }

  return new NextResponse(response.body, {
    status: response.status,
    headers: responseHeaders,
  });
}

export const GET = proxy;
export const HEAD = proxy;
export const OPTIONS = proxy;
export const POST = proxy;
export const PUT = proxy;
export const PATCH = proxy;
export const DELETE = proxy;
