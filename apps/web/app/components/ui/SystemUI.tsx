import type { ButtonHTMLAttributes, ReactNode } from "react";
import { uiLabel } from "../../lib/ui/labels";

export function PageHeader({ eyebrow, title, description, actions }: { eyebrow?: string; title: string; description?: string; actions?: ReactNode }) {
  return <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between"><div className="min-w-0">{eyebrow&&<p className="text-xs font-bold uppercase tracking-[.2em] text-emerald-400">{eyebrow}</p>}<h1 className="mt-1 text-2xl font-black tracking-tight text-white sm:text-3xl">{title}</h1>{description&&<p className="mt-2 max-w-3xl text-sm text-zinc-400">{description}</p>}</div>{actions&&<div className="flex flex-wrap gap-2">{actions}</div>}</header>;
}

export function StatCard({ label, value, hint }: { label: string; value: ReactNode; hint?: string }) {
  return <article className="rounded-2xl border border-white/10 bg-white/[.04] p-4 shadow-sm"><p className="text-xs font-semibold text-zinc-400">{label}</p><p className="mt-1 text-2xl font-black text-white">{value}</p>{hint&&<p className="mt-1 text-xs text-zinc-500">{hint}</p>}</article>;
}

export function SectionCard({ title, children, className="" }: { title?: string; children: ReactNode; className?: string }) {
  return <section className={`rounded-2xl border border-white/10 bg-white/[.04] p-4 sm:p-5 ${className}`}>{title&&<h2 className="mb-4 text-lg font-bold text-white">{title}</h2>}{children}</section>;
}

export function StatusBadge({ value }: { value: string | null | undefined }) {
  const normalized=(value??"").toLowerCase(); const positive=/approved|active|available|completed|open|inflow/.test(normalized); const negative=/rejected|cancelled|blocked|damaged|expired|critical|outflow/.test(normalized);
  return <span className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-bold ${positive?"border-emerald-400/30 bg-emerald-400/10 text-emerald-300":negative?"border-red-400/30 bg-red-400/10 text-red-300":"border-amber-400/30 bg-amber-400/10 text-amber-200"}`}>{uiLabel(value)}</span>;
}

const buttonBase="inline-flex min-h-11 items-center justify-center rounded-xl px-4 py-2 text-sm font-bold transition disabled:cursor-not-allowed disabled:opacity-50";
export function PrimaryButton(props: ButtonHTMLAttributes<HTMLButtonElement>){return <button {...props} className={`${buttonBase} bg-emerald-400 text-zinc-950 hover:bg-emerald-300 ${props.className??""}`}/>}
export function SecondaryButton(props: ButtonHTMLAttributes<HTMLButtonElement>){return <button {...props} className={`${buttonBase} border border-white/10 bg-white/5 text-white hover:bg-white/10 ${props.className??""}`}/>}
export function DangerButton(props: ButtonHTMLAttributes<HTMLButtonElement>){return <button {...props} className={`${buttonBase} border border-red-400/30 bg-red-400/10 text-red-200 hover:bg-red-400/20 ${props.className??""}`}/>}

export function EmptyState({ title="Kayıt bulunamadı", description="Filtreleri değiştirerek yeniden deneyebilirsiniz." }: { title?: string; description?: string }) { return <div className="rounded-xl border border-dashed border-white/15 p-8 text-center"><p className="font-bold text-white">{title}</p><p className="mt-1 text-sm text-zinc-400">{description}</p></div> }
export function LoadingState(){return <div className="animate-pulse rounded-xl border border-white/10 bg-white/5 p-8 text-center text-sm text-zinc-400">Yükleniyor…</div>}
export function ErrorState({ message="Bilgiler alınamadı. Lütfen yeniden deneyin." }: { message?: string }){return <div role="alert" className="rounded-xl border border-red-400/30 bg-red-400/10 p-4 text-sm text-red-200">{message}</div>}
