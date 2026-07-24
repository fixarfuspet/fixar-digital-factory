const baseUrl = process.env.FIXAR_TEST_BASE_URL?.replace(/\/$/, "");
const email = process.env.FIXAR_TEST_EMAIL;
const password = process.env.FIXAR_TEST_PASSWORD;

if (!baseUrl || !email || !password) {
  throw new Error(
    "FIXAR_TEST_BASE_URL, FIXAR_TEST_EMAIL ve FIXAR_TEST_PASSWORD zorunludur.",
  );
}

const cookies = new Map();

function updateCookies(response) {
  const values = typeof response.headers.getSetCookie === "function"
    ? response.headers.getSetCookie()
    : [response.headers.get("set-cookie") ?? ""];

  for (const value of values.flatMap((header) => header.split(/,(?=\s*[^;,]+=)/))) {
    const pair = value.split(";", 1)[0];
    const separator = pair.indexOf("=");
    if (separator < 1) continue;
    cookies.set(pair.slice(0, separator), pair.slice(separator + 1));
  }
}

function cookieHeader() {
  return [...cookies].map(([name, value]) => `${name}=${value}`).join("; ");
}

async function request(path, init = {}) {
  const headers = new Headers(init.headers);
  if (cookies.size) headers.set("Cookie", cookieHeader());
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers,
    redirect: "manual",
  });
  updateCookies(response);
  return response;
}

function expect(response, status, label) {
  if (response.status !== status) {
    throw new Error(`${label}: HTTP ${response.status}; ${status} bekleniyordu.`);
  }
}

const login = await request("/api/auth/login", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    Origin: baseUrl,
  },
  body: JSON.stringify({ email, password }),
});
expect(login, 200, "login");

const accessToken = cookies.get("fixar_access_token");
if (!accessToken || accessToken.split(".").length !== 3) {
  throw new Error("Login geçerli JWT access-token cookie üretmedi.");
}

const me = await request("/api/backend/api/v1/auth/me");
expect(me, 200, "auth/me");

const products = await request("/api/backend/api/v1/products");
expect(products, 200, "products");

const refresh = await request("/api/auth/refresh", {
  method: "POST",
  headers: { Origin: baseUrl },
});
expect(refresh, 200, "refresh");

const refreshedAccessToken = cookies.get("fixar_access_token");
if (!refreshedAccessToken || refreshedAccessToken === accessToken) {
  throw new Error("Refresh yeni access-token cookie üretmedi.");
}

const retriedProducts = await request("/api/backend/api/v1/products");
expect(retriedProducts, 200, "products after refresh");

console.log(
  "Başarılı: gerçek login, auth/me, products, refresh ve refresh sonrası products çağrıları doğrulandı.",
);
