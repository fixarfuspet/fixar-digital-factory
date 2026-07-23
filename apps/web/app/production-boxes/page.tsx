"use client";
import { authenticatedFetch, API_PROXY } from "@/app/lib/api/client";


import { useEffect, useMemo, useState } from "react";
import Link from "next/link";

type ApiResponse<T> = { data?: T; message?: string; errorMessage?: string };
type Box = { id: string; boxNumber: string; cuttingRecordId?: string | null; cuttingRecordNumber?: string | null; customerName?: string | null; productCode?: string | null; productName?: string | null; workOrderNumber?: string | null; pairCount: number; status: string; warehouseLocation?: string | null; rackCode?: string | null; shipmentReference?: string | null; packedAt?: string | null; receivedToWarehouseAt?: string | null; readyForShipmentAt?: string | null; shippedAt?: string | null };
type CuttingRecord = { id: string; recordNumber: string; customerName?: string | null; productName?: string | null; goodPairs: number; boxedPairs: number; remainingForPacking: number; status: string };
type Summary = { totalBoxes?: number; totalPairs?: number; packedBoxes?: number; packedPairs?: number; warehouseBoxes?: number; readyBoxes?: number; shippedBoxes?: number };

const API = API_PROXY;
const CONTROL = "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none focus:border-emerald-400/60";

