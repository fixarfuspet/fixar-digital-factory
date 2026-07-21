"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";

type ApiResponse<T> = { data?: T; message?: string; errorMessage?: string };
type Box = { id: string; boxNumber: string; customerName?: string | null; productName?: string | null; productCode?: string | null; workOrderNumber?: string | null; pairCount: number; status: string; warehouseLocation?: string | null; rackCode?: string | null; receivedToWarehouseAt?: string | null };
type Summary = { warehouseBoxes?: number; warehousePairs?: number; readyBoxes?: number; readyPairs?: number };
const API = (process.env.NEXT_PUBLIC_API_BASE_URL?.trim() || "/api/backend/api/v1").replace(/\/$/, "");
const CONTROL = "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none focus:border-emerald-400/60";

export default function WarehousePage() {
  const [boxes, setBoxes] = useState<Box[]>([]);
  const [summary, setSummary] = useState<Summary | null>(null);
  const [location, setLocation] = useState("Mamül Depo");
  const [rack, setRack] = useState("");
  const [search, setSearch] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => { void loadData(); }, []);

  async function loadData() {
    try {
      const [boxData, summaryData] = await Promise.all([apiGet<unknown>("/production-boxes"), apiGet<Summary>("/production-boxes/summary")]);
      setBoxes(extractArray(boxData).map(mapBox).filter(Boolean) as Box[]);
      setSummary(summaryData);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Depo verileri alınamadı.");
    }
  }

  async function action(path: string, body: unknown, message: string) {
    try {
      setError(null);
      await apiPost(path, body);
      setSuccess(message);
      await loadData();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "İşlem başarısız.");
    }
  }

  const filtered = useMemo(() => {
    const term = search.toLocaleLowerCase("tr-TR").trim();
    return boxes
      .filter((box) => box.status === "InWarehouse" || box.status === "ReadyForShipment" || box.status === "Packed")
      .filter((box) => !term || [box.boxNumber, box.customerName, box.productName, box.productCode, box.workOrderNumber, box.warehouseLocation, box.rackCode].filter(Boolean).some((value) => String(value).toLocaleLowerCase("tr-TR").includes(term)));
  }, [boxes, search]);

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="mx-auto max-w-7xl space-y-6 px-4 py-6 sm:px-6 lg:px-8">
        <Link href="/traceability" className="inline-block rounded-xl border border-violet-400/30 bg-violet-500/10 px-4 py-2 text-sm font-black text-violet-200">Koli Ara · QR · İzlenebilirlik</Link>
        <header className="border-b border-white/10 pb-6"><p className="text-sm font-bold tracking-[0.36em] text-emerald-400">FIXAR OS</p><h1 className="mt-2 text-3xl font-black sm:text-4xl">Bitmiş Ürün Deposu</h1><p className="mt-2 text-sm text-zinc-400">Hammadde stoklarından bağımsız, ProductionBox statüleriyle mamül depo yönetimi.</p></header>
        {(error || success) && <div className={"rounded-2xl border p-4 text-sm font-bold " + (error ? "border-red-400/30 bg-red-500/10 text-red-200" : "border-emerald-400/30 bg-emerald-500/10 text-emerald-200")}>{error ?? success}</div>}
        <section className="grid grid-cols-1 gap-4 md:grid-cols-4"><Card label="Depodaki Koli" value={summary?.warehouseBoxes ?? 0} /><Card label="Depodaki Çift" value={summary?.warehousePairs ?? 0} /><Card label="Sevkiyata Hazır Koli" value={summary?.readyBoxes ?? 0} tone="emerald" /><Card label="Sevkiyata Hazır Çift" value={summary?.readyPairs ?? 0} tone="emerald" /></section>
        <section className="rounded-3xl border border-white/10 bg-white/[0.055] p-5">
          <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-3"><input className={CONTROL} value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Koli, müşteri, ürün, raf ara..." /><input className={CONTROL} value={location} onChange={(e) => setLocation(e.target.value)} placeholder="Depo konumu" /><input className={CONTROL} value={rack} onChange={(e) => setRack(e.target.value)} placeholder="Raf" /></div>
          {filtered.length === 0 ? <Empty text="Depo kaydı bulunamadı." /> : <div className="overflow-x-auto"><table className="w-full min-w-[980px] text-left text-sm"><thead className="text-xs uppercase tracking-[0.16em] text-zinc-500"><tr>{["Koli No","Müşteri","Ürün","İş Emri","Çift","Depo Konumu","Raf","Durum","Depoya Giriş","İşlemler"].map((h)=><th key={h} className="pb-3 pr-4">{h}</th>)}</tr></thead><tbody className="divide-y divide-white/10">{filtered.map((box)=><tr key={box.id}><td className="py-4 pr-4 font-black">{box.boxNumber}</td><td className="py-4 pr-4">{box.customerName ?? "-"}</td><td className="py-4 pr-4">{[box.productCode, box.productName].filter(Boolean).join(" - ")}</td><td className="py-4 pr-4">{box.workOrderNumber ?? "-"}</td><td className="py-4 pr-4">{box.pairCount}</td><td className="py-4 pr-4">{box.warehouseLocation ?? "-"}</td><td className="py-4 pr-4">{box.rackCode ?? "-"}</td><td className="py-4 pr-4">{translateStatus(box.status)}</td><td className="py-4 pr-4">{formatDate(box.receivedToWarehouseAt)}</td><td className="space-x-2 py-4 pr-4">{box.status === "Packed" && <button onClick={() => action(`/production-boxes/${box.id}/receive-to-warehouse`, { warehouseLocation: location, rackCode: rack || null, note: null }, "Depoya kabul edildi.")} className="rounded-lg bg-cyan-500 px-3 py-2 text-xs font-black text-black">Depoya Kabul</button>}{box.status === "InWarehouse" && <button onClick={() => action(`/production-boxes/${box.id}/mark-ready-for-shipment`, { warehouseLocation: box.warehouseLocation ?? location, rackCode: box.rackCode ?? rack, note: null }, "Sevkiyata hazır.")} className="rounded-lg bg-emerald-500 px-3 py-2 text-xs font-black text-black">Hazır Yap</button>}</td></tr>)}</tbody></table></div>}
        </section>
      </div>
    </main>
  );
}

