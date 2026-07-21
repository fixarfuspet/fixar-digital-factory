"use client";

import { Suspense, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";

type ApiResponse<T> = { data?: T; message?: string; errorMessage?: string };
type Assignment = { stationAssignmentId: string; stationNumber: number; workOrderNumber?: string | null; customerName?: string | null; productCode?: string | null; productName?: string | null; producedPairs: number; injectionFirePairs: number; alreadyCutInputPairs: number; remainingForCutting: number };
type Machine = { id: string; name: string; machineType?: string; operatorName?: string; isActive?: boolean };
type CuttingRecord = { id: string; recordNumber: string; recordDate: string; stationAssignmentId?: string | null; stationNumber?: number | null; workOrderNumber?: string | null; customerName?: string | null; productCode?: string | null; productName?: string | null; cuttingMachineId: string; cuttingMachineName: string; operatorName?: string | null; inputPairs: number; goodPairs: number; rejectedPairs: number; reworkPairs: number; boxedPairs: number; remainingForPacking: number; status: string };
type Summary = { totalInputPairs?: number; todayCutPairs?: number; goodPairs?: number; rejectedPairs?: number; reworkPairs?: number; waitingForPacking?: number };
type FormState = { stationAssignmentId: string; cuttingMachineId: string; shift: string; inputPairs: string; goodPairs: string; rejectedPairs: string; reworkPairs: string; notes: string };

const API = (process.env.NEXT_PUBLIC_API_BASE_URL?.trim() || "/api/backend/api/v1").replace(/\/$/, "");
const CONTROL = "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none focus:border-emerald-400/60";
const emptyForm: FormState = { stationAssignmentId: "", cuttingMachineId: "", shift: "1", inputPairs: "", goodPairs: "", rejectedPairs: "0", reworkPairs: "0", notes: "" };

export default function CuttingPage() {
  return (
    <Suspense fallback={<main className="min-h-screen bg-[#05070A] p-10 text-white">Kesim yükleniyor...</main>}>
      <CuttingContent />
    </Suspense>
  );
}

function CuttingContent() {
  const params = useSearchParams();
  const [records, setRecords] = useState<CuttingRecord[]>([]);
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [machines, setMachines] = useState<Machine[]>([]);
  const [summary, setSummary] = useState<Summary | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [search, setSearch] = useState("");

  useEffect(() => { void loadData(); }, []);
  useEffect(() => {
    const assignmentId = params.get("stationAssignmentId");
    if (assignmentId) setForm((current) => ({ ...current, stationAssignmentId: assignmentId }));
  }, [params]);

  async function loadData() {
    setLoading(true);
    setError(null);
    try {
      const [recordData, assignmentData, machineData, summaryData] = await Promise.all([
        apiGet<unknown>("/cutting-records"),
        apiGet<unknown>("/cutting-records/available-station-assignments"),
        apiGet<unknown>("/cutting-machines"),
        apiGet<Summary>("/cutting-records/summary"),
      ]);
      setRecords(extractArray(recordData).map(mapRecord).filter(Boolean) as CuttingRecord[]);
      setAssignments(extractArray(assignmentData).map(mapAssignment).filter(Boolean) as Assignment[]);
      setMachines(extractArray(machineData).map(mapMachine).filter((item): item is Machine => Boolean(item?.isActive !== false)));
      setSummary(summaryData);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Kesim verileri alınamadı.");
    } finally {
      setLoading(false);
    }
  }

  async function saveCutting() {
    try {
      setError(null);
      const body = {
        stationAssignmentId: form.stationAssignmentId,
        cuttingMachineId: form.cuttingMachineId,
        operatorId: null,
        shift: numberOrNull(form.shift),
        inputPairs: toNumber(form.inputPairs),
        goodPairs: toNumber(form.goodPairs),
        rejectedPairs: toNumber(form.rejectedPairs),
        reworkPairs: toNumber(form.reworkPairs),
        notes: form.notes || null,
      };
      const created = await apiPost<CuttingRecord>("/cutting-records", body);
      await apiPost(`/cutting-records/${created.id}/complete`, {});
      setForm(emptyForm);
      setSuccess("Kesim kaydı oluşturuldu ve tamamlandı.");
      await loadData();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Kesim kaydı oluşturulamadı.");
    }
  }

  const selectedAssignment = assignments.find((item) => item.stationAssignmentId === form.stationAssignmentId);
  const filtered = useMemo(() => {
    const term = search.toLocaleLowerCase("tr-TR").trim();
    if (!term) return records;
    return records.filter((item) => [item.recordNumber, item.customerName, item.productName, item.productCode, item.workOrderNumber, item.cuttingMachineName].filter(Boolean).some((value) => String(value).toLocaleLowerCase("tr-TR").includes(term)));
  }, [records, search]);

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="mx-auto max-w-7xl space-y-6 px-4 py-6 sm:px-6 lg:px-8">
        <header className="border-b border-white/10 pb-6">
          <p className="text-sm font-bold tracking-[0.36em] text-emerald-400">FIXAR OS</p>
          <h1 className="mt-2 text-3xl font-black sm:text-4xl">Kesim</h1>
          <p className="mt-2 text-sm text-zinc-400">Enjeksiyon üretiminden gelen çiftlerin kesim operasyon kaydı.</p>
        </header>
        {(error || success) && <div className={"rounded-2xl border p-4 text-sm font-bold " + (error ? "border-red-400/30 bg-red-500/10 text-red-200" : "border-emerald-400/30 bg-emerald-500/10 text-emerald-200")}>{error ?? success}</div>}
        <section className="grid grid-cols-1 gap-4 md:grid-cols-3 xl:grid-cols-6">
          <Card label="Kesim Bekleyen" value={assignments.reduce((sum, x) => sum + x.remainingForCutting, 0)} />
          <Card label="Bugün Kesilen" value={summary?.todayCutPairs ?? 0} />
          <Card label="Sağlam Kesim" value={summary?.goodPairs ?? 0} tone="emerald" />
          <Card label="Kesim Firesi" value={summary?.rejectedPairs ?? 0} tone="red" />
          <Card label="Yeniden İşlem" value={summary?.reworkPairs ?? 0} tone="amber" />
          <Card label="Kolilenmeyi Bekleyen" value={summary?.waitingForPacking ?? 0} />
        </section>
        <section className="rounded-3xl border border-white/10 bg-white/[0.055] p-5">
          <h2 className="text-xl font-black">Yeni Kesim Kaydı</h2>
          <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-4">
            <Field label="Kesilebilir StationAssignment"><select className={CONTROL} value={form.stationAssignmentId} onChange={(e) => setForm({ ...form, stationAssignmentId: e.target.value })}><option value="">Seçin</option>{assignments.map((item) => <option key={item.stationAssignmentId} value={item.stationAssignmentId}>{item.stationNumber} - {item.customerName} - {item.productCode} {item.productName}</option>)}</select></Field>
            <Field label="Kesim Makinesi"><select className={CONTROL} value={form.cuttingMachineId} onChange={(e) => setForm({ ...form, cuttingMachineId: e.target.value })}><option value="">Seçin</option>{machines.map((item) => <option key={item.id} value={item.id}>{item.name} {item.machineType ? `(${item.machineType})` : ""}</option>)}</select></Field>
            <Field label="Vardiya"><input className={CONTROL} value={form.shift} onChange={(e) => setForm({ ...form, shift: e.target.value })} type="number" /></Field>
            <Field label="Girdi Çift"><input className={CONTROL} value={form.inputPairs} onChange={(e) => setForm({ ...form, inputPairs: e.target.value })} type="number" /></Field>
            <Field label="Sağlam"><input className={CONTROL} value={form.goodPairs} onChange={(e) => setForm({ ...form, goodPairs: e.target.value })} type="number" /></Field>
            <Field label="Fire"><input className={CONTROL} value={form.rejectedPairs} onChange={(e) => setForm({ ...form, rejectedPairs: e.target.value })} type="number" /></Field>
            <Field label="Yeniden İşlem"><input className={CONTROL} value={form.reworkPairs} onChange={(e) => setForm({ ...form, reworkPairs: e.target.value })} type="number" /></Field>
            <Field label="Not"><input className={CONTROL} value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></Field>
          </div>
          {selectedAssignment && <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-6"><Mini label="Üretilen" value={selectedAssignment.producedPairs} /><Mini label="Enj. Fire" value={selectedAssignment.injectionFirePairs} /><Mini label="Önce Kesilen" value={selectedAssignment.alreadyCutInputPairs} /><Mini label="Kalan" value={selectedAssignment.remainingForCutting} /><Mini label="İş Emri" value={selectedAssignment.workOrderNumber ?? "-"} /><Mini label="Müşteri" value={selectedAssignment.customerName ?? "-"} /></div>}
          <button onClick={saveCutting} className="mt-5 rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black">Kaydet</button>
        </section>
        <section className="rounded-3xl border border-white/10 bg-white/[0.055] p-5">
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between"><div><h2 className="text-xl font-black">Kesim Kayıtları</h2><p className="text-sm text-zinc-400">{filtered.length} kayıt</p></div><input className={CONTROL + " sm:max-w-sm"} value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Ara..." /></div>
          {loading ? <Empty text="Kesim kayıtları yükleniyor..." /> : <Table records={filtered} />}
        </section>
      </div>
    </main>
  );
}

function Table({ records }: { records: CuttingRecord[] }) {
  if (records.length === 0) return <Empty text="Kesim kaydı yok." />;
  return <div className="overflow-x-auto"><table className="w-full min-w-[1100px] text-left text-sm"><thead className="text-xs uppercase tracking-[0.16em] text-zinc-500"><tr>{["Kesim No","Tarih","İstasyon","İş Emri","Müşteri","Ürün","Girdi","Sağlam","Fire","Y.İşlem","Kolilenen","Kalan","Makine","Operatör","Durum"].map((h)=><th key={h} className="pb-3 pr-4">{h}</th>)}</tr></thead><tbody className="divide-y divide-white/10">{records.map((r)=><tr key={r.id}><td className="py-4 pr-4 font-black">{r.recordNumber}</td><td className="py-4 pr-4">{formatDate(r.recordDate)}</td><td className="py-4 pr-4">{r.stationNumber ?? "-"}</td><td className="py-4 pr-4">{r.workOrderNumber ?? "-"}</td><td className="py-4 pr-4">{r.customerName ?? "-"}</td><td className="py-4 pr-4">{[r.productCode,r.productName].filter(Boolean).join(" - ")}</td><td className="py-4 pr-4">{r.inputPairs}</td><td className="py-4 pr-4">{r.goodPairs}</td><td className="py-4 pr-4">{r.rejectedPairs}</td><td className="py-4 pr-4">{r.reworkPairs}</td><td className="py-4 pr-4">{r.boxedPairs}</td><td className="py-4 pr-4">{r.remainingForPacking}</td><td className="py-4 pr-4">{r.cuttingMachineName}</td><td className="py-4 pr-4">{r.operatorName ?? "-"}</td><td className="py-4 pr-4">{r.status}</td></tr>)}</tbody></table></div>;
}

function Card({ label, value, tone = "cyan" }: { label: string; value: number; tone?: "cyan" | "emerald" | "red" | "amber" }) { return <div className="rounded-2xl border border-white/10 bg-white/[0.055] p-5"><p className="text-xs font-bold uppercase tracking-[0.16em] text-zinc-500">{label}</p><p className={"mt-3 text-3xl font-black " + (tone === "emerald" ? "text-emerald-300" : tone === "red" ? "text-red-300" : tone === "amber" ? "text-amber-300" : "text-cyan-300")}>{value}</p></div>; }
function Field({ label, children }: { label: string; children: React.ReactNode }) { return <label><span className="mb-2 block text-xs font-bold uppercase tracking-[0.16em] text-zinc-500">{label}</span>{children}</label>; }
function Mini({ label, value }: { label: string; value: string | number }) { return <div className="rounded-xl border border-white/10 bg-black/20 p-3"><p className="text-xs text-zinc-500">{label}</p><p className="font-black">{value}</p></div>; }
function Empty({ text }: { text: string }) { return <div className="rounded-2xl border border-white/10 bg-black/20 p-10 text-center text-zinc-400">{text}</div>; }
async function apiGet<T>(path: string): Promise<T> { return apiRequest<T>(path, { method: "GET" }); }
async function apiPost<T>(path: string, body: unknown): Promise<T> { return apiRequest<T>(path, { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() }, body: JSON.stringify(body) }); }
async function apiRequest<T>(path: string, init: RequestInit): Promise<T> { const res = await fetch(API + path, init); const text = await res.text(); const payload = text ? JSON.parse(text) as ApiResponse<T> | T : undefined; if (!res.ok) { const p = isRecord(payload) ? payload as ApiResponse<T> : undefined; throw new Error(p?.message || p?.errorMessage || "İstek başarısız oldu."); } if (isRecord(payload) && "data" in payload) return (payload as ApiResponse<T>).data as T; return payload as T; }
function extractArray(value: unknown): unknown[] { if (Array.isArray(value)) return value; if (isRecord(value) && Array.isArray(value.data)) return value.data; return []; }
function mapRecord(value: unknown): CuttingRecord | null { if (!isRecord(value)) return null; return { id: String(value.id ?? ""), recordNumber: String(value.recordNumber ?? ""), recordDate: String(value.recordDate ?? ""), stationAssignmentId: readString(value.stationAssignmentId), stationNumber: numberOrNull(value.stationNumber), workOrderNumber: readString(value.workOrderNumber), customerName: readString(value.customerName), productCode: readString(value.productCode), productName: readString(value.productName), cuttingMachineId: String(value.cuttingMachineId ?? ""), cuttingMachineName: String(value.cuttingMachineName ?? ""), operatorName: readString(value.operatorName), inputPairs: toNumber(value.inputPairs), goodPairs: toNumber(value.goodPairs), rejectedPairs: toNumber(value.rejectedPairs), reworkPairs: toNumber(value.reworkPairs), boxedPairs: toNumber(value.boxedPairs), remainingForPacking: toNumber(value.remainingForPacking), status: String(value.status ?? "") }; }
function mapAssignment(value: unknown): Assignment | null { if (!isRecord(value)) return null; return { stationAssignmentId: String(value.stationAssignmentId ?? ""), stationNumber: toNumber(value.stationNumber), workOrderNumber: readString(value.workOrderNumber), customerName: readString(value.customerName), productCode: readString(value.productCode), productName: readString(value.productName), producedPairs: toNumber(value.producedPairs), injectionFirePairs: toNumber(value.injectionFirePairs), alreadyCutInputPairs: toNumber(value.alreadyCutInputPairs), remainingForCutting: toNumber(value.remainingForCutting) }; }
function mapMachine(value: unknown): Machine | null { if (!isRecord(value)) return null; return { id: String(value.id ?? ""), name: String(value.name ?? ""), machineType: readString(value.machineType) ?? undefined, operatorName: readString(value.operatorName) ?? undefined, isActive: value.isActive !== false }; }
function isRecord(value: unknown): value is Record<string, unknown> { return typeof value === "object" && value !== null; }
function readString(value: unknown): string | null { return typeof value === "string" && value.trim() ? value : null; }
function toNumber(value: unknown): number { const n = Number(value); return Number.isFinite(n) ? n : 0; }
function numberOrNull(value: unknown): number | null { const n = Number(value); return Number.isFinite(n) ? n : null; }
function formatDate(value: string) { return value ? new Intl.DateTimeFormat("tr-TR", { dateStyle: "short", timeStyle: "short" }).format(new Date(value)) : "-"; }
