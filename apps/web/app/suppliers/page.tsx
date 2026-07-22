"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import { safeResponseJson } from "../lib/api/client";

type DashboardTone = "emerald" | "red" | "cyan" | "amber";
type DialogMode = "create" | "edit" | null;

type Supplier = {
  id: string;
  name?: string | null;
  code?: string | null;
  contactPerson?: string | null;
  phone?: string | null;
  email?: string | null;
  taxOffice?: string | null;
  taxNumber?: string | null;
  address?: string | null;
  defaultCurrency?: string | null;
  paymentTermDays?: number | null;
  note?: string | null;
  isActive?: boolean | null;
  createdAt?: string | null;
};

type SupplierFormState = {
  name: string;
  code: string;
  contactPerson: string;
  phone: string;
  email: string;
  taxOffice: string;
  taxNumber: string;
  address: string;
  defaultCurrency: string;
  paymentTermDays: string;
  note: string;
  isActive: boolean;
};

const API = "/api/backend/api/v1";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60";

export default function SuppliersPage() {
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [dialogMode, setDialogMode] = useState<DialogMode>(null);
  const [dialogSupplier, setDialogSupplier] = useState<Supplier | null>(null);
  const [detailSupplier, setDetailSupplier] = useState<Supplier | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    loadSuppliers();
  }, []);

  async function loadSuppliers() {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(API + "/suppliers");

      if (!response.ok) {
        throw new Error("Tedarikçi listesi alınamadı.");
      }

      const result: unknown = await safeResponseJson(response);
      setSuppliers(extractSuppliers(result));
    } catch (err) {
      setSuppliers([]);
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  function openForm(mode: DialogMode, supplier: Supplier | null = null) {
    setSuccessMessage(null);
    setDialogMode(mode);
    setDialogSupplier(supplier);
  }

  function closeForm() {
    setDialogMode(null);
    setDialogSupplier(null);
  }

  function handleSaved(message: string) {
    closeForm();
    setSuccessMessage(message);
    loadSuppliers();
  }

  const filteredSuppliers = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase("tr-TR");

    if (!normalizedSearch) return suppliers;

    return suppliers.filter((supplier) =>
      [
        supplier.name,
        supplier.code,
        supplier.contactPerson,
        supplier.phone,
        supplier.email,
        supplier.taxOffice,
        supplier.taxNumber,
        supplier.address,
      ]
        .filter(Boolean)
        .some((value) => String(value).toLocaleLowerCase("tr-TR").includes(normalizedSearch))
    );
  }, [search, suppliers]);

  const activeCount = suppliers.filter((supplier) => supplier.isActive !== false).length;
  const passiveCount = suppliers.length - activeCount;
  const terms = suppliers.map((supplier) => supplier.paymentTermDays).filter((value): value is number => typeof value === "number");
  const averageTerm = terms.length ? Math.round(terms.reduce((sum, value) => sum + value, 0) / terms.length) : 0;
  const dashboardCards = [
    {
      title: "Toplam Tedarikçi",
      value: suppliers.length.toLocaleString("tr-TR"),
      note: "Kayıtlı firma sayısı",
      tone: "emerald" as DashboardTone,
    },
    {
      title: "Aktif Tedarikçi",
      value: activeCount.toLocaleString("tr-TR"),
      note: "Satın almaya açık",
      tone: "cyan" as DashboardTone,
    },
    {
      title: "Pasif Tedarikçi",
      value: passiveCount.toLocaleString("tr-TR"),
      note: "Geçici olarak kapalı",
      tone: "red" as DashboardTone,
    },
    {
      title: "Ortalama Vade",
      value: `${averageTerm.toLocaleString("tr-TR")} gün`,
      note: "Tanımlı ödeme vadeleri",
      tone: "amber" as DashboardTone,
    },
  ];

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.18),transparent_34%),radial-gradient(circle_at_bottom_right,rgba(14,165,233,0.13),transparent_32%)] px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.38em] text-emerald-300">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">Tedarikçi Yönetimi</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                Firma bilgileri, ödeme vadeleri ve satın alma iletişimlerini tek merkezden yönetin.
              </p>
            </div>

            <div className="flex flex-col gap-3 sm:flex-row">
              <button
                onClick={() => {
                  setSuccessMessage(null);
                  loadSuppliers();
                }}
                disabled={loading}
                className="rounded-xl border border-white/10 bg-white/[0.08] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:opacity-50"
              >
                {loading ? "Yenileniyor..." : "Yenile"}
              </button>
              <button onClick={() => openForm("create")} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
                + Yeni Tedarikçi
              </button>
            </div>
          </header>

          {successMessage && (
            <div className="rounded-xl border border-emerald-400/30 bg-emerald-500/10 p-4 text-sm font-bold text-emerald-100">
              {successMessage}
            </div>
          )}

          <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {dashboardCards.map((card) => (
              <DashboardCard key={card.title} title={card.title} value={card.value} note={card.note} tone={card.tone} />
            ))}
          </section>

          <section className="rounded-2xl border border-white/10 bg-white/[0.06] p-5 shadow-2xl backdrop-blur">
            <div className="flex flex-col gap-4 border-b border-white/10 pb-5 xl:flex-row xl:items-end xl:justify-between">
              <div>
                <h2 className="text-2xl font-black">Tedarikçi Listesi</h2>
                <p className="mt-1 text-sm text-zinc-400">
                  {filteredSuppliers.length.toLocaleString("tr-TR")} tedarikçi listeleniyor.
                </p>
              </div>
              <div className="w-full xl:max-w-md">
                <Field label="Arama">
                  <input
                    value={search}
                    onChange={(event) => setSearch(event.target.value)}
                    className={CONTROL_CLASS}
                    placeholder="Firma, kod, yetkili, telefon, e-posta"
                  />
                </Field>
              </div>
            </div>

            {loading && <LoadingState />}

            {!loading && error && (
              <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-5 text-sm text-red-100">
                <p className="font-black">Tedarikçi listesi yüklenemedi.</p>
                <p className="mt-1 text-red-200">{error}</p>
              </div>
            )}

            {!loading && !error && filteredSuppliers.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-zinc-300">
                Tedarikçi kaydı bulunmuyor.
              </div>
            )}

            {!loading && !error && filteredSuppliers.length > 0 && (
              <div className="mt-5 overflow-hidden rounded-xl border border-white/10 bg-black/20">
                <div className="overflow-x-auto">
                  <table className="w-full min-w-[980px] border-collapse text-left">
                    <thead className="bg-white/[0.06] text-xs uppercase tracking-[0.16em] text-zinc-400">
                      <tr>
                        <TableHead>Firma</TableHead>
                        <TableHead>Kod</TableHead>
                        <TableHead>Yetkili</TableHead>
                        <TableHead>Telefon</TableHead>
                        <TableHead>E-posta</TableHead>
                        <TableHead>Para Birimi</TableHead>
                        <TableHead>Vade</TableHead>
                        <TableHead>Durum</TableHead>
                        <TableHead>İşlem</TableHead>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-white/10">
                      {filteredSuppliers.map((supplier) => (
                        <tr key={supplier.id} className="transition hover:bg-white/[0.04]">
                          <TableCell>
                            <span className="font-black text-white">{supplier.name || "-"}</span>
                          </TableCell>
                          <TableCell>{supplier.code || "-"}</TableCell>
                          <TableCell>{supplier.contactPerson || "-"}</TableCell>
                          <TableCell>{supplier.phone || "-"}</TableCell>
                          <TableCell>{supplier.email || "-"}</TableCell>
                          <TableCell>{supplier.defaultCurrency || "TRY"}</TableCell>
                          <TableCell>{formatTerm(supplier.paymentTermDays)}</TableCell>
                          <TableCell>
                            <StatusBadge active={supplier.isActive !== false} />
                          </TableCell>
                          <TableCell>
                            <div className="flex gap-2">
                              <button
                                onClick={() => setDetailSupplier(supplier)}
                                className="rounded-lg border border-white/10 bg-white/[0.06] px-3 py-2 text-xs font-bold text-zinc-200 transition hover:bg-white/[0.1]"
                              >
                                Detay
                              </button>
                              <button
                                onClick={() => openForm("edit", supplier)}
                                className="rounded-lg border border-emerald-400/20 bg-emerald-500/10 px-3 py-2 text-xs font-bold text-emerald-100 transition hover:bg-emerald-500/20"
                              >
                                Düzenle
                              </button>
                            </div>
                          </TableCell>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </section>
        </div>
      </div>

      <SupplierFormModal
        mode={dialogMode}
        supplier={dialogSupplier}
        onClose={closeForm}
        onSaved={handleSaved}
      />
      <SupplierDetailModal
        supplier={detailSupplier}
        onClose={() => setDetailSupplier(null)}
        onEdit={(supplier) => {
          setDetailSupplier(null);
          openForm("edit", supplier);
        }}
      />
    </main>
  );
}

function SupplierFormModal({
  mode,
  supplier,
  onClose,
  onSaved,
}: {
  mode: DialogMode;
  supplier: Supplier | null;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const [form, setForm] = useState<SupplierFormState>(() => toSupplierForm(supplier));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!mode) return;
    const timer = window.setTimeout(() => {
      setForm(toSupplierForm(supplier));
      setError(null);
      setSaving(false);
    }, 0);
    return () => window.clearTimeout(timer);
  }, [mode, supplier]);

  if (!mode) return null;

  const isEdit = mode === "edit" && supplier;

  function updateForm(key: keyof SupplierFormState, value: string | boolean) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function saveSupplier() {
    if (saving) return;

    setError(null);

    if (!form.name.trim()) {
      setError("Firma adı zorunludur.");
      return;
    }

    setSaving(true);

    try {
      const response = await fetch(isEdit ? `${API}/suppliers/${supplier.id}` : API + "/suppliers", {
        method: isEdit ? "PUT" : "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          name: form.name.trim(),
          code: form.code.trim() || null,
          contactPerson: form.contactPerson.trim() || null,
          phone: form.phone.trim() || null,
          email: form.email.trim() || null,
          taxOffice: form.taxOffice.trim() || null,
          taxNumber: form.taxNumber.trim() || null,
          address: form.address.trim() || null,
          defaultCurrency: form.defaultCurrency || "TRY",
          paymentTermDays: form.paymentTermDays ? Number(form.paymentTermDays) : null,
          note: form.note.trim() || null,
          isActive: form.isActive,
        }),
      });

      const resultText = await response.text();

      if (!response.ok) {
        throw new Error(resultText || "Tedarikçi kaydedilemedi.");
      }

      onSaved(isEdit ? "Tedarikçi güncellendi." : "Tedarikçi oluşturuldu.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Tedarikçi kaydedilirken beklenmeyen bir hata oluştu.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4 backdrop-blur-sm">
      <div className="max-h-[92vh] w-full max-w-5xl overflow-y-auto rounded-2xl border border-white/10 bg-[#0F1115] p-5 shadow-2xl sm:p-8">
        <ModalHeader title={isEdit ? "Tedarikçi Düzenle" : "Yeni Tedarikçi"} subtitle="Firma ve ödeme bilgilerini hazırlayın." onClose={onClose} disabled={saving} />

        {error && <div className="mb-5 rounded-xl border border-red-400/30 bg-red-500/10 p-4 text-sm text-red-100">{error}</div>}

        <section className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          <Field label="Firma Adı">
            <input value={form.name} onChange={(event) => updateForm("name", event.target.value)} className={CONTROL_CLASS} placeholder="Firma adı" />
          </Field>
          <Field label="Kod">
            <input value={form.code} onChange={(event) => updateForm("code", event.target.value)} className={CONTROL_CLASS} placeholder="SUP-001" />
          </Field>
          <Field label="Yetkili Kişi">
            <input value={form.contactPerson} onChange={(event) => updateForm("contactPerson", event.target.value)} className={CONTROL_CLASS} placeholder="Ad Soyad" />
          </Field>
          <Field label="Telefon">
            <input value={form.phone} onChange={(event) => updateForm("phone", event.target.value)} className={CONTROL_CLASS} placeholder="+90..." />
          </Field>
          <Field label="E-posta">
            <input value={form.email} onChange={(event) => updateForm("email", event.target.value)} className={CONTROL_CLASS} placeholder="mail@firma.com" />
          </Field>
          <Field label="Vergi Dairesi">
            <input value={form.taxOffice} onChange={(event) => updateForm("taxOffice", event.target.value)} className={CONTROL_CLASS} placeholder="Vergi dairesi" />
          </Field>
          <Field label="Vergi No">
            <input value={form.taxNumber} onChange={(event) => updateForm("taxNumber", event.target.value)} className={CONTROL_CLASS} placeholder="Vergi numarası" />
          </Field>
          <Field label="Varsayılan Para Birimi">
            <select value={form.defaultCurrency} onChange={(event) => updateForm("defaultCurrency", event.target.value)} className={CONTROL_CLASS}>
              <option value="TRY">TRY</option>
              <option value="EUR">EUR</option>
              <option value="USD">USD</option>
            </select>
          </Field>
          <Field label="Vade Günü">
            <input value={form.paymentTermDays} onChange={(event) => updateForm("paymentTermDays", event.target.value)} className={CONTROL_CLASS} inputMode="numeric" placeholder="30" />
          </Field>
          <label className="md:col-span-2 xl:col-span-3">
            <p className="mb-2 text-sm font-bold text-zinc-300">Adres</p>
            <textarea value={form.address} onChange={(event) => updateForm("address", event.target.value)} className={`${CONTROL_CLASS} min-h-24`} placeholder="Firma adresi" />
          </label>
          <label className="md:col-span-2 xl:col-span-3">
            <p className="mb-2 text-sm font-bold text-zinc-300">Not</p>
            <textarea value={form.note} onChange={(event) => updateForm("note", event.target.value)} className={`${CONTROL_CLASS} min-h-24`} placeholder="İsteğe bağlı not" />
          </label>
          <label className="flex items-center gap-3 rounded-xl border border-white/10 bg-black/20 p-4 md:col-span-2 xl:col-span-3">
            <input type="checkbox" checked={form.isActive} onChange={(event) => updateForm("isActive", event.target.checked)} />
            <span className="text-sm font-bold text-zinc-200">Tedarikçi aktif</span>
          </label>
        </section>

        <ModalActions onClose={onClose} onSubmit={saveSupplier} saving={saving} submitLabel={isEdit ? "Değişiklikleri Kaydet" : "Kaydet"} />
      </div>
    </div>
  );
}

function SupplierDetailModal({ supplier, onClose, onEdit }: { supplier: Supplier | null; onClose: () => void; onEdit: (supplier: Supplier) => void }) {
  if (!supplier) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4 backdrop-blur-sm">
      <div className="max-h-[92vh] w-full max-w-4xl overflow-y-auto rounded-2xl border border-white/10 bg-[#0F1115] p-5 shadow-2xl sm:p-8">
        <ModalHeader title="Tedarikçi Detayı" subtitle={supplier.code || "Kod tanımsız"} onClose={onClose} />

        <section className="grid grid-cols-1 gap-3 md:grid-cols-2">
          <DetailInfo label="Firma Adı" value={supplier.name || "-"} />
          <DetailInfo label="Kod" value={supplier.code || "-"} />
          <DetailInfo label="Yetkili Kişi" value={supplier.contactPerson || "-"} />
          <DetailInfo label="Telefon" value={supplier.phone || "-"} />
          <DetailInfo label="E-posta" value={supplier.email || "-"} />
          <DetailInfo label="Vergi Dairesi" value={supplier.taxOffice || "-"} />
          <DetailInfo label="Vergi No" value={supplier.taxNumber || "-"} />
          <DetailInfo label="Varsayılan Para Birimi" value={supplier.defaultCurrency || "TRY"} />
          <DetailInfo label="Vade Günü" value={formatTerm(supplier.paymentTermDays)} />
          <div className="rounded-xl border border-white/10 bg-black/20 p-4">
            <p className="text-xs text-zinc-500">Durum</p>
            <div className="mt-2">
              <StatusBadge active={supplier.isActive !== false} />
            </div>
          </div>
        </section>

        <section className="mt-5 grid grid-cols-1 gap-3">
          <DetailInfo label="Adres" value={supplier.address || "-"} />
          <DetailInfo label="Not" value={supplier.note || "-"} />
        </section>

        <div className="mt-7 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <button onClick={onClose} className="rounded-xl bg-zinc-700 px-5 py-3 font-bold text-white transition hover:bg-zinc-600">
            Kapat
          </button>
          <button
            onClick={() => onEdit(supplier)}
            className="rounded-xl bg-emerald-500 px-5 py-3 font-black text-black transition hover:bg-emerald-400"
          >
            Düzenle
          </button>
        </div>
      </div>
    </div>
  );
}

