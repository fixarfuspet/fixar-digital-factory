import { readdir } from "node:fs/promises";
import { spawn } from "node:child_process";
import path from "node:path";

const root = process.cwd();
const port = Number(process.env.SMOKE_PORT || 3210);
const baseUrl = `http://127.0.0.1:${port}`;

async function discoverRoutes(directory, segments = []) {
  const entries = await readdir(directory, { withFileTypes: true });
  const routes = [];
  if (entries.some((entry) => entry.isFile() && entry.name === "page.tsx")) {
    const route = segments.map((segment) => segment === "[code]" ? "TEST-SMOKE" : segment).join("/");
    routes.push(`/${route}`.replace(/\/$/, "") || "/");
  }
  for (const entry of entries) {
    if (!entry.isDirectory() || entry.name.startsWith("_") || ["api", "components", "lib"].includes(entry.name)) continue;
    routes.push(...await discoverRoutes(path.join(directory, entry.name), [...segments, entry.name]));
  }
  return routes;
}

async function waitUntilReady() {
  for (let attempt = 0; attempt < 60; attempt += 1) {
    try {
      const response = await fetch(baseUrl, { redirect: "manual" });
      if (response.status > 0) return;
    } catch {}
    await new Promise((resolve) => setTimeout(resolve, 500));
  }
  throw new Error("Next.js smoke sunucusu başlatılamadı.");
}

const server = spawn(path.join(root, "node_modules", ".bin", "next"), ["start", "--hostname", "127.0.0.1", "--port", String(port)], {
  cwd: root,
  stdio: ["ignore", "pipe", "pipe"],
  env: { ...process.env, NODE_ENV: "production" },
});

try {
  await waitUntilReady();
  const routes = [...new Set(await discoverRoutes(path.join(root, "app")))].sort();
  const failures = [];
  for (const route of routes) {
    const response = await fetch(`${baseUrl}${route}`, { redirect: "manual" });
    if (route === "/") {
      if (response.status !== 200) failures.push(`${route}: ${response.status}, 200 bekleniyordu`);
      continue;
    }
    const location = response.headers.get("location");
    if (![307, 308].includes(response.status) || !location?.endsWith("/")) {
      failures.push(`${route}: ${response.status} ${location ?? ""}, login yönlendirmesi bekleniyordu`);
    }
  }
  if (failures.length) throw new Error(`Route smoke başarısız:\n${failures.join("\n")}`);
  console.log(`Başarılı: ${routes.length} uygulama route'u ve session yönlendirmesi doğrulandı.`);
} finally {
  server.kill("SIGTERM");
}
