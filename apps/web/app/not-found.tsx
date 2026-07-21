import Link from "next/link";

export default function NotFound() {
  return <main className="flex min-h-[60dvh] items-center justify-center bg-zinc-950 p-6 text-white">
    <section className="w-full max-w-lg rounded-3xl border border-white/10 bg-white/5 p-7 text-center">
      <p className="text-sm font-black tracking-widest text-amber-300">404</p>
      <h1 className="mt-2 text-2xl font-black">Sayfa bulunamadı</h1>
      <p className="mt-3 text-zinc-400">İstenen sayfa kaldırılmış veya adresi değişmiş olabilir.</p>
      <Link className="mt-6 inline-flex min-h-11 items-center rounded-xl bg-emerald-400 px-5 font-bold text-black" href="/home">Ana Sayfaya Dön</Link>
    </section>
  </main>;
}
