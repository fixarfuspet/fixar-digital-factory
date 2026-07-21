"use client";

import { useEffect } from "react";

export default function GlobalError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => {
    console.error("Sayfa çalıştırma hatası.", { message: error.message, digest: error.digest });
  }, [error]);

  return <main className="flex min-h-[60dvh] items-center justify-center bg-zinc-950 p-6 text-white">
    <section className="w-full max-w-lg rounded-3xl border border-red-400/20 bg-red-400/5 p-7 text-center">
      <h1 className="text-2xl font-black">Sayfa görüntülenemedi</h1>
      <p className="mt-3 text-zinc-400">Beklenmeyen bir hata oluştu. İşleminizi tekrar deneyebilirsiniz.</p>
      <button className="mt-6 min-h-11 rounded-xl bg-emerald-400 px-5 font-bold text-black" onClick={reset}>Tekrar Dene</button>
    </section>
  </main>;
}
