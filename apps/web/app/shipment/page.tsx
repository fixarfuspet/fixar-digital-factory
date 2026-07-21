"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";

type ApiResponse<T> = { data?: T; message?: string; errorMessage?: string };
type Box = { id: string; boxNumber: string; customerName?: string | null; productName?: string | null; productCode?: string | null; workOrderNumber?: string | null; pairCount: number; status: string; warehouseLocation?: string | null; rackCode?: string | null; readyForShipmentAt?: string | null; shippedAt?: string | null; shipmentReference?: string | null };
const API = (process.env.NEXT_PUBLIC_API_BASE_URL?.trim() || "/api/backend/api/v1").replace(/\/$/, "");
const CONTROL = "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none focus:border-emerald-400/60";

export default function ShipmentPage() {
  const [boxes, setBoxes] = useState<Box[]>([]);
  const [selected, setSelected] = useState<string[]>([]);
  const [tab, setTab] = useState<"ready" | "shipped">("ready");
  const [reference, setReference] = useState("");
  const [notes, setNotes] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => { void loadData(); }, []);

  async function loadData() {
    try {
      const data = await apiGet<unknown>("/production-boxes");
      setBoxes(extractArray(data).map(mapBox).filter(Boolean) as Box[]);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Sevkiyat verileri alınamadı.");
    }
  }

  async function shipSelected() {
    try {
      setError(null);
      await apiPost("/production-boxes/bulk-ship", { boxIds: selected, shipmentReference: reference, shipmentDate: new Date().toISOString(), notes: notes || null });
      setSuccess("Toplu sevkiyat tamamlandı.");
      setSelected([]);
      setReference("");
      setNotes("");
      await loadData();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Sevkiyat tamamlanamadı.");
    }
  }

  const ready = boxes.filter((box) => box.status === "ReadyForShipment");
  const shipped = boxes.filter((box) => box.status === "Shipped");
  const list = tab === "ready" ? ready : shipped;
  const selectedBoxes = useMemo(() => ready.filter((box) => selected.includes(box.id)), [ready, selected]);
  const selectedPairs = selectedBoxes.reduce((sum, box) => sum + box.pairCount, 0);

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="mx-auto max-w-7xl space-y-6 px-4 py-6 sm:px-6 lg:px-8">
        <Link href="/traceability" className="inline-block rounded-xl border border-violet-400/30 bg-violet-500/10 px-4 py-2 text-sm font-black text-violet-200">Sevkiyat İzlenebilirliği · QR / Etiket</Link>
        <header className="border-b border-white/10 pb-6"><p className="text-sm font-bold tracking-[0.36em] text-emerald-400">FIXAR OS</p><h1 className="mt-2 text-3xl font-black sm:text-4xl">Sevkiyat</h1><p className="mt-2 text-sm text-zinc-400">Sevkiyata hazır kolileri seç, referansla toplu sevk et.</p></header>
        {(error || success) && <div className={"rounded-2xl border p-4 text-sm font-bold " + (error ? "border-red-400/30 bg-red-500/10 text-red-200" : "border-emerald-400/30 bg-emerald-500/10 text-emerald-200")}>{error ?? success}</div>}
        <section className="rounded-3xl border border-white/10 bg-white/[0.055] p-5">
          <div className="mb-4 flex gap-2">{(["ready","shipped"] as const).map((item)=><button key={item} onClick={()=>setTab(item)} className={"rounded-xl px-4 py-2 text-sm font-black " + (tab===item ? "bg-emerald-500 text-black" : "bg-white/[0.07] text-zinc-300")}>{item === "ready" ? "Sevkiyata Hazır" : "Sevk Edilenler"}</button>)}</div>
          {tab === "ready" && <div className="mb-5 grid grid-cols-1 gap-3 md:grid-cols-4"><input className={CONTROL} value={reference} onChange={(e)=>setReference(e.target.value)} placeholder="Sevkiyat referansı" /><input className={CONTROL} value={notes} onChange={(e)=>setNotes(e.target.value)} placeholder="Not" /><div className="rounded-xl border border-white/10 bg-black/20 p-3 text-sm"><b>{selected.length}</b> koli · <b>{selectedPairs}</b> çift</div><button onClick={shipSelected} disabled={selected.length===0} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black disabled:opacity-50">Toplu Sevk Et</button></div>}
          {list.length === 0 ? <Empty text="Kayıt bulunamadı." /> : <div className="overflow-x-auto"><table className="w-full min-w-[980px] text-left text-sm"><thead className="text-xs uppercase tracking-[0.16em] text-zinc-500"><tr>{(tab==="ready"?["","Koli No","Müşteri","Ürün","İş Emri","Çift","Depo/Raf","Hazır Tarihi"]:["Koli No","Müşteri","Ürün","Çift","Sevkiyat Referansı","Sevk Tarihi"]).map((h)=><th key={h} className="pb-3 pr-4">{h}</th>)}</tr></thead><tbody className="divide-y divide-white/10">{list.map((box)=><tr key={box.id}>{tab==="ready" && <td className="py-4 pr-4"><input type="checkbox" checked={selected.includes(box.id)} onChange={(e)=>setSelected((current)=>e.target.checked?[...current,box.id]:current.filter((id)=>id!==box.id))} /></td>}<td className="py-4 pr-4 font-black">{box.boxNumber}</td><td className="py-4 pr-4">{box.customerName ?? "-"}</td><td className="py-4 pr-4">{[box.productCode, box.productName].filter(Boolean).join(" - ")}</td>{tab==="ready" && <td className="py-4 pr-4">{box.workOrderNumber ?? "-"}</td>}<td className="py-4 pr-4">{box.pairCount}</td>{tab==="ready" ? <><td className="py-4 pr-4">{[box.warehouseLocation, box.rackCode].filter(Boolean).join(" / ") || "-"}</td><td className="py-4 pr-4">{formatDate(box.readyForShipmentAt)}</td></> : <><td className="py-4 pr-4">{box.shipmentReference ?? "-"}</td><td className="py-4 pr-4">{formatDate(box.shippedAt)}</td></>}</tr>)}</tbody></table></div>}
        </section>
      </div>
    </main>
  );
}

