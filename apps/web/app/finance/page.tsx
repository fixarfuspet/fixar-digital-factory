"use client";

import { useEffect, useState } from "react";
import { FinanceOpsNav } from "../components/finance/FinanceOpsNav";
import { API_PROXY, apiRequest } from "../lib/api/client";

type Dashboard = {
  reportingCurrency: string; totalIncome: number; totalExpense: number; netCashFlow: number;
  accounts: Array<{ id: string; accountCode: string; name: string; accountType: string; currency: string; balance: number }>;
  receivables: Array<{ currency: string; outstanding: number; overdue: number }>;
  payables: Array<{ currency: string; outstanding: number; overdue: number }>;
  expenseCategories: Array<{ categoryName: string; amount: number }>;
  topCustomers: Array<{ customerId: string; name?: string; amount: number }>;
  topSuppliers: Array<{ supplierId: string; name?: string; amount: number }>;
};

export default function FinanceDashboard() {
  const [data, setData] = useState<Dashboard | null>(null);
  const [message, setMessage] = useState("");
  const [currency, setCurrency] = useState("TRY");
  useEffect(() => {
    void (async () => {
      const result = await apiRequest<Dashboard>(`${API_PROXY}/financial-transactions/dashboard?reportingCurrency=${currency}`);
      setData(result.data); setMessage(result.ok ? "" : "Finans özeti yüklenemedi.");
    })();
  }, [currency]);
  const money = (value: number, unit = currency) => new Intl.NumberFormat("tr-TR", { style: "currency", currency: unit }).format(value);
  return <main className="min-h-screen bg-zinc-950 p-4 text-white md:p-6"><div className="mx-auto max-w-7xl space-y-5">
    <header className="flex flex-wrap items-end justify-between gap-3"><div><p className="text-lime-300">TEK FİNANS OMURGASI</p><h1 className="text-3xl font-black">Finans Yönetim Merkezi</h1></div><select className={control} value={currency} onChange={e => setCurrency(e.target.value)}>{["TRY", "EUR", "USD", "GBP"].map(x => <option key={x}>{x}</option>)}</select></header>
    <FinanceOpsNav />{message && <p className={box}>{message}</p>}
    <section className="flex flex-wrap gap-2">{[["cash-transactions", "Kasa Hareketleri"], ["income-expense", "Gelir / Gider"]].flatMap(([report, label]) => [<a className={control} key={`${report}-xlsx`} href={`${API_PROXY}/finance-exports/${report}.xlsx?currency=${currency}`}>{label} XLSX</a>, <a className={control} key={`${report}-pdf`} href={`${API_PROXY}/finance-exports/${report}.pdf?currency=${currency}`}>{label} PDF</a>])}</section>
    <section className="grid gap-3 sm:grid-cols-3">{[["Toplam Gelir", data?.totalIncome ?? 0], ["Toplam Gider", data?.totalExpense ?? 0], ["Net Nakit Akışı", data?.netCashFlow ?? 0]].map(([label, value]) => <article className={box} key={String(label)}><p className="text-sm text-zinc-400">{label}</p><p className="text-2xl font-black">{money(Number(value))}</p></article>)}</section>
    <section className="grid gap-4 lg:grid-cols-2"><Panel title="Kasa ve Banka Bakiyeleri">{data?.accounts.map(x => <Row key={x.id} label={`${x.accountCode} · ${x.name}`} value={money(x.balance, x.currency)} />)}</Panel><Panel title="Açık Cari Bakiyeler">{data?.receivables.map(x => <Row key={`r-${x.currency}`} label={`${x.currency} müşteri alacağı`} value={money(x.outstanding, x.currency)} />)}{data?.payables.map(x => <Row key={`p-${x.currency}`} label={`${x.currency} tedarikçi borcu`} value={money(x.outstanding, x.currency)} />)}</Panel><Panel title="Gider Dağılımı">{data?.expenseCategories.map(x => <Row key={x.categoryName} label={x.categoryName} value={money(x.amount)} />)}</Panel><Panel title="Öne Çıkan Taraflar">{data?.topCustomers.map(x => <Row key={x.customerId} label={`Müşteri · ${x.name ?? "-"}`} value={money(x.amount)} />)}{data?.topSuppliers.map(x => <Row key={x.supplierId} label={`Tedarikçi · ${x.name ?? "-"}`} value={money(x.amount)} />)}</Panel></section>
  </div></main>;
}
function Panel({ title, children }: { title: string; children: React.ReactNode }) { return <section className={box}><h2 className="mb-3 text-lg font-black">{title}</h2><div className="space-y-2">{children}</div></section>; }
function Row({ label, value }: { label: string; value: string }) { return <div className="flex justify-between gap-3 border-t border-white/10 py-2"><span>{label}</span><strong>{value}</strong></div>; }
const box = "rounded-2xl border border-white/10 bg-white/5 p-4", control = "rounded-xl border border-white/10 bg-zinc-900 px-3 py-2";
