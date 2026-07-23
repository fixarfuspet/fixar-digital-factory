"use client";

import { useEffect, useState } from "react";
import { FinanceOpsNav } from "../components/finance/FinanceOpsNav";
import { API_PROXY, safeResponseJson, authenticatedFetch } from "../lib/api/client";

type Row = { id: string; transactionNumber: string; accountName: string; transactionDate: string; transactionType: string; direction: string; sourceType: string; currency: string; amount: number; referenceNumber?: string; isReversed: boolean };
type Summary = { currency: string; todayIn: number; todayOut: number; monthIn: number; monthOut: number };

export default function Page() {
  const [rows, setRows] = useState<Row[]>([]), [summary, setSummary] = useState<Summary[]>([]);
  useEffect(() => { void (async () => {
    const [transactions, totals] = await Promise.all([authenticatedFetch(`${API_PROXY}/financial-transactions`), authenticatedFetch(`${API_PROXY}/financial-transactions/summary`)]);
    setRows((await safeResponseJson<Row[]>(transactions)).data ?? []);
    setSummary((await safeResponseJson<Summary[]>(totals)).data ?? []);
  })(); }, []);
  return <main className="min-h-screen bg-zinc-950 p-6 text-white"><div className="mx-auto max-w-7xl space-y-5"><h1 className="text-3xl font-black">Finansal Hareketler</h1><FinanceOpsNav />
    <section className="grid gap-3 md:grid-cols-6">{summary.flatMap(x => [[`${x.currency} Bugün Giriş`, x.todayIn], [`${x.currency} Bugün Çıkış`, x.todayOut], [`${x.currency} Bugün Net`, x.todayIn - x.todayOut], [`${x.currency} Ay Giriş`, x.monthIn], [`${x.currency} Ay Çıkış`, x.monthOut], [`${x.currency} Ay Net`, x.monthIn - x.monthOut]] as Array<[string, number]>).map(([label, value]) => <div className={box} key={label}><small>{label}</small><p className="text-xl font-black">{value.toLocaleString("tr-TR")}</p></div>)}</section>
    <div className={`${box} overflow-auto`}><table className="w-full min-w-[1000px] text-left"><thead><tr>{["İşlem No", "Tarih", "Hesap", "Yön", "Tür", "Kaynak", "Referans", "Tutar", "Para Birimi", "Durum"].map(x => <th className="p-2" key={x}>{x}</th>)}</tr></thead><tbody>{rows.map(x => <tr className="border-t border-white/10" key={x.id}><td className="p-2">{x.transactionNumber}</td><td>{new Date(x.transactionDate).toLocaleDateString("tr-TR")}</td><td>{x.accountName}</td><td>{x.direction}</td><td>{x.transactionType}</td><td>{x.sourceType}</td><td>{x.referenceNumber ?? "-"}</td><td>{x.amount}</td><td>{x.currency}</td><td>{x.isReversed ? "Geri Alındı" : "Aktif"}</td></tr>)}</tbody></table></div>
  </div></main>;
}

const box = "rounded-2xl border border-white/10 bg-white/5 p-4";
