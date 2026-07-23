"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { API_PROXY, apiRequest } from "../lib/api/client";

type Supplier = { id: string; code?: string; name: string };
type Entry = { id: string; transactionDate: string; referenceNumber: string; sourceType: string; dueDate?: string; debitAmount: number; creditAmount: number; runningBalance: number; currency: string; description?: string };
const nav = [["/supplier-payables", "Tedarikçi Borçları"], ["/supplier-payments", "Tedarikçi Ödemeleri"], ["/supplier-ledger", "Tedarikçi Cari"], ["/cheque-endorsements", "Çek Ciroları"]];

export default function Page() {
  const [suppliers, setSuppliers] = useState<Supplier[]>([]), [supplier, setSupplier] = useState(""), [currency, setCurrency] = useState("TRY"), [entries, setEntries] = useState<Entry[]>([]), [message, setMessage] = useState("");
  useEffect(() => { void (async () => setSuppliers((await apiRequest<Supplier[]>(`${API_PROXY}/suppliers`)).data ?? []))(); }, []);
  async function load(id: string, unit = currency) { if (!id) return; const result = await apiRequest<Entry[]>(`${API_PROXY}/supplier-ledger/supplier/${id}/statement`); setEntries((result.data ?? []).filter(x => x.currency === unit)); setMessage(result.ok ? "" : "Tedarikçi cari bilgileri yüklenemedi."); }
  const exportBase = `${API_PROXY}/finance-exports`;
  return <main className="min-h-screen bg-zinc-950 p-4 text-white md:p-6"><div className="mx-auto max-w-7xl space-y-5"><header><p className="text-lime-300">CARİ HESAP</p><h1 className="text-3xl font-black">Tedarikçi Cari Ekstresi</h1></header><nav className="flex flex-wrap gap-2">{nav.map(x => <Link className={control} href={x[0]} key={x[0]}>{x[1]}</Link>)}</nav>{message && <p className={box}>{message}</p>}
    <div className="flex flex-wrap gap-2"><select className={control} value={supplier} onChange={e => { setSupplier(e.target.value); void load(e.target.value); }}><option value="">Tedarikçi seçin</option>{suppliers.map(x => <option key={x.id} value={x.id}>{x.code ? `${x.code} · ` : ""}{x.name}</option>)}</select><select className={control} value={currency} onChange={e => { setCurrency(e.target.value); if (supplier) void load(supplier, e.target.value); }}>{["TRY", "EUR", "USD", "GBP"].map(x => <option key={x}>{x}</option>)}</select>{supplier && <>{[["supplier-statement", "Ekstre"], ["supplier-reconciliation", "Mutabakat"]].flatMap(([report, label]) => [<a className={control} key={`${report}-xlsx`} href={`${exportBase}/${report}.xlsx?supplierId=${supplier}&currency=${currency}`}>{label} XLSX</a>, <a className={control} key={`${report}-pdf`} href={`${exportBase}/${report}.pdf?supplierId=${supplier}&currency=${currency}`}>{label} PDF</a>])}</>}</div>
    <div className={`${box} overflow-x-auto`}><table className="w-full min-w-[1000px] text-left text-sm"><thead><tr>{["Tarih", "Belge No", "Kaynak", "Açıklama", "Vade", "Borç", "Alacak", "Bakiye", "Para Birimi"].map(x => <th className="p-2" key={x}>{x}</th>)}</tr></thead><tbody>{entries.map(x => <tr className="border-t border-white/10" key={x.id}><td className="p-2">{date(x.transactionDate)}</td><td>{x.referenceNumber}</td><td>{x.sourceType}</td><td>{x.description ?? "-"}</td><td>{x.dueDate ? date(x.dueDate) : "-"}</td><td>{x.debitAmount}</td><td>{x.creditAmount}</td><td>{x.runningBalance}</td><td>{x.currency}</td></tr>)}</tbody></table>{entries.length === 0 && supplier && <p className="py-5 text-center text-zinc-400">Cari hareket bulunamadı.</p>}</div>
  </div></main>;
}
const box = "rounded-2xl border border-white/10 bg-white/5 p-4", control = "rounded-xl border border-white/10 bg-black/40 px-4 py-2";
const date = (value: string) => new Date(value).toLocaleDateString("tr-TR");
