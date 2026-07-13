"use client";

import Image from "next/image";
import { useEffect, useMemo, useState, type ReactNode } from "react";

type DashboardTone = "emerald" | "cyan" | "amber" | "red" | "blue" | "violet" | "zinc";
type DialogMode = "create" | "edit" | "detail" | null;
type WorkOrderTab = "general" | "product" | "plan" | "materials" | "operations" | "quality" | "notes";

type Product = {
  id: string;
  code?: string | null;
  name?: string | null;
  customerName?: string | null;
  category?: string | null;
  modelCode?: string | null;
  description?: string | null;
  foamType?: string | null;
  productType?: string | null;
  isFabric?: boolean | null;
  isAdhesive?: boolean | null;
  hasDTFLabel?: boolean | null;
  averageWeight?: number | null;
  targetDensity?: number | null;
  standardCycleTime?: number | null;
  isActive?: boolean | null;
};

type Material = {
  id: string;
  code?: string | null;
  name?: string | null;
  materialType?: string | null;
  unit?: string | null;
  currency?: string | null;
  lastPurchasePrice?: number | null;
  isActive?: boolean | null;
};

type StockItem = {
  id: string;
  materialId?: string | null;
  materialCode?: string | null;
  code?: string | null;
  name?: string | null;
  currentQuantity?: number | null;
  criticalQuantity?: number | null;
  unit?: string | null;
};

type ProductDetails = {
  model?: string;
  number?: string;
  color?: string;
  productionType?: string;
  fabricType?: string;
  adhesiveType?: string;
  productImageName?: string;
  productImageDataUrl?: string;
  moldImageName?: string;
  moldImageDataUrl?: string;
  standardWasteRate?: string;
  standardMold?: string;
  standardDailyCapacity?: string;
  cuttingMachine?: string;
  defaultOperationNote?: string;
  minWeight?: string;
  maxWeight?: string;
  minDensity?: string;
  maxDensity?: string;
  acceptedWasteRate?: string;
  qualityNote?: string;
};

type WorkOrderMaterialLine = {
  key: string;
  role: string;
  materialId: string;
  usagePerPair: string;
  wasteRate: string;
  note: string;
};

type OperationLine = {
  key: string;
  operation: string;
  status: string;
  responsible: string;
  estimatedTime: string;
  actualTime: string;
  note: string;
};

type WorkOrder = {
  id: string;
  workOrderNo: string;
  productId: string;
  version: string;
  revisionNo: string;
  customerName: string;
  orderNo: string;
  plannedPairs: string;
  producedPairs: string;
  priority: string;
  startDate: string;
  dueDate: string;
  status: string;
  machine: string;
  mold: string;
  operatorName: string;
  shift: string;
  plannedStart: string;
  plannedEnd: string;
  estimatedProductionTime: string;
  estimatedDailyCapacity: string;
  estimatedWasteRate: string;
  operationNote: string;
  materialLines: WorkOrderMaterialLine[];
  operations: OperationLine[];
  productionNote: string;
  customerNote: string;
  qualityNote: string;
  generalNote: string;
  updatedAt: string;
};

type ApiResponse<T> = {
  data?: T;
};

type MaterialTotals = {
  totalCost: number;
  totalGram: number;
  missingCount: number;
  criticalCount: number;
};

const API = "http://localhost:5000/api/v1";
const PRODUCT_MARKER = "\n\n---FIXAR_PRODUCT_MASTER_JSON---\n";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60 disabled:cursor-not-allowed disabled:opacity-70";
const WORK_ORDER_STATUSES = ["Taslak", "Hazır", "Üretimde", "Tamamlandı", "İptal"];
const PRIORITIES = ["Normal", "Yüksek", "Acil"];
const OPERATION_NAMES = ["Enjeksiyon", "Pişirme", "Kesim", "DTF", "Kalite", "Paketleme", "Depo"];
const RECIPE_ROLES = ["Poliol", "İzosiyanat", "Crosskim", "Pigment", "Solvent", "Kalıp Ayırıcı", "Kumaş", "Yapışkan", "DTF", "İşçilik", "Diğer"];
const TABS: Array<{ id: WorkOrderTab; label: string }> = [
  { id: "general", label: "1 Genel" },
  { id: "product", label: "2 Ürün Bilgileri" },
  { id: "plan", label: "3 Üretim Planı" },
  { id: "materials", label: "4 Kullanılacak Hammaddeler" },
  { id: "operations", label: "5 Operasyonlar" },
  { id: "quality", label: "6 Kalite" },
  { id: "notes", label: "7 Notlar" },
];

