export default function Home() {
  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="flex min-h-screen items-center justify-center px-6">
        <div className="w-full max-w-md rounded-3xl border border-white/10 bg-white/5 p-8 shadow-2xl">
          <p className="mb-3 text-sm tracking-[0.35em] text-emerald-400">
            FIXAR
          </p>

          <h1 className="mb-2 text-4xl font-bold">
            Digital Factory
          </h1>

          <p className="mb-8 text-sm text-zinc-400">
            Smart Manufacturing & Full Traceability Platform
          </p>

          <div className="space-y-4">
            <input
              className="w-full rounded-xl border border-white/10 bg-black/40 px-4 py-3 text-white outline-none"
              placeholder="Kullanıcı adı"
            />

            <input
              className="w-full rounded-xl border border-white/10 bg-black/40 px-4 py-3 text-white outline-none"
              placeholder="Şifre"
              type="password"
            />

            <button className="w-full rounded-xl bg-emerald-500 py-3 font-semibold text-black">
              Giriş Yap
            </button>
          </div>

          <div className="mt-8 grid grid-cols-2 gap-3 text-sm">
            <div className="rounded-xl bg-white/5 p-4">
              <p className="text-zinc-400">Bugünkü Üretim</p>
              <p className="text-2xl font-bold">0</p>
            </div>

            <div className="rounded-xl bg-white/5 p-4">
              <p className="text-zinc-400">Aktif İş Emri</p>
              <p className="text-2xl font-bold">0</p>
            </div>

            <div className="rounded-xl bg-white/5 p-4">
              <p className="text-zinc-400">Makine</p>
              <p className="text-2xl font-bold">PU-01</p>
            </div>

            <div className="rounded-xl bg-white/5 p-4">
              <p className="text-zinc-400">AI Durum</p>
              <p className="text-2xl font-bold">Hazır</p>
            </div>
          </div>
        </div>
      </div>
    </main>
  );
}