function DashboardCard({ title, value, note, tone }: { title: string; value: string; note: string; tone: DashboardTone }) {
  const color =
    tone === "red"
      ? "text-red-300"
      : tone === "cyan"
      ? "text-cyan-300"
      : tone === "amber"
      ? "text-amber-300"
      : "text-emerald-300";

  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.06] p-5 shadow-xl backdrop-blur xl:min-h-36">
      <p className="text-sm text-zinc-400">{title}</p>
      <h3 className="mt-2 break-words text-3xl font-black text-white">{value}</h3>
      <p className={`mt-2 text-xs font-bold ${color}`}>{note}</p>
    </div>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="block">
      <p className="mb-2 text-sm font-bold text-zinc-300">{label}</p>
      {children}
    </label>
  );
}

function DetailInfo({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs text-zinc-500">{label}</p>
      <p className="mt-2 break-words text-sm font-black text-white">{value}</p>
    </div>
  );
}

function ModalHeader({ title, subtitle, onClose, disabled = false }: { title: string; subtitle: string; onClose: () => void; disabled?: boolean }) {
  return (
    <div className="mb-6 flex flex-col gap-4 border-b border-white/10 pb-5 sm:flex-row sm:items-start sm:justify-between">
      <div>
        <p className="text-xs font-black tracking-[0.28em] text-emerald-300">FIXAR OS</p>
        <h2 className="mt-2 text-3xl font-black">{title}</h2>
        <p className="mt-1 text-sm text-zinc-400">{subtitle}</p>
      </div>
      <button onClick={onClose} disabled={disabled} className="w-fit rounded-xl bg-zinc-800 px-4 py-2 text-sm font-bold text-white transition hover:bg-zinc-700 disabled:opacity-50">
        Kapat
      </button>
    </div>
  );
}

