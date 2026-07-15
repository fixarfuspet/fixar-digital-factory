import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  ...(process.env.NODE_ENV === "development"
    ? {
        allowedDevOrigins: [
          "localhost",
          "127.0.0.1",
          "192.168.*.*",
          "10.*.*.*",
          "172.16.*.*",
          "172.17.*.*",
          "172.18.*.*",
          "172.19.*.*",
          "172.2*.*.*",
          "172.30.*.*",
          "172.31.*.*",
        ],
      }
    : {}),
};

export default nextConfig;
