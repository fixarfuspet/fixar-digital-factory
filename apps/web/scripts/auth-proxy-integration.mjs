import { createServer } from "node:http";
import { spawn } from "node:child_process";
import path from "node:path";

const root = process.cwd();
const webPort = Number(process.env.AUTH_PROXY_TEST_PORT || 3211);
const webBaseUrl = `http://127.0.0.1:${webPort}`;
const accessCookie = "fixar_access_token";
const refreshCookie = "fixar_refresh_token";
const refreshToken = "integration-refresh-token";

function jwt(expiresAt) {
  const encode = (value) => Buffer.from(JSON.stringify(value)).toString("base64url");
  return `${encode({ alg: "none", typ: "JWT" })}.${encode({ exp: expiresAt })}.test`;
}

const initialAccessToken = jwt(Math.floor(Date.now() / 1000) + 3600);
const refreshedAccessToken = jwt(Math.floor(Date.now() / 1000) + 7200);
let acceptedAccessToken = initialAccessToken;
const receivedAuthorization = [];

function json(response, status, body) {
  response.writeHead(status, { "content-type": "application/json" });
  response.end(JSON.stringify(body));
}

const backend = createServer((request, response) => {
  const authorization = request.headers.authorization;

  if (request.url === "/api/v1/auth/refresh-token" && request.method === "POST") {
    let body = "";
    request.on("data", (chunk) => { body += chunk; });
    request.on("end", () => {
      const payload = JSON.parse(body);
      if (payload.refreshToken !== refreshToken) {
        json(response, 401, { success: false });
        return;
      }

      acceptedAccessToken = refreshedAccessToken;
      json(response, 200, {
        success: true,
        data: {
          accessToken: refreshedAccessToken,
          refreshToken,
          accessTokenExpiresAtUtc: new Date(Date.now() + 7_200_000).toISOString(),
          email: "integration@fixar.test",
          fullName: "Integration Test",
          roles: ["Admin"],
        },
      });
    });
    return;
  }

  if (request.url === "/api/v1/auth/me" || request.url === "/api/v1/products") {
    receivedAuthorization.push({ url: request.url, authorization });
    if (authorization !== `Bearer ${acceptedAccessToken}` || request.headers.cookie) {
      json(response, 401, { success: false });
      return;
    }

    const data = request.url.endsWith("/auth/me")
      ? { email: "integration@fixar.test", fullName: "Integration Test", roles: ["Admin"] }
      : [{ id: "product-1", name: "Integration Product" }];
    json(response, 200, { success: true, data });
    return;
  }

  json(response, 404, { success: false });
});

await new Promise((resolve) => backend.listen(0, "127.0.0.1", resolve));
const backendAddress = backend.address();
if (!backendAddress || typeof backendAddress === "string") throw new Error("Mock backend başlatılamadı.");
const backendBaseUrl = `http://127.0.0.1:${backendAddress.port}`;

const web = spawn(
  path.join(root, "node_modules", ".bin", "next"),
  ["start", "--hostname", "127.0.0.1", "--port", String(webPort)],
  {
    cwd: root,
    stdio: ["ignore", "pipe", "pipe"],
    env: { ...process.env, NODE_ENV: "production", API_BASE_URL: backendBaseUrl },
  },
);

async function waitUntilReady() {
  for (let attempt = 0; attempt < 60; attempt += 1) {
    try {
      const response = await fetch(webBaseUrl, { redirect: "manual" });
      if (response.status > 0) return;
    } catch {}
    await new Promise((resolve) => setTimeout(resolve, 500));
  }
  throw new Error("Next.js integration sunucusu başlatılamadı.");
}

function assertStatus(response, expected, label) {
  if (response.status !== expected) {
    throw new Error(`${label}: ${response.status}, ${expected} bekleniyordu`);
  }
}

function cookieHeaderFrom(response) {
  const values = typeof response.headers.getSetCookie === "function"
    ? response.headers.getSetCookie()
    : [response.headers.get("set-cookie") ?? ""];
  return values
    .flatMap((value) => value.split(/,(?=\s*[^;,]+=)/))
    .map((value) => value.split(";", 1)[0])
    .filter(Boolean)
    .join("; ");
}

try {
  await waitUntilReady();
  const initialCookies = `${accessCookie}=${initialAccessToken}; ${refreshCookie}=${refreshToken}`;

  const me = await fetch(`${webBaseUrl}/api/backend/api/v1/auth/me`, {
    headers: { Cookie: initialCookies, Authorization: "Bearer browser-token-must-not-be-used" },
  });
  assertStatus(me, 200, "auth/me");

  const products = await fetch(`${webBaseUrl}/api/backend/api/v1/products`, {
    headers: { Cookie: initialCookies, Authorization: "Bearer browser-token-must-not-be-used" },
  });
  assertStatus(products, 200, "products");

  acceptedAccessToken = "expired";
  const expiredProducts = await fetch(`${webBaseUrl}/api/backend/api/v1/products`, {
    headers: { Cookie: initialCookies },
  });
  assertStatus(expiredProducts, 401, "expired products");

  const refresh = await fetch(`${webBaseUrl}/api/auth/refresh`, {
    method: "POST",
    headers: { Cookie: initialCookies, Origin: webBaseUrl },
  });
  assertStatus(refresh, 200, "refresh");

  const refreshedCookies = cookieHeaderFrom(refresh);
  if (!refreshedCookies.includes(`${accessCookie}=${refreshedAccessToken}`)) {
    throw new Error("Refresh yanıtı access tokenı ortak cookie adıyla yazmadı.");
  }

  const retriedProducts = await fetch(`${webBaseUrl}/api/backend/api/v1/products`, {
    headers: { Cookie: refreshedCookies },
  });
  assertStatus(retriedProducts, 200, "retried products");

  if (receivedAuthorization.some(({ authorization }) => authorization === "Bearer browser-token-must-not-be-used")) {
    throw new Error("Backend proxy tarayıcı Authorization headerını kullandı.");
  }

  console.log("Başarılı: auth/me, products, refresh ve yenilenmiş products isteği aynı session cookie akışıyla doğrulandı.");
} finally {
  web.kill("SIGTERM");
  await new Promise((resolve) => backend.close(resolve));
}