function ModalActions({
  onClose,
  onSubmit,
  saving,
  submitLabel,
}: {
  onClose: () => void;
  onSubmit: () => void | Promise<void>;
  saving: boolean;
  submitLabel: string;
}) {
  return (
    <div className="mt-7 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
      <button onClick={onClose} disabled={saving} className="rounded-xl bg-zinc-700 px-5 py-3 font-bold text-white transition hover:bg-zinc-600 disabled:opacity-50">
        Vazgeç
      </button>
      <button onClick={onSubmit} disabled={saving} className="rounded-xl bg-emerald-500 px-5 py-3 font-black text-black transition hover:bg-emerald-400 disabled:opacity-50">
        {saving ? "Kaydediliyor..." : submitLabel}
      </button>
    </div>
  );
}

function LoadingState() {
  return (
    <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-5">
      <div className="space-y-3">
        <div className="h-4 w-48 animate-pulse rounded bg-white/10" />
        <div className="h-12 animate-pulse rounded-xl bg-white/10" />
        <div className="h-12 animate-pulse rounded-xl bg-white/10" />
        <div className="h-12 animate-pulse rounded-xl bg-white/10" />
      </div>
    </div>
  );
}

function TableHead({ children }: { children: ReactNode }) {
  return <th className="px-4 py-4 font-black">{children}</th>;
}

