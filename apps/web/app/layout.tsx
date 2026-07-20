import type { Metadata } from "next";
import "./globals.css";
import { getSession } from "./lib/auth/session";
import { AppShell } from "./components/shell/AppShell";

export const metadata: Metadata = {
  title: "FIXAR OS — Digital Factory",
  description: "Kurumsal fabrika yönetimi ve izlenebilirlik platformu",
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const user = await getSession();
  return (
    <html lang="tr" className="h-full antialiased">
      <body className="min-h-full"><AppShell user={user}>{children}</AppShell></body>
    </html>
  );
}