function Empty({ text }: { text: string }) { return <div className="rounded-2xl border border-white/10 bg-black/20 p-10 text-center text-zinc-400">{text}</div>; }
async function apiGet<T>(path: string): Promise<T> { return apiRequest<T>(path, { method: "GET" }); }
async function apiPost<T>(path: string, body: unknown): Promise<T> { return apiRequest<T>(path, { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() }, body: JSON.stringify(body) }); }
async function apiRequest<T>(path: string, init: RequestInit): Promise<T> { const res = await fetch(API + path, init); const text = await res.text(); const payload = text ? JSON.parse(text) as ApiResponse<T> | T : undefined; if (!res.ok) { const p = isRecord(payload) ? payload as ApiResponse<T> : undefined; throw new Error(p?.message || p?.errorMessage || "İstek başarısız oldu."); } if (isRecord(payload) && "data" in payload) return (payload as ApiResponse<T>).data as T; return payload as T; }
function extractArray(value: unknown): unknown[] { if (Array.isArray(value)) return value; if (isRecord(value) && Array.isArray(value.data)) return value.data; return []; }
function mapBox(value: unknown): Box | null { if (!isRecord(value)) return null; return { id: String(value.id ?? ""), boxNumber: String(value.boxNumber ?? ""), customerName: readString(value.customerName), productCode: readString(value.productCode), productName: readString(value.productName), workOrderNumber: readString(value.workOrderNumber), pairCount: toNumber(value.pairCount), status: String(value.status ?? ""), warehouseLocation: readString(value.warehouseLocation), rackCode: readString(value.rackCode), readyForShipmentAt: readString(value.readyForShipmentAt), shippedAt: readString(value.shippedAt), shipmentReference: readString(value.shipmentReference) }; }
function isRecord(value: unknown): value is Record<string, unknown> { return typeof value === "object" && value !== null; }
function readString(value: unknown): string | null { return typeof value === "string" && value.trim() ? value : null; }
function toNumber(value: unknown): number { const n = Number(value); return Number.isFinite(n) ? n : 0; }
function formatDate(value?: string | null) { return value ? new Intl.DateTimeFormat("tr-TR", { dateStyle: "short", timeStyle: "short" }).format(new Date(value)) : "-"; }