function TableCell({ children }: { children: ReactNode }) {
  return <td className="px-4 py-4 align-middle text-sm text-zinc-300">{children}</td>;
}

function StatusBadge({ active }: { active: boolean }) {
  return (
    <span className={`inline-flex rounded-full px-3 py-1 text-xs font-black ${active ? "bg-emerald-500/20 text-emerald-200" : "bg-zinc-500/20 text-zinc-300"}`}>
      {active ? "Aktif" : "Pasif"}
    </span>
  );
}

function extractSuppliers(result: unknown): Supplier[] {
  if (Array.isArray(result)) {
    return result.filter(isSupplier);
  }

  if (isRecord(result) && Array.isArray(result.data)) {
    return result.data.filter(isSupplier);
  }

  return [];
}

function isSupplier(value: unknown): value is Supplier {
  return isRecord(value) && typeof value.id === "string";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function toSupplierForm(supplier: Supplier | null): SupplierFormState {
  return {
    name: supplier?.name || "",
    code: supplier?.code || "",
    contactPerson: supplier?.contactPerson || "",
    phone: supplier?.phone || "",
    email: supplier?.email || "",
    taxOffice: supplier?.taxOffice || "",
    taxNumber: supplier?.taxNumber || "",
    address: supplier?.address || "",
    defaultCurrency: supplier?.defaultCurrency || "TRY",
    paymentTermDays: supplier?.paymentTermDays === null || supplier?.paymentTermDays === undefined ? "" : String(supplier.paymentTermDays),
    note: supplier?.note || "",
    isActive: supplier?.isActive !== false,
  };
}

function formatTerm(value: number | null | undefined) {
  if (typeof value !== "number" || !Number.isFinite(value)) return "-";
  return `${value.toLocaleString("tr-TR")} gün`;
}