export default function WorkOrdersPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [materials, setMaterials] = useState<Material[]>([]);
  const [stocks, setStocks] = useState<StockItem[]>([]);
  const [workOrders, setWorkOrders] = useState<WorkOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [dialogMode, setDialogMode] = useState<DialogMode>(null);
  const [dialogWorkOrder, setDialogWorkOrder] = useState<WorkOrder | null>(null);

  useEffect(() => {
    loadMasters();
  }, []);

  async function loadMasters() {
    setLoading(true);
    setError(null);

    try {
      const [productsResponse, materialsResponse, stocksResponse] = await Promise.all([
        fetch(API + "/products"),
        fetch(API + "/materials"),
        fetch(API + "/stocks"),
      ]);

      if (!productsResponse.ok || !materialsResponse.ok || !stocksResponse.ok) {
        throw new Error("Master veriler alınamadı.");
      }

      setProducts(extractProducts(await productsResponse.json()).filter((item) => item.isActive !== false));
      setMaterials(extractMaterials(await materialsResponse.json()).filter((item) => item.isActive !== false));
      setStocks(extractStocks(await stocksResponse.json()));
    } catch (err) {
      setProducts([]);
      setMaterials([]);
      setStocks([]);
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  function openDialog(mode: DialogMode, workOrder: WorkOrder | null = null) {
    setSuccessMessage(null);
    setDialogWorkOrder(workOrder);
    setDialogMode(mode);
  }

  function closeDialog() {
    setDialogMode(null);
    setDialogWorkOrder(null);
  }

  function handleSaved(workOrder: WorkOrder, message: string) {
    setWorkOrders((current) => {
      const exists = current.some((item) => item.id === workOrder.id);
      return exists ? current.map((item) => (item.id === workOrder.id ? workOrder : item)) : [workOrder, ...current];
    });
    closeDialog();
    setSuccessMessage(message);
  }

  function startProduction(workOrder: WorkOrder) {
    setWorkOrders((current) =>
      current.map((item) => (item.id === workOrder.id ? { ...item, status: "Üretimde", updatedAt: new Date().toISOString() } : item))
    );
    setSuccessMessage("İş emri üretimde durumuna alındı.");
  }

  const filteredWorkOrders = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase("tr-TR");
    if (!normalizedSearch) return workOrders;

    return workOrders.filter((order) => {
      const product = products.find((item) => item.id === order.productId);
      return [order.workOrderNo, order.orderNo, product?.name, product?.code, order.customerName, order.status]
        .filter(Boolean)
        .some((value) => String(value).toLocaleLowerCase("tr-TR").includes(normalizedSearch));
    });
  }, [products, search, workOrders]);

  const today = formatDateInput(new Date());
  const totalPlannedPairs = workOrders.reduce((sum, order) => sum + safeParsedNumber(order.plannedPairs), 0);
  const dashboardCards = [
    { title: "Toplam İş Emri", value: workOrders.length.toLocaleString("tr-TR"), note: "Frontend iş emri", tone: "emerald" as DashboardTone },
    { title: "Bekleyen", value: countByStatus(workOrders, "Taslak"), note: "Taslak kayıt", tone: "zinc" as DashboardTone },
    { title: "Hazırlanıyor", value: countByStatus(workOrders, "Hazır"), note: "Üretime hazır", tone: "cyan" as DashboardTone },
    { title: "Üretimde", value: countByStatus(workOrders, "Üretimde"), note: "Canlı üretime aday", tone: "amber" as DashboardTone },
    { title: "Tamamlanan", value: countByStatus(workOrders, "Tamamlandı"), note: "Kapanan emir", tone: "blue" as DashboardTone },
    { title: "İptal", value: countByStatus(workOrders, "İptal"), note: "Durdurulan emir", tone: "red" as DashboardTone },
    { title: "Bugünkü Üretim", value: workOrders.filter((order) => order.startDate === today).length.toLocaleString("tr-TR"), note: "Bugün başlayan", tone: "violet" as DashboardTone },
    { title: "Toplam Planlanan Çift", value: formatNumber(totalPlannedPairs), note: "Tüm iş emirleri", tone: "emerald" as DashboardTone },
  ];

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.18),transparent_34%),radial-gradient(circle_at_bottom_right,rgba(14,165,233,0.13),transparent_32%)] px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.38em] text-emerald-300">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">İş Emri Yönetimi</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                Product, Recipe, Material ve Stock master verilerinden beslenen üretim başlangıç noktası.
              </p>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <button
                onClick={loadMasters}
                disabled={loading}
                className="rounded-xl border border-white/10 bg-white/[0.08] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:opacity-50"
              >
                {loading ? "Yenileniyor..." : "Masterları Yenile"}
              </button>
              <button onClick={() => openDialog("create")} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
                + Yeni İş Emri
              </button>
            </div>
          </header>

          {successMessage && <div className="rounded-xl border border-emerald-400/30 bg-emerald-500/10 p-4 text-sm font-bold text-emerald-100">{successMessage}</div>}

          <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {dashboardCards.map((card) => (
              <DashboardCard key={card.title} title={card.title} value={card.value} note={card.note} tone={card.tone} />
            ))}
          </section>

          <section className="rounded-2xl border border-white/10 bg-white/[0.06] p-5 shadow-2xl backdrop-blur">
            <div className="flex flex-col gap-4 border-b border-white/10 pb-5 xl:flex-row xl:items-end xl:justify-between">
              <div>
                <h2 className="text-2xl font-black">İş Emri Listesi</h2>
                <p className="mt-1 text-sm text-zinc-400">{filteredWorkOrders.length.toLocaleString("tr-TR")} iş emri listeleniyor.</p>
              </div>
              <div className="w-full xl:max-w-md">
                <Field label="Arama">
                  <input value={search} onChange={(event) => setSearch(event.target.value)} className={CONTROL_CLASS} placeholder="İş emri, ürün, müşteri, durum" />
                </Field>
              </div>
            </div>

            {loading && <LoadingState />}

            {!loading && error && (
              <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-5 text-sm text-red-100">
                <p className="font-black">Master veriler yüklenemedi.</p>
                <p className="mt-1 text-red-200">{error}</p>
              </div>
            )}

            {!loading && !error && filteredWorkOrders.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-zinc-300">
                Henüz iş emri yok. Product Master seçerek yeni iş emri oluşturun.
              </div>
            )}

            {!loading && !error && filteredWorkOrders.length > 0 && (
              <div className="mt-5 overflow-x-auto">
                <table className="min-w-[1240px] w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-white/10 text-xs uppercase tracking-[0.18em] text-zinc-500">
                      <th className="py-3 pr-4">İş Emri No</th>
                      <th className="py-3 pr-4">Ürün</th>
                      <th className="py-3 pr-4">Müşteri</th>
                      <th className="py-3 pr-4">Numara</th>
                      <th className="py-3 pr-4">Foam</th>
                      <th className="py-3 pr-4">Planlanan Çift</th>
                      <th className="py-3 pr-4">Üretilen</th>
                      <th className="py-3 pr-4">Kalan</th>
                      <th className="py-3 pr-4">Başlangıç</th>
                      <th className="py-3 pr-4">Termin</th>
                      <th className="py-3 pr-4">Durum</th>
                      <th className="py-3 pr-4">Öncelik</th>
                      <th className="py-3 text-right">İşlemler</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/10">
                    {filteredWorkOrders.map((order) => {
                      const product = products.find((item) => item.id === order.productId);
                      const details = parseProductDetails(product?.description);
                      const planned = safeParsedNumber(order.plannedPairs);
                      const produced = safeParsedNumber(order.producedPairs);

                      return (
                        <tr key={order.id} className="align-middle text-zinc-200 transition hover:bg-white/[0.04]">
                          <td className="py-4 pr-4 font-mono text-xs text-emerald-200">{order.workOrderNo}</td>
                          <td className="py-4 pr-4 font-black text-white">{product?.name || "-"}</td>
                          <td className="py-4 pr-4">{order.customerName || product?.customerName || "-"}</td>
                          <td className="py-4 pr-4">{details.number || "-"}</td>
                          <td className="py-4 pr-4">{product?.foamType || "-"}</td>
                          <td className="py-4 pr-4">{formatNumber(planned)}</td>
                          <td className="py-4 pr-4">{formatNumber(produced)}</td>
                          <td className="py-4 pr-4">{formatNumber(Math.max(planned - produced, 0))}</td>
                          <td className="py-4 pr-4">{formatDate(order.startDate)}</td>
                          <td className="py-4 pr-4">{formatDate(order.dueDate)}</td>
                          <td className="py-4 pr-4"><StatusBadge status={order.status} /></td>
                          <td className="py-4 pr-4">{order.priority}</td>
                          <td className="py-4">
                            <div className="flex justify-end gap-2">
                              <button onClick={() => openDialog("detail", order)} className="rounded-lg border border-cyan-400/30 bg-cyan-400/10 px-3 py-2 text-xs font-black text-cyan-100 transition hover:bg-cyan-400/20">Detay</button>
                              <button onClick={() => openDialog("edit", order)} className="rounded-lg border border-emerald-400/30 bg-emerald-400/10 px-3 py-2 text-xs font-black text-emerald-100 transition hover:bg-emerald-400/20">Düzenle</button>
                              <button onClick={() => startProduction(order)} disabled={order.status === "Üretimde" || order.status === "Tamamlandı" || order.status === "İptal"} className="rounded-lg border border-amber-400/30 bg-amber-400/10 px-3 py-2 text-xs font-black text-amber-100 transition hover:bg-amber-400/20 disabled:cursor-not-allowed disabled:opacity-40">Üretimi Başlat</button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      </div>

      {dialogMode && (
        <WorkOrderModal
          mode={dialogMode}
          workOrder={dialogWorkOrder}
          products={products}
          materials={materials}
          stocks={stocks}
          onClose={closeDialog}
          onSaved={handleSaved}
        />
      )}
    </main>
  );
}

function WorkOrderModal({
  mode,
  workOrder,
  products,
  materials,
  stocks,
  onClose,
  onSaved,
}: {
  mode: DialogMode;
  workOrder: WorkOrder | null;
  products: Product[];
  materials: Material[];
  stocks: StockItem[];
  onClose: () => void;
  onSaved: (workOrder: WorkOrder, message: string) => void;
}) {
  const [activeTab, setActiveTab] = useState<WorkOrderTab>("general");
  const [form, setForm] = useState<WorkOrder>(() => (workOrder ? cloneWorkOrder(workOrder) : createEmptyWorkOrder()));
  const [error, setError] = useState<string | null>(null);
  const readonly = mode === "detail";
  const product = products.find((item) => item.id === form.productId);
  const details = parseProductDetails(product?.description);
  const materialTotals = calculateMaterialTotals(form, materials, stocks);
  const checks = buildChecks(form, product, materials, stocks);

  function updateForm<K extends keyof WorkOrder>(key: K, value: WorkOrder[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function selectProduct(productId: string) {
    const selected = products.find((item) => item.id === productId);
    const productDetails = parseProductDetails(selected?.description);

    setForm((current) => ({
      ...current,
      productId,
      customerName: selected?.customerName || current.customerName,
      mold: productDetails.standardMold || current.mold,
      estimatedProductionTime: selected?.standardCycleTime ? String(selected.standardCycleTime) : current.estimatedProductionTime,
      estimatedDailyCapacity: productDetails.standardDailyCapacity || current.estimatedDailyCapacity,
      estimatedWasteRate: productDetails.standardWasteRate || current.estimatedWasteRate,
      operationNote: productDetails.defaultOperationNote || current.operationNote,
      materialLines: createMaterialLinesFromMasters(materials, selected),
    }));
  }

  function updateMaterialLine(key: string, field: keyof WorkOrderMaterialLine, value: string) {
    setForm((current) => ({
      ...current,
      materialLines: current.materialLines.map((line) => (line.key === key ? { ...line, [field]: value } : line)),
    }));
  }

  function updateOperation(key: string, field: keyof OperationLine, value: string) {
    setForm((current) => ({
      ...current,
      operations: current.operations.map((line) => (line.key === key ? { ...line, [field]: value } : line)),
    }));
  }

  function saveWorkOrder() {
    setError(null);

    if (!form.workOrderNo.trim()) {
      setError("İş emri no zorunludur.");
      setActiveTab("general");
      return;
    }

    if (!form.productId) {
      setError("Product Master seçmelisiniz.");
      setActiveTab("general");
      return;
    }

    if (safeParsedNumber(form.plannedPairs) <= 0) {
      setError("Planlanan üretim miktarı 0'dan büyük olmalıdır.");
      setActiveTab("general");
      return;
    }

    onSaved({ ...cloneWorkOrder(form), updatedAt: new Date().toISOString() }, mode === "edit" ? "İş emri güncellendi." : "İş emri oluşturuldu.");
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-3 backdrop-blur-sm sm:p-5">
      <div className="flex max-h-[94vh] w-full max-w-7xl flex-col overflow-hidden rounded-2xl border border-white/10 bg-[#080B10] shadow-2xl">
        <div className="border-b border-white/10 bg-white/[0.04] p-5">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.34em] text-emerald-300">WORK ORDER</p>
              <h2 className="mt-2 text-2xl font-black text-white">{readonly ? "İş Emri Detayı" : mode === "edit" ? "İş Emri Düzenle" : "Yeni İş Emri"}</h2>
              <p className="mt-1 text-sm text-zinc-400">Product, Recipe, Material ve Stock bilgileri üzerinden üretimi başlatın.</p>
            </div>
            <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white transition hover:bg-white/[0.12]">Kapat</button>
          </div>
          <WorkOrderSummary form={form} product={product} details={details} totals={materialTotals} />
        </div>

        <div className="border-b border-white/10 px-5 pt-4">
          <div className="flex gap-2 overflow-x-auto pb-4">
            {TABS.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`whitespace-nowrap rounded-xl px-4 py-2 text-sm font-black transition ${
                  activeTab === tab.id ? "bg-emerald-500 text-black" : "border border-white/10 bg-black/30 text-zinc-300 hover:bg-white/[0.08]"
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>
        </div>

        <div className="overflow-y-auto p-5">
          {error && <div className="mb-5 rounded-xl border border-red-400/30 bg-red-500/10 p-4 text-sm font-bold text-red-100">{error}</div>}
          <ControlCards checks={checks} />

          {activeTab === "general" && <GeneralTab form={form} product={product} details={details} products={products} readonly={readonly} selectProduct={selectProduct} updateForm={updateForm} />}
          {activeTab === "product" && <ProductInfoTab product={product} details={details} />}
          {activeTab === "plan" && <ProductionPlanTab form={form} readonly={readonly} updateForm={updateForm} />}
          {activeTab === "materials" && <MaterialsTab form={form} materials={materials} stocks={stocks} readonly={readonly} updateMaterialLine={updateMaterialLine} totals={materialTotals} />}
          {activeTab === "operations" && <OperationsTab form={form} readonly={readonly} updateOperation={updateOperation} />}
          {activeTab === "quality" && <QualityTab details={details} product={product} />}
          {activeTab === "notes" && <NotesTab form={form} readonly={readonly} updateForm={updateForm} />}
        </div>

        <div className="flex flex-col gap-3 border-t border-white/10 bg-black/30 p-5 sm:flex-row sm:justify-end">
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.12]">
            {readonly ? "Kapat" : "Vazgeç"}
          </button>
          {!readonly && (
            <button onClick={saveWorkOrder} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
              Kaydet
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function WorkOrderSummary({ form, product, details, totals }: { form: WorkOrder; product: Product | undefined; details: ProductDetails; totals: MaterialTotals }) {
  const planned = safeParsedNumber(form.plannedPairs);
  const produced = safeParsedNumber(form.producedPairs);
  const items = [
    ["İş Emri", form.workOrderNo || "-"],
    ["Ürün", product?.name || "-"],
    ["Müşteri", form.customerName || product?.customerName || "-"],
    ["Numara", details.number || "-"],
    ["Foam", product?.foamType || "-"],
    ["Plan", formatNumber(planned)],
    ["Kalan", formatNumber(Math.max(planned - produced, 0))],
    ["Durum", form.status],
    ["Öncelik", form.priority],
    ["Hammadde", formatMoney(totals.totalCost, "TRY")],
  ];

  return (
    <div className="mt-5 grid gap-2 sm:grid-cols-2 lg:grid-cols-5">
      {items.map(([label, value]) => (
        <div key={label} className="rounded-xl border border-white/10 bg-black/30 px-3 py-2">
          <p className="text-[10px] font-black uppercase tracking-[0.18em] text-zinc-500">{label}</p>
          <p className="mt-1 truncate text-sm font-black text-white" title={value}>{value}</p>
        </div>
      ))}
    </div>
  );
}

function ControlCards({ checks }: { checks: Array<{ label: string; status: string; tone: "emerald" | "amber" | "red" }> }) {
  return (
    <section className="mb-5 grid gap-3 md:grid-cols-2 xl:grid-cols-5">
      {checks.map((check) => (
        <div key={check.label} className={`rounded-xl border p-4 ${check.tone === "emerald" ? "border-emerald-400/30 bg-emerald-500/10 text-emerald-100" : check.tone === "amber" ? "border-amber-400/30 bg-amber-500/10 text-amber-100" : "border-red-400/30 bg-red-500/10 text-red-100"}`}>
          <p className="text-xs font-black uppercase tracking-[0.16em] opacity-80">{check.label}</p>
          <p className="mt-2 text-sm font-black">{check.status}</p>
        </div>
      ))}
    </section>
  );
}

function GeneralTab({
  form,
  product,
  details,
  products,
  readonly,
  selectProduct,
  updateForm,
}: {
  form: WorkOrder;
  product: Product | undefined;
  details: ProductDetails;
  products: Product[];
  readonly: boolean;
  selectProduct: (productId: string) => void;
  updateForm: <K extends keyof WorkOrder>(key: K, value: WorkOrder[K]) => void;
}) {
  return (
    <TabPanel title="Genel" note="Ürün seçildiğinde Product Master teknik bilgileri otomatik gelir.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="İş Emri No" value={form.workOrderNo} readonly={readonly} onChange={(value) => updateForm("workOrderNo", value)} />
        <Field label="Ürün Seç">
          <select value={form.productId} disabled={readonly} onChange={(event) => selectProduct(event.target.value)} className={CONTROL_CLASS}>
            <option value="">Product Master seç</option>
            {products.map((item) => (
              <option key={item.id} value={item.id}>{[item.code, item.name, item.foamType].filter(Boolean).join(" - ")}</option>
            ))}
          </select>
        </Field>
        <ReadOnlyInfo label="Kod" value={product?.code || "-"} />
        <ReadOnlyInfo label="Ürün Adı" value={product?.name || "-"} />
        <ReadOnlyInfo label="Foam" value={product?.foamType || "-"} />
        <ReadOnlyInfo label="Numara" value={details.number || "-"} />
        <ReadOnlyInfo label="Gramaj" value={formatNumber(product?.averageWeight)} />
        <ReadOnlyInfo label="Yoğunluk" value={formatNumber(product?.targetDensity)} />
        <ReadOnlyInfo label="Kumaş" value={details.fabricType || (product?.isFabric ? "Var" : "Yok")} />
        <ReadOnlyInfo label="DTF" value={product?.hasDTFLabel ? "Var" : "Yok"} />
        <ReadOnlyInfo label="Yapışkan" value={details.adhesiveType || (product?.isAdhesive ? "Var" : "Yok")} />
        <ReadOnlyInfo label="Standart Kalıp" value={details.standardMold || form.mold || "-"} />
        <ReadOnlyInfo label="Standart Pişme Süresi" value={product?.standardCycleTime ? String(product.standardCycleTime) : "-"} />
        <ReadOnlyInfo label="Standart Fire" value={details.standardWasteRate || "-"} />
        <ReadOnlyInfo label="Standart Günlük Kapasite" value={details.standardDailyCapacity || "-"} />
        <TextInput label="Versiyon" value="Product Master" readonly onChange={() => undefined} />
        <TextInput label="Revizyon" value="Recipe state" readonly onChange={() => undefined} />
        <TextInput label="Müşteri" value={form.customerName} readonly={readonly} onChange={(value) => updateForm("customerName", value)} />
        <TextInput label="Sipariş No" value={form.orderNo} readonly={readonly} onChange={(value) => updateForm("orderNo", value)} />
        <TextInput label="Planlanan Üretim Miktarı (Çift)" value={form.plannedPairs} readonly={readonly} type="number" onChange={(value) => updateForm("plannedPairs", value)} />
        <SelectInput label="Öncelik" value={form.priority} readonly={readonly} options={PRIORITIES} onChange={(value) => updateForm("priority", value)} />
        <TextInput label="Başlangıç Tarihi" value={form.startDate} readonly={readonly} type="date" onChange={(value) => updateForm("startDate", value)} />
        <TextInput label="Termin Tarihi" value={form.dueDate} readonly={readonly} type="date" onChange={(value) => updateForm("dueDate", value)} />
        <SelectInput label="Durum" value={form.status} readonly={readonly} options={WORK_ORDER_STATUSES} onChange={(value) => updateForm("status", value)} />
      </div>
    </TabPanel>
  );
}

function ProductInfoTab({ product, details }: { product: Product | undefined; details: ProductDetails }) {
  return (
    <TabPanel title="Ürün Bilgileri" note="Readonly. Product Master'dan otomatik gelir.">
      <div className="grid gap-4 lg:grid-cols-2">
        <ImagePreview title="Ürün resmi" name={details.productImageName || ""} dataUrl={details.productImageDataUrl || ""} />
        <ImagePreview title="Kalıp resmi" name={details.moldImageName || ""} dataUrl={details.moldImageDataUrl || ""} />
      </div>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <ReadOnlyInfo label="Model" value={details.model || product?.modelCode || "-"} />
        <ReadOnlyInfo label="Kategori" value={product?.category || "-"} />
        <ReadOnlyInfo label="Foam" value={product?.foamType || "-"} />
        <ReadOnlyInfo label="Numara" value={details.number || "-"} />
        <ReadOnlyInfo label="Renk" value={details.color || "-"} />
        <ReadOnlyInfo label="Üretim Tipi" value={details.productionType || "-"} />
        <ReadOnlyInfo label="Gramaj" value={formatNumber(product?.averageWeight)} />
        <ReadOnlyInfo label="Yoğunluk" value={formatNumber(product?.targetDensity)} />
      </div>
    </TabPanel>
  );
}

function ProductionPlanTab({ form, readonly, updateForm }: WorkOrderTabProps) {
  return (
    <TabPanel title="Üretim Planı" note="Makine, kalıp, operatör, vardiya ve zaman planı.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Makine" value={form.machine} readonly={readonly} onChange={(value) => updateForm("machine", value)} />
        <TextInput label="Kalıp" value={form.mold} readonly={readonly} onChange={(value) => updateForm("mold", value)} />
        <TextInput label="Operatör" value={form.operatorName} readonly={readonly} onChange={(value) => updateForm("operatorName", value)} />
        <SelectInput label="Vardiya" value={form.shift} readonly={readonly} options={["1", "2", "3"]} onChange={(value) => updateForm("shift", value)} />
        <TextInput label="Planlanan Başlangıç" value={form.plannedStart} readonly={readonly} type="datetime-local" onChange={(value) => updateForm("plannedStart", value)} />
        <TextInput label="Planlanan Bitiş" value={form.plannedEnd} readonly={readonly} type="datetime-local" onChange={(value) => updateForm("plannedEnd", value)} />
        <TextInput label="Tahmini Üretim Süresi" value={form.estimatedProductionTime} readonly={readonly} onChange={(value) => updateForm("estimatedProductionTime", value)} />
        <TextInput label="Tahmini Günlük Kapasite" value={form.estimatedDailyCapacity} readonly={readonly} type="number" onChange={(value) => updateForm("estimatedDailyCapacity", value)} />
        <TextInput label="Tahmini Fire" value={form.estimatedWasteRate} readonly={readonly} type="number" onChange={(value) => updateForm("estimatedWasteRate", value)} />
      </div>
      <TextAreaInput label="Operasyon Notu" value={form.operationNote} readonly={readonly} onChange={(value) => updateForm("operationNote", value)} />
    </TabPanel>
  );
}

function MaterialsTab({
  form,
  materials,
  stocks,
  readonly,
  updateMaterialLine,
  totals,
}: {
  form: WorkOrder;
  materials: Material[];
  stocks: StockItem[];
  readonly: boolean;
  updateMaterialLine: (key: string, field: keyof WorkOrderMaterialLine, value: string) => void;
  totals: MaterialTotals;
}) {
  const plannedPairs = safeParsedNumber(form.plannedPairs);

  return (
    <TabPanel title="Kullanılacak Hammaddeler" note="Recipe/BOM mantığıyla Material Master'dan otomatik seçilir, stok yeterliliği kontrol edilir.">
      <div className="overflow-x-auto rounded-xl border border-white/10 bg-black/20">
        <table className="min-w-[1120px] w-full text-left text-sm">
          <thead>
            <tr className="border-b border-white/10 text-xs uppercase tracking-[0.16em] text-zinc-500">
              <th className="p-3">Malzeme</th>
              <th className="p-3">Kod</th>
              <th className="p-3">Tip</th>
              <th className="p-3">Birim</th>
              <th className="p-3">Birim Fiyat</th>
              <th className="p-3">1 Çift Kullanım</th>
              <th className="p-3">Toplam Kullanım</th>
              <th className="p-3">Stok Durumu</th>
              <th className="p-3">Eksik</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/10">
            {form.materialLines.map((line) => {
              const material = materials.find((item) => item.id === line.materialId);
              const stock = findStockForMaterial(material, stocks);
              const totalUsage = safeParsedNumber(line.usagePerPair) * plannedPairs;
              const missing = Math.max(totalUsage - safeNumber(stock?.currentQuantity), 0);
              const status = getStockStatus(stock, totalUsage);

              return (
                <tr key={line.key}>
                  <td className="p-3">
                    <select value={line.materialId} disabled={readonly} onChange={(event) => updateMaterialLine(line.key, "materialId", event.target.value)} className={CONTROL_CLASS}>
                      <option value="">{line.role} seç</option>
                      {materials.map((item) => (
                        <option key={item.id} value={item.id}>{formatMaterialOption(item)}</option>
                      ))}
                    </select>
                    {line.role === "Crosskim" && <p className="mt-2 text-xs font-bold text-amber-200">Crosskim 180 kg Poliol kazanına ilave edilen katkıdır.</p>}
                  </td>
                  <td className="p-3 font-mono text-xs text-emerald-200">{material?.code || "-"}</td>
                  <td className="p-3">{material?.materialType || "-"}</td>
                  <td className="p-3">{material?.unit || "-"}</td>
                  <td className="p-3">{formatMoney(safeNumber(material?.lastPurchasePrice), material?.currency || "TRY")}</td>
                  <td className="p-3"><input value={line.usagePerPair} type="number" step="0.01" disabled={readonly} onChange={(event) => updateMaterialLine(line.key, "usagePerPair", event.target.value)} className={CONTROL_CLASS} /></td>
                  <td className="p-3">{formatNumber(totalUsage)}</td>
                  <td className="p-3"><StockStatusBadge status={status} /></td>
                  <td className="p-3">{missing > 0 ? formatNumber(missing) : "-"}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <MetricCard label="Toplam Hammadde Maliyeti" value={formatMoney(totals.totalCost, "TRY")} tone="emerald" />
        <MetricCard label="Toplam Hammadde Gramajı" value={`${formatNumber(totals.totalGram)} gr`} />
        <MetricCard label="Eksik / Kritik" value={`${totals.missingCount} eksik / ${totals.criticalCount} kritik`} tone={totals.missingCount > 0 ? "red" : "cyan"} />
      </div>
    </TabPanel>
  );
}

function OperationsTab({ form, readonly, updateOperation }: { form: WorkOrder; readonly: boolean; updateOperation: (key: string, field: keyof OperationLine, value: string) => void }) {
  return (
    <TabPanel title="Operasyonlar" note="Şimdilik frontend state. İleride canlı üretim modülüne bağlanacak.">
      <div className="space-y-3">
        {form.operations.map((operation) => (
          <div key={operation.key} className="grid gap-3 rounded-xl border border-white/10 bg-black/20 p-4 md:grid-cols-2 xl:grid-cols-[1.1fr_1fr_1fr_1fr_1fr_1.4fr]">
            <ReadOnlyInfo label="Operasyon" value={operation.operation} />
            <SelectInput label="Durum" value={operation.status} readonly={readonly} options={["Bekliyor", "Hazır", "Başladı", "Tamamlandı"]} onChange={(value) => updateOperation(operation.key, "status", value)} />
            <TextInput label="Sorumlu" value={operation.responsible} readonly={readonly} onChange={(value) => updateOperation(operation.key, "responsible", value)} />
            <TextInput label="Tahmini Süre" value={operation.estimatedTime} readonly={readonly} onChange={(value) => updateOperation(operation.key, "estimatedTime", value)} />
            <TextInput label="Gerçek Süre" value={operation.actualTime} readonly={readonly} onChange={(value) => updateOperation(operation.key, "actualTime", value)} />
            <TextInput label="Not" value={operation.note} readonly={readonly} onChange={(value) => updateOperation(operation.key, "note", value)} />
          </div>
        ))}
      </div>
    </TabPanel>
  );
}

function QualityTab({ details, product }: { details: ProductDetails; product: Product | undefined }) {
  return (
    <TabPanel title="Kalite" note="Readonly. Product Master kalite toleranslarından otomatik gelir.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <ReadOnlyInfo label="Minimum Gramaj" value={details.minWeight || "-"} />
        <ReadOnlyInfo label="Maksimum Gramaj" value={details.maxWeight || "-"} />
        <ReadOnlyInfo label="Minimum Yoğunluk" value={details.minDensity || "-"} />
        <ReadOnlyInfo label="Maksimum Yoğunluk" value={details.maxDensity || "-"} />
        <ReadOnlyInfo label="Kabul Edilen Fire" value={details.acceptedWasteRate || "-"} />
      </div>
      <ReadOnlyInfo label="Kalite Notu" value={details.qualityNote || product?.description || "-"} />
    </TabPanel>
  );
}

function NotesTab({ form, readonly, updateForm }: WorkOrderTabProps) {
  return (
    <TabPanel title="Notlar" note="İş emrine özel üretim, müşteri, kalite ve genel notlar.">
      <div className="grid gap-4 lg:grid-cols-2">
        <TextAreaInput label="Üretim Notu" value={form.productionNote} readonly={readonly} onChange={(value) => updateForm("productionNote", value)} />
        <TextAreaInput label="Müşteri Notu" value={form.customerNote} readonly={readonly} onChange={(value) => updateForm("customerNote", value)} />
        <TextAreaInput label="Kalite Notu" value={form.qualityNote} readonly={readonly} onChange={(value) => updateForm("qualityNote", value)} />
        <TextAreaInput label="Genel Not" value={form.generalNote} readonly={readonly} onChange={(value) => updateForm("generalNote", value)} />
      </div>
    </TabPanel>
  );
}

type WorkOrderTabProps = {
  form: WorkOrder;
  readonly: boolean;
  updateForm: <K extends keyof WorkOrder>(key: K, value: WorkOrder[K]) => void;
};

function TabPanel({ title, note, children }: { title: string; note: string; children: ReactNode }) {
  return (
    <section className="space-y-5">
      <div>
        <h3 className="text-xl font-black text-white">{title}</h3>
        <p className="mt-1 text-sm text-zinc-400">{note}</p>
      </div>
      {children}
    </section>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="block">
      <span className="mb-2 block text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{label}</span>
      {children}
    </label>
  );
}

function TextInput({ label, value, readonly, onChange, type = "text" }: { label: string; value: string; readonly: boolean; onChange: (value: string) => void; type?: string }) {
  return (
    <Field label={label}>
      <input value={value} type={type} step={type === "number" ? "0.01" : undefined} disabled={readonly} readOnly={readonly} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS} />
    </Field>
  );
}

function TextAreaInput({ label, value, readonly, onChange }: { label: string; value: string; readonly: boolean; onChange: (value: string) => void }) {
  return (
    <Field label={label}>
      <textarea value={value} disabled={readonly} readOnly={readonly} rows={4} onChange={(event) => onChange(event.target.value)} className={`${CONTROL_CLASS} min-h-28 resize-y`} />
    </Field>
  );
}

function SelectInput({ label, value, readonly, options, onChange }: { label: string; value: string; readonly: boolean; options: string[]; onChange: (value: string) => void }) {
  return (
    <Field label={label}>
      <select value={value} disabled={readonly} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS}>
        {options.map((option) => <option key={option}>{option}</option>)}
      </select>
    </Field>
  );
}

function ReadOnlyInfo({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs font-black uppercase tracking-[0.16em] text-zinc-500">{label}</p>
      <p className="mt-2 break-words text-sm font-black text-white">{value}</p>
    </div>
  );
}

function ImagePreview({ title, name, dataUrl }: { title: string; name: string; dataUrl: string }) {
  return (
    <div className="overflow-hidden rounded-xl border border-white/10 bg-black/30">
      <div className="relative flex aspect-[4/3] items-center justify-center bg-white/[0.04]">
        {dataUrl ? <Image src={dataUrl} alt={title} fill sizes="(max-width: 1024px) 100vw, 50vw" unoptimized className="object-cover" /> : <p className="text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{title}</p>}
      </div>
      <div className="border-t border-white/10 px-3 py-2 text-xs font-bold text-zinc-300">{name || "Önizleme yok"}</div>
    </div>
  );
}

function DashboardCard({ title, value, note, tone }: { title: string; value: string; note: string; tone: DashboardTone }) {
  const toneClass = {
    emerald: "border-emerald-400/25 bg-emerald-500/10 text-emerald-200",
    cyan: "border-cyan-400/25 bg-cyan-500/10 text-cyan-200",
    amber: "border-amber-400/25 bg-amber-500/10 text-amber-200",
    red: "border-red-400/25 bg-red-500/10 text-red-200",
    blue: "border-blue-400/25 bg-blue-500/10 text-blue-200",
    violet: "border-violet-400/25 bg-violet-500/10 text-violet-200",
    zinc: "border-zinc-400/25 bg-zinc-500/10 text-zinc-200",
  }[tone];
  return (
    <article className={`rounded-2xl border p-5 shadow-xl ${toneClass}`}>
      <p className="text-xs font-black uppercase tracking-[0.18em] opacity-80">{title}</p>
      <p className="mt-3 text-2xl font-black text-white">{value}</p>
      <p className="mt-2 text-sm opacity-80">{note}</p>
    </article>
  );
}

function MetricCard({ label, value, tone = "zinc" }: { label: string; value: string; tone?: "emerald" | "cyan" | "red" | "zinc" }) {
  const toneClass = tone === "emerald" ? "border-emerald-400/30 bg-emerald-500/10 text-emerald-200" : tone === "cyan" ? "border-cyan-400/30 bg-cyan-500/10 text-cyan-200" : tone === "red" ? "border-red-400/30 bg-red-500/10 text-red-200" : "border-white/10 bg-black/25 text-zinc-300";
  return (
    <div className={`rounded-xl border p-4 ${toneClass}`}>
      <p className="text-xs font-black uppercase tracking-[0.18em] opacity-80">{label}</p>
      <p className="mt-2 text-2xl font-black text-white">{value}</p>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const className =
    status === "Tamamlandı" ? "bg-emerald-500/15 text-emerald-200" :
    status === "Üretimde" ? "bg-amber-500/15 text-amber-200" :
    status === "İptal" ? "bg-red-500/15 text-red-200" :
    status === "Hazır" ? "bg-cyan-500/15 text-cyan-200" :
    "bg-zinc-500/15 text-zinc-200";
  return <span className={`rounded-full px-3 py-1 text-xs font-black ${className}`}>{status}</span>;
}

function StockStatusBadge({ status }: { status: string }) {
  const className = status === "Yeterli" ? "bg-emerald-500/15 text-emerald-200" : status === "Kritik" ? "bg-amber-500/15 text-amber-200" : "bg-red-500/15 text-red-200";
  return <span className={`rounded-full px-3 py-1 text-xs font-black ${className}`}>{status}</span>;
}

function LoadingState() {
  return <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-sm font-bold text-zinc-400">Yükleniyor...</div>;
}

function createEmptyWorkOrder(): WorkOrder {
  return {
    id: `wo-${Date.now()}-${Math.random().toString(36).slice(2)}`,
    workOrderNo: `WO-${new Date().getFullYear()}-${String(Date.now()).slice(-5)}`,
    productId: "",
    version: "V1",
    revisionNo: "R0",
    customerName: "",
    orderNo: "",
    plannedPairs: "",
    producedPairs: "0",
    priority: "Normal",
    startDate: formatDateInput(new Date()),
    dueDate: "",
    status: "Taslak",
    machine: "",
    mold: "",
    operatorName: "",
    shift: "1",
    plannedStart: "",
    plannedEnd: "",
    estimatedProductionTime: "",
    estimatedDailyCapacity: "",
    estimatedWasteRate: "",
    operationNote: "",
    materialLines: [],
    operations: OPERATION_NAMES.map((operation, index) => ({
      key: `op-${index + 1}`,
      operation,
      status: "Bekliyor",
      responsible: "",
      estimatedTime: "",
      actualTime: "",
      note: "",
    })),
    productionNote: "",
    customerNote: "",
    qualityNote: "",
    generalNote: "",
    updatedAt: new Date().toISOString(),
  };
}

function cloneWorkOrder(order: WorkOrder): WorkOrder {
  return {
    ...order,
    materialLines: order.materialLines.map((line) => ({ ...line })),
    operations: order.operations.map((line) => ({ ...line })),
  };
}

function createMaterialLinesFromMasters(materials: Material[], product: Product | undefined): WorkOrderMaterialLine[] {
  return RECIPE_ROLES.map((role, index) => {
    const matched = findMaterialForRole(role, materials, product);
    return {
      key: `${role}-${index}`,
      role,
      materialId: matched?.id || "",
      usagePerPair: "",
      wasteRate: "0",
      note: "",
    };
  });
}

function findMaterialForRole(role: string, materials: Material[], product: Product | undefined) {
  const normalizedRole = normalizeText(role);
  return materials.find((material) => {
    const type = normalizeText(material.materialType);
    const text = normalizeText(`${material.code || ""} ${material.name || ""}`);
    if (normalizedRole === "kumaş") return type === "fabric";
    if (normalizedRole === "yapışkan") return type === "adhesive";
    if (normalizedRole === "dtf") return product?.hasDTFLabel ? type === "dtf" : false;
    if (normalizedRole === "işçilik") return false;
    return text.includes(normalizedRole);
  });
}

function calculateMaterialTotals(order: WorkOrder, materials: Material[], stocks: StockItem[]): MaterialTotals {
  const plannedPairs = safeParsedNumber(order.plannedPairs);
  return order.materialLines.reduce(
    (totals, line) => {
      const material = materials.find((item) => item.id === line.materialId);
      const stock = findStockForMaterial(material, stocks);
      const totalUsage = safeParsedNumber(line.usagePerPair) * plannedPairs;
      const lineCost = totalUsage * safeNumber(material?.lastPurchasePrice);
      const status = getStockStatus(stock, totalUsage);
      const isGram = normalizeText(material?.unit) === "gr";
      return {
        totalCost: totals.totalCost + lineCost,
        totalGram: totals.totalGram + (isGram ? totalUsage : 0),
        missingCount: totals.missingCount + (status === "Yetersiz" ? 1 : 0),
        criticalCount: totals.criticalCount + (status === "Kritik" ? 1 : 0),
      };
    },
    { totalCost: 0, totalGram: 0, missingCount: 0, criticalCount: 0 }
  );
}

function buildChecks(order: WorkOrder, product: Product | undefined, materials: Material[], stocks: StockItem[]) {
  const hasRecipe = order.materialLines.some((line) => line.materialId);
  const hasMaterials = order.materialLines.every((line) => !line.usagePerPair || line.materialId);
  const totals = calculateMaterialTotals(order, materials, stocks);
  const hasCost = totals.totalCost > 0;

  return [
    { label: "Recipe var mı?", status: hasRecipe ? "Reçete satırı hazır" : "Reçete bekleniyor", tone: hasRecipe ? "emerald" as const : "amber" as const },
    { label: "Product seçimi", status: product ? "Product Master bağlı" : "Ürün seçilmedi", tone: product ? "emerald" as const : "red" as const },
    { label: "Material var mı?", status: hasMaterials ? "Material seçimleri uygun" : "Eksik Material var", tone: hasMaterials ? "emerald" as const : "red" as const },
    { label: "Stok yeterli mi?", status: totals.missingCount > 0 ? "Yetersiz stok var" : totals.criticalCount > 0 ? "Kritik stok var" : "Stok yeterli", tone: totals.missingCount > 0 ? "red" as const : totals.criticalCount > 0 ? "amber" as const : "emerald" as const },
    { label: "Maliyet", status: hasCost ? "Tahmini maliyet var" : "Maliyet hesaplanamıyor", tone: hasCost ? "emerald" as const : "amber" as const },
  ];
}

function getStockStatus(stock: StockItem | undefined, requiredQuantity: number) {
  if (!stock || requiredQuantity > safeNumber(stock.currentQuantity)) return "Yetersiz";
  if (safeNumber(stock.currentQuantity) <= safeNumber(stock.criticalQuantity)) return "Kritik";
  return "Yeterli";
}

function findStockForMaterial(material: Material | undefined, stocks: StockItem[]) {
  if (!material) return undefined;
  return stocks.find((stock) => (stock.materialId && stock.materialId === material.id) || normalizeText(stock.materialCode || stock.code) === normalizeText(material.code));
}

function parseProductDetails(description?: string | null): ProductDetails {
  const raw = description || "";
  const markerIndex = raw.indexOf(PRODUCT_MARKER);
  if (markerIndex === -1) return {};
  try {
    return JSON.parse(raw.slice(markerIndex + PRODUCT_MARKER.length)) as ProductDetails;
  } catch {
    return {};
  }
}

function extractProducts(result: unknown): Product[] {
  if (Array.isArray(result)) return result.filter(isProduct);
  if (isRecord(result) && Array.isArray((result as ApiResponse<unknown[]>).data)) return (result as ApiResponse<unknown[]>).data!.filter(isProduct);
  return [];
}

function extractMaterials(result: unknown): Material[] {
  if (Array.isArray(result)) return result.filter(isMaterial);
  if (isRecord(result) && Array.isArray((result as ApiResponse<unknown[]>).data)) return (result as ApiResponse<unknown[]>).data!.filter(isMaterial);
  return [];
}

function extractStocks(result: unknown): StockItem[] {
  if (Array.isArray(result)) return result.filter(isStockItem);
  if (isRecord(result) && Array.isArray((result as ApiResponse<unknown[]>).data)) return (result as ApiResponse<unknown[]>).data!.filter(isStockItem);
  return [];
}

function isProduct(value: unknown): value is Product {
  return isRecord(value) && typeof value.id === "string";
}

function isMaterial(value: unknown): value is Material {
  return isRecord(value) && typeof value.id === "string";
}

function isStockItem(value: unknown): value is StockItem {
  return isRecord(value) && typeof value.id === "string";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function countByStatus(orders: WorkOrder[], status: string) {
  return orders.filter((order) => order.status === status).length.toLocaleString("tr-TR");
}

function formatMaterialOption(material: Material) {
  return `${[material.code, material.name].filter(Boolean).join(" - ") || material.id} | ${material.materialType || "-"} | ${material.unit || "-"}`;
}

function safeNumber(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function safeParsedNumber(value: string) {
  if (!value.trim()) return 0;
  const parsed = Number(value.replace(",", "."));
  return Number.isFinite(parsed) ? parsed : 0;
}

function normalizeText(value: string | null | undefined) {
  return String(value || "").trim().toLocaleLowerCase("tr-TR");
}

function formatNumber(value?: number | null) {
  if (typeof value !== "number" || !Number.isFinite(value)) return "-";
  return value.toLocaleString("tr-TR", { maximumFractionDigits: 2 });
}

function formatMoney(value: number, currency: string) {
  return value.toLocaleString("tr-TR", { style: "currency", currency: currency || "TRY", maximumFractionDigits: 2 });
}

function formatDateInput(value: Date) {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, "0");
  const day = String(value.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatDate(value: string) {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleDateString("tr-TR");
}
