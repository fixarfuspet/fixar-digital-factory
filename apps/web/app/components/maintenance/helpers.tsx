"use client";
import { useState } from "react";
import { api, post } from "./api";
import { Action, field } from "./ui";
import type { MaintenanceRow as Row } from "./types";

export function Backfill({ onDone, show }: { onDone: () => Promise<void>; show: (s: string, ok?: boolean) => void }) {
  const [type, setType] = useState("Machine");
  const [ids, setIds] = useState("");
  const [preview, setPreview] = useState<Row | null>(null);
  async function previewNow() { try { setPreview(await api<Row>("maintenance-assets/backfill-preview")); } catch (e) { show((e as Error).message); } }
  async function create() { try { await post("maintenance-assets/backfill", { assetType: type, ids: ids.split(",").map(x => x.trim()).filter(Boolean) }); show("Seçili varlıklar oluşturuldu.", true); await onDone(); } catch (e) { show((e as Error).message); } }
  return <div className="space-y-4">
    <label>Varlık türü<select className={field} value={type} onChange={e => setType(e.target.value)}>{["Machine", "InjectionStation", "CuttingMachine", "Mold"].map(x => <option key={x}>{x}</option>)}</select></label>
    <Action onClick={previewNow}>Preview Getir</Action>
    {preview && <pre className="overflow-auto rounded-xl bg-black/30 p-3 text-xs">{JSON.stringify(preview, null, 2)}</pre>}
    <label>Seçili kimlikler (virgülle)<textarea className={field} value={ids} onChange={e => setIds(e.target.value)} /></label>
    <Action onClick={create}>Seçilileri Oluştur</Action>
  </div>;
}
export function fmt(v: unknown) { if (v === null || v === undefined || v === "") return "—"; if (typeof v === "string" && /^\d{4}-\d{2}-\d{2}T/.test(v)) return new Date(v).toLocaleString("tr-TR"); return String(v); }
export function label(k: string) { return ({ requestNumber: "Talep No", maintenanceWorkOrderNumber: "İş Emri No", planCode: "Plan Kodu", assetCode: "Varlık Kodu", assetName: "Varlık", asset: "Ekipman", title: "Başlık", name: "Ad", priority: "Öncelik", status: "Durum", reportedAt: "Bildirim", plannedStart: "Planlanan", nextDueDate: "Sonraki Bakım", workType: "İş Türü", frequencyType: "Sıklık", assetType: "Tür", criticality: "Kritiklik", maintenanceStrategy: "Strateji", isActive: "Aktif" } as Record<string,string>)[k] ?? k.replace(/([A-Z])/g, " $1"); }
