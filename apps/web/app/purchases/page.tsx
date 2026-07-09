"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";

type DashboardTone = "emerald" | "red" | "cyan" | "amber";

type PurchaseOrderLine = {
  id?: string;
  stockItemId?: string;
  stockName?: string | null;
  quantity?: number | null;
  unit?: string | null;
  unitPrice?: number | null;
  lineTotal?: number | null;
  note?: string | null;
};

type PurchaseOrder = {
  id: string;
  supplierName?: string | null;
  supplierCode?: string | null;
  documentNo?: string | null;
  invoiceNo?: string | null;
  orderDate?: string | null;
  dueDate?: string | null;
  paymentType?: string | null;
  currency?: string | null;
  vatRate?: number | null;
  subTotal?: number | null;
  vatTotal?: number | null;
  grandTotal?: number | null;
  status?: string | null;
  note?: string | null;
  createdAt?: string | null;
  lines?: PurchaseOrderLine[];
};

type StockItem = {
  id: string;
  name?: string | null;
  code?: string | null;
  unit?: string | null;
};

type PurchaseFormState = {
  supplierName: string;
  documentNo: string;
  invoiceNo: string;
  orderDate: string;
  currency: string;
  paymentType: string;
};

type PurchaseFormLine = {
  key: string;
  stockItemId: string;
  quantity: string;
  unit: string;
  unitPrice: string;
  vatRate: string;
};

const API = "http://localhost:5000/api/v1";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60";
const PAYMENT_TYPES = ["Nakit", "Havale", "Çek", "Vadeli"];

