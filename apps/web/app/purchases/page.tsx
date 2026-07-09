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

type Supplier = {
  id: string;
  name?: string | null;
  code?: string | null;
  defaultCurrency?: string | null;
  isActive?: boolean | null;
};

type PurchaseFormState = {
  supplierId: string;
  supplierName: string;
  supplierCode: string;
  documentNo: string;
  invoiceNo: string;
  orderDate: string;
  currency: string;
  paymentType: string;
  note: string;
};

type PurchaseFormLine = {
  key: string;
  stockItemId: string;
  quantity: string;
  unit: string;
  unitPrice: string;
  vatRate: string;
};

type PdfPageImage = {
  dataUrl: string;
  width: number;
  height: number;
};

const API = "http://localhost:5000/api/v1";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60";
const PAYMENT_TYPES = ["Nakit", "Havale", "Kredi Kartı", "Çek", "Vadeli"];

export default function PurchasesPage() {
  const [purchases, setPurchases] = useState<PurchaseOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [formPurchase, setFormPurchase] = useState<PurchaseOrder | null>(null);
  const [detailId, setDetailId] = useState<string | null>(null);
  const [cancelingId, setCancelingId] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [supplierFilter, setSupplierFilter] = useState("Tümü");
  const [statusFilter, setStatusFilter] = useState("Tümü");
  const [paymentFilter, setPaymentFilter] = useState("Tümü");

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

  function handleSaved(message: string) {
    setFormOpen(false);
    setFormPurchase(null);
    setSuccessMessage(message);
    loadPurchases();
  }

  function openCreateForm() {
    setSuccessMessage(null);
    setFormPurchase(null);
    setFormOpen(true);
  }

  function openEditForm(purchase: PurchaseOrder) {
    if (isPurchaseCancelled(purchase)) return;

    setSuccessMessage(null);
    setFormPurchase(purchase);
    setFormOpen(true);
  }

  async function cancelPurchase(purchase: PurchaseOrder) {
    if (cancelingId || isPurchaseCancelled(purchase)) return;

    const ok = confirm(`${purchase.documentNo || purchase.invoiceNo || purchase.supplierName || "Satın alma"} iptal edilecek.\n\nOnaylıyor musunuz?`);

    if (!ok) return;

    setCancelingId(purchase.id);
    setSuccessMessage(null);

    try {
      const response = await fetch(`${API}/purchases/${purchase.id}/cancel`, {
        method: "POST",
      });
      const resultText = await response.text();

      if (!response.ok) {
        alert("Satın alma iptal edilemedi: " + resultText);
        return;
      }

      setSuccessMessage("Satın alma iptal edildi.");
      loadPurchases();
    } catch (err) {
      alert(err instanceof Error ? err.message : "Satın alma iptal edilirken beklenmeyen bir hata oluştu.");
    } finally {
      setCancelingId(null);
    }
  }

  const todayCount = useMemo(() => purchases.filter(isTodayPurchase).length, [purchases]);
  const pendingCount = useMemo(() => purchases.filter(isPendingPurchase).length, [purchases]);
  const totalAmount = useMemo(() => purchases.reduce((sum, item) => sum + safeNumber(item.grandTotal), 0), [purchases]);
  const primaryCurrency = purchases.find((item) => item.currency)?.currency || "TRY";
  const supplierOptions = useMemo(() => {
    const values = purchases.map((item) => item.supplierName).filter(Boolean).map(String);
    return ["Tümü", ...Array.from(new Set(values)).sort((a, b) => a.localeCompare(b, "tr-TR"))];
  }, [purchases]);
  const filteredPurchases = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase("tr-TR");

    return purchases.filter((purchase) => {
      const matchesSearch =
        !normalizedSearch ||
        [purchase.documentNo, purchase.invoiceNo, purchase.supplierName]
          .filter(Boolean)
          .some((value) => String(value).toLocaleLowerCase("tr-TR").includes(normalizedSearch));
      const matchesSupplier = supplierFilter === "Tümü" || purchase.supplierName === supplierFilter;
      const matchesStatus =
        statusFilter === "Tümü" ||
        (statusFilter === "Oluşturuldu" && !isPurchaseCancelled(purchase)) ||
        (statusFilter === "İptal Edildi" && isPurchaseCancelled(purchase));
      const matchesPayment = paymentFilter === "Tümü" || (purchase.paymentType || "") === paymentFilter;

      return matchesSearch && matchesSupplier && matchesStatus && matchesPayment;
    });
  }, [paymentFilter, purchases, search, statusFilter, supplierFilter]);
  const filteredTotalAmount = useMemo(() => filteredPurchases.reduce((sum, item) => sum + safeNumber(item.grandTotal), 0), [filteredPurchases]);

  function exportPurchasesCsv() {
    if (filteredPurchases.length === 0) {
      alert("Aktarılacak kayıt yok.");
      return;
    }

    const headers = [
      "Tarih",
      "Belge No",
      "Fatura No",
      "Tedarikçi",
      "Ödeme Şekli",
      "Para Birimi",
      "Ara Toplam",
      "KDV Toplam",
      "Genel Toplam",
      "Durum",
      "Not",
    ];
    const rows = filteredPurchases.map((purchase) => [
      formatDate(purchase.orderDate ?? purchase.createdAt),
      purchase.documentNo || "",
      purchase.invoiceNo || "",
      purchase.supplierName || "",
      purchase.paymentType || "",
      purchase.currency || "",
      formatDecimalForCsv(safeNumber(purchase.subTotal)),
      formatDecimalForCsv(safeNumber(purchase.vatTotal)),
      formatDecimalForCsv(safeNumber(purchase.grandTotal)),
      formatStatusLabel(purchase.status),
      purchase.note || "",
    ]);
    const csv = [headers, ...rows].map((row) => row.map(escapeCsvCell).join(";")).join("\n");
    const blob = new Blob(["\ufeff" + csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");

    link.href = url;
    link.download = "fixar-satin-alma-kayitlari.csv";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  function downloadPurchasesPdf() {
    if (filteredPurchases.length === 0) {
      alert("Aktarılacak kayıt yok.");
      return;
    }

    const generatedAt = new Date().toLocaleString("tr-TR");
    const pages = renderPurchaseReportPages(filteredPurchases, generatedAt, filteredTotalAmount, primaryCurrency);
    const pdfBlob = createPdfFromJpegPages(pages);
    const url = URL.createObjectURL(pdfBlob);
    const link = document.createElement("a");

    link.href = url;
    link.download = "fixar-satin-alma-raporu.pdf";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

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
                onClick={openCreateForm}
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
                  {filteredPurchases.length.toLocaleString("tr-TR")} kayıt listeleniyor.
                </p>
              </div>
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                <button
                  onClick={exportPurchasesCsv}
                  disabled={filteredPurchases.length === 0}
                  className="rounded-xl border border-emerald-400/30 bg-emerald-500/10 px-4 py-3 text-sm font-black text-emerald-100 transition hover:bg-emerald-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  Excel’e Aktar
                </button>
                <button
                  onClick={downloadPurchasesPdf}
                  disabled={filteredPurchases.length === 0}
                  className="rounded-xl border border-white/10 bg-white/[0.08] px-4 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:cursor-not-allowed disabled:opacity-40"
                >
                  PDF / Yazdır
                </button>
                <span className="w-fit rounded-full bg-white/[0.08] px-3 py-1 text-xs font-bold text-zinc-300">
                  Canlı API verisi
                </span>
              </div>
            </div>

            <div className="mt-5 grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4">
              <Field label="Arama">
                <input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  className={CONTROL_CLASS}
                  placeholder="Belge no, fatura no, tedarikçi"
                />
              </Field>
              <Field label="Tedarikçi">
                <select value={supplierFilter} onChange={(event) => setSupplierFilter(event.target.value)} className={CONTROL_CLASS}>
                  {supplierOptions.map((supplier) => (
                    <option key={supplier} value={supplier}>
                      {supplier}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Durum">
                <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} className={CONTROL_CLASS}>
                  <option value="Tümü">Tümü</option>
                  <option value="Oluşturuldu">Oluşturuldu</option>
                  <option value="İptal Edildi">İptal Edildi</option>
                </select>
              </Field>
              <Field label="Ödeme Şekli">
                <select value={paymentFilter} onChange={(event) => setPaymentFilter(event.target.value)} className={CONTROL_CLASS}>
                  <option value="Tümü">Tümü</option>
                  <option value="Nakit">Nakit</option>
                  <option value="Havale">Havale</option>
                  <option value="Kredi Kartı">Kredi Kartı</option>
                  <option value="Çek">Çek</option>
                  <option value="Cari Hesap">Cari Hesap</option>
                </select>
              </Field>
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

            {!loading && !error && purchases.length > 0 && filteredPurchases.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-zinc-300">
                Filtreye uygun satın alma kaydı bulunamadı.
              </div>
            )}

            {!loading && !error && filteredPurchases.length > 0 && (
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
                        <TableHead>İşlem</TableHead>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-white/10">
                      {filteredPurchases.map((purchase) => {
                        const cancelled = isPurchaseCancelled(purchase);

                        return (
                          <tr key={purchase.id} className={`transition hover:bg-white/[0.04] ${cancelled ? "bg-red-500/10" : ""}`}>
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
                              <span className={cancelled ? "font-black text-red-200" : "font-black text-emerald-200"}>
                                {formatMoney(safeNumber(purchase.grandTotal), purchase.currency || "TRY")}
                              </span>
                            </TableCell>
                            <TableCell>
                              <StatusBadge status={purchase.status} />
                            </TableCell>
                            <TableCell>
                              <div className="flex flex-wrap gap-2">
                                <button
                                  onClick={() => setDetailId(purchase.id)}
                                  className="rounded-lg border border-white/10 bg-white/[0.06] px-3 py-2 text-xs font-bold text-zinc-200 transition hover:bg-white/[0.1]"
                                >
                                  Detay
                                </button>
                                <button
                                  onClick={() => openEditForm(purchase)}
                                  disabled={cancelled}
                                  className="rounded-lg border border-emerald-400/20 bg-emerald-500/10 px-3 py-2 text-xs font-bold text-emerald-100 transition hover:bg-emerald-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                                >
                                  Düzenle
                                </button>
                                <button
                                  onClick={() => cancelPurchase(purchase)}
                                  disabled={cancelled || cancelingId === purchase.id}
                                  className="rounded-lg border border-red-400/20 bg-red-500/10 px-3 py-2 text-xs font-bold text-red-200 transition hover:bg-red-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                                >
                                  {cancelingId === purchase.id ? "İptal..." : "İptal"}
                                </button>
                              </div>
                            </TableCell>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </section>
        </div>
      </div>

      <PurchaseFormModal
        open={formOpen}
        initialPurchase={formPurchase}
        onClose={() => {
          setFormOpen(false);
          setFormPurchase(null);
        }}
        onSaved={handleSaved}
      />
      <PurchaseDetailModal purchaseId={detailId} onClose={() => setDetailId(null)} />
    </main>
  );
}

function PurchaseFormModal({
  open,
  initialPurchase,
  onClose,
  onSaved,
}: {
  open: boolean;
  initialPurchase: PurchaseOrder | null;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const [stocks, setStocks] = useState<StockItem[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [stocksLoading, setStocksLoading] = useState(false);
  const [suppliersLoading, setSuppliersLoading] = useState(false);
  const [stocksError, setStocksError] = useState<string | null>(null);
  const [suppliersError, setSuppliersError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState<PurchaseFormState>(() => createEmptyPurchaseForm());
  const [lines, setLines] = useState<PurchaseFormLine[]>(() => [createEmptyPurchaseLine()]);

  useEffect(() => {
    if (!open) return;

    setError(null);
    setForm(toPurchaseForm(initialPurchase));
    setLines(toPurchaseFormLines(initialPurchase));
    loadStocks();
    loadSuppliers();
  }, [open, initialPurchase]);

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

  async function loadSuppliers() {
    setSuppliersLoading(true);
    setSuppliersError(null);

    try {
      const response = await fetch(API + "/suppliers");

      if (!response.ok) {
        throw new Error("Tedarikçi listesi alınamadı.");
      }

      const result: unknown = await response.json();
      setSuppliers(extractSuppliers(result).filter((supplier) => supplier.isActive !== false));
    } catch (err) {
      setSuppliers([]);
      setSuppliersError(err instanceof Error ? err.message : "Tedarikçi listesi alınırken beklenmeyen bir hata oluştu.");
    } finally {
      setSuppliersLoading(false);
    }
  }

  function updateForm(key: keyof PurchaseFormState, value: string) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function selectSupplier(supplierId: string) {
    if (supplierId === "__existing__") {
      setForm((current) => ({ ...current, supplierId }));
      return;
    }

    const supplier = suppliers.find((item) => item.id === supplierId);

    setForm((current) => ({
      ...current,
      supplierId,
      supplierName: supplier?.name || "",
      supplierCode: supplier?.code || "",
      currency: supplier?.defaultCurrency || current.currency,
    }));
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

    if (!form.supplierId || !form.supplierName.trim()) {
      setError("Tedarikçi seçmelisiniz.");
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
      const response = await fetch(initialPurchase ? `${API}/purchases/${initialPurchase.id}` : API + "/purchases", {
        method: initialPurchase ? "PUT" : "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          supplierName: form.supplierName.trim(),
          supplierCode: form.supplierCode.trim() || null,
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
          status: initialPurchase?.status || "Oluşturuldu",
          note: form.note.trim() || null,
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

      onSaved(initialPurchase ? "Satın alma güncellendi." : "Satın alma kaydı oluşturuldu.");
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
            <h2 className="mt-2 text-3xl font-black">{initialPurchase ? "Satın Alma Düzenle" : "Yeni Satın Alma"}</h2>
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
        {suppliersError && <div className="mb-5 rounded-xl border border-amber-400/30 bg-amber-500/10 p-4 text-sm text-amber-100">{suppliersError}</div>}
        {suppliersLoading && <div className="mb-5 rounded-xl border border-white/10 bg-black/20 p-4 text-sm text-zinc-400">Tedarikçi listesi yükleniyor...</div>}

        <section className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          <Field label="Tedarikçi">
            <select
              value={form.supplierId}
              onChange={(event) => selectSupplier(event.target.value)}
              className={CONTROL_CLASS}
              disabled={suppliersLoading || (suppliers.length === 0 && form.supplierId !== "__existing__")}
            >
              <option value="">{suppliers.length === 0 ? "Önce tedarikçi oluşturun" : "Tedarikçi seç"}</option>
              {form.supplierId === "__existing__" && <option value="__existing__">{form.supplierName || "Mevcut tedarikçi"}</option>}
              {suppliers.map((supplier) => (
                <option key={supplier.id} value={supplier.id}>
                  {[supplier.code, supplier.name].filter(Boolean).join(" - ") || supplier.id}
                </option>
              ))}
            </select>
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
          <label className="md:col-span-2 xl:col-span-3">
            <p className="mb-2 text-sm font-bold text-zinc-300">Not</p>
            <textarea
              value={form.note}
              onChange={(event) => updateForm("note", event.target.value)}
              className={`${CONTROL_CLASS} min-h-24`}
              placeholder="Örn: Cem’in kredi kartı ile ödendi, 3 taksit, provizyon no..."
            />
            {form.paymentType === "Kredi Kartı" && (
              <p className="mt-2 text-xs font-bold text-amber-200">
                Kredi kartı ile ödeme seçildiğinde kart sahibini ve ödeme bilgisini not alanına yazın.
              </p>
            )}
          </label>
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
            disabled={saving || stocksLoading || suppliersLoading}
            className="rounded-xl bg-emerald-500 px-5 py-3 font-black text-black transition hover:bg-emerald-400 disabled:opacity-50"
          >
            {saving ? "Kaydediliyor..." : initialPurchase ? "Değişiklikleri Kaydet" : "Kaydet"}
          </button>
        </div>
      </div>
    </div>
  );
}

function PurchaseDetailModal({ purchaseId, onClose }: { purchaseId: string | null; onClose: () => void }) {
  const [purchase, setPurchase] = useState<PurchaseOrder | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!purchaseId) {
      setPurchase(null);
      setError(null);
      setLoading(false);
      return;
    }

    const controller = new AbortController();

    async function loadPurchaseDetail() {
      setLoading(true);
      setError(null);
      setPurchase(null);

      try {
        const response = await fetch(`${API}/purchases/${purchaseId}`, {
          signal: controller.signal,
        });

        if (!response.ok) {
          throw new Error("Satın alma detayı alınamadı.");
        }

        const result: unknown = await response.json();
        const detail = extractPurchase(result);

        if (!detail) {
          throw new Error("Satın alma detayı beklenen formatta gelmedi.");
        }

        setPurchase(detail);
      } catch (err) {
        if (err instanceof DOMException && err.name === "AbortError") return;
        setError(err instanceof Error ? err.message : "Satın alma detayı alınırken beklenmeyen bir hata oluştu.");
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    }

    loadPurchaseDetail();

    return () => controller.abort();
  }, [purchaseId]);

  if (!purchaseId) return null;

  const currency = purchase?.currency || "TRY";
  const lines = purchase?.lines ?? [];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4 backdrop-blur-sm">
      <div className="max-h-[92vh] w-full max-w-6xl overflow-y-auto rounded-2xl border border-white/10 bg-[#0F1115] p-5 shadow-2xl sm:p-8">
        <div className="mb-6 flex flex-col gap-4 border-b border-white/10 pb-5 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <p className="text-xs font-black tracking-[0.28em] text-emerald-300">FIXAR OS</p>
            <h2 className="mt-2 text-3xl font-black">Satın Alma Detayı</h2>
            <p className="mt-1 text-sm text-zinc-400">{purchase?.documentNo || purchase?.invoiceNo || "Kayıt detayı yükleniyor"}</p>
          </div>
          <button onClick={onClose} className="w-fit rounded-xl bg-zinc-800 px-4 py-2 text-sm font-bold text-white transition hover:bg-zinc-700">
            Kapat
          </button>
        </div>

        {loading && (
          <div className="rounded-xl border border-white/10 bg-black/20 p-5">
            <div className="space-y-3">
              <div className="h-5 w-56 animate-pulse rounded bg-white/10" />
              <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
                <div className="h-20 animate-pulse rounded-xl bg-white/10" />
                <div className="h-20 animate-pulse rounded-xl bg-white/10" />
                <div className="h-20 animate-pulse rounded-xl bg-white/10" />
              </div>
              <div className="h-40 animate-pulse rounded-xl bg-white/10" />
            </div>
          </div>
        )}

        {!loading && error && (
          <div className="rounded-xl border border-red-400/30 bg-red-500/10 p-5 text-sm text-red-100">
            <p className="font-black">Detay yüklenemedi.</p>
            <p className="mt-1 text-red-200">{error}</p>
          </div>
        )}

        {!loading && !error && purchase && (
          <>
            <section className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4">
              <DetailInfo label="Tedarikçi" value={purchase.supplierName || "-"} />
              <DetailInfo label="Belge No" value={purchase.documentNo || "-"} />
              <DetailInfo label="Fatura No" value={purchase.invoiceNo || "-"} />
              <DetailInfo label="Sipariş Tarihi" value={formatDate(purchase.orderDate)} />
              <DetailInfo label="Vade Tarihi" value={formatDate(purchase.dueDate)} />
              <DetailInfo label="Ödeme Şekli" value={purchase.paymentType || "-"} />
              <DetailInfo label="Para Birimi" value={currency} />
              <div className="rounded-xl border border-white/10 bg-black/20 p-4">
                <p className="text-xs text-zinc-500">Durum</p>
                <div className="mt-2">
                  <StatusBadge status={purchase.status} />
                </div>
              </div>
            </section>

            <section className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-4">
              <p className="text-xs text-zinc-500">Not</p>
              <p className="mt-2 text-sm text-zinc-300">{purchase.note || "-"}</p>
            </section>

            <section className="mt-7 rounded-2xl border border-white/10 bg-white/[0.04] p-4">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
                <div>
                  <h3 className="text-xl font-black">Ürün Satırları</h3>
                  <p className="mt-1 text-sm text-zinc-400">{lines.length.toLocaleString("tr-TR")} satır listeleniyor.</p>
                </div>
              </div>

              {lines.length === 0 ? (
                <div className="mt-4 rounded-xl border border-white/10 bg-black/20 p-5 text-sm text-zinc-400">Bu satın alma kaydında ürün satırı yok.</div>
              ) : (
                <div className="mt-4 overflow-hidden rounded-xl border border-white/10 bg-black/20">
                  <div className="overflow-x-auto">
                    <table className="w-full min-w-[760px] border-collapse text-left">
                      <thead className="bg-white/[0.06] text-xs uppercase tracking-[0.16em] text-zinc-400">
                        <tr>
                          <TableHead>Stok adı</TableHead>
                          <TableHead>Miktar</TableHead>
                          <TableHead>Birim</TableHead>
                          <TableHead>Birim fiyat</TableHead>
                          <TableHead>Satır toplamı</TableHead>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-white/10">
                        {lines.map((line, index) => (
                          <tr key={line.id ?? `${line.stockItemId}-${index}`} className="transition hover:bg-white/[0.04]">
                            <TableCell>
                              <span className="font-black text-white">{line.stockName || "-"}</span>
                            </TableCell>
                            <TableCell>{formatNumber(safeNumber(line.quantity))}</TableCell>
                            <TableCell>{line.unit || "-"}</TableCell>
                            <TableCell>{formatMoney(safeNumber(line.unitPrice), currency)}</TableCell>
                            <TableCell>
                              <span className="font-black text-emerald-200">{formatMoney(safeNumber(line.lineTotal), currency)}</span>
                            </TableCell>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </section>

            <section className="mt-6 grid grid-cols-1 gap-3 sm:ml-auto sm:max-w-md">
              <SummaryRow label="Ara Toplam" value={formatMoney(safeNumber(purchase.subTotal), currency)} />
              <SummaryRow label="KDV Toplam" value={formatMoney(safeNumber(purchase.vatTotal), currency)} />
              <SummaryRow label="Genel Toplam" value={formatMoney(safeNumber(purchase.grandTotal), currency)} strong />
            </section>
          </>
        )}
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

function DetailInfo({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs text-zinc-500">{label}</p>
      <p className="mt-2 break-words text-sm font-black text-white">{value}</p>
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
  const rawLabel = status || "Oluşturuldu";
  const label = formatStatusLabel(rawLabel);
  const isDone = ["Tamamlandı", "Ödendi", "Kapandı"].includes(rawLabel);
  const isProblem = ["Cancelled", "İptal", "İptal Edildi"].includes(rawLabel);
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

function extractPurchase(result: unknown): PurchaseOrder | null {
  if (isPurchaseOrder(result)) {
    return result;
  }

  if (isRecord(result) && isPurchaseOrder(result.data)) {
    return result.data;
  }

  return null;
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

function extractSuppliers(result: unknown): Supplier[] {
  if (Array.isArray(result)) {
    return result.filter(isSupplier);
  }

  if (isRecord(result) && Array.isArray(result.data)) {
    return result.data.filter(isSupplier);
  }

  return [];
}

function isPurchaseOrder(value: unknown): value is PurchaseOrder {
  return isRecord(value) && typeof value.id === "string";
}

function isStockItem(value: unknown): value is StockItem {
  return isRecord(value) && typeof value.id === "string";
}

function isSupplier(value: unknown): value is Supplier {
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
  if (isPurchaseCancelled(item)) return false;

  const status = (item.status || "Oluşturuldu").toLocaleLowerCase("tr-TR");
  return status.includes("bekleyen") || status.includes("oluşturuldu") || status.includes("açık");
}

function isPurchaseCancelled(item: PurchaseOrder) {
  return (item.status || "").toLocaleLowerCase("tr-TR") === "cancelled";
}

function formatStatusLabel(status: string | null | undefined) {
  return status === "Cancelled" ? "İptal edildi" : status || "Oluşturuldu";
}

function safeNumber(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function formatNumber(value: number) {
  return safeNumber(value).toLocaleString("tr-TR");
}

function formatDecimalForCsv(value: number) {
  return safeNumber(value).toLocaleString("tr-TR", {
    maximumFractionDigits: 2,
    minimumFractionDigits: 2,
  });
}

function escapeCsvCell(value: string) {
  return `"${value.replaceAll('"', '""')}"`;
}

function renderPurchaseReportPages(purchases: PurchaseOrder[], generatedAt: string, totalAmount: number, currency: string): PdfPageImage[] {
  const pageWidth = 1240;
  const pageHeight = 1754;
  const margin = 58;
  const headerHeight = 184;
  const rowHeight = 54;
  const footerHeight = 90;
  const rowsPerPage = Math.max(1, Math.floor((pageHeight - margin * 2 - headerHeight - footerHeight) / rowHeight));
  const chunks: PurchaseOrder[][] = [];

  for (let index = 0; index < purchases.length; index += rowsPerPage) {
    chunks.push(purchases.slice(index, index + rowsPerPage));
  }

  return chunks.map((chunk, pageIndex) => {
    const canvas = document.createElement("canvas");
    canvas.width = pageWidth;
    canvas.height = pageHeight;

    const ctx = canvas.getContext("2d");

    if (!ctx) {
      throw new Error("PDF raporu oluşturulamadı.");
    }

    ctx.fillStyle = "#ffffff";
    ctx.fillRect(0, 0, pageWidth, pageHeight);

    ctx.fillStyle = "#064e3b";
    ctx.fillRect(0, 0, pageWidth, 22);
    drawReportText(ctx, "FIXAR OS - Satın Alma Raporu", margin, 78, 760, "bold 34px Arial", "#111827");
    drawReportText(ctx, `Rapor tarihi: ${generatedAt}`, margin, 118, 520, "20px Arial", "#4b5563");
    drawReportText(ctx, `Filtrelenmiş kayıt sayısı: ${purchases.length.toLocaleString("tr-TR")}`, margin, 148, 520, "20px Arial", "#4b5563");
    drawReportText(ctx, `Toplam genel tutar: ${formatMoney(totalAmount, currency)}`, pageWidth - margin, 118, 520, "bold 22px Arial", "#064e3b", "right");

    const columns = [
      { title: "Tarih", width: 118 },
      { title: "Belge No", width: 140 },
      { title: "Fatura No", width: 140 },
      { title: "Tedarikçi", width: 260 },
      { title: "Ödeme Şekli", width: 160 },
      { title: "Para Birimi", width: 118 },
      { title: "Genel Toplam", width: 150 },
      { title: "Durum", width: 98 },
    ];
    let x = margin;
    const tableTop = margin + headerHeight;

    ctx.fillStyle = "#ecfdf5";
    ctx.fillRect(margin, tableTop, pageWidth - margin * 2, rowHeight);
    ctx.strokeStyle = "#d1d5db";
    ctx.lineWidth = 2;
    ctx.strokeRect(margin, tableTop, pageWidth - margin * 2, rowHeight);

    columns.forEach((column) => {
      ctx.strokeStyle = "#d1d5db";
      ctx.strokeRect(x, tableTop, column.width, rowHeight);
      drawReportText(ctx, column.title, x + 10, tableTop + 34, column.width - 20, "bold 17px Arial", "#064e3b");
      x += column.width;
    });

    chunk.forEach((purchase, rowIndex) => {
      const y = tableTop + rowHeight * (rowIndex + 1);
      const values = [
        formatDate(purchase.orderDate ?? purchase.createdAt),
        purchase.documentNo || "-",
        purchase.invoiceNo || "-",
        purchase.supplierName || "-",
        purchase.paymentType || "-",
        purchase.currency || "-",
        formatMoney(safeNumber(purchase.grandTotal), purchase.currency || "TRY"),
        formatStatusLabel(purchase.status),
      ];

      x = margin;
      ctx.fillStyle = rowIndex % 2 === 0 ? "#ffffff" : "#f9fafb";
      ctx.fillRect(margin, y, pageWidth - margin * 2, rowHeight);

      values.forEach((value, columnIndex) => {
        const column = columns[columnIndex];
        ctx.strokeStyle = "#e5e7eb";
        ctx.strokeRect(x, y, column.width, rowHeight);
        drawReportText(ctx, value, x + 10, y + 34, column.width - 20, columnIndex === 6 ? "bold 16px Arial" : "16px Arial", "#111827", columnIndex === 6 ? "right" : "left");
        x += column.width;
      });
    });

    const footerY = pageHeight - margin;
    drawReportText(ctx, `Sayfa ${pageIndex + 1} / ${chunks.length}`, pageWidth - margin, footerY, 240, "18px Arial", "#6b7280", "right");

    if (pageIndex === chunks.length - 1) {
      drawReportText(ctx, `Toplam Genel Tutar: ${formatMoney(totalAmount, currency)}`, margin, footerY, 620, "bold 24px Arial", "#111827");
    }

    return {
      dataUrl: canvas.toDataURL("image/jpeg", 0.92),
      width: pageWidth,
      height: pageHeight,
    };
  });
}

function drawReportText(
  ctx: CanvasRenderingContext2D,
  value: string,
  x: number,
  y: number,
  maxWidth: number,
  font: string,
  color: string,
  align: CanvasTextAlign = "left"
) {
  ctx.font = font;
  ctx.fillStyle = color;
  ctx.textAlign = align;
  ctx.textBaseline = "alphabetic";

  let text = value;

  while (ctx.measureText(text).width > maxWidth && text.length > 1) {
    text = text.slice(0, -2);
  }

  if (text !== value) {
    text = text.slice(0, Math.max(0, text.length - 1)) + "...";
  }

  ctx.fillText(text, x, y);
}

function createPdfFromJpegPages(pages: PdfPageImage[]) {
  const pageWidth = 595.28;
  const pageHeight = 841.89;
  const objects: string[] = [];
  const pageObjectIds: number[] = [];

  objects.push("<< /Type /Catalog /Pages 2 0 R >>");
  objects.push("");

  pages.forEach((page, index) => {
    const pageObjectId = objects.length + 1;
    const contentObjectId = pageObjectId + 1;
    const imageObjectId = pageObjectId + 2;
    const imageName = `Im${index + 1}`;
    const imageBinary = atob(page.dataUrl.split(",")[1] || "");
    const content = `q\n${pageWidth} 0 0 ${pageHeight} 0 0 cm\n/${imageName} Do\nQ`;

    pageObjectIds.push(pageObjectId);
    objects.push(`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 ${pageWidth} ${pageHeight}] /Resources << /XObject << /${imageName} ${imageObjectId} 0 R >> >> /Contents ${contentObjectId} 0 R >>`);
    objects.push(`<< /Length ${content.length} >>\nstream\n${content}\nendstream`);
    objects.push(`<< /Type /XObject /Subtype /Image /Width ${page.width} /Height ${page.height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length ${imageBinary.length} >>\nstream\n${imageBinary}\nendstream`);
  });

  objects[1] = `<< /Type /Pages /Kids [${pageObjectIds.map((id) => `${id} 0 R`).join(" ")}] /Count ${pageObjectIds.length} >>`;

  let pdf = "%PDF-1.4\n";
  const offsets = [0];

  objects.forEach((object, index) => {
    offsets.push(pdf.length);
    pdf += `${index + 1} 0 obj\n${object}\nendobj\n`;
  });

  const xrefOffset = pdf.length;
  pdf += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
  offsets.slice(1).forEach((offset) => {
    pdf += `${String(offset).padStart(10, "0")} 00000 n \n`;
  });
  pdf += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xrefOffset}\n%%EOF`;

  const bytes = new Uint8Array(pdf.length);

  for (let index = 0; index < pdf.length; index += 1) {
    bytes[index] = pdf.charCodeAt(index) & 0xff;
  }

  return new Blob([bytes], { type: "application/pdf" });
}

function createEmptyPurchaseForm(): PurchaseFormState {
  return {
    supplierId: "",
    supplierName: "",
    supplierCode: "",
    documentNo: "",
    invoiceNo: "",
    orderDate: formatDateInput(new Date()),
    currency: "TRY",
    paymentType: "Nakit",
    note: "",
  };
}

function toPurchaseForm(purchase: PurchaseOrder | null): PurchaseFormState {
  if (!purchase) return createEmptyPurchaseForm();

  return {
    supplierId: "__existing__",
    supplierName: purchase.supplierName || "",
    supplierCode: purchase.supplierCode || "",
    documentNo: purchase.documentNo || "",
    invoiceNo: purchase.invoiceNo || "",
    orderDate: formatDateInputFromValue(purchase.orderDate) || formatDateInput(new Date()),
    currency: purchase.currency || "TRY",
    paymentType: fromBackendPaymentType(purchase.paymentType),
    note: purchase.note || "",
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

function toPurchaseFormLines(purchase: PurchaseOrder | null): PurchaseFormLine[] {
  if (!purchase || !purchase.lines || purchase.lines.length === 0) {
    return [createEmptyPurchaseLine()];
  }

  return purchase.lines.map((line) => ({
    key: line.id || `${line.stockItemId}-${Math.random().toString(36).slice(2)}`,
    stockItemId: line.stockItemId || "",
    quantity: line.quantity === null || line.quantity === undefined ? "" : String(line.quantity),
    unit: line.unit || "",
    unitPrice: line.unitPrice === null || line.unitPrice === undefined ? "" : String(line.unitPrice),
    vatRate: purchase.vatRate === null || purchase.vatRate === undefined ? "0" : String(purchase.vatRate),
  }));
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

function fromBackendPaymentType(paymentType: string | null | undefined) {
  return paymentType === "Cari Hesap" ? "Vadeli" : paymentType || "Nakit";
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

function formatDateInputFromValue(value: string | null | undefined) {
  if (!value) return "";

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";

  return formatDateInput(date);
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
