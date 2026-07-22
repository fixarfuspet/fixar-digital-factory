"use client";

import { useEffect, useState } from "react";
import { SupplierFinanceView } from "../components/finance/SupplierFinanceView";
import { apiRequest } from "../lib/api/client";

type Supplier = { id: string; supplierCode?: string; name: string };
type Account = { id: string; accountCode: string; name: string; currency: string };
const initial = { supplierId: "", financialAccountId: "", paymentDate: new Date().toISOString().slice(0, 10), currency: "TRY", amount: "", paymentMethod: "BankTransfer", externalReference: "", autoAllocate: true, allowAdvance: true, exchangeRate: "1", reportingCurrency: "TRY", bankReference: "", notes: "" };

export default function Page() {
  const [suppliers, setSuppliers] = useState<Supplier[]>([]), [accounts, setAccounts] = useState<Account[]>([]), [form, setForm] = useState(initial), [message, setMessage] = useState(""), [busy, setBusy] = useState(false), [refresh, setRefresh] = useState(0);
  useEffect(() => { void (async () => { const [s, a] = await Promise.all([apiRequest<Supplier[]>("/api/backend/api/v1/suppliers"), apiRequest<Account[]>("/api/backend/api/v1/financial-accounts?isActive=true")]); setSuppliers(s.data ?? []); setAccounts(a.data ?? []); })(); }, []);
  const update = (key: string, value: string | boolean) => setForm(x => ({ ...x, [key]: value }));
  async function record() {
    if (!form.supplierId || !form.financialAccountId || !form.amount || !form.externalReference) { setMessage("Tedarikçi, hesap, tutar ve tekil referans zorunludur."); return; }
    setBusy(true); setMessage("");
    const result = await apiRequest("/api/backend/api/v1/supplier-payments/record-and-allocate", { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() }, body: JSON.stringify({ ...form, paymentDate: new Date(form.paymentDate).toISOString(), amount: Number(form.amount), exchangeRate: Number(form.exchangeRate), allocations: null }) });
    setBusy(false); setMessage(result.ok ? "Ödeme, cari dağıtım ve kasa çıkışı tek işlemde kaydedildi." : (result.message ?? "Tedarikçi ödemesi kaydedilemedi."));
    if (result.ok) { setForm(initial); setRefresh(x => x + 1); }
  }
  return <><main className="bg-zinc-950 px-4 pt-5 text-white md:px-6"><div className="mx-auto max-w-7xl"><section className="rounded-2xl border border-white/10 bg-white/5 p-4"><h2 className="mb-3 text-xl font-black">Atomik Tedarikçi Ödemesi</h2>{message && <p className="mb-3 rounded-xl bg-white/10 p-3">{message}</p>}<div className="grid gap-3 md:grid-cols-4">
    <select className={control} value={form.supplierId} onChange={e => update("supplierId", e.target.value)}><option value="">Tedarikçi seçin</option>{suppliers.map(x => <option key={x.id} value={x.id}>{x.supplierCode ? `${x.supplierCode} · ` : ""}{x.name}</option>)}</select>
    <select className={control} value={form.financialAccountId} onChange={e => update("financialAccountId", e.target.value)}><option value="">Kasa / banka seçin</option>{accounts.filter(x => x.currency === form.currency).map(x => <option key={x.id} value={x.id}>{x.accountCode} · {x.name}</option>)}</select>
    <input className={control} type="date" value={form.paymentDate} onChange={e => update("paymentDate", e.target.value)} />
    <select className={control} value={form.currency} onChange={e => update("currency", e.target.value)}>{["TRY", "EUR", "USD", "GBP"].map(x => <option key={x}>{x}</option>)}</select>
    <input className={control} type="number" min="0.01" step="0.01" placeholder="Tutar" value={form.amount} onChange={e => update("amount", e.target.value)} />
    <select className={control} value={form.paymentMethod} onChange={e => update("paymentMethod", e.target.value)}><option value="Cash">Nakit</option><option value="BankTransfer">Havale / EFT</option><option value="CreditCard">Kredi Kartı</option><option value="Other">Diğer</option></select>
    <input className={control} placeholder="Tekil işlem referansı" value={form.externalReference} onChange={e => update("externalReference", e.target.value)} />
    <input className={control} type="number" min="0.000001" step="0.000001" placeholder="Kur" value={form.exchangeRate} onChange={e => update("exchangeRate", e.target.value)} />
    <label className="flex items-center gap-2"><input type="checkbox" checked={form.autoAllocate} onChange={e => update("autoAllocate", e.target.checked)} />En eski borçlardan otomatik kapat</label>
    <label className="flex items-center gap-2"><input type="checkbox" checked={form.allowAdvance} onChange={e => update("allowAdvance", e.target.checked)} />Kalanı tedarikçi avansı kaydet</label>
    <button disabled={busy} onClick={() => void record()} className="rounded-xl bg-lime-300 px-4 py-2 font-black text-black disabled:opacity-50 md:col-span-2">{busy ? "Kaydediliyor…" : "Ödemeyi Tek İşlemde Kaydet"}</button>
  </div></section></div></main><div key={refresh}><SupplierFinanceView title="Tedarikçi Ödemeleri" endpoint="supplier-payments" /></div></>;
}
const control = "rounded-xl border border-white/10 bg-black/40 px-3 py-2";
