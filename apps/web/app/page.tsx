import { LoginForm } from "./components/auth/LoginForm";

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

          <LoginForm />
        </div>
      </div>
    </main>
  );
}