export default function ProductionBoxesPage() {
  const [boxes, setBoxes] = useState<Box[]>([]);
  const [records, setRecords] = useState<CuttingRecord[]>([]);
  const [summary, setSummary] = useState<Summary | null>(null);
  const [cuttingRecordId, setCuttingRecordId] = useState("");
  const [pairCount, setPairCount] = useState("");
  const [note, setNote] = useState("");
  const [location, setLocation] = useState("");
  const [rack, setRack] = useState("");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => { void loadData(); }, []);

  async function loadData() {
    setLoading(true);
    setError(null);
    try {
      const [boxData, cuttingData, summaryData] = await Promise.all([
        apiGet<unknown>("/production-boxes"),
        apiGet<unknown>("/cutting-records?status=Completed&isActive=true"),
        apiGet<Summary>("/production-boxes/summary"),
      ]);
      setBoxes(extractArray(boxData).map(mapBox).filter(Boolean) as Box[]);
      setRecords(extractArray(cuttingData).map(mapCutting).filter((item): item is CuttingRecord => Boolean(item && item.remainingForPacking > 0)));
      setSummary(summaryData);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Koli verileri alınamadı.");
    } finally {
      setLoading(false);
    }
  }

  async function createBox() {
    try {
      setError(null);
      await apiPost("/production-boxes", { cuttingRecordId, pairCount: toNumber(pairCount), note: note || null });
      setCuttingRecordId("");
      setPairCount("");
      setNote("");
      setSuccess("Koli oluşturuldu.");
      await loadData();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Koli oluşturulamadı.");
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

  const selectedRecord = records.find((item) => item.id === cuttingRecordId);
  const filtered = useMemo(() => {
    const term = search.toLocaleLowerCase("tr-TR").trim();
    if (!term) return boxes;
    return boxes.filter((box) => [box.boxNumber, box.cuttingRecordNumber, box.customerName, box.productName, box.productCode, box.workOrderNumber, box.status].filter(Boolean).some((value) => String(value).toLocaleLowerCase("tr-TR").includes(term)));
  }, [boxes, search]);

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="mx-auto max-w-7xl space-y-6 px-4 py-6 sm:px-6 lg:px-8">
        <Link href="/traceability" className="inline-block rounded-xl border border-violet-400/30 bg-violet-500/10 px-4 py-2 text-sm font-black text-violet-200">QR ve İzlenebilirlik</Link>
        <header className="border-b border-white/10 pb-6"><p className="text-sm font-bold tracking-[0.36em] text-emerald-400">FIXAR OS</p><h1 className="mt-2 text-3xl font-black sm:text-4xl">Koli Yönetimi</h1><p className="mt-2 text-sm text-zinc-400">Kesimden gelen sağlam çiftleri kolile ve statü geçmişini izle.</p></header>
        {(error || success) && <div className={"rounded-2xl border p-4 text-sm font-bold " + (error ? "border-red-400/30 bg-red-500/10 text-red-200" : "border-emerald-400/30 bg-emerald-500/10 text-emerald-200")}>{error ?? success}</div>}
        <section className="grid grid-cols-1 gap-4 md:grid-cols-3 xl:grid-cols-6">
          <Card label="Toplam Koli" value={summary?.totalBoxes ?? 0} />
          <Card label="Kolilenen Çift" value={summary?.totalPairs ?? 0} />
          <Card label="Depoya Bekleyen" value={summary?.packedBoxes ?? 0} />
          <Card label="Depodaki" value={summary?.warehouseBoxes ?? 0} />
          <Card label="Sevkiyata Hazır" value={summary?.readyBoxes ?? 0} tone="emerald" />
          <Card label="Sevk Edilen" value={summary?.shippedBoxes ?? 0} />
        </section>
        <section className="rounded-3xl border border-white/10 bg-white/[0.055] p-5">
          <h2 className="text-xl font-black">Yeni Koli</h2>
          <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-4">
            <Field label="Tamamlanmış Kesim"><select className={CONTROL} value={cuttingRecordId} onChange={(e) => setCuttingRecordId(e.target.value)}><option value="">Seçin</option>{records.map((record) => <option key={record.id} value={record.id}>{record.recordNumber} - {record.customerName} - kalan {record.remainingForPacking}</option>)}</select></Field>
            <Field label="Kolilenecek Çift"><input className={CONTROL} value={pairCount} onChange={(e) => setPairCount(e.target.value)} type="number" /></Field>
            <Field label="Not"><input className={CONTROL} value={note} onChange={(e) => setNote(e.target.value)} /></Field>
            <div className="flex items-end"><button onClick={createBox} className="w-full rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black">Koli Oluştur</button></div>
          </div>
          {selectedRecord && <div className="mt-4 grid grid-cols-3 gap-3 text-sm"><Mini label="Sağlam Kesim" value={selectedRecord.goodPairs} /><Mini label="Kolilenen" value={selectedRecord.boxedPairs} /><Mini label="Kalan" value={selectedRecord.remainingForPacking} /></div>}
        </section>
        <section className="rounded-3xl border border-white/10 bg-white/[0.055] p-5">
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between"><div><h2 className="text-xl font-black">Koliler</h2><p className="text-sm text-zinc-400">{filtered.length} kayıt</p></div><input className={CONTROL + " sm:max-w-sm"} value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Ara..." /></div>
          {loading ? <Empty text="Koliler yükleniyor..." /> : filtered.length === 0 ? <Empty text="Koli kaydı yok." /> : <div className="overflow-x-auto"><table className="w-full min-w-[1100px] text-left text-sm"><thead className="text-xs uppercase tracking-[0.16em] text-zinc-500"><tr>{["Koli No","Kesim No","Müşteri","Ürün","İş Emri","Çift","Durum","Depo","Raf","Sevkiyat","Tarihler","İşlemler"].map((h)=><th key={h} className="pb-3 pr-4">{h}</th>)}</tr></thead><tbody className="divide-y divide-white/10">{filtered.map((box)=><tr key={box.id}><td className="py-4 pr-4 font-black">{box.boxNumber}</td><td className="py-4 pr-4">{box.cuttingRecordNumber ?? "-"}</td><td className="py-4 pr-4">{box.customerName ?? "-"}</td><td className="py-4 pr-4">{[box.productCode, box.productName].filter(Boolean).join(" - ")}</td><td className="py-4 pr-4">{box.workOrderNumber ?? "-"}</td><td className="py-4 pr-4">{box.pairCount}</td><td className="py-4 pr-4">{translateStatus(box.status)}</td><td className="py-4 pr-4">{box.warehouseLocation ?? "-"}</td><td className="py-4 pr-4">{box.rackCode ?? "-"}</td><td className="py-4 pr-4">{box.shipmentReference ?? "-"}</td><td className="py-4 pr-4 text-xs text-zinc-400">{box.shippedAt ? formatDate(box.shippedAt) : box.readyForShipmentAt ? formatDate(box.readyForShipmentAt) : box.receivedToWarehouseAt ? formatDate(box.receivedToWarehouseAt) : formatDate(box.packedAt)}</td><td className="space-x-2 py-4 pr-4">{box.status === "Packed" && <button onClick={() => action(`/production-boxes/${box.id}/receive-to-warehouse`, { warehouseLocation: location || "Mamül Depo", rackCode: rack || null, note: null }, "Koli depoya alındı.")} className="rounded-lg bg-cyan-500 px-3 py-2 text-xs font-black text-black">Depoya Al</button>}{box.status === "InWarehouse" && <button onClick={() => action(`/production-boxes/${box.id}/mark-ready-for-shipment`, { warehouseLocation: box.warehouseLocation, rackCode: box.rackCode, note: null }, "Koli sevkiyata hazır.")} className="rounded-lg bg-emerald-500 px-3 py-2 text-xs font-black text-black">Hazır Yap</button>}</td></tr>)}</tbody></table></div>}
          <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2"><input className={CONTROL} value={location} onChange={(e)=>setLocation(e.target.value)} placeholder="Depo konumu" /><input className={CONTROL} value={rack} onChange={(e)=>setRack(e.target.value)} placeholder="Raf" /></div>
        </section>
      </div>
    </main>
  );
}