export default function PurchasesPage() {
  const [purchases, setPurchases] = useState<PurchaseOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    loadPurchases();
  }, []);

  async function loadPurchases() {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(API + "/purchases");

      if (!response.ok) {
        throw new Error("Satın alma kayıtları alınamadı.");
      }

      const result: unknown = await response.json();
      setPurchases(extractPurchases(result));
    } catch (err) {
      setPurchases([]);
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  function handleSaved() {
    setFormOpen(false);
    setSuccessMessage("Satın alma kaydı oluşturuldu.");
    loadPurchases();
  }

  const todayCount = useMemo(() => purchases.filter(isTodayPurchase).length, [purchases]);
  const pendingCount = useMemo(() => purchases.filter(isPendingPurchase).length, [purchases]);
  const totalAmount = useMemo(() => purchases.reduce((sum, item) => sum + safeNumber(item.grandTotal), 0), [purchases]);
  const primaryCurrency = purchases.find((item) => item.currency)?.currency || "TRY";

  const dashboardCards = [
    {
      title: "Toplam Satın Alma",
      value: purchases.length.toLocaleString("tr-TR"),
      note: "Kayıtlı satın alma fişi",
      tone: "emerald" as DashboardTone,
    },
    {
      title: "Bugünkü Satın Alma",
      value: todayCount.toLocaleString("tr-TR"),
      note: "Bugün oluşturulan kayıtlar",
      tone: "cyan" as DashboardTone,
    },
    {
      title: "Bekleyen",
      value: pendingCount.toLocaleString("tr-TR"),
      note: "Takip gerektiren satın almalar",
      tone: "amber" as DashboardTone,
    },
    {
      title: "Toplam Tutar",
      value: formatMoney(totalAmount, primaryCurrency),
      note: "Genel satın alma toplamı",
      tone: "red" as DashboardTone,
    },
  ];

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.18),transparent_34%),radial-gradient(circle_at_bottom_right,rgba(14,165,233,0.13),transparent_32%)] px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.38em] text-emerald-300">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">Satın Alma Yönetimi</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                Tedarikçi, belge, ödeme ve tutar bilgilerini tek ekrandan izleyin.
              </p>
            </div>

            <div className="flex flex-col gap-3 sm:flex-row">
              <button
                onClick={() => {
                  setSuccessMessage(null);
                  loadPurchases();
                }}
                disabled={loading}
                className="rounded-xl border border-white/10 bg-white/[0.08] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:opacity-50"
              >
                {loading ? "Yenileniyor..." : "Yenile"}
              </button>
              <button
                onClick={() => {
                  setSuccessMessage(null);
                  setFormOpen(true);
                }}
                className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400"
              >
                + Yeni Satın Alma
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
            <div className="flex flex-col gap-3 border-b border-white/10 pb-5 sm:flex-row sm:items-end sm:justify-between">
              <div>
                <h2 className="text-2xl font-black">Satın Alma Kayıtları</h2>
                <p className="mt-1 text-sm text-zinc-400">
                  {purchases.length.toLocaleString("tr-TR")} kayıt listeleniyor.
                </p>
              </div>
              <span className="w-fit rounded-full bg-white/[0.08] px-3 py-1 text-xs font-bold text-zinc-300">
                Canlı API verisi
              </span>
            </div>

            {loading && <LoadingState />}

            {!loading && error && (
              <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-5 text-sm text-red-100">
                <p className="font-black">Satın alma kayıtları yüklenemedi.</p>
                <p className="mt-1 text-red-200">{error}</p>
              </div>
            )}

            {!loading && !error && purchases.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-zinc-300">
                Henüz satın alma kaydı bulunmuyor.
              </div>
            )}

            {!loading && !error && purchases.length > 0 && (
              <div className="mt-5 overflow-hidden rounded-xl border border-white/10 bg-black/20">
                <div className="overflow-x-auto">
                  <table className="w-full min-w-[920px] border-collapse text-left">
                    <thead className="bg-white/[0.06] text-xs uppercase tracking-[0.16em] text-zinc-400">
                      <tr>
                        <TableHead>Tarih</TableHead>
                        <TableHead>Belge No</TableHead>
                        <TableHead>Tedarikçi</TableHead>
                        <TableHead>Ödeme Şekli</TableHead>
                        <TableHead>Para Birimi</TableHead>
                        <TableHead>Toplam</TableHead>
                        <TableHead>Durum</TableHead>
                        <TableHead>Detay</TableHead>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-white/10">
                      {purchases.map((purchase) => (
                        <tr key={purchase.id} className="transition hover:bg-white/[0.04]">
                          <TableCell>{formatDate(purchase.orderDate ?? purchase.createdAt)}</TableCell>
                          <TableCell>
                            <div className="font-black text-white">{purchase.documentNo || purchase.invoiceNo || "-"}</div>
                            {purchase.invoiceNo && purchase.documentNo !== purchase.invoiceNo && (
                              <div className="mt-1 text-xs text-zinc-500">Fatura: {purchase.invoiceNo}</div>
                            )}
                          </TableCell>
                          <TableCell>
                            <div className="font-black text-white">{purchase.supplierName || "-"}</div>
                            {purchase.supplierCode && <div className="mt-1 text-xs text-zinc-500">{purchase.supplierCode}</div>}
                          </TableCell>
                          <TableCell>{purchase.paymentType || "-"}</TableCell>
                          <TableCell>{purchase.currency || "-"}</TableCell>
                          <TableCell>
                            <span className="font-black text-emerald-200">
                              {formatMoney(safeNumber(purchase.grandTotal), purchase.currency || "TRY")}
                            </span>
                          </TableCell>
                          <TableCell>
                            <StatusBadge status={purchase.status} />
                          </TableCell>
                          <TableCell>
                            <button
                              onClick={() => alert("Detay ekranı yakında eklenecek.")}
                              className="rounded-lg border border-white/10 bg-white/[0.06] px-3 py-2 text-xs font-bold text-zinc-200 transition hover:bg-white/[0.1]"
                            >
                              Detay
                            </button>
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

      <PurchaseFormModal open={formOpen} onClose={() => setFormOpen(false)} onSaved={handleSaved} />
    </main>
  );
}

function PurchaseFormModal({ open, onClose, onSaved }: { open: boolean; onClose: () => void; onSaved: () => void }) {
  const [stocks, setStocks] = useState<StockItem[]>([]);
  const [stocksLoading, setStocksLoading] = useState(false);
  const [stocksError, setStocksError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState<PurchaseFormState>(() => createEmptyPurchaseForm());
  const [lines, setLines] = useState<PurchaseFormLine[]>(() => [createEmptyPurchaseLine()]);

  useEffect(() => {
    if (!open) return;

    setError(null);
    setForm(createEmptyPurchaseForm());
    setLines([createEmptyPurchaseLine()]);
    loadStocks();
  }, [open]);

  async function loadStocks() {
    setStocksLoading(true);
    setStocksError(null);

    try {
      const response = await fetch(API + "/stocks");

      if (!response.ok) {
        throw new Error("Stok listesi alınamadı.");
      }

      const result: unknown = await response.json();
      setStocks(extractStocks(result));
    } catch (err) {
      setStocks([]);
      setStocksError(err instanceof Error ? err.message : "Stok listesi alınırken beklenmeyen bir hata oluştu.");
    } finally {
      setStocksLoading(false);
    }
  }

  function updateForm(key: keyof PurchaseFormState, value: string) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function updateLine(key: string, field: keyof PurchaseFormLine, value: string) {
    setLines((current) =>
      current.map((line) => {
        if (line.key !== key) return line;

        if (field !== "stockItemId") {
          return { ...line, [field]: value };
        }

        const stock = stocks.find((item) => item.id === value);
        return { ...line, stockItemId: value, unit: stock?.unit || line.unit };
      })
    );
  }

  function addLine() {
    setLines((current) => [...current, createEmptyPurchaseLine()]);
  }

  function removeLine(key: string) {
    setLines((current) => (current.length === 1 ? current : current.filter((line) => line.key !== key)));
  }

  const totals = useMemo(() => calculatePurchaseTotals(lines), [lines]);

  async function savePurchase() {
    if (saving) return;

    setError(null);

    if (!form.supplierName.trim()) {
      setError("Tedarikçi alanı zorunludur.");
      return;
    }

    const preparedLines = lines.map((line) => {
      const stock = stocks.find((item) => item.id === line.stockItemId);
      const quantity = Number(line.quantity || 0);
      const unitPrice = Number(line.unitPrice || 0);

      return {
        stock,
        quantity,
        unitPrice,
        lineTotal: quantity * unitPrice,
        unit: line.unit.trim() || stock?.unit || "adet",
      };
    });

    if (preparedLines.some((line) => !line.stock)) {
      setError("Her satır için stok seçmelisiniz.");
      return;
    }

    if (preparedLines.some((line) => line.quantity <= 0)) {
      setError("Satır miktarı 0'dan büyük olmalıdır.");
      return;
    }

    if (preparedLines.some((line) => line.unitPrice < 0)) {
      setError("Birim fiyat negatif olamaz.");
      return;
    }

    setSaving(true);

    try {
      const response = await fetch(API + "/purchases", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          supplierName: form.supplierName.trim(),
          supplierCode: null,
          documentNo: form.documentNo.trim() || null,
          invoiceNo: form.invoiceNo.trim() || null,
          orderDate: form.orderDate ? `${form.orderDate}T00:00:00` : null,
          dueDate: null,
          paymentType: toBackendPaymentType(form.paymentType),
          currency: form.currency,
          vatRate: getSharedVatRate(lines),
          subTotal: totals.subTotal,
          vatTotal: totals.vatTotal,
          grandTotal: totals.grandTotal,
          status: "Oluşturuldu",
          note: null,
          lines: preparedLines.map((line) => ({
            stockItemId: line.stock!.id,
            stockName: line.stock!.name || null,
            quantity: line.quantity,
            unit: line.unit,
            unitPrice: line.unitPrice,
            lineTotal: line.lineTotal,
            note: null,
          })),
        }),
      });

      const resultText = await response.text();

      if (!response.ok) {
        throw new Error(resultText || "Satın alma kaydı oluşturulamadı.");
      }

      onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Satın alma kaydedilirken beklenmeyen bir hata oluştu.");
    } finally {
      setSaving(false);
    }
  }

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4 backdrop-blur-sm">
      <div className="max-h-[92vh] w-full max-w-6xl overflow-y-auto rounded-2xl border border-white/10 bg-[#0F1115] p-5 shadow-2xl sm:p-8">
        <div className="mb-6 flex flex-col gap-4 border-b border-white/10 pb-5 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <p className="text-xs font-black tracking-[0.28em] text-emerald-300">FIXAR OS</p>
            <h2 className="mt-2 text-3xl font-black">Yeni Satın Alma</h2>
            <p className="mt-1 text-sm text-zinc-400">Tedarikçi, belge ve stok satırlarını tek kayıt altında hazırlayın.</p>
          </div>
          <button
            onClick={onClose}
            disabled={saving}
            className="w-fit rounded-xl bg-zinc-800 px-4 py-2 text-sm font-bold text-white transition hover:bg-zinc-700 disabled:opacity-50"
          >
            Kapat
          </button>
        </div>

        {error && <div className="mb-5 rounded-xl border border-red-400/30 bg-red-500/10 p-4 text-sm text-red-100">{error}</div>}
        {stocksError && <div className="mb-5 rounded-xl border border-amber-400/30 bg-amber-500/10 p-4 text-sm text-amber-100">{stocksError}</div>}

        <section className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          <Field label="Tedarikçi">
            <input
              value={form.supplierName}
              onChange={(event) => updateForm("supplierName", event.target.value)}
              className={CONTROL_CLASS}
              placeholder="Tedarikçi adı"
            />
          </Field>
          <Field label="Belge No">
            <input
              value={form.documentNo}
              onChange={(event) => updateForm("documentNo", event.target.value)}
              className={CONTROL_CLASS}
              placeholder="İrsaliye / belge no"
            />
          </Field>
          <Field label="Fatura No">
            <input
              value={form.invoiceNo}
              onChange={(event) => updateForm("invoiceNo", event.target.value)}
              className={CONTROL_CLASS}
              placeholder="Fatura no"
            />
          </Field>
          <Field label="Tarih">
            <input
              type="date"
              value={form.orderDate}
              onChange={(event) => updateForm("orderDate", event.target.value)}
              className={CONTROL_CLASS}
            />
          </Field>
          <Field label="Para Birimi">
            <select value={form.currency} onChange={(event) => updateForm("currency", event.target.value)} className={CONTROL_CLASS}>
              <option value="TRY">TRY</option>
              <option value="EUR">EUR</option>
              <option value="USD">USD</option>
            </select>
          </Field>
          <Field label="Ödeme Şekli">
            <select value={form.paymentType} onChange={(event) => updateForm("paymentType", event.target.value)} className={CONTROL_CLASS}>
              {PAYMENT_TYPES.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </Field>
        </section>

        <section className="mt-7 rounded-2xl border border-white/10 bg-white/[0.04] p-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h3 className="text-xl font-black">Ürün Satırları</h3>
              <p className="mt-1 text-sm text-zinc-400">Stok, miktar, fiyat ve KDV bilgilerini girin.</p>
            </div>
            <button
              onClick={addLine}
              disabled={saving}
              className="rounded-xl border border-emerald-400/30 bg-emerald-500/10 px-4 py-3 text-sm font-black text-emerald-100 transition hover:bg-emerald-500/20 disabled:opacity-50"
            >
              + Satır Ekle
            </button>
          </div>

          {stocksLoading ? (
            <div className="mt-4 rounded-xl border border-white/10 bg-black/20 p-5 text-sm text-zinc-400">Stok listesi yükleniyor...</div>
          ) : (
            <div className="mt-4 space-y-3">
              <div className="hidden grid-cols-[minmax(220px,1.5fr)_120px_110px_140px_100px_140px_44px] gap-3 px-1 text-xs font-black uppercase tracking-[0.14em] text-zinc-500 xl:grid">
                <span>Stok</span>
                <span>Miktar</span>
                <span>Birim</span>
                <span>Birim Fiyat</span>
                <span>KDV</span>
                <span>Toplam</span>
                <span />
              </div>

              {lines.map((line) => {
                const lineNetTotal = safeNumber(Number(line.quantity || 0)) * safeNumber(Number(line.unitPrice || 0));
                const lineVatTotal = lineNetTotal * safeNumber(Number(line.vatRate || 0)) / 100;

                return (
                  <div
                    key={line.key}
                    className="grid grid-cols-1 gap-3 rounded-xl border border-white/10 bg-black/20 p-3 xl:grid-cols-[minmax(220px,1.5fr)_120px_110px_140px_100px_140px_44px]"
                  >
                    <Field label="Stok" compact>
                      <select
                        value={line.stockItemId}
                        onChange={(event) => updateLine(line.key, "stockItemId", event.target.value)}
                        className={CONTROL_CLASS}
                      >
                        <option value="">Stok seç</option>
                        {stocks.map((stock) => (
                          <option key={stock.id} value={stock.id}>
                            {[stock.code, stock.name].filter(Boolean).join(" - ") || stock.id}
                          </option>
                        ))}
                      </select>
                    </Field>
                    <Field label="Miktar" compact>
                      <input
                        value={line.quantity}
                        onChange={(event) => updateLine(line.key, "quantity", event.target.value)}
                        className={CONTROL_CLASS}
                        inputMode="decimal"
                        placeholder="0"
                      />
                    </Field>
                    <Field label="Birim" compact>
                      <input
                        value={line.unit}
                        onChange={(event) => updateLine(line.key, "unit", event.target.value)}
                        className={CONTROL_CLASS}
                        placeholder="kg"
                      />
                    </Field>
                    <Field label="Birim Fiyat" compact>
                      <input
                        value={line.unitPrice}
                        onChange={(event) => updateLine(line.key, "unitPrice", event.target.value)}
                        className={CONTROL_CLASS}
                        inputMode="decimal"
                        placeholder="0.00"
                      />
                    </Field>
                    <Field label="KDV" compact>
                      <input
                        value={line.vatRate}
                        onChange={(event) => updateLine(line.key, "vatRate", event.target.value)}
                        className={CONTROL_CLASS}
                        inputMode="decimal"
                        placeholder="20"
                      />
                    </Field>
                    <div>
                      <p className="mb-2 text-sm font-bold text-zinc-300 xl:hidden">Toplam</p>
                      <div className="rounded-xl border border-white/10 bg-black/30 p-3 text-sm font-black text-emerald-200">
                        {formatMoney(lineNetTotal + lineVatTotal, form.currency)}
                      </div>
                    </div>
                    <button
                      onClick={() => removeLine(line.key)}
                      disabled={saving || lines.length === 1}
                      className="h-12 rounded-xl border border-red-400/20 bg-red-500/10 text-sm font-black text-red-200 transition hover:bg-red-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                      aria-label="Satırı kaldır"
                    >
                      X
                    </button>
                  </div>
                );
              })}
            </div>
          )}
        </section>

        <section className="mt-6 grid grid-cols-1 gap-3 sm:ml-auto sm:max-w-md">
          <SummaryRow label="Ara Toplam" value={formatMoney(totals.subTotal, form.currency)} />
          <SummaryRow label="KDV" value={formatMoney(totals.vatTotal, form.currency)} />
          <SummaryRow label="Genel Toplam" value={formatMoney(totals.grandTotal, form.currency)} strong />
        </section>

        <div className="mt-7 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <button
            onClick={onClose}
            disabled={saving}
            className="rounded-xl bg-zinc-700 px-5 py-3 font-bold text-white transition hover:bg-zinc-600 disabled:opacity-50"
          >
            Vazgeç
          </button>
          <button
            onClick={savePurchase}
            disabled={saving || stocksLoading}
            className="rounded-xl bg-emerald-500 px-5 py-3 font-black text-black transition hover:bg-emerald-400 disabled:opacity-50"
          >
            {saving ? "Kaydediliyor..." : "Kaydet"}
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

function Field({ label, children, compact = false }: { label: string; children: ReactNode; compact?: boolean }) {
  return (
    <label className="block">
      <p className={`mb-2 text-sm font-bold text-zinc-300 ${compact ? "xl:hidden" : ""}`}>{label}</p>
      {children}
    </label>
  );
}

function SummaryRow({ label, value, strong = false }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className={`flex items-center justify-between rounded-xl border border-white/10 bg-black/20 p-4 ${strong ? "text-emerald-100" : "text-zinc-300"}`}>
      <span className="text-sm font-bold">{label}</span>
      <span className={strong ? "text-xl font-black" : "text-base font-black"}>{value}</span>
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

function StatusBadge({ status }: { status?: string | null }) {
  const label = status || "Oluşturuldu";
  const isDone = ["Tamamlandı", "Ödendi", "Kapandı"].includes(label);
  const isProblem = ["İptal", "İptal Edildi"].includes(label);
  const className = isProblem
    ? "bg-red-500/20 text-red-200"
    : isDone
    ? "bg-emerald-500/20 text-emerald-200"
    : "bg-amber-500/20 text-amber-200";

  return <span className={`inline-flex rounded-full px-3 py-1 text-xs font-black ${className}`}>{label}</span>;
}

function extractPurchases(result: unknown): PurchaseOrder[] {
  if (Array.isArray(result)) {
    return result.filter(isPurchaseOrder);
  }

  if (isRecord(result) && Array.isArray(result.data)) {
    return result.data.filter(isPurchaseOrder);
  }

  return [];
}

function extractStocks(result: unknown): StockItem[] {
  if (Array.isArray(result)) {
    return result.filter(isStockItem);
  }

  if (isRecord(result) && Array.isArray(result.data)) {
    return result.data.filter(isStockItem);
  }

  return [];
}

function isPurchaseOrder(value: unknown): value is PurchaseOrder {
  return isRecord(value) && typeof value.id === "string";
}

function isStockItem(value: unknown): value is StockItem {
  return isRecord(value) && typeof value.id === "string";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function isTodayPurchase(item: PurchaseOrder) {
  const value = item.orderDate ?? item.createdAt;
  if (!value) return false;

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return false;

  const today = new Date();
  return date.getFullYear() === today.getFullYear() && date.getMonth() === today.getMonth() && date.getDate() === today.getDate();
}

function isPendingPurchase(item: PurchaseOrder) {
  const status = (item.status || "Oluşturuldu").toLocaleLowerCase("tr-TR");
  return status.includes("bekleyen") || status.includes("oluşturuldu") || status.includes("açık");
}

function safeNumber(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function createEmptyPurchaseForm(): PurchaseFormState {
  return {
    supplierName: "",
    documentNo: "",
    invoiceNo: "",
    orderDate: formatDateInput(new Date()),
    currency: "TRY",
    paymentType: "Nakit",
  };
}

function createEmptyPurchaseLine(): PurchaseFormLine {
  return {
    key: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
    stockItemId: "",
    quantity: "",
    unit: "",
    unitPrice: "",
    vatRate: "20",
  };
}

function calculatePurchaseTotals(lines: PurchaseFormLine[]) {
  return lines.reduce(
    (totals, line) => {
      const quantity = safeNumber(Number(line.quantity || 0));
      const unitPrice = safeNumber(Number(line.unitPrice || 0));
      const vatRate = safeNumber(Number(line.vatRate || 0));
      const lineTotal = quantity * unitPrice;
      const vatTotal = lineTotal * vatRate / 100;

      return {
        subTotal: totals.subTotal + lineTotal,
        vatTotal: totals.vatTotal + vatTotal,
        grandTotal: totals.grandTotal + lineTotal + vatTotal,
      };
    },
    { subTotal: 0, vatTotal: 0, grandTotal: 0 }
  );
}

function getSharedVatRate(lines: PurchaseFormLine[]) {
  const rates = Array.from(new Set(lines.map((line) => safeNumber(Number(line.vatRate || 0)))));
  return rates.length === 1 ? rates[0] : null;
}

function toBackendPaymentType(paymentType: string) {
  return paymentType === "Vadeli" ? "Cari Hesap" : paymentType;
}

function formatMoney(value: number, currency: string) {
  try {
    return value.toLocaleString("tr-TR", {
      maximumFractionDigits: 2,
      style: "currency",
      currency,
    });
  } catch {
    return `${value.toLocaleString("tr-TR", { maximumFractionDigits: 2 })} ${currency}`;
  }
}

function formatDateInput(value: Date) {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, "0");
  const day = String(value.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

function formatDate(value: string | null | undefined) {
  if (!value) return "-";

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;

  return date.toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}
