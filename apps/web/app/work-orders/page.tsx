"use client";

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

type Recipe = {
  id: string;
  code?: string | null;
  name?: string | null;
  productId?: string | null;
  foamType?: string | null;
  version?: string | null;
  revision?: string | null;
  totalGram?: number | null;
  totalCost?: number | null;
  isActive?: boolean | null;
};

type Machine = {
  id: string;
  code?: string | null;
  name?: string | null;
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

type OrderItemLookup = {
  id: string;
  productId?: string | null;
  moldId?: string | null;
  productName?: string | null;
  moldName?: string | null;
  quantityPairs?: number | null;
  producedPairs?: number | null;
  remainingPairs?: number | null;
  productionType?: string | null;
  fabricColor?: string | null;
};

type OrderLookup = {
  id: string;
  orderNumber?: string | null;
  dueDate?: string | null;
  customerName?: string | null;
  productName?: string | null;
  quantity?: number | null;
  remainingQuantity?: number | null;
  items?: OrderItemLookup[];
};

type ProductDetails = {
  model?: string;
  number?: string;
  color?: string;
  productionType?: string;
  fabricType?: string;
  adhesiveType?: string;
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

type WorkOrderRequirementLine = {
  materialId: string;
  materialCode?: string | null;
  materialName?: string | null;
  recipeQuantity?: number | null;
  recipeUnit?: string | null;
  wastePercent?: number | null;
  netRequiredQuantity?: number | null;
  wasteQuantity?: number | null;
  totalRequiredQuantity?: number | null;
  stockItemId?: string | null;
  stockCode?: string | null;
  availableStock?: number | null;
  reservedQuantity?: number | null;
  freeStock?: number | null;
  stockUnit?: string | null;
  conversionApplied?: boolean | null;
  conversionFactor?: number | null;
  shortageQuantity?: number | null;
  coveragePercent?: number | null;
  isSufficient?: boolean | null;
  materialUnitPrice?: number | null;
  currency?: string | null;
  estimatedMaterialCost?: number | null;
  warning?: string | null;
  isUnitMismatch?: boolean | null;
};

type RequirementPayload = {
  materialCount?: number | null;
  sufficientMaterialCount?: number | null;
  shortageMaterialCount?: number | null;
  hasShortage?: boolean | null;
  canStartProduction?: boolean | null;
  items?: WorkOrderRequirementLine[];
  totalsByCurrency?: Record<string, number>;
  warnings?: string[];
};

type WorkOrderQualitySummary = {
  inspectionCount?: number | null;
  passed?: number | null;
  failed?: number | null;
  totalChecked?: number | null;
  totalRejected?: number | null;
  defectRate?: number | null;
  latestResult?: string | null;
  canCloseQuality?: boolean | null;
};

type WorkOrderOperationSummary = {
  cutPairs: number;
  boxedPairs: number;
  warehousePairs: number;
  readyPairs: number;
  shippedPairs: number;
};

type WorkOrder = {
  id: string;
  workOrderNumber: string;
  orderNumber: string;
  orderItemId: string;
  customerId?: string | null;
  customerName?: string | null;
  productId: string;
  productCode?: string | null;
  productName?: string | null;
  recipeId?: string | null;
  recipeCode?: string | null;
  recipeName?: string | null;
  plannedPairs: number;
  assignedPairs: number;
  producedPairs: number;
  goodPairs: number;
  firePairs: number;
  remainingPairs: number;
  progressPercent: number;
  priority: string;
  status: string;
  plannedStartDate?: string | null;
  plannedEndDate?: string | null;
  actualStartDate?: string | null;
  actualEndDate?: string | null;
  assignedMachineId?: string | null;
  assignedMachineCode?: string | null;
  shift?: number | null;
  isActive?: boolean | null;
  isCancelled?: boolean | null;
  notes?: string | null;
  cancellationReason?: string | null;
  requirements?: RequirementPayload | null;
  hasRecipe?: boolean | null;
  hasMaterialShortage?: boolean | null;
  shortageMaterialCount?: number | null;
  materialCoveragePercent?: number | null;
  estimatedMaterialCostByCurrency?: Record<string, number>;
  canStartProduction?: boolean | null;
  materialWarnings?: string[];
  stationAssignments?: unknown[];
  updatedAt?: string | null;
};

type WorkOrderForm = {
  id: string;
  orderItemId: string;
  recipeId: string;
  plannedPairs: string;
  priority: string;
  plannedStartDate: string;
  plannedEndDate: string;
  assignedMachineId: string;
  shift: string;
  notes: string;
};

type ApiResponse<T> = {
  data?: T;
  message?: string;
  errorMessage?: string;
  errorCode?: string;
  success?: boolean;
};

const API = "http://localhost:5000/api/v1";
const PRODUCT_MARKER = "\n\n---FIXAR_PRODUCT_MASTER_JSON---\n";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60 disabled:cursor-not-allowed disabled:opacity-70";
const TABS: Array<{ id: WorkOrderTab; label: string }> = [
  { id: "general", label: "1 Genel" },
  { id: "product", label: "2 Ürün Bilgileri" },
  { id: "plan", label: "3 Üretim Planı" },
  { id: "materials", label: "4 Kullanılacak Hammaddeler" },
  { id: "operations", label: "5 Operasyonlar" },
  { id: "quality", label: "6 Kalite" },
  { id: "notes", label: "7 Notlar" },
];
const PRIORITY_OPTIONS = [
  { value: "Normal", label: "Normal" },
  { value: "High", label: "Yüksek" },
  { value: "Urgent", label: "Acil" },
];

export default function WorkOrdersPage() {
  const [workOrders, setWorkOrders] = useState<WorkOrder[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [materials, setMaterials] = useState<Material[]>([]);
  const [stocks, setStocks] = useState<StockItem[]>([]);
  const [machines, setMachines] = useState<Machine[]>([]);
  const [orders, setOrders] = useState<OrderLookup[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [dialogMode, setDialogMode] = useState<DialogMode>(null);
  const [dialogWorkOrder, setDialogWorkOrder] = useState<WorkOrder | null>(null);

  useEffect(() => {
    void loadData();
  }, []);

  async function loadData() {
    setLoading(true);
    setError(null);

    try {
      const [workOrdersData, productsData, recipesData, materialsData, stocksData, machinesData, ordersData] = await Promise.all([
        apiGet<unknown>("/work-orders"),
        apiGet<unknown>("/products"),
        apiGet<unknown>("/recipes?includeItems=true"),
        apiGet<unknown>("/materials"),
        apiGet<unknown>("/stocks"),
        apiGet<unknown>("/machines"),
        apiGet<unknown>("/production-planning/lookups/orders"),
      ]);

      setWorkOrders(extractArray(workOrdersData).map(mapWorkOrder).filter(Boolean) as WorkOrder[]);
      setProducts(extractArray(productsData).filter(isProduct).filter((item) => item.isActive !== false));
      setRecipes(extractArray(recipesData).filter(isRecipe).filter((item) => item.isActive !== false));
      setMaterials(extractArray(materialsData).filter(isMaterial).filter((item) => item.isActive !== false));
      setStocks(extractArray(stocksData).filter(isStockItem));
      setMachines(extractArray(machinesData).filter(isMachine).filter((item) => item.isActive !== false));
      setOrders(extractArray(ordersData).filter(isOrderLookup));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  async function openDialog(mode: DialogMode, workOrder: WorkOrder | null = null) {
    setSuccessMessage(null);
    setDialogMode(mode);
    setDialogWorkOrder(workOrder);

    if (workOrder?.id && mode !== "create") {
      try {
        const detail = await apiGet<unknown>("/work-orders/" + workOrder.id);
        setDialogWorkOrder(mapDetailWorkOrder(detail) ?? workOrder);
      } catch (err) {
        setError(err instanceof Error ? err.message : "İş emri detayı alınamadı.");
      }
    }
  }

  function closeDialog() {
    setDialogMode(null);
    setDialogWorkOrder(null);
  }

  async function handleSaved(message: string) {
    await loadData();
    closeDialog();
    setSuccessMessage(message);
  }

  async function transitionWorkOrder(workOrder: WorkOrder) {
    try {
      const path = getNextTransitionPath(workOrder.status);
      if (!path) {
        alert("Bu iş emri için üretim başlatma geçişi uygun değil.");
        return;
      }

      let body: Record<string, unknown> = {};
      if (path === "/start") {
        const requirements = await apiGet<RequirementPayload>("/work-orders/" + workOrder.id + "/requirements");
        const blockingLines = (requirements.items ?? []).filter((line) =>
          line.isUnitMismatch || !line.stockItemId || safeNumber(line.shortageQuantity) > 0
        );

        if (blockingLines.length > 0) {
          const summary = blockingLines
            .map((line) => `${line.materialCode || "-"} ${line.materialName || ""}: ${getRequirementStatus(line)} (${formatNumber(line.shortageQuantity)} ${line.stockUnit || ""})`)
            .join("\n");
          const reason = window.prompt(
            "Bu iş emrinde hammadde uyarıları var.\n\n" +
              summary +
              "\n\nEksik stokla başlatmak için gerekçe yazın. Vazgeçmek için boş bırakın."
          );

          if (!reason?.trim()) {
            alert("Eksik stok override gerekçesi zorunludur.");
            return;
          }

          body = { allowMaterialShortage: true, shortageReason: reason.trim() };
        }
      }

      await apiPost<unknown>("/work-orders/" + workOrder.id + path, body);
      await loadData();
      setSuccessMessage(path === "/start" ? "İş emri üretimde durumuna alındı." : "İş emri durumu güncellendi.");
    } catch (err) {
      alert(err instanceof Error ? err.message : "İş emri durumu güncellenemedi.");
    }
  }

  const orderItems = useMemo(() => flattenOrderItems(orders), [orders]);
  const filteredWorkOrders = useMemo(() => {
    const normalizedSearch = normalizeText(search);
    if (!normalizedSearch) return workOrders;

    return workOrders.filter((order) =>
      [
        order.workOrderNumber,
        order.orderNumber,
        order.customerName,
        order.productCode,
        order.productName,
        translateStatus(order.status),
        translatePriority(order.priority),
      ]
        .filter(Boolean)
        .some((value) => normalizeText(String(value)).includes(normalizedSearch))
    );
  }, [search, workOrders]);

  const today = formatDateInput(new Date());
  const dashboardCards = [
    { title: "Toplam İş Emri", value: workOrders.length.toLocaleString("tr-TR"), note: "Veritabanındaki kayıt", tone: "emerald" as DashboardTone },
    { title: "Bekleyen", value: countByStatus(workOrders, "Draft"), note: "Taslak iş emri", tone: "zinc" as DashboardTone },
    { title: "Hazırlanıyor", value: countByStatuses(workOrders, ["Planned", "Ready"]), note: "Planlandı / hazır", tone: "cyan" as DashboardTone },
    { title: "Üretimde", value: countByStatuses(workOrders, ["InProduction", "Paused"]), note: "Atamaya açık üretim", tone: "amber" as DashboardTone },
    { title: "Tamamlanan", value: countByStatus(workOrders, "Completed"), note: "Kapanan emir", tone: "blue" as DashboardTone },
    { title: "İptal", value: countByStatus(workOrders, "Cancelled"), note: "İptal edilen", tone: "red" as DashboardTone },
    { title: "Bugünkü Üretim", value: workOrders.filter((order) => dateOnly(order.actualStartDate) === today).length.toLocaleString("tr-TR"), note: "Bugün başlayan", tone: "violet" as DashboardTone },
    { title: "Toplam Planlanan Çift", value: formatNumber(workOrders.reduce((sum, order) => sum + order.plannedPairs, 0)), note: "WorkOrder planı", tone: "emerald" as DashboardTone },
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
                Sipariş kalemi, Product Master ve Recipe/BOM üzerinden veritabanına kaydedilen üretim iş emirleri.
              </p>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <button
                onClick={() => void loadData()}
                disabled={loading}
                className="rounded-xl border border-white/10 bg-white/[0.08] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:opacity-50"
              >
                {loading ? "Yenileniyor..." : "Verileri Yenile"}
              </button>
              <button onClick={() => void openDialog("create")} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
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
                  <input value={search} onChange={(event) => setSearch(event.target.value)} className={CONTROL_CLASS} placeholder="İş emri, sipariş, ürün, müşteri, durum" />
                </Field>
              </div>
            </div>

            {loading && <LoadingState />}

            {!loading && error && (
              <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-5 text-sm text-red-100">
                <p className="font-black">Veriler yüklenemedi.</p>
                <p className="mt-1 text-red-200">{error}</p>
              </div>
            )}

            {!loading && !error && filteredWorkOrders.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-zinc-300">
                Henüz iş emri yok. Sipariş kalemi seçerek yeni iş emri oluşturun.
              </div>
            )}

            {!loading && !error && filteredWorkOrders.length > 0 && (
              <div className="mt-5 overflow-x-auto">
                <table className="min-w-[1320px] w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-white/10 text-xs uppercase tracking-[0.18em] text-zinc-500">
                      <th className="py-3 pr-4">İş Emri No</th>
                      <th className="py-3 pr-4">Ürün</th>
                      <th className="py-3 pr-4">Müşteri</th>
                      <th className="py-3 pr-4">Numara</th>
                      <th className="py-3 pr-4">Foam</th>
                      <th className="py-3 pr-4">Planlanan Çift</th>
                      <th className="py-3 pr-4">Üretilen</th>
                      <th className="py-3 pr-4">Fire</th>
                      <th className="py-3 pr-4">Kalan</th>
                      <th className="py-3 pr-4">Başlangıç</th>
                      <th className="py-3 pr-4">Termin</th>
                      <th className="py-3 pr-4">Durum</th>
                      <th className="py-3 pr-4">Malzeme</th>
                      <th className="py-3 pr-4">Öncelik</th>
                      <th className="py-3 text-right">İşlemler</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/10">
                    {filteredWorkOrders.map((order) => {
                      const product = products.find((item) => item.id === order.productId);
                      const details = parseProductDetails(product?.description);
                      return (
                        <tr key={order.id} className="align-middle text-zinc-200 transition hover:bg-white/[0.04]">
                          <td className="py-4 pr-4 font-mono text-xs text-emerald-200">{order.workOrderNumber}</td>
                          <td className="py-4 pr-4 font-black text-white">{order.productName || product?.name || "-"}</td>
                          <td className="py-4 pr-4">{order.customerName || product?.customerName || "-"}</td>
                          <td className="py-4 pr-4">{details.number || "-"}</td>
                          <td className="py-4 pr-4">{product?.foamType || "-"}</td>
                          <td className="py-4 pr-4">{formatNumber(order.plannedPairs)}</td>
                          <td className="py-4 pr-4">{formatNumber(order.producedPairs)}</td>
                          <td className="py-4 pr-4">{formatNumber(order.firePairs)}</td>
                          <td className="py-4 pr-4">{formatNumber(order.remainingPairs)}</td>
                          <td className="py-4 pr-4">{formatDate(order.plannedStartDate)}</td>
                          <td className="py-4 pr-4">{formatDate(order.plannedEndDate)}</td>
                          <td className="py-4 pr-4"><StatusBadge status={order.status} /></td>
                          <td className="py-4 pr-4"><MaterialReadinessBadge order={order} /></td>
                          <td className="py-4 pr-4">{translatePriority(order.priority)}</td>
                          <td className="py-4">
                            <div className="flex justify-end gap-2">
                              <button onClick={() => void openDialog("detail", order)} className="rounded-lg border border-cyan-400/30 bg-cyan-400/10 px-3 py-2 text-xs font-black text-cyan-100 transition hover:bg-cyan-400/20">Detay</button>
                              <button onClick={() => void openDialog("edit", order)} disabled={isLocked(order)} className="rounded-lg border border-emerald-400/30 bg-emerald-400/10 px-3 py-2 text-xs font-black text-emerald-100 transition hover:bg-emerald-400/20 disabled:cursor-not-allowed disabled:opacity-40">Düzenle</button>
                              <button onClick={() => void transitionWorkOrder(order)} disabled={!getNextTransitionPath(order.status)} className="rounded-lg border border-amber-400/30 bg-amber-400/10 px-3 py-2 text-xs font-black text-amber-100 transition hover:bg-amber-400/20 disabled:cursor-not-allowed disabled:opacity-40">{getTransitionLabel(order.status)}</button>
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
          recipes={recipes}
          materials={materials}
          stocks={stocks}
          machines={machines}
          orderItems={orderItems}
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
  recipes,
  materials,
  stocks,
  machines,
  orderItems,
  onClose,
  onSaved,
}: {
  mode: DialogMode;
  workOrder: WorkOrder | null;
  products: Product[];
  recipes: Recipe[];
  materials: Material[];
  stocks: StockItem[];
  machines: Machine[];
  orderItems: FlattenedOrderItem[];
  onClose: () => void;
  onSaved: (message: string) => Promise<void>;
}) {
  const [activeTab, setActiveTab] = useState<WorkOrderTab>("general");
  const [form, setForm] = useState<WorkOrderForm>(() => createForm(workOrder));
  const [requirements, setRequirements] = useState<RequirementPayload | null>(workOrder?.requirements ?? null);
  const [qualitySummary, setQualitySummary] = useState<WorkOrderQualitySummary | null>(null);
  const [operationSummary, setOperationSummary] = useState<WorkOrderOperationSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const readonly = mode === "detail";
  const selectedOrderItem = orderItems.find((item) => item.id === form.orderItemId);
  const product = products.find((item) => item.id === selectedOrderItem?.productId || item.id === workOrder?.productId);
  const productRecipes = recipes.filter((recipe) => recipe.productId === product?.id);
  const selectedRecipe = recipes.find((recipe) => recipe.id === form.recipeId);
  const selectedMachine = machines.find((machine) => machine.id === form.assignedMachineId);
  const details = parseProductDetails(product?.description);
  const requirementTotals = buildRequirementTotals(requirements, selectedRecipe);
  const checks = buildChecks(form, selectedOrderItem, product, selectedRecipe, requirements);

  useEffect(() => {
    if (!workOrder?.id) return;
    if (workOrder.requirements) {
      setRequirements(workOrder.requirements);
      return;
    }

    void apiGet<RequirementPayload>("/work-orders/" + workOrder.id + "/requirements")
      .then(setRequirements)
      .catch(() => setRequirements(null));
  }, [workOrder]);

  useEffect(() => {
    if (!workOrder?.id) {
      setQualitySummary(null);
      setOperationSummary(null);
      return;
    }

    void apiGet<WorkOrderQualitySummary>("/quality-inspections/work-order/" + workOrder.id + "/summary")
      .then(setQualitySummary)
      .catch(() => setQualitySummary(null));

    void Promise.all([
      apiGet<unknown>("/cutting-records?workOrderId=" + workOrder.id),
      apiGet<unknown>("/production-boxes?workOrderId=" + workOrder.id),
    ])
      .then(([cuttingData, boxData]) => setOperationSummary(buildOperationSummary(cuttingData, boxData)))
      .catch(() => setOperationSummary(null));
  }, [workOrder?.id]);

  function updateForm<K extends keyof WorkOrderForm>(key: K, value: WorkOrderForm[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function selectOrderItem(orderItemId: string) {
    const item = orderItems.find((entry) => entry.id === orderItemId);
    const recipe = recipes.find((entry) => entry.productId === item?.productId);
    setForm((current) => ({
      ...current,
      orderItemId,
      recipeId: recipe?.id ?? "",
      plannedPairs: item?.remainingPairs ? String(item.remainingPairs) : current.plannedPairs,
    }));
    setRequirements(null);
  }

  async function saveWorkOrder() {
    setError(null);

    if (!form.orderItemId) {
      setError("Sipariş kalemi seçmelisiniz.");
      setActiveTab("general");
      return;
    }

    const plannedPairs = parseInteger(form.plannedPairs);
    if (plannedPairs <= 0) {
      setError("Planlanan üretim miktarı 0'dan büyük olmalıdır.");
      setActiveTab("general");
      return;
    }

    setSaving(true);
    try {
      const body = {
        orderItemId: form.orderItemId,
        recipeId: form.recipeId || null,
        plannedPairs,
        priority: form.priority,
        plannedStartDate: form.plannedStartDate ? new Date(form.plannedStartDate).toISOString() : null,
        plannedEndDate: form.plannedEndDate ? new Date(form.plannedEndDate).toISOString() : null,
        assignedMachineId: form.assignedMachineId || null,
        shift: form.shift ? Number(form.shift) : null,
        notes: form.notes || null,
        isActive: true,
      };

      if (mode === "edit" && form.id) {
        await apiPut<unknown>("/work-orders/" + form.id, body);
        await onSaved("İş emri güncellendi.");
      } else {
        await apiPost<unknown>("/work-orders", body);
        await onSaved("İş emri oluşturuldu.");
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "İş emri kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-3 backdrop-blur-sm sm:p-5">
      <div className="flex max-h-[94vh] w-full max-w-7xl flex-col overflow-hidden rounded-2xl border border-white/10 bg-[#080B10] shadow-2xl">
        <div className="border-b border-white/10 bg-white/[0.04] p-5">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.34em] text-emerald-300">WORK ORDER</p>
              <h2 className="mt-2 text-2xl font-black text-white">{readonly ? "İş Emri Detayı" : mode === "edit" ? "İş Emri Düzenle" : "Yeni İş Emri"}</h2>
              <p className="mt-1 text-sm text-zinc-400">Sipariş kalemi seçilir; ürün, reçete ve hammadde bilgileri master kartlardan gelir.</p>
            </div>
            <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white transition hover:bg-white/[0.12]">Kapat</button>
          </div>
          <WorkOrderSummary workOrder={workOrder} form={form} product={product} orderItem={selectedOrderItem} recipe={selectedRecipe} machine={selectedMachine} />
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

          {activeTab === "general" && (
            <GeneralTab
              form={form}
              workOrder={workOrder}
              product={product}
              details={details}
              orderItems={orderItems}
              productRecipes={productRecipes}
              readonly={readonly || isLocked(workOrder)}
              selectOrderItem={selectOrderItem}
              updateForm={updateForm}
            />
          )}
          {activeTab === "product" && <ProductInfoTab product={product} details={details} orderItem={selectedOrderItem} />}
          {activeTab === "plan" && <ProductionPlanTab form={form} machines={machines} readonly={readonly || isLocked(workOrder)} updateForm={updateForm} />}
          {activeTab === "materials" && <MaterialsTab requirements={requirements} materials={materials} stocks={stocks} totals={requirementTotals} />}
          {activeTab === "operations" && <OperationsTab workOrder={workOrder} operationSummary={operationSummary} />}
          {activeTab === "quality" && <QualityTab details={details} product={product} summary={qualitySummary} />}
          {activeTab === "notes" && <NotesTab form={form} readonly={readonly || isLocked(workOrder)} updateForm={updateForm} />}
        </div>

        <div className="flex flex-col gap-3 border-t border-white/10 bg-black/30 p-5 sm:flex-row sm:justify-end">
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.12]">
            {readonly ? "Kapat" : "Vazgeç"}
          </button>
          {!readonly && !isLocked(workOrder) && (
            <button onClick={() => void saveWorkOrder()} disabled={saving} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400 disabled:opacity-50">
              {saving ? "Kaydediliyor..." : "Kaydet"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function WorkOrderSummary({
  workOrder,
  form,
  product,
  orderItem,
  recipe,
  machine,
}: {
  workOrder: WorkOrder | null;
  form: WorkOrderForm;
  product: Product | undefined;
  orderItem: FlattenedOrderItem | undefined;
  recipe: Recipe | undefined;
  machine: Machine | undefined;
}) {
  const items = [
    ["İş Emri", workOrder?.workOrderNumber || "Otomatik"],
    ["Ürün", product?.name || orderItem?.productName || "-"],
    ["Müşteri", orderItem?.customerName || workOrder?.customerName || "-"],
    ["Reçete", recipe ? [recipe.code, recipe.name].filter(Boolean).join(" - ") : "-"],
    ["Plan", formatNumber(parseInteger(form.plannedPairs))],
    ["Üretilen", formatNumber(workOrder?.producedPairs ?? 0)],
    ["Durum", workOrder ? translateStatus(workOrder.status) : "Taslak"],
    ["Öncelik", translatePriority(form.priority)],
    ["Makine", machine ? [machine.code, machine.name].filter(Boolean).join(" - ") : "-"],
    ["Vardiya", form.shift || "-"],
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
  workOrder,
  product,
  details,
  orderItems,
  productRecipes,
  readonly,
  selectOrderItem,
  updateForm,
}: {
  form: WorkOrderForm;
  workOrder: WorkOrder | null;
  product: Product | undefined;
  details: ProductDetails;
  orderItems: FlattenedOrderItem[];
  productRecipes: Recipe[];
  readonly: boolean;
  selectOrderItem: (orderItemId: string) => void;
  updateForm: <K extends keyof WorkOrderForm>(key: K, value: WorkOrderForm[K]) => void;
}) {
  return (
    <TabPanel title="Genel" note="İş emri bir OrderItem üzerine açılır. Product ve müşteri bilgisi tekrar girilmez.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <ReadOnlyInfo label="İş Emri No" value={workOrder?.workOrderNumber || "Kaydedince oluşur"} />
        <Field label="Sipariş Kalemi">
          <select value={form.orderItemId} disabled={readonly || !!workOrder?.id} onChange={(event) => selectOrderItem(event.target.value)} className={CONTROL_CLASS}>
            <option value="">Sipariş kalemi seç</option>
            {orderItems.map((item) => (
              <option key={item.id} value={item.id}>
                {item.orderNumber} / {item.customerName} / {item.productName} / {formatNumber(item.remainingPairs)} çift kaldı{item.dueDate ? ` / ${formatDate(item.dueDate)}` : ""}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Recipe / BOM">
          <select value={form.recipeId} disabled={readonly || !product} onChange={(event) => updateForm("recipeId", event.target.value)} className={CONTROL_CLASS}>
            <option value="">Reçete seçilmedi</option>
            {productRecipes.map((recipe) => (
              <option key={recipe.id} value={recipe.id}>
                {[recipe.code, recipe.name, recipe.version].filter(Boolean).join(" - ")}
              </option>
            ))}
          </select>
        </Field>
        <TextInput label="Planlanan Üretim Miktarı (Çift)" value={form.plannedPairs} readonly={readonly} type="number" onChange={(value) => updateForm("plannedPairs", value)} />
        <ReadOnlyInfo label="Ürün Kodu" value={product?.code || "-"} />
        <ReadOnlyInfo label="Ürün Adı" value={product?.name || "-"} />
        <ReadOnlyInfo label="Foam" value={product?.foamType || "-"} />
        <ReadOnlyInfo label="Numara" value={details.number || "-"} />
        <ReadOnlyInfo label="Gramaj" value={formatNumber(product?.averageWeight)} />
        <ReadOnlyInfo label="Yoğunluk" value={formatNumber(product?.targetDensity)} />
        <ReadOnlyInfo label="Kumaş" value={details.fabricType || (product?.isFabric ? "Var" : "Yok")} />
        <ReadOnlyInfo label="DTF" value={product?.hasDTFLabel ? "Var" : "Yok"} />
        <ReadOnlyInfo label="Yapışkan" value={details.adhesiveType || (product?.isAdhesive ? "Var" : "Yok")} />
        <ReadOnlyInfo label="Standart Kalıp" value={details.standardMold || "-"} />
        <ReadOnlyInfo label="Standart Pişme Süresi" value={product?.standardCycleTime ? String(product.standardCycleTime) : "-"} />
        <ReadOnlyInfo label="Standart Fire" value={details.standardWasteRate || "-"} />
        <ReadOnlyInfo label="Standart Günlük Kapasite" value={details.standardDailyCapacity || "-"} />
        <SelectInput label="Öncelik" value={form.priority} readonly={readonly} options={PRIORITY_OPTIONS} onChange={(value) => updateForm("priority", value)} />
        <TextInput label="Başlangıç Tarihi" value={form.plannedStartDate} readonly={readonly} type="date" onChange={(value) => updateForm("plannedStartDate", value)} />
        <TextInput label="Termin Tarihi" value={form.plannedEndDate} readonly={readonly} type="date" onChange={(value) => updateForm("plannedEndDate", value)} />
        <ReadOnlyInfo label="Durum" value={workOrder ? translateStatus(workOrder.status) : "Taslak"} />
      </div>
    </TabPanel>
  );
}

function ProductInfoTab({ product, details, orderItem }: { product: Product | undefined; details: ProductDetails; orderItem: FlattenedOrderItem | undefined }) {
  return (
    <TabPanel title="Ürün Bilgileri" note="Readonly. Product Master ve sipariş kaleminden otomatik gelir.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <ReadOnlyInfo label="Müşteri" value={orderItem?.customerName || product?.customerName || "-"} />
        <ReadOnlyInfo label="Model" value={details.model || product?.modelCode || "-"} />
        <ReadOnlyInfo label="Kategori" value={product?.category || "-"} />
        <ReadOnlyInfo label="Foam" value={product?.foamType || "-"} />
        <ReadOnlyInfo label="Numara" value={details.number || "-"} />
        <ReadOnlyInfo label="Renk" value={details.color || orderItem?.fabricColor || "-"} />
        <ReadOnlyInfo label="Üretim Tipi" value={details.productionType || orderItem?.productionType || "-"} />
        <ReadOnlyInfo label="Gramaj" value={formatNumber(product?.averageWeight)} />
        <ReadOnlyInfo label="Yoğunluk" value={formatNumber(product?.targetDensity)} />
        <ReadOnlyInfo label="Sipariş Miktarı" value={formatNumber(orderItem?.quantityPairs)} />
        <ReadOnlyInfo label="Siparişte Üretilen" value={formatNumber(orderItem?.producedPairs)} />
        <ReadOnlyInfo label="Sipariş Kalan" value={formatNumber(orderItem?.remainingPairs)} />
      </div>
    </TabPanel>
  );
}

function ProductionPlanTab({ form, machines, readonly, updateForm }: { form: WorkOrderForm; machines: Machine[]; readonly: boolean; updateForm: <K extends keyof WorkOrderForm>(key: K, value: WorkOrderForm[K]) => void }) {
  return (
    <TabPanel title="Üretim Planı" note="Makine ve vardiya planı. Operatör ve istasyon ataması Üretim Planlama ekranında yapılır.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <Field label="Makine">
          <select value={form.assignedMachineId} disabled={readonly} onChange={(event) => updateForm("assignedMachineId", event.target.value)} className={CONTROL_CLASS}>
            <option value="">Makine seçilmedi</option>
            {machines.map((machine) => (
              <option key={machine.id} value={machine.id}>
                {[machine.code, machine.name].filter(Boolean).join(" - ")}
              </option>
            ))}
          </select>
        </Field>
        <SelectInput label="Vardiya" value={form.shift} readonly={readonly} options={[{ value: "", label: "Seçilmedi" }, { value: "1", label: "1" }, { value: "2", label: "2" }, { value: "3", label: "3" }]} onChange={(value) => updateForm("shift", value)} />
        <TextInput label="Planlanan Başlangıç" value={form.plannedStartDate} readonly={readonly} type="date" onChange={(value) => updateForm("plannedStartDate", value)} />
        <TextInput label="Planlanan Bitiş" value={form.plannedEndDate} readonly={readonly} type="date" onChange={(value) => updateForm("plannedEndDate", value)} />
      </div>
    </TabPanel>
  );
}

function MaterialsTab({ requirements, materials, stocks, totals }: { requirements: RequirementPayload | null; materials: Material[]; stocks: StockItem[]; totals: Array<{ currency: string; total: number }> }) {
  const lines = requirements?.items ?? [];
  return (
    <TabPanel title="Kullanılacak Hammaddeler" note="Backend Recipe/BOM üzerinden hesaplanır; stok sadece okunur, tüketim yapılmaz.">
      {!requirements && <div className="rounded-xl border border-amber-400/30 bg-amber-500/10 p-5 text-sm font-bold text-amber-100">Bu iş emrine bağlı aktif reçete bulunmadığı için hammadde ihtiyacı hesaplanamıyor.</div>}
      {requirements && (
        <>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
            <MetricCard label="Malzeme Sayısı" value={formatNumber(requirements.materialCount ?? lines.length)} />
            <MetricCard label="Yeterli Malzeme" value={formatNumber(requirements.sufficientMaterialCount ?? lines.filter((line) => line.isSufficient).length)} tone="emerald" />
            <MetricCard label="Eksik Malzeme" value={formatNumber(requirements.shortageMaterialCount ?? lines.filter((line) => !line.isSufficient).length)} tone={requirements.hasShortage ? "red" : "cyan"} />
            <MetricCard label="Üretime Başlanabilir" value={requirements.canStartProduction ? "Evet" : "Hayır"} tone={requirements.canStartProduction ? "emerald" : "red"} />
            <MetricCard label="Karşılama" value={`${formatNumber(averageCoverage(lines))}%`} tone={requirements.hasShortage ? "amber" : "emerald"} />
          </div>
          {requirements.warnings && requirements.warnings.length > 0 && (
            <div className="rounded-xl border border-amber-400/30 bg-amber-500/10 p-4 text-sm font-bold text-amber-100">
              {requirements.warnings.map((warning) => <p key={warning}>{warning}</p>)}
            </div>
          )}
          <div className="overflow-x-auto rounded-xl border border-white/10 bg-black/20">
            <table className="min-w-[1480px] w-full text-left text-sm">
              <thead>
                <tr className="border-b border-white/10 text-xs uppercase tracking-[0.16em] text-zinc-500">
                  <th className="p-3">Malzeme</th>
                  <th className="p-3">Kod</th>
                  <th className="p-3">Tip</th>
                  <th className="p-3">Reçete Tüketimi</th>
                  <th className="p-3">Fire %</th>
                  <th className="p-3">Toplam İhtiyaç</th>
                  <th className="p-3">Mevcut Stok</th>
                  <th className="p-3">Eksik</th>
                  <th className="p-3">Karşılama</th>
                  <th className="p-3">Birim Fiyat</th>
                  <th className="p-3">Para Birimi</th>
                  <th className="p-3">Tahmini Maliyet</th>
                  <th className="p-3">Durum</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-white/10">
                {lines.map((line) => {
                  const material = materials.find((item) => item.id === line.materialId);
                  const stock = findStockForMaterial(material, stocks);
                  return (
                    <tr key={line.materialId}>
                      <td className="p-3 font-black text-white">{line.materialName || material?.name || "-"}</td>
                      <td className="p-3 font-mono text-xs text-emerald-200">{line.materialCode || material?.code || "-"}</td>
                      <td className="p-3">{material?.materialType || "-"}</td>
                      <td className="p-3">{formatNumber(line.recipeQuantity)} {line.recipeUnit || ""}</td>
                      <td className="p-3">{formatNumber(line.wastePercent)}</td>
                      <td className="p-3">{formatNumber(line.totalRequiredQuantity)} {line.stockUnit || material?.unit || ""}</td>
                      <td className="p-3">{formatNumber(line.availableStock ?? stock?.currentQuantity)} {line.stockUnit || stock?.unit || ""}</td>
                      <td className="p-3">{formatNumber(line.shortageQuantity)} {line.stockUnit || ""}</td>
                      <td className="p-3">{formatNumber(line.coveragePercent)}%</td>
                      <td className="p-3">{line.materialUnitPrice == null ? "-" : formatNumber(line.materialUnitPrice)}</td>
                      <td className="p-3">{line.currency || material?.currency || "-"}</td>
                      <td className="p-3">{line.estimatedMaterialCost == null ? "-" : formatMoney(line.estimatedMaterialCost, line.currency || material?.currency || "TRY")}</td>
                      <td className="p-3"><StockStatusBadge status={getRequirementStatus(line)} /></td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {totals.map((total) => (
              <MetricCard key={total.currency} label={"Toplam Maliyet " + total.currency} value={formatMoney(total.total, total.currency)} tone="emerald" />
            ))}
            <MetricCard label="Eksik Malzeme" value={String(lines.filter((line) => safeNumber(line.shortageQuantity) > 0).length)} tone={lines.some((line) => safeNumber(line.shortageQuantity) > 0) ? "red" : "cyan"} />
          </div>
        </>
      )}
    </TabPanel>
  );
}

function OperationsTab({ workOrder, operationSummary }: { workOrder: WorkOrder | null; operationSummary: WorkOrderOperationSummary | null }) {
  const assignments = workOrder?.stationAssignments ?? [];
  return (
    <TabPanel title="Operasyonlar" note="İstasyon atamaları Üretim Planlama üzerinden gelir. Tur Ekle akışı burada tekrar edilmez.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <ReadOnlyInfo label="Atanan Çift" value={formatNumber(workOrder?.assignedPairs)} />
        <ReadOnlyInfo label="Üretilen Çift" value={formatNumber(workOrder?.producedPairs)} />
        <ReadOnlyInfo label="Sağlam" value={formatNumber(workOrder?.goodPairs)} />
        <ReadOnlyInfo label="Fire" value={formatNumber(workOrder?.firePairs)} />
      </div>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <ReadOnlyInfo label="Kesilen Çift" value={formatNumber(operationSummary?.cutPairs)} />
        <ReadOnlyInfo label="Kolilenen Çift" value={formatNumber(operationSummary?.boxedPairs)} />
        <ReadOnlyInfo label="Depodaki Çift" value={formatNumber(operationSummary?.warehousePairs)} />
        <ReadOnlyInfo label="Sevkiyata Hazır" value={formatNumber(operationSummary?.readyPairs)} />
        <ReadOnlyInfo label="Sevk Edilen" value={formatNumber(operationSummary?.shippedPairs)} />
      </div>
      <div className="rounded-xl border border-white/10 bg-black/20 p-5 text-sm text-zinc-300">
        {assignments.length > 0 ? `${assignments.length} istasyon ataması bağlı.` : "Henüz bu iş emrine bağlı istasyon ataması yok."}
      </div>
    </TabPanel>
  );
}

function QualityTab({ details, product, summary }: { details: ProductDetails; product: Product | undefined; summary: WorkOrderQualitySummary | null }) {
  return (
    <TabPanel title="Kalite" note="Readonly. Product Master kalite toleranslarından otomatik gelir.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
        <ReadOnlyInfo label="Kontrol Sayısı" value={formatNumber(summary?.inspectionCount)} />
        <ReadOnlyInfo label="Son Sonuç" value={summary?.latestResult || "-"} />
        <ReadOnlyInfo label="Kontrol Edilen" value={formatNumber(summary?.totalChecked)} />
        <ReadOnlyInfo label="Uygunsuz" value={formatNumber(summary?.totalRejected)} />
        <ReadOnlyInfo label="Hata Oranı" value={`%${formatNumber(summary?.defectRate)}`} />
        <ReadOnlyInfo label="Açık Bekletme" value={summary?.canCloseQuality === false ? "Kontrol bekliyor" : "Yok"} />
      </div>
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

function NotesTab({ form, readonly, updateForm }: { form: WorkOrderForm; readonly: boolean; updateForm: <K extends keyof WorkOrderForm>(key: K, value: WorkOrderForm[K]) => void }) {
  return (
    <TabPanel title="Notlar" note="İş emrine özel genel not. Üretim sayaçları burada tutulmaz.">
      <TextAreaInput label="Genel Not" value={form.notes} readonly={readonly} onChange={(value) => updateForm("notes", value)} />
    </TabPanel>
  );
}

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
      <input value={value} type={type} step={type === "number" ? "1" : undefined} disabled={readonly} readOnly={readonly} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS} />
    </Field>
  );
}

function TextAreaInput({ label, value, readonly, onChange }: { label: string; value: string; readonly: boolean; onChange: (value: string) => void }) {
  return (
    <Field label={label}>
      <textarea value={value} disabled={readonly} readOnly={readonly} rows={5} onChange={(event) => onChange(event.target.value)} className={`${CONTROL_CLASS} min-h-28 resize-y`} />
    </Field>
  );
}

function SelectInput({ label, value, readonly, options, onChange }: { label: string; value: string; readonly: boolean; options: Array<string | { value: string; label: string }>; onChange: (value: string) => void }) {
  return (
    <Field label={label}>
      <select value={value} disabled={readonly} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS}>
        {options.map((option) => {
          const item = typeof option === "string" ? { value: option, label: option } : option;
          return <option key={item.value} value={item.value}>{item.label}</option>;
        })}
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

function MetricCard({ label, value, tone = "zinc" }: { label: string; value: string; tone?: "emerald" | "cyan" | "amber" | "red" | "zinc" }) {
  const toneClass = tone === "emerald" ? "border-emerald-400/30 bg-emerald-500/10 text-emerald-200" : tone === "cyan" ? "border-cyan-400/30 bg-cyan-500/10 text-cyan-200" : tone === "amber" ? "border-amber-400/30 bg-amber-500/10 text-amber-200" : tone === "red" ? "border-red-400/30 bg-red-500/10 text-red-200" : "border-white/10 bg-black/25 text-zinc-300";
  return (
    <div className={`rounded-xl border p-4 ${toneClass}`}>
      <p className="text-xs font-black uppercase tracking-[0.18em] opacity-80">{label}</p>
      <p className="mt-2 text-2xl font-black text-white">{value}</p>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const label = translateStatus(status);
  const className =
    status === "Completed" ? "bg-emerald-500/15 text-emerald-200" :
    status === "InProduction" || status === "Paused" ? "bg-amber-500/15 text-amber-200" :
    status === "Cancelled" ? "bg-red-500/15 text-red-200" :
    status === "Ready" || status === "Planned" ? "bg-cyan-500/15 text-cyan-200" :
    "bg-zinc-500/15 text-zinc-200";
  return <span className={`rounded-full px-3 py-1 text-xs font-black ${className}`}>{label}</span>;
}

function MaterialReadinessBadge({ order }: { order: WorkOrder }) {
  const hasUnitMismatch = (order.materialWarnings ?? []).some((warning) => normalizeText(warning).includes("dönüşüm"));
  const label = !order.hasRecipe
    ? "Reçete Yok"
    : hasUnitMismatch
      ? "Birim Uyumsuzluğu"
      : order.hasMaterialShortage
        ? "Hammadde Eksik"
        : "Üretime Hazır";
  const className = !order.hasRecipe || hasUnitMismatch
    ? "bg-red-500/15 text-red-200"
    : order.hasMaterialShortage
      ? "bg-amber-500/15 text-amber-200"
      : "bg-emerald-500/15 text-emerald-200";

  return <span className={`rounded-full px-3 py-1 text-xs font-black ${className}`}>{label}</span>;
}

function StockStatusBadge({ status }: { status: string }) {
  const className =
    status === "Yeterli"
      ? "bg-emerald-500/15 text-emerald-200"
      : status === "Fiyat Bulunamadı"
        ? "bg-zinc-500/15 text-zinc-200"
        : status === "Eksik"
          ? "bg-amber-500/15 text-amber-200"
          : "bg-red-500/15 text-red-200";
  return <span className={`rounded-full px-3 py-1 text-xs font-black ${className}`}>{status}</span>;
}

function LoadingState() {
  return <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-sm font-bold text-zinc-400">Yükleniyor...</div>;
}

type FlattenedOrderItem = OrderItemLookup & {
  orderId: string;
  orderNumber: string;
  customerName: string;
  productName: string;
  quantityPairs: number;
  producedPairs: number;
  remainingPairs: number;
  dueDate?: string | null;
};

function flattenOrderItems(orders: OrderLookup[]): FlattenedOrderItem[] {
  return orders.flatMap((order) =>
    (order.items ?? []).map((item) => ({
      ...item,
      orderId: order.id,
      orderNumber: order.orderNumber || "ORD-" + order.id.substring(0, 8).toUpperCase(),
      dueDate: order.dueDate,
      customerName: order.customerName ?? "-",
      productName: item.productName ?? order.productName ?? "-",
      quantityPairs: safeNumber(item.quantityPairs),
      producedPairs: safeNumber(item.producedPairs),
      remainingPairs: safeNumber(item.remainingPairs),
    }))
  );
}

function createForm(workOrder: WorkOrder | null): WorkOrderForm {
  return {
    id: workOrder?.id ?? "",
    orderItemId: workOrder?.orderItemId ?? "",
    recipeId: workOrder?.recipeId ?? "",
    plannedPairs: workOrder ? String(workOrder.plannedPairs) : "",
    priority: workOrder?.priority ?? "Normal",
    plannedStartDate: dateOnly(workOrder?.plannedStartDate) || formatDateInput(new Date()),
    plannedEndDate: dateOnly(workOrder?.plannedEndDate) || "",
    assignedMachineId: workOrder?.assignedMachineId ?? "",
    shift: workOrder?.shift ? String(workOrder.shift) : "",
    notes: workOrder?.notes ?? "",
  };
}

function buildChecks(form: WorkOrderForm, orderItem: FlattenedOrderItem | undefined, product: Product | undefined, recipe: Recipe | undefined, requirements: RequirementPayload | null) {
  const hasShortage = (requirements?.items ?? []).some((item) => safeNumber(item.shortageQuantity) > 0);
  return [
    { label: "Sipariş Kalemi", status: orderItem ? "OrderItem bağlı" : "Sipariş kalemi bekleniyor", tone: orderItem ? "emerald" as const : "red" as const },
    { label: "Product Master", status: product ? "Ürün bilgisi otomatik" : "Product bulunamadı", tone: product ? "emerald" as const : "red" as const },
    { label: "Recipe / BOM", status: recipe ? "Reçete bağlı" : "Reçete seçilmedi", tone: recipe ? "emerald" as const : "amber" as const },
    { label: "Stok Kontrolü", status: !requirements ? "Reçete bekleniyor" : hasShortage ? "Eksik malzeme var" : "Stok yeterli", tone: !requirements ? "amber" as const : hasShortage ? "red" as const : "emerald" as const },
    { label: "Plan", status: parseInteger(form.plannedPairs) > 0 ? "Plan miktarı hazır" : "Plan miktarı girilmeli", tone: parseInteger(form.plannedPairs) > 0 ? "emerald" as const : "red" as const },
  ];
}

function buildRequirementTotals(requirements: RequirementPayload | null, recipe: Recipe | undefined) {
  if (requirements?.totalsByCurrency) {
    return Object.entries(requirements.totalsByCurrency).map(([currency, total]) => ({ currency, total: safeNumber(total) }));
  }

  if (recipe?.totalCost) {
    return [{ currency: "TRY", total: recipe.totalCost }];
  }

  return [{ currency: "TRY", total: 0 }];
}

function averageCoverage(lines: WorkOrderRequirementLine[]) {
  if (lines.length === 0) return 0;
  return lines.reduce((sum, line) => sum + safeNumber(line.coveragePercent), 0) / lines.length;
}

function getRequirementStatus(line: WorkOrderRequirementLine) {
  if (line.isUnitMismatch) return "Birim Uyumsuz";
  if (!line.stockItemId) return "Stok Kartı Yok";
  if (safeNumber(line.shortageQuantity) > 0) return "Eksik";
  if (line.materialUnitPrice == null || line.estimatedMaterialCost == null) return "Fiyat Bulunamadı";
  return "Yeterli";
}

function buildOperationSummary(cuttingData: unknown, boxData: unknown): WorkOrderOperationSummary {
  const cuts = extractArray(cuttingData).filter(isRecord);
  const boxes = extractArray(boxData).filter(isRecord);
  return {
    cutPairs: cuts.reduce((sum, item) => sum + safeNumber(item.goodPairs), 0),
    boxedPairs: boxes.reduce((sum, item) => sum + safeNumber(item.pairCount), 0),
    warehousePairs: boxes.filter((item) => item.status === "InWarehouse").reduce((sum, item) => sum + safeNumber(item.pairCount), 0),
    readyPairs: boxes.filter((item) => item.status === "ReadyForShipment").reduce((sum, item) => sum + safeNumber(item.pairCount), 0),
    shippedPairs: boxes.filter((item) => item.status === "Shipped").reduce((sum, item) => sum + safeNumber(item.pairCount), 0),
  };
}

async function apiGet<T>(path: string): Promise<T> {
  return apiRequest<T>(path, { method: "GET" });
}

async function apiPost<T>(path: string, body: unknown): Promise<T> {
  return apiRequest<T>(path, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
}

async function apiPut<T>(path: string, body: unknown): Promise<T> {
  return apiRequest<T>(path, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
}

async function apiRequest<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(API + path, init);
  const text = await response.text();
  const payload = text ? JSON.parse(text) as ApiResponse<T> | T : undefined;

  if (!response.ok) {
    const apiPayload = isRecord(payload) ? payload as ApiResponse<T> : undefined;
    throw new Error(apiPayload?.message || apiPayload?.errorMessage || "İstek başarısız oldu.");
  }

  if (isRecord(payload) && "data" in payload) return (payload as ApiResponse<T>).data as T;
  return payload as T;
}

function extractArray(value: unknown): unknown[] {
  if (Array.isArray(value)) return value;
  if (isRecord(value) && Array.isArray(value.data)) return value.data;
  return [];
}

function mapDetailWorkOrder(value: unknown): WorkOrder | null {
  const data = isRecord(value) && "workOrder" in value ? value : value;
  if (!isRecord(data)) return null;
  const workOrder = mapWorkOrder(data.workOrder ?? data);
  if (!workOrder) return null;
  return {
    ...workOrder,
    notes: typeof data.notes === "string" ? data.notes : workOrder.notes,
    cancellationReason: typeof data.cancellationReason === "string" ? data.cancellationReason : workOrder.cancellationReason,
    requirements: isRecord(data.requirements) ? data.requirements as RequirementPayload : workOrder.requirements,
    stationAssignments: Array.isArray(data.stationAssignments) ? data.stationAssignments : [],
  };
}

function mapWorkOrder(value: unknown): WorkOrder | null {
  if (!isRecord(value) || typeof value.id !== "string") return null;
  return {
    id: value.id,
    workOrderNumber: stringValue(value.workOrderNumber),
    orderNumber: stringValue(value.orderNumber),
    orderItemId: stringValue(value.orderItemId),
    customerId: nullableString(value.customerId),
    customerName: nullableString(value.customerName),
    productId: stringValue(value.productId),
    productCode: nullableString(value.productCode),
    productName: nullableString(value.productName),
    recipeId: nullableString(value.recipeId),
    recipeCode: nullableString(value.recipeCode),
    recipeName: nullableString(value.recipeName),
    plannedPairs: safeNumber(value.plannedPairs),
    assignedPairs: safeNumber(value.assignedPairs),
    producedPairs: safeNumber(value.producedPairs),
    goodPairs: safeNumber(value.goodPairs),
    firePairs: safeNumber(value.firePairs),
    remainingPairs: safeNumber(value.remainingPairs),
    progressPercent: safeNumber(value.progressPercent),
    priority: stringValue(value.priority) || "Normal",
    status: stringValue(value.status) || "Draft",
    plannedStartDate: nullableString(value.plannedStartDate),
    plannedEndDate: nullableString(value.plannedEndDate),
    actualStartDate: nullableString(value.actualStartDate),
    actualEndDate: nullableString(value.actualEndDate),
    assignedMachineId: nullableString(value.assignedMachineId),
    assignedMachineCode: nullableString(value.assignedMachineCode),
    shift: nullableNumber(value.shift),
    isActive: typeof value.isActive === "boolean" ? value.isActive : true,
    isCancelled: typeof value.isCancelled === "boolean" ? value.isCancelled : false,
    hasRecipe: typeof value.hasRecipe === "boolean" ? value.hasRecipe : false,
    hasMaterialShortage: typeof value.hasMaterialShortage === "boolean" ? value.hasMaterialShortage : true,
    shortageMaterialCount: nullableNumber(value.shortageMaterialCount),
    materialCoveragePercent: nullableNumber(value.materialCoveragePercent),
    estimatedMaterialCostByCurrency: isRecord(value.estimatedMaterialCostByCurrency) ? value.estimatedMaterialCostByCurrency as Record<string, number> : {},
    canStartProduction: typeof value.canStartProduction === "boolean" ? value.canStartProduction : false,
    materialWarnings: Array.isArray(value.materialWarnings) ? value.materialWarnings.filter((item): item is string => typeof item === "string") : [],
    updatedAt: nullableString(value.updatedAt),
  };
}

function isProduct(value: unknown): value is Product {
  return isRecord(value) && typeof value.id === "string";
}

function isRecipe(value: unknown): value is Recipe {
  return isRecord(value) && typeof value.id === "string";
}

function isMaterial(value: unknown): value is Material {
  return isRecord(value) && typeof value.id === "string";
}

function isStockItem(value: unknown): value is StockItem {
  return isRecord(value) && typeof value.id === "string";
}

function isMachine(value: unknown): value is Machine {
  return isRecord(value) && typeof value.id === "string";
}

function isOrderLookup(value: unknown): value is OrderLookup {
  return isRecord(value) && typeof value.id === "string";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
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

function getNextTransitionPath(status: string) {
  if (status === "Draft") return "/plan";
  if (status === "Planned") return "/mark-ready";
  if (status === "Ready") return "/start";
  if (status === "Paused") return "/resume";
  return null;
}

function getTransitionLabel(status: string) {
  if (status === "Draft") return "Planla";
  if (status === "Planned") return "Hazır Yap";
  if (status === "Ready") return "Üretimi Başlat";
  if (status === "Paused") return "Devam Et";
  return "Başlat";
}

function isLocked(workOrder: WorkOrder | null | undefined) {
  return !!workOrder && (workOrder.status === "Completed" || workOrder.status === "Cancelled" || workOrder.status === "InProduction");
}

function countByStatus(orders: WorkOrder[], status: string) {
  return orders.filter((order) => order.status === status).length.toLocaleString("tr-TR");
}

function countByStatuses(orders: WorkOrder[], statuses: string[]) {
  return orders.filter((order) => statuses.includes(order.status)).length.toLocaleString("tr-TR");
}

function translateStatus(status: string) {
  const map: Record<string, string> = {
    Draft: "Taslak",
    Planned: "Planlandı",
    Ready: "Hazır",
    InProduction: "Üretimde",
    Paused: "Duraklatıldı",
    Completed: "Tamamlandı",
    Cancelled: "İptal",
  };
  return map[status] ?? status;
}

function translatePriority(priority: string) {
  const map: Record<string, string> = {
    Normal: "Normal",
    High: "Yüksek",
    Urgent: "Acil",
  };
  return map[priority] ?? priority;
}

function safeNumber(value: unknown) {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function nullableNumber(value: unknown) {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function parseInteger(value: string) {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : 0;
}

function stringValue(value: unknown) {
  return typeof value === "string" ? value : "";
}

function nullableString(value: unknown) {
  return typeof value === "string" && value.length > 0 ? value : null;
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

function dateOnly(value?: string | null) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return formatDateInput(date);
}

function formatDate(value?: string | null) {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleDateString("tr-TR");
}