function Card({ label, value, tone = "cyan" }: { label: string; value: number; tone?: "cyan" | "emerald" }) { return <div className="rounded-2xl border border-white/10 bg-white/[0.055] p-5"><p className="text-xs font-bold uppercase tracking-[0.16em] text-zinc-500">{label}</p><p className={"mt-3 text-3xl font-black " + (tone === "emerald" ? "text-emerald-300" : "text-cyan-300")}>{value}</p></div>; }
function Field({ label, children }: { label: string; children: React.ReactNode }) { return <label><span className="mb-2 block text-xs font-bold uppercase tracking-[0.16em] text-zinc-500">{label}</span>{children}</label>; }
function Mini({ label, value }: { label: string; value: string | number }) { return <div className="rounded-xl border border-white/10 bg-black/20 p-3"><p className="text-xs text-zinc-500">{label}</p><p className="font-black">{value}</p></div>; }
function Empty({ text }: { text: string }) { return <div className="rounded-2xl border border-white/10 bg-black/20 p-10 text-center text-zinc-400">{text}</div>; }
async function apiGet<T>(path: string): Promise<T> { return apiRequest<T>(path, { method: "GET" }); }
async function apiPost<T>(path: string, body: unknown): Promise<T> { return apiRequest<T>(path, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) }); }
async function apiRequest<T>(path: string, init: RequestInit): Promise<T> { const res = await authenticatedFetch(API + path, init); const text = await res.text(); const payload = text ? JSON.parse(text) as ApiResponse<T> | T : undefined; if (!res.ok) { const p = isRecord(payload) ? payload as ApiResponse<T> : undefined; throw new Error(p?.message || p?.errorMessage || "İstek başarısız oldu."); } if (isRecord(payload) && "data" in payload) return (payload as ApiResponse<T>).data as T; return payload as T; }
function extractArray(value: unknown): unknown[] { if (Array.isArray(value)) return value; if (isRecord(value) && Array.isArray(value.data)) return value.data; return []; }
function mapBox(value: unknown): Box | null { if (!isRecord(value)) return null; return { id: String(value.id ?? ""), boxNumber: String(value.boxNumber ?? ""), cuttingRecordId: readString(value.cuttingRecordId), cuttingRecordNumber: readString(value.cuttingRecordNumber), customerName: readString(value.customerName), productCode: readString(value.productCode), productName: readString(value.productName), workOrderNumber: readString(value.workOrderNumber), pairCount: toNumber(value.pairCount), status: String(value.status ?? ""), warehouseLocation: readString(value.warehouseLocation), rackCode: readString(value.rackCode), shipmentReference: readString(value.shipmentReference), packedAt: readString(value.packedAt), receivedToWarehouseAt: readString(value.receivedToWarehouseAt), readyForShipmentAt: readString(value.readyForShipmentAt), shippedAt: readString(value.shippedAt) }; }
function mapCutting(value: unknown): CuttingRecord | null { if (!isRecord(value)) return null; return { id: String(value.id ?? ""), recordNumber: String(value.recordNumber ?? ""), customerName: readString(value.customerName), productName: readString(value.productName), goodPairs: toNumber(value.goodPairs), boxedPairs: toNumber(value.boxedPairs), remainingForPacking: toNumber(value.remainingForPacking), status: String(value.status ?? "") }; }
function isRecord(value: unknown): value is Record<string, unknown> { return typeof value === "object" && value !== null; }
function readString(value: unknown): string | null { return typeof value === "string" && value.trim() ? value : null; }
function toNumber(value: unknown): number { const n = Number(value); return Number.isFinite(n) ? n : 0; }
function formatDate(value?: string | null) { return value ? new Intl.DateTimeFormat("tr-TR", { dateStyle: "short", timeStyle: "short" }).format(new Date(value)) : "-"; }
function translateStatus(status: string) { return ({ Packed: "Kolilendi", InWarehouse: "Depoda", ReadyForShipment: "Sevkiyata Hazır", Shipped: "Sevk Edildi", Cancelled: "İptal" } as Record<string, string>)[status] ?? status; }
