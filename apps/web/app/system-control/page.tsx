"use client";

import { useCallback, useEffect, useState } from "react";
import { ErrorState, LoadingState, PageHeader, SectionCard, StatCard, StatusBadge } from "../components/ui/SystemUI";
import { apiRequest } from "../lib/api/client";

const API = "/api/backend/api/v1/system-control";
type Check = { key: string; title: string; count: number; status: string; details: unknown[] };
type Report = { checkedAt: string; backend: string; database: string; authentication: string; activeUser?: string; healthy: boolean; checks: Check[] };

export default function SystemControlPage() {
  const [report, setReport] = useState<Report | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    setLoading(true); setError("");
    try {
      const response = await apiRequest<Report>(API, { cache: "no-store" });
      if (response.status === 403) throw new Error("Bu ekran yalnız CEO tarafından görüntülenebilir.");
      if (!response.ok || !response.data) throw new Error("Sistem kontrolü tamamlanamadı.");
      setReport(response.data);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Sistem kontrolü tamamlanamadı.");
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);
  return <main className="min-h-screen bg-zinc-950 p-4 text-white sm:p-7"><div className="mx-auto max-w-7xl space-y-6">
    <PageHeader eyebrow="Yalnız CEO" title="Sistem Kontrol Merkezi" description="Veri değiştirmeden bağlantı, migration ve operasyonel veri bütünlüğünü denetler." actions={<button onClick={()=>void load()} disabled={loading} className="min-h-11 rounded-xl bg-emerald-400 px-4 font-bold text-black disabled:opacity-50">Yeniden Kontrol Et</button>}/>
    {loading&&<LoadingState/>}{error&&<ErrorState message={error}/>} {report&&<>
      <section className="grid grid-cols-2 gap-3 lg:grid-cols-5"><StatCard label="Genel Durum" value={report.healthy?"Sağlıklı":"Uyarı Var"}/><StatCard label="Backend" value={report.backend}/><StatCard label="Veritabanı" value={report.database}/><StatCard label="Kimlik Doğrulama" value={report.authentication}/><StatCard label="Kontrol Zamanı" value={new Date(report.checkedAt).toLocaleTimeString("tr-TR")}/></section>
      <SectionCard title="Otomatik Kontroller"><div className="grid gap-3 md:grid-cols-2">{report.checks.map(check=><details key={check.key} className="rounded-xl border border-white/10 bg-black/20 p-4"><summary className="flex cursor-pointer list-none items-center justify-between gap-3"><span className="font-semibold">{check.title}</span><span className="flex items-center gap-2"><b>{check.count}</b><StatusBadge value={check.status}/></span></summary>{check.count>0&&<pre className="mt-4 max-h-72 overflow-auto whitespace-pre-wrap rounded-lg bg-black/50 p-3 text-xs text-zinc-300">{JSON.stringify(check.details,null,2)}</pre>}</details>)}</div></SectionCard>
      <p className="text-xs text-zinc-500">Bu ekran otomatik düzeltme yapmaz. Uyarılar ilgili operasyon kayıtları incelendikten sonra yetkili işlemle giderilmelidir.</p>
    </>}
  </div></main>;
}
