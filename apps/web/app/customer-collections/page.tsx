"use client";

import { useEffect, useState } from "react";
import { FinanceNav } from "../components/finance/FinanceNav";
import { apiRequest } from "../lib/api/client";

const API = "/api/backend/api/v1/customer-collections";
type Row = { id: string; collectionNumber: string; customerName: string; collectionDate: string; currency: string; amount: number; paymentMethod: string; status: string; unallocatedAmount: number; isReversed: boolean; financePostingStatus?: string };
type Customer = { id: string; customerCode: string; name: string; companyName?: string; defaultCurrency: string };
type Account = { id: string; accountCode: string; name: string; accountType: string; currency: string; isActive: boolean };
const initial = { customerId: "", financialAccountId: "", collectionDate: new Date().toISOString().slice(0, 10), currency: "TRY", amount: "", paymentMethod: "BankTransfer", externalReference: "", bankReference: "", notes: "", autoAllocate: true, allowAdvance: true, exchangeRate: "1", reportingCurrency: "TRY" };

export default function Page() {
  const [rows, setRows] = useState<Row[]>([]), [customers, setCustomers] = useState<Customer[]>([]), [accounts, setAccounts] = useState<Account[]>([]), [message, setMessage] = useState(""), [busy, setBusy] = useState(false), [form, setForm] = useState(initial);
  async function load() {
    const [collections, customerLookup, accountLookup] = await Promise.all([apiRequest<Row[]>(API), apiRequest<Customer[]>("/api/backend/api/v1/customer-receivables/customer-lookup"), apiRequest<Account[]>("/api/backend/api/v1/financial-accounts?isActive=true")]);
    setRows(collections.data ?? []); setCustomers(customerLookup.data ?? []); setAccounts(accountLookup.data ?? []);
    if (!collections.ok) setMessage("Tahsilatlar yüklenemedi.");
  }
  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, []);
  async function record() {
    if (!form.customerId || !form.financialAccountId || !form.amount || !form.externalReference) { setMessage("Müşteri, hesap, tutar ve tekil referans zorunludur."); return; }
    setBusy(true); setMessage("");
    const result = await apiRequest(`${API}/record-and-allocate`, { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() }, body: JSON.stringify({ ...form, amount: Number(form.amount), exchangeRate: Number(form.exchangeRate), collectionDate: new Date(form.collectionDate).toISOString(), allocations: null }) });
    setBusy(false); setMessage(result.ok ? "Tahsilat, cari dağıtım ve kasa girişi tek işlemde kaydedildi." : (result.message ?? "Tahsilat kaydedilemedi."));
    if (result.ok) { setForm(initial); await load(); }
  }
  async function reverse(id: string) { const result = await apiRequest(`${API}/${id}/reverse`, { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() }, body: JSON.stringify({ reason: "Kullanıcı ters kayıt işlemi" }) }); setMessage(result.ok ? "Tahsilat ters kayıtla geri alındı." : (result.message ?? "Tahsilat geri alınamadı.")); if (result.ok) await load(); }
  const update = (key: string, value: string | boolean) => setForm(x => ({ ...x, [key]: value }));
  return <main className="min-h-screen bg-zinc-950 p-4 text-white md:p-6"><div className="mx-auto max-w-7xl space-y-5"><header><p className="text-emerald-300">ATOMİK TAHSİLAT</p><h1 className="text-3xl font-black">Müşteri Tahsilatları</h1></header><FinanceNav />{message && <p className={box}>{message}</p>}
    <section className={box}><h2 className="mb-3 font-black">Tahsilat ve Cari Dağıtım</h2><div className="grid gap-3 md:grid-cols-4">
      <select className={control} value={form.customerId} onChange={e => { const customer = customers.find(x => x.id === e.target.value); setForm(x => ({ ...x, customerId: e.target.value, currency: customer?.defaultCurrency ?? "TRY" })); }}><option value="">Müşteri seçin</option>{customers.map(x => <option value={x.id} key={x.id}>{x.customerCode} · {x.companyName ?? x.name}</option>)}</select>
      <select className={control} value={form.financialAccountId} onChange={e => update("financialAccountId", e.target.value)}><option value="">Kasa / banka seçin</option>{accounts.filter(x => x.currency === form.currency).map(x => <option value={x.id} key={x.id}>{x.accountCode} · {x.name}</option>)}</select>
      <input className={control} type="date" value={form.collectionDate} onChange={e => update("collectionDate", e.target.value)} />
      <select className={control} value={form.currency} onChange={e => update("currency", e.target.value)}>{["TRY", "EUR", "USD", "GBP"].map(x => <option key={x}>{x}</option>)}</select>
      <input className={control} type="number" min="0.01" step="0.01" placeholder="Tutar" value={form.amount} onChange={e => update("amount", e.target.value)} />
      <select className={control} value={form.paymentMethod} onChange={e => update("paymentMethod", e.target.value)}><option value="Cash">Nakit</option><option value="BankTransfer">Havale / EFT</option><option value="CreditCard">Kredi Kartı</option><option value="Other">Diğer</option></select>
      <input className={control} placeholder="Tekil işlem referansı" value={form.externalReference} onChange={e => update("externalReference", e.target.value)} />
      <input className={control} type="number" min="0.000001" step="0.000001" placeholder="Kur" value={form.exchangeRate} onChange={e => update("exchangeRate", e.target.value)} />
      <label className="flex items-center gap-2"><input type="checkbox" checked={form.autoAllocate} onChange={e => update("autoAllocate", e.target.checked)} />En eski alacaklardan otomatik kapat</label>
      <label className="flex items-center gap-2"><input type="checkbox" checked={form.allowAdvance} onChange={e => update("allowAdvance", e.target.checked)} />Kalanı müşteri avansı kaydet</label>
      <button disabled={busy} className="rounded-xl bg-emerald-400 px-4 py-2 font-black text-black disabled:opacity-50 md:col-span-2" onClick={() => void record()}>{busy ? "Kaydediliyor…" : "Tahsilatı Tek İşlemde Kaydet"}</button>
    </div></section>
    <div className={`${box} overflow-x-auto`}><table className="w-full min-w-[950px] text-left text-sm"><thead><tr>{["Tahsilat No", "Müşteri", "Tarih", "Tutar", "Avans / Dağıtılmamış", "Yöntem", "Finans", "Durum", "İşlem"].map(x => <th className="p-2" key={x}>{x}</th>)}</tr></thead><tbody>{rows.map(x => <tr className="border-t border-white/10" key={x.id}><td className="p-2 font-bold">{x.collectionNumber}</td><td>{x.customerName}</td><td>{new Date(x.collectionDate).toLocaleDateString("tr-TR")}</td><td>{x.amount} {x.currency}</td><td>{x.unallocatedAmount} {x.currency}</td><td>{x.paymentMethod}</td><td>{x.financePostingStatus ?? "-"}</td><td>{x.status}</td><td>{!x.isReversed && !["Draft", "Cancelled"].includes(x.status) && <button className={control} onClick={() => void reverse(x.id)}>Ters Kayıt</button>}</td></tr>)}</tbody></table>{rows.length === 0 && <p className="py-6 text-center text-zinc-400">Tahsilat bulunamadı.</p>}</div>
  </div></main>;
}
const box = "rounded-2xl border border-white/10 bg-white/5 p-4", control = "rounded-xl border border-white/10 bg-black/40 px-3 py-2";
