"use client";

import { useEffect, useState } from "react";
import { FinanceNav } from "../components/finance/FinanceNav";
import { apiRequest } from "../lib/api/client";

type Customer = { id: string; customerCode: string; name: string; companyName?: string };
type Balance = { currency: string; totalReceivable: number; totalCollected: number; unallocatedCollection: number; outstandingBalance: number; overdueBalance: number };
type Entry = { id: string; transactionDate: string; referenceNumber: string; sourceType: string; description?: string; dueDate?: string; debitAmount: number; creditAmount: number; runningBalance: number; currency: string };

export default function Page() {
  const [customers, setCustomers] = useState<Customer[]>([]), [customer, setCustomer] = useState(""), [currency, setCurrency] = useState("TRY"), [balances, setBalances] = useState<Balance[]>([]), [entries, setEntries] = useState<Entry[]>([]), [aging, setAging] = useState<Record<string, unknown>[]>([]), [message, setMessage] = useState("");
  useEffect(() => { void (async () => setCustomers((await apiRequest<Customer[]>("/api/backend/api/v1/customer-receivables/customer-lookup")).data ?? []))(); }, []);
  async function load(id = customer, unit = currency) { if (!id) return; const base = "/api/backend/api/v1/customer-ledger"; const [b, s, a] = await Promise.all([apiRequest<Balance[]>(`${base}/customer/${id}/balance`), apiRequest<Entry[]>(`${base}/customer/${id}/statement?currency=${unit}`), apiRequest<Record<string, unknown>[]>(`${base}/aging?customerId=${id}`)]); setBalances(b.data ?? []); setEntries(s.data ?? []); setAging(a.data ?? []); setMessage(b.ok && s.ok && a.ok ? "" : "Müşteri cari bilgileri yüklenemedi."); }
  const exportBase = `/api/backend/api/v1/finance-exports`;
  return <main className="min-h-screen bg-zinc-950 p-4 text-white md:p-6"><div className="mx-auto max-w-7xl space-y-5"><header><p className="text-cyan-300">CARİ HESAP</p><h1 className="text-3xl font-black">Müşteri Cari Ekstresi</h1></header><FinanceNav />{message && <p className={box}>{message}</p>}
    <div className="flex flex-wrap gap-2"><select className={control} value={customer} onChange={e => { setCustomer(e.target.value); void load(e.target.value); }}><option value="">Müşteri seçin</option>{customers.map(x => <option key={x.id} value={x.id}>{x.customerCode} · {x.companyName ?? x.name}</option>)}</select><select className={control} value={currency} onChange={e => { setCurrency(e.target.value); void load(customer, e.target.value); }}>{["TRY", "EUR", "USD", "GBP"].map(x => <option key={x}>{x}</option>)}</select>{customer && <>{[["customer-statement", "Ekstre"], ["customer-reconciliation", "Mutabakat"]].flatMap(([report, label]) => [<a className={control} key={`${report}-xlsx`} href={`${exportBase}/${report}.xlsx?customerId=${customer}&currency=${currency}`}>{label} XLSX</a>, <a className={control} key={`${report}-pdf`} href={`${exportBase}/${report}.pdf?customerId=${customer}&currency=${currency}`}>{label} PDF</a>])}</>}</div>
    <section className="grid gap-3 md:grid-cols-5">{balances.filter(x => x.currency === currency).flatMap(x => [[`${x.currency} Borç`, x.totalReceivable], [`${x.currency} Tahsilat`, x.totalCollected], [`${x.currency} Bakiye`, x.outstandingBalance], [`${x.currency} Geciken`, x.overdueBalance], [`${x.currency} Avans`, x.unallocatedCollection]] as Array<[string, number]>).map(([label, value]) => <div className={box} key={label}><small>{label}</small><p className="text-xl font-black">{value.toLocaleString("tr-TR")}</p></div>)}</section>
    <section className={box}><h2 className="font-black">Yaşlandırma</h2><pre className="mt-2 overflow-auto text-xs text-zinc-300">{JSON.stringify(aging, null, 2)}</pre></section>
    <div className={`${box} overflow-x-auto`}><table className="w-full min-w-[1000px] text-left text-sm"><thead><tr>{["Tarih", "Belge No", "Kaynak", "Açıklama", "Vade", "Borç", "Alacak", "Bakiye", "Para Birimi"].map(x => <th className="p-2" key={x}>{x}</th>)}</tr></thead><tbody>{entries.map(x => <tr className="border-t border-white/10" key={x.id}><td className="p-2">{date(x.transactionDate)}</td><td>{x.referenceNumber}</td><td>{x.sourceType}</td><td>{x.description ?? "-"}</td><td>{x.dueDate ? date(x.dueDate) : "-"}</td><td>{x.debitAmount}</td><td>{x.creditAmount}</td><td>{x.runningBalance}</td><td>{x.currency}</td></tr>)}</tbody></table>{entries.length === 0 && customer && <p className="py-5 text-center text-zinc-400">Cari hareket bulunamadı.</p>}</div>
  </div></main>;
}
const box = "rounded-2xl border border-white/10 bg-white/5 p-4", control = "rounded-xl border border-white/10 bg-black/40 px-4 py-2";
const date = (value: string) => new Date(value).toLocaleDateString("tr-TR");