function Card({ label, value, tone = "cyan" }: { label: string; value: number; tone?: "cyan" | "emerald" }) { return <div className="rounded-2xl border border-white/10 bg-white/[0.055] p-5"><p className="text-xs font-bold uppercase tracking-[0.16em] text-zinc-500">{label}</p><p className={"mt-3 text-3xl font-black " + (tone === "emerald" ? "text-emerald-300" : "text-cyan-300")}>{value}</p></div>; }
function Empty({ text }: { text: string }) { return <div className="rounded-2xl border border-white/10 bg-black/20 p-10 text-center text-zinc-400">{text}</div>; }
async function apiGet<T>(path: string): Promise<T> { return apiRequest<T>(path, { method: "GET" }); }
async function apiPost<T>(path: string, body: unknown): Promise<T> { return apiRequest<T>(path, { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() }, body: JSON.stringify(body) }); }
async function apiRequest<T>(path: string, init: RequestInit): Promise<T> { const res = await fetch(API + path, init); const text = await res.text(); const payload = text ? JSON.parse(text) as ApiResponse<T> | T : undefined; if (!res.ok) { const p = isRecord(payload) ? payload as ApiResponse<T> : undefined; throw new Error(p?.message || p?.errorMessage || "İstek başarısız oldu."); } if (isRecord(payload) && "data" in payload) return (payload as ApiResponse<T>).data as T; return payload as T; }
function extractArray(value: unknown): unknown[] { if (Array.isArray(value)) return value; if (isRecord(value) && Array.isArray(value.data)) return value.data; return []; }
function mapBox(value: unknown): Box | null { if (!isRecord(value)) return null; return { id: String(value.id ?? ""), boxNumber: String(value.boxNumber ?? ""), customerName: readString(value.customerName), productCode: readString(value.productCode), productName: readString(value.productName), workOrderNumber: readString(value.workOrderNumber), pairCount: toNumber(value.pairCount), status: String(value.status ?? ""), warehouseLocation: readString(value.warehouseLocation), rackCode: readString(value.rackCode), receivedToWarehouseAt: readString(value.receivedToWarehouseAt) }; }
function isRecord(value: unknown): value is Record<string, unknown> { return typeof value === "object" && value !== null; }
function readString(value: unknown): string | null { return typeof value === "string" && value.trim() ? value : null; }
function toNumber(value: unknown): number { const n = Number(value); return Number.isFinite(n) ? n : 0; }
function formatDate(value?: string | null) { return value ? new Intl.DateTimeFormat("tr-TR", { dateStyle: "short", timeStyle: "short" }).format(new Date(value)) : "-"; }
function translateStatus(status: string) { return ({ Packed: "Kolilendi", InWarehouse: "Depoda", ReadyForShipment: "Sevkiyata Hazır", Shipped: "Sevk Edildi", Cancelled: "İptal" } as Record<string, string>)[status] ?? status; }
