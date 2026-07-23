"use client";

import { useEffect, useState } from "react";
import { FinanceOpsNav } from "../components/finance/FinanceOpsNav";
import { API_PROXY, apiRequest } from "../lib/api/client";

type Account = { id: string; accountCode: string; name: string; currency: string; isActive: boolean };
type Category = { id: string; code: string; name: string; categoryType: string; isActive: boolean };
const initial = { financialAccountId: "", transactionDate: new Date().toISOString().slice(0, 10), direction: "Outflow", transactionType: "Expense", currency: "TRY", amount: "", exchangeRate: "1", reportingCurrency: "TRY", financeCategoryId: "", paymentMethod: "BankTransfer", counterpartyName: "", documentNumber: "", referenceNumber: "", description: "", businessReference: "" };

export default function IncomeExpensePage() {
  const [accounts, setAccounts] = useState<Account[]>([]), [categories, setCategories] = useState<Category[]>([]), [form, setForm] = useState(initial), [message, setMessage] = useState(""), [busy, setBusy] = useState(false);
  useEffect(() => { void (async () => { const [a, c] = await Promise.all([apiRequest<Account[]>(`${API_PROXY}/financial-accounts?isActive=true`), apiRequest<Category[]>(`${API_PROXY}/finance-categories?isActive=true`)]); setAccounts(a.data ?? []); setCategories(c.data ?? []); })(); }, []);
  const update = (key: string, value: string) => setForm(x => ({ ...x, [key]: value }));
  async function save() { setBusy(true); setMessage(""); const action = form.direction === "Inflow" ? "manual-income" : "manual-expense"; const result = await apiRequest<{ transactionNumber: string }>(`${API_PROXY}/financial-transactions/${action}`, { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() }, body: JSON.stringify({ ...form, transactionDate: new Date(form.transactionDate).toISOString(), amount: Number(form.amount), exchangeRate: Number(form.exchangeRate), financeCategoryId: form.financeCategoryId || null, businessReference: form.businessReference || null }) }); setBusy(false); setMessage(result.ok ? `Finans hareketi kaydedildi: ${result.data?.transactionNumber ?? ""}` : (result.message ?? "Finans hareketi kaydedilemedi.")); if (result.ok) setForm(initial); }
  return <main className="min-h-screen bg-zinc-950 p-4 text-white md:p-6"><div className="mx-auto max-w-6xl space-y-5"><header><p className="text-lime-300">AUDİTLİ KASA HAREKETİ</p><h1 className="text-3xl font-black">Gelir ve Gider Kaydı</h1></header><FinanceOpsNav />{message && <p className={box}>{message}</p>}<section className={`${box} grid gap-3 md:grid-cols-3`}>
    <Field label="Hesap"><select className={control} value={form.financialAccountId} onChange={e => update("financialAccountId", e.target.value)}><option value="">Hesap seçin</option>{accounts.map(x => <option key={x.id} value={x.id}>{x.accountCode} · {x.name} ({x.currency})</option>)}</select></Field>
    <Field label="İşlem tarihi"><input className={control} type="date" value={form.transactionDate} onChange={e => update("transactionDate", e.target.value)} /></Field>
    <Field label="Yön"><select className={control} value={form.direction} onChange={e => update("direction", e.target.value)}><option value="Inflow">Gelir</option><option value="Outflow">Gider</option></select></Field>
    <Field label="Kategori"><select className={control} value={form.financeCategoryId} onChange={e => update("financeCategoryId", e.target.value)}><option value="">Kategori seçin</option>{categories.filter(x => x.categoryType === (form.direction === "Inflow" ? "Income" : "Expense")).map(x => <option key={x.id} value={x.id}>{x.code} · {x.name}</option>)}</select></Field>
    <Field label="Tutar"><input className={control} type="number" min="0.01" step="0.01" value={form.amount} onChange={e => update("amount", e.target.value)} /></Field>
    <Field label="Para birimi"><select className={control} value={form.currency} onChange={e => update("currency", e.target.value)}>{["TRY", "EUR", "USD", "GBP"].map(x => <option key={x}>{x}</option>)}</select></Field>
    <Field label="Kur"><input className={control} type="number" min="0.000001" step="0.000001" value={form.exchangeRate} onChange={e => update("exchangeRate", e.target.value)} /></Field>
    <Field label="Ödeme yöntemi"><select className={control} value={form.paymentMethod} onChange={e => update("paymentMethod", e.target.value)}>{[["Cash", "Nakit"], ["BankTransfer", "Havale / EFT"], ["CreditCard", "Kredi Kartı"], ["Cheque", "Çek"], ["Other", "Diğer"]].map(x => <option key={x[0]} value={x[0]}>{x[1]}</option>)}</select></Field>
    <Field label="Karşı taraf"><input className={control} value={form.counterpartyName} onChange={e => update("counterpartyName", e.target.value)} /></Field>
    <Field label="Belge no"><input className={control} value={form.documentNumber} onChange={e => update("documentNumber", e.target.value)} /></Field>
    <Field label="Referans"><input className={control} value={form.referenceNumber} onChange={e => update("referenceNumber", e.target.value)} /></Field>
    <Field label="Açıklama"><input className={control} value={form.description} onChange={e => update("description", e.target.value)} /></Field>
    <button disabled={busy} onClick={() => void save()} className="rounded-xl bg-lime-300 px-5 py-3 font-black text-black disabled:opacity-50 md:col-span-3">{busy ? "Kaydediliyor…" : "Finans Hareketini Kaydet"}</button>
  </section></div></main>;
}
function Field({ label, children }: { label: string; children: React.ReactNode }) { return <label className="space-y-1 text-sm"><span className="text-zinc-400">{label}</span>{children}</label>; }
const box = "rounded-2xl border border-white/10 bg-white/5 p-4", control = "block w-full rounded-xl border border-white/10 bg-zinc-900 px-3 py-2";
