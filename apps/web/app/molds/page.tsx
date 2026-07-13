"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";

type DashboardTone = "emerald" | "cyan" | "amber" | "red" | "blue" | "violet" | "zinc";
type DialogMode = "create" | "edit" | "detail" | null;
type MoldTab = "general" | "technical" | "station" | "ownership" | "maintenance" | "counters" | "documents";
type ActionMode = "station" | "cycle" | "cleaning" | "maintenance" | null;

type ApiResponse<T> = {
  data?: T;
  message?: string;
  errorCode?: string;
  errors?: string[];
  success?: boolean;
};

type Product = {
  id: string;
  code?: string | null;
  name?: string | null;
  customerName?: string | null;
  modelCode?: string | null;
  foamType?: string | null;
  productType?: string | null;
  isActive?: boolean | null;
};

type Mold = {
  id: string;
  productId?: string | null;
  productCode?: string | null;
  productName?: string | null;
  code?: string | null;
  name?: string | null;
  customerName?: string | null;
  productModel?: string | null;
  modelCode?: string | null;
  size?: string | null;
  sizeGroup?: string | null;
  sizeRange?: string | null;
  description?: string | null;
  moldType?: string | null;
  cavityCount?: number | null;
  isRightLeftCombined?: boolean | null;
  foamType?: string | null;
  productType?: string | null;
  xCoordinate?: number | null;
  yCoordinate?: number | null;
  targetPairWeight?: number | null;
  minimumPairWeight?: number | null;
  maximumPairWeight?: number | null;
  targetDensity?: number | null;
  minimumDensity?: number | null;
  maximumDensity?: number | null;
  standardCuringTimeSeconds?: number | null;
  standardMoldTemperature?: number | null;
  standardCycleTimeSeconds?: number | null;
  releaseFrequencyCycles?: number | null;
  moldWeightKg?: number | null;
  machineName?: string | null;
  compatibleMachineCode?: string | null;
  currentStationNumber?: number | null;
  storageLocation?: string | null;
  shelfCode?: string | null;
  totalCycleCount?: number | null;
  totalProducedPairs?: number | null;
  lastCleaningDate?: string | null;
  nextCleaningDate?: string | null;
  lastMaintenanceDate?: string | null;
  nextMaintenanceDate?: string | null;
  estimatedLifeCycles?: number | null;
  photoPath?: string | null;
  technicalDocumentPath?: string | null;
  cadFilePath?: string | null;
  qrCode?: string | null;
  barcode?: string | null;
  ownerType?: string | null;
  ownerCustomerName?: string | null;
  isActive?: boolean | null;
  createdAt?: string | null;
  updatedAt?: string | null;
};

type MoldFormState = {
  productId: string;
  code: string;
  name: string;
  customerName: string;
  productModel: string;
  modelCode: string;
  size: string;
  sizeGroup: string;
  description: string;
  moldType: string;
  cavityCount: string;
  isRightLeftCombined: boolean;
  foamType: string;
  productType: string;
  xCoordinate: string;
  yCoordinate: string;
  targetPairWeight: string;
  minimumPairWeight: string;
  maximumPairWeight: string;
  targetDensity: string;
  minimumDensity: string;
  maximumDensity: string;
  standardCuringTimeSeconds: string;
  standardMoldTemperature: string;
  standardCycleTimeSeconds: string;
  releaseFrequencyCycles: string;
  moldWeightKg: string;
  machineName: string;
  compatibleMachineCode: string;
  currentStationNumber: string;
  storageLocation: string;
  shelfCode: string;
  totalCycleCount: string;
  totalProducedPairs: string;
  lastCleaningDate: string;
  nextCleaningDate: string;
  lastMaintenanceDate: string;
  nextMaintenanceDate: string;
  estimatedLifeCycles: string;
  photoPath: string;
  technicalDocumentPath: string;
  cadFilePath: string;
  qrCode: string;
  barcode: string;
  ownerType: string;
  ownerCustomerName: string;
  isActive: boolean;
};

const API = "http://localhost:5000/api/v1";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60 disabled:cursor-not-allowed disabled:opacity-70";
const MOLD_TYPES = ["Single", "Pair", "Right", "Left", "Combined"];
const FOAM_TYPES = ["10100", "10900"];
const PRODUCT_TYPES = ["Normal", "Memory Foam"];
const OWNER_TYPES = ["Fixar", "Customer"];
const STATUS_FILTERS = ["Tümü", "Aktif", "Pasif"];
const LOCATION_FILTERS = ["Tümü", "İstasyonda", "Depoda"];
const MAINTENANCE_FILTERS = ["Tümü", "Bakım Yaklaşan", "Temizlik Yaklaşan", "Ömrü Dolan"];
const TABS: Array<{ id: MoldTab; label: string }> = [
  { id: "general", label: "1 Genel Bilgiler" },
  { id: "technical", label: "2 Teknik Parametreler" },
  { id: "station", label: "3 Makine ve İstasyon" },
  { id: "ownership", label: "4 Depolama ve Sahiplik" },
  { id: "maintenance", label: "5 Bakım ve Temizlik" },
  { id: "counters", label: "6 Sayaçlar ve Ömür" },
  { id: "documents", label: "7 Dokümanlar ve QR" },
];

const emptyForm: MoldFormState = {
  productId: "",
  code: "",
  name: "",
  customerName: "",
  productModel: "",
  modelCode: "",
  size: "",
  sizeGroup: "",
  description: "",
  moldType: "Pair",
  cavityCount: "1",
  isRightLeftCombined: false,
  foamType: "10100",
  productType: "Normal",
  xCoordinate: "",
  yCoordinate: "",
  targetPairWeight: "",
  minimumPairWeight: "",
  maximumPairWeight: "",
  targetDensity: "",
  minimumDensity: "",
  maximumDensity: "",
  standardCuringTimeSeconds: "",
  standardMoldTemperature: "",
  standardCycleTimeSeconds: "",
  releaseFrequencyCycles: "",
  moldWeightKg: "",
  machineName: "",
  compatibleMachineCode: "",
  currentStationNumber: "",
  storageLocation: "",
  shelfCode: "",
  totalCycleCount: "0",
  totalProducedPairs: "0",
  lastCleaningDate: "",
  nextCleaningDate: "",
  lastMaintenanceDate: "",
  nextMaintenanceDate: "",
  estimatedLifeCycles: "",
  photoPath: "",
  technicalDocumentPath: "",
  cadFilePath: "",
  qrCode: "",
  barcode: "",
  ownerType: "Fixar",
  ownerCustomerName: "",
  isActive: true,
};

export default function MoldsPage() {
  const [molds, setMolds] = useState<Mold[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [foamFilter, setFoamFilter] = useState("Tümü");
  const [moldTypeFilter, setMoldTypeFilter] = useState("Tümü");
  const [ownerFilter, setOwnerFilter] = useState("Tümü");
  const [statusFilter, setStatusFilter] = useState("Tümü");
  const [locationFilter, setLocationFilter] = useState("Tümü");
  const [maintenanceFilter, setMaintenanceFilter] = useState("Tümü");
  const [dialogMode, setDialogMode] = useState<DialogMode>(null);
  const [selectedMold, setSelectedMold] = useState<Mold | null>(null);
  const [actionMode, setActionMode] = useState<ActionMode>(null);
  const [actionMold, setActionMold] = useState<Mold | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  async function loadData() {
    setLoading(true);
    setError(null);

    try {
      const [moldsResponse, productsResponse] = await Promise.all([
        fetch(API + "/molds"),
        fetch(API + "/products"),
      ]);

      if (!moldsResponse.ok) {
        throw new Error(await readError(moldsResponse, "Kalıp listesi alınamadı."));
      }

      if (!productsResponse.ok) {
        throw new Error(await readError(productsResponse, "Product Master listesi alınamadı."));
      }

      setMolds(extractArray<Mold>(await moldsResponse.json()));
      setProducts(extractArray<Product>(await productsResponse.json()).filter((item) => item.isActive !== false));
    } catch (err) {
      setMolds([]);
      setProducts([]);
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  function openDialog(mode: DialogMode, mold: Mold | null = null) {
    setSuccessMessage(null);
    setSelectedMold(mold);
    setDialogMode(mode);
  }

  function closeDialog() {
    setDialogMode(null);
    setSelectedMold(null);
  }

  function openAction(mode: ActionMode, mold: Mold) {
    setSuccessMessage(null);
    setActionMold(mold);
    setActionMode(mode);
  }

  function closeAction() {
    setActionMode(null);
    setActionMold(null);
  }

  async function handleActionSuccess(message: string, moldId?: string) {
    await loadData();
    if (moldId && selectedMold?.id === moldId) {
      const response = await fetch(`${API}/molds/${moldId}`);
      if (response.ok) {
        setSelectedMold(extractOne<Mold>(await response.json()));
      }
    }
    closeAction();
    setSuccessMessage(message);
  }

  const filteredMolds = useMemo(() => {
    const term = normalizeText(search);
    return molds.filter((mold) => {
      const size = getMoldSize(mold);
      const inStation = Boolean(mold.currentStationNumber);
      const maintenance = getMaintenanceState(mold);
      const haystack = [
        mold.code,
        mold.name,
        mold.customerName,
        mold.productModel,
        mold.productName,
        size,
        mold.shelfCode,
        mold.storageLocation,
        mold.currentStationNumber ? String(mold.currentStationNumber) : "",
      ].join(" ");

      return (
        (!term || normalizeText(haystack).includes(term)) &&
        (foamFilter === "Tümü" || mold.foamType === foamFilter) &&
        (moldTypeFilter === "Tümü" || getMoldType(mold) === moldTypeFilter) &&
        (ownerFilter === "Tümü" || getOwnerType(mold) === ownerFilter) &&
        (statusFilter === "Tümü" || (statusFilter === "Aktif" ? mold.isActive !== false : mold.isActive === false)) &&
        (locationFilter === "Tümü" || (locationFilter === "İstasyonda" ? inStation : !inStation)) &&
        (maintenanceFilter === "Tümü" || maintenance === maintenanceFilter)
      );
    });
  }, [foamFilter, locationFilter, maintenanceFilter, moldTypeFilter, molds, ownerFilter, search, statusFilter]);

  const dashboardCards = [
    { title: "Toplam Kalıp", value: molds.length, note: "Mold Master kaydı", tone: "emerald" as DashboardTone },
    { title: "Aktif Kalıp", value: molds.filter((mold) => mold.isActive !== false).length, note: "Kullanılabilir", tone: "cyan" as DashboardTone },
    { title: "Fixar’a Ait", value: molds.filter((mold) => getOwnerType(mold) === "Fixar").length, note: "Firma mülkiyeti", tone: "blue" as DashboardTone },
    { title: "Müşteriye Ait", value: molds.filter((mold) => getOwnerType(mold) === "Customer").length, note: "Customer owner", tone: "violet" as DashboardTone },
    { title: "İstasyonda", value: molds.filter((mold) => mold.currentStationNumber).length, note: "1-24 aktif saha", tone: "amber" as DashboardTone },
    { title: "Bakım Yaklaşan", value: molds.filter((mold) => getMaintenanceState(mold) === "Bakım Yaklaşan").length, note: "Planlı bakım", tone: "amber" as DashboardTone },
    { title: "Temizlik Yaklaşan", value: molds.filter((mold) => getMaintenanceState(mold) === "Temizlik Yaklaşan").length, note: "Temizlik planı", tone: "zinc" as DashboardTone },
    { title: "Ömrü Dolan", value: molds.filter(isLifeExpired).length, note: "Çevrim limiti", tone: "red" as DashboardTone },
  ];

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.16),transparent_34%),radial-gradient(circle_at_bottom_right,rgba(14,165,233,0.12),transparent_32%)] px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.38em] text-emerald-300">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">Kalıp Yönetimi</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                Product Master, İş Emri, Üretim, Bakım, Kalite ve QR izlenebilirlik için tek kalıp ana veri kaynağı.
              </p>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <button onClick={loadData} disabled={loading} className="rounded-xl border border-white/10 bg-white/[0.08] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:opacity-50">
                {loading ? "Yenileniyor..." : "Listeyi Yenile"}
              </button>
              <button onClick={() => openDialog("create")} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
                + Yeni Kalıp
              </button>
            </div>
          </header>

          {successMessage && <div className="rounded-xl border border-emerald-400/30 bg-emerald-500/10 p-4 text-sm font-bold text-emerald-100">{successMessage}</div>}

          <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {dashboardCards.map((card) => (
              <DashboardCard key={card.title} title={card.title} value={card.value.toLocaleString("tr-TR")} note={card.note} tone={card.tone} />
            ))}
          </section>

          <section className="rounded-2xl border border-white/10 bg-white/[0.06] p-5 shadow-2xl backdrop-blur">
            <div className="flex flex-col gap-4 border-b border-white/10 pb-5">
              <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
                <div>
                  <h2 className="text-2xl font-black">Kalıp Listesi</h2>
                  <p className="mt-1 text-sm text-zinc-400">{filteredMolds.length.toLocaleString("tr-TR")} kalıp listeleniyor.</p>
                </div>
                <div className="w-full xl:max-w-md">
                  <Field label="Arama">
                    <input value={search} onChange={(event) => setSearch(event.target.value)} className={CONTROL_CLASS} placeholder="Kod, kalıp, müşteri, numara, raf, istasyon" />
                  </Field>
                </div>
              </div>

              <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-6">
                <FilterSelect label="Foam Tipi" value={foamFilter} options={["Tümü", ...FOAM_TYPES]} onChange={setFoamFilter} />
                <FilterSelect label="Kalıp Tipi" value={moldTypeFilter} options={["Tümü", ...MOLD_TYPES]} onChange={setMoldTypeFilter} />
                <FilterSelect label="Sahiplik" value={ownerFilter} options={["Tümü", ...OWNER_TYPES]} onChange={setOwnerFilter} />
                <FilterSelect label="Aktif/Pasif" value={statusFilter} options={STATUS_FILTERS} onChange={setStatusFilter} />
                <FilterSelect label="Konum" value={locationFilter} options={LOCATION_FILTERS} onChange={setLocationFilter} />
                <FilterSelect label="Bakım Durumu" value={maintenanceFilter} options={MAINTENANCE_FILTERS} onChange={setMaintenanceFilter} />
              </div>
            </div>

            {loading && <LoadingState />}

            {!loading && error && (
              <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-5 text-sm text-red-100">
                <p className="font-black">Kalıp verileri yüklenemedi.</p>
                <p className="mt-1 text-red-200">{error}</p>
              </div>
            )}

            {!loading && !error && filteredMolds.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-zinc-300">
                Kalıp kaydı bulunamadı.
              </div>
            )}

            {!loading && !error && filteredMolds.length > 0 && (
              <div className="mt-5 overflow-x-auto">
                <table className="min-w-[1560px] w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-white/10 text-xs uppercase tracking-[0.16em] text-zinc-500">
                      <th className="py-3 pr-4">Kod</th>
                      <th className="py-3 pr-4">Kalıp</th>
                      <th className="py-3 pr-4">Müşteri</th>
                      <th className="py-3 pr-4">Ürün</th>
                      <th className="py-3 pr-4">Numara</th>
                      <th className="py-3 pr-4">Foam</th>
                      <th className="py-3 pr-4">Tip</th>
                      <th className="py-3 pr-4">Hedef Gramaj</th>
                      <th className="py-3 pr-4">Yoğunluk</th>
                      <th className="py-3 pr-4">Pişme Süresi</th>
                      <th className="py-3 pr-4">İstasyon</th>
                      <th className="py-3 pr-4">Raf</th>
                      <th className="py-3 pr-4">Toplam Çevrim</th>
                      <th className="py-3 pr-4">Üretilen Çift</th>
                      <th className="py-3 pr-4">Sahiplik</th>
                      <th className="py-3 pr-4">Durum</th>
                      <th className="py-3 text-right">İşlemler</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/10">
                    {filteredMolds.map((mold) => (
                      <tr key={mold.id} className="align-middle text-zinc-200 transition hover:bg-white/[0.04]">
                        <td className="py-4 pr-4 font-mono text-xs text-emerald-200">{mold.code || "-"}</td>
                        <td className="py-4 pr-4 font-black text-white">{mold.name || "-"}</td>
                        <td className="py-4 pr-4">{mold.customerName || mold.ownerCustomerName || "-"}</td>
                        <td className="py-4 pr-4">{mold.productModel || mold.productName || "-"}</td>
                        <td className="py-4 pr-4">{getMoldSize(mold) || "-"}</td>
                        <td className="py-4 pr-4">{mold.foamType || "-"}</td>
                        <td className="py-4 pr-4">{getMoldType(mold)}</td>
                        <td className="py-4 pr-4">{formatNumber(mold.targetPairWeight)}</td>
                        <td className="py-4 pr-4">{formatNumber(mold.targetDensity)}</td>
                        <td className="py-4 pr-4">{formatSeconds(mold.standardCuringTimeSeconds)}</td>
                        <td className="py-4 pr-4">{mold.currentStationNumber ? `İstasyon ${mold.currentStationNumber}` : "Depoda"}</td>
                        <td className="py-4 pr-4">{mold.shelfCode || mold.storageLocation || "-"}</td>
                        <td className="py-4 pr-4">{formatNumber(mold.totalCycleCount)}</td>
                        <td className="py-4 pr-4">{formatNumber(mold.totalProducedPairs)}</td>
                        <td className="py-4 pr-4">{getOwnerType(mold)}</td>
                        <td className="py-4 pr-4"><StatusBadge active={mold.isActive !== false} /></td>
                        <td className="py-4">
                          <div className="flex min-w-[420px] flex-wrap justify-end gap-2">
                            <ActionButton label="Detay" tone="cyan" onClick={() => openDialog("detail", mold)} />
                            <ActionButton label="Düzenle" tone="emerald" onClick={() => openDialog("edit", mold)} />
                            <ActionButton label="İstasyon Ata" tone="amber" onClick={() => openAction("station", mold)} />
                            <ActionButton label="Çevrim Ekle" tone="blue" onClick={() => openAction("cycle", mold)} />
                            <ActionButton label="Temizlik Kaydı" tone="zinc" onClick={() => openAction("cleaning", mold)} />
                            <ActionButton label="Bakım Kaydı" tone="red" onClick={() => openAction("maintenance", mold)} />
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      </div>

      {dialogMode && (
        <MoldModal
          mode={dialogMode}
          mold={selectedMold}
          products={products}
          onClose={closeDialog}
          onSaved={async (message) => {
            await loadData();
            closeDialog();
            setSuccessMessage(message);
          }}
        />
      )}

      {actionMode && actionMold && (
        <MoldActionModal
          mode={actionMode}
          mold={actionMold}
          onClose={closeAction}
          onSuccess={(message) => handleActionSuccess(message, actionMold.id)}
        />
      )}
    </main>
  );
}

function MoldModal({
  mode,
  mold,
  products,
  onClose,
  onSaved,
}: {
  mode: DialogMode;
  mold: Mold | null;
  products: Product[];
  onClose: () => void;
  onSaved: (message: string) => Promise<void>;
}) {
  const [activeTab, setActiveTab] = useState<MoldTab>("general");
  const [form, setForm] = useState<MoldFormState>(() => moldToForm(mold));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const readonly = mode === "detail";
  const selectedProduct = products.find((product) => product.id === form.productId);
  const life = calculateLife(form);

  function updateField<K extends keyof MoldFormState>(key: K, value: MoldFormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function selectProduct(productId: string) {
    const product = products.find((item) => item.id === productId);
    setForm((current) => ({
      ...current,
      productId,
      customerName: product?.customerName || current.customerName,
      productModel: product?.name || current.productModel,
      modelCode: product?.modelCode || current.modelCode,
      foamType: product?.foamType || current.foamType,
      productType: product?.productType || current.productType,
    }));
  }

  async function saveMold() {
    setError(null);

    const validation = validateForm(form);
    if (validation) {
      setError(validation);
      setActiveTab("general");
      return;
    }

    setSaving(true);
    try {
      const response = await fetch(mode === "edit" && mold ? `${API}/molds/${mold.id}` : `${API}/molds`, {
        method: mode === "edit" ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(toMoldRequest(form)),
      });

      if (!response.ok) {
        throw new Error(await readError(response, "Kalıp kaydedilemedi."));
      }

      await onSaved(mode === "edit" ? "Kalıp güncellendi." : "Kalıp oluşturuldu.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
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
              <p className="text-xs font-black tracking-[0.34em] text-emerald-300">MOLD MASTER</p>
              <h2 className="mt-2 text-2xl font-black text-white">{readonly ? "Kalıp Detayı" : mode === "edit" ? "Kalıp Düzenle" : "Yeni Kalıp"}</h2>
              <p className="mt-1 text-sm text-zinc-400">Kalıp teknik bilgisi tek kartta tutulur; Product ve İş Emri yalnızca buradan seçim yapar.</p>
            </div>
            <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white transition hover:bg-white/[0.12]">Kapat</button>
          </div>
          <MoldSummary form={form} selectedProduct={selectedProduct} life={life} />
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

          {activeTab === "general" && <GeneralTab form={form} products={products} selectedProduct={selectedProduct} readonly={readonly} updateField={updateField} selectProduct={selectProduct} />}
          {activeTab === "technical" && <TechnicalTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "station" && <StationTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "ownership" && <OwnershipTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "maintenance" && <MaintenanceTab form={form} />}
          {activeTab === "counters" && <CountersTab form={form} life={life} />}
          {activeTab === "documents" && <DocumentsTab form={form} readonly={readonly} updateField={updateField} />}
        </div>

        <div className="flex flex-col gap-3 border-t border-white/10 bg-black/30 p-5 sm:flex-row sm:justify-end">
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.12]">
            {readonly ? "Kapat" : "Vazgeç"}
          </button>
          {!readonly && (
            <button onClick={saveMold} disabled={saving} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400 disabled:opacity-60">
              {saving ? "Kaydediliyor..." : "Kaydet"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function MoldActionModal({ mode, mold, onClose, onSuccess }: { mode: ActionMode; mold: Mold; onClose: () => void; onSuccess: (message: string) => void }) {
  const [stationNumber, setStationNumber] = useState(mold.currentStationNumber ? String(mold.currentStationNumber) : "");
  const [cycleCount, setCycleCount] = useState("");
  const [producedPairs, setProducedPairs] = useState("");
  const [eventDate, setEventDate] = useState(formatDateInput(new Date()));
  const [nextDate, setNextDate] = useState("");
  const [note, setNote] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function submit() {
    setError(null);
    setSaving(true);

    try {
      let url = "";
      let body: Record<string, unknown> = {};
      let message = "";

      if (mode === "station") {
        const station = Number(stationNumber);
        if (!Number.isFinite(station) || station < 1 || station > 24) {
          throw new Error("İstasyon numarası 1 ile 24 arasında olmalıdır.");
        }
        url = `${API}/molds/${mold.id}/assign-station`;
        body = { stationNumber: station };
        message = "Kalıp istasyona atandı.";
      }

      if (mode === "cycle") {
        const cycle = Number(cycleCount);
        const pairs = Number(producedPairs || "0");
        if (!Number.isFinite(cycle) || cycle <= 0) {
          throw new Error("Çevrim sayısı 0'dan büyük olmalıdır.");
        }
        if (!Number.isFinite(pairs) || pairs < 0) {
          throw new Error("Üretilen çift negatif olamaz.");
        }
        url = `${API}/molds/${mold.id}/record-cycle`;
        body = { cycleCount: cycle, producedPairs: pairs };
        message = "Çevrim kaydı eklendi.";
      }

      if (mode === "cleaning") {
        url = `${API}/molds/${mold.id}/record-cleaning`;
        body = { cleaningDate: toIsoOrNull(eventDate), nextCleaningDate: toIsoOrNull(nextDate), note };
        message = "Temizlik kaydı işlendi.";
      }

      if (mode === "maintenance") {
        url = `${API}/molds/${mold.id}/record-maintenance`;
        body = { maintenanceDate: toIsoOrNull(eventDate), nextMaintenanceDate: toIsoOrNull(nextDate), note };
        message = "Bakım kaydı işlendi.";
      }

      const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });

      if (!response.ok) {
        throw new Error(await readError(response, "İşlem tamamlanamadı."));
      }

      onSuccess(message);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setSaving(false);
    }
  }

  const title = mode === "station" ? "İstasyon Ata" : mode === "cycle" ? "Çevrim Ekle" : mode === "cleaning" ? "Temizlik Kaydı" : "Bakım Kaydı";

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/80 p-4 backdrop-blur">
      <div className="w-full max-w-xl rounded-2xl border border-white/10 bg-[#080B10] p-5 shadow-2xl">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-black tracking-[0.28em] text-emerald-300">MOLD ACTION</p>
            <h3 className="mt-2 text-2xl font-black">{title}</h3>
            <p className="mt-1 text-sm text-zinc-400">{mold.code || "-"} - {mold.name || "-"}</p>
          </div>
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white">Kapat</button>
        </div>

        {error && <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-4 text-sm font-bold text-red-100">{error}</div>}

        <div className="mt-5 space-y-4">
          {mode === "station" && <TextInput label="İstasyon Numarası" value={stationNumber} readonly={false} type="number" onChange={setStationNumber} />}
          {mode === "cycle" && (
            <div className="grid gap-4 sm:grid-cols-2">
              <TextInput label="Çevrim Sayısı" value={cycleCount} readonly={false} type="number" onChange={setCycleCount} />
              <TextInput label="Üretilen Çift" value={producedPairs} readonly={false} type="number" onChange={setProducedPairs} />
            </div>
          )}
          {(mode === "cleaning" || mode === "maintenance") && (
            <>
              <div className="grid gap-4 sm:grid-cols-2">
                <TextInput label={mode === "cleaning" ? "Temizlik Tarihi" : "Bakım Tarihi"} value={eventDate} readonly={false} type="date" onChange={setEventDate} />
                <TextInput label={mode === "cleaning" ? "Sonraki Temizlik Tarihi" : "Sonraki Bakım Tarihi"} value={nextDate} readonly={false} type="date" onChange={setNextDate} />
              </div>
              <TextAreaInput label="Not" value={note} readonly={false} onChange={setNote} />
            </>
          )}
        </div>

        <div className="mt-6 flex justify-end gap-3">
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-5 py-3 text-sm font-black text-white">Vazgeç</button>
          <button onClick={submit} disabled={saving} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black disabled:opacity-60">
            {saving ? "İşleniyor..." : "Kaydet"}
          </button>
        </div>
      </div>
    </div>
  );
}

function MoldSummary({ form, selectedProduct, life }: { form: MoldFormState; selectedProduct: Product | undefined; life: { remaining: number | null; percent: number | null; expired: boolean } }) {
  const items = [
    ["Kod", form.code || "-"],
    ["Kalıp", form.name || "-"],
    ["Product", selectedProduct ? `${selectedProduct.code || "-"} / ${selectedProduct.name || "-"}` : "-"],
    ["Müşteri", form.customerName || form.ownerCustomerName || "-"],
    ["Numara", form.size || "-"],
    ["Foam", form.foamType || "-"],
    ["Tip", form.moldType || "-"],
    ["İstasyon", form.currentStationNumber ? `İstasyon ${form.currentStationNumber}` : "Depoda"],
    ["Sahiplik", form.ownerType || "-"],
    ["Ömür", life.percent === null ? "-" : `%${formatNumber(life.percent)}`],
  ];

  return (
    <div className="mt-5 grid gap-2 sm:grid-cols-2 lg:grid-cols-5">
      {items.map(([label, value]) => (
        <div key={label} className={`rounded-xl border px-3 py-2 ${life.expired && label === "Ömür" ? "border-red-400/30 bg-red-500/10" : "border-white/10 bg-black/30"}`}>
          <p className="text-[10px] font-black uppercase tracking-[0.18em] text-zinc-500">{label}</p>
          <p className="mt-1 truncate text-sm font-black text-white" title={value}>{value}</p>
        </div>
      ))}
    </div>
  );
}

function GeneralTab({
  form,
  products,
  selectedProduct,
  readonly,
  updateField,
  selectProduct,
}: {
  form: MoldFormState;
  products: Product[];
  selectedProduct: Product | undefined;
  readonly: boolean;
  updateField: <K extends keyof MoldFormState>(key: K, value: MoldFormState[K]) => void;
  selectProduct: (productId: string) => void;
}) {
  return (
    <TabPanel title="Genel Bilgiler" note="Product seçimi teknik bilgiyi tekrar yazmadan kalıba bağlar.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Kalıp Kodu" value={form.code} readonly={readonly} onChange={(value) => updateField("code", value)} />
        <TextInput label="Kalıp Adı" value={form.name} readonly={readonly} onChange={(value) => updateField("name", value)} />
        <Field label="Product Master">
          <select value={form.productId} disabled={readonly} onChange={(event) => selectProduct(event.target.value)} className={CONTROL_CLASS}>
            <option value="">Product seç</option>
            {products.map((product) => (
              <option key={product.id} value={product.id}>{[product.code, product.name, product.foamType].filter(Boolean).join(" - ")}</option>
            ))}
          </select>
        </Field>
        <ReadOnlyInfo label="Product Kod/Ad" value={selectedProduct ? `${selectedProduct.code || "-"} / ${selectedProduct.name || "-"}` : "-"} />
        <TextInput label="Müşteri" value={form.customerName} readonly={readonly} onChange={(value) => updateField("customerName", value)} />
        <TextInput label="Ürün Modeli" value={form.productModel} readonly={readonly} onChange={(value) => updateField("productModel", value)} />
        <TextInput label="Model Kodu" value={form.modelCode} readonly={readonly} onChange={(value) => updateField("modelCode", value)} />
        <TextInput label="Numara" value={form.size} readonly={readonly} onChange={(value) => updateField("size", value)} />
        <TextInput label="Numara Grubu" value={form.sizeGroup} readonly={readonly} onChange={(value) => updateField("sizeGroup", value)} />
        <SelectInput label="Kalıp Tipi" value={form.moldType} readonly={readonly} options={MOLD_TYPES} onChange={(value) => updateField("moldType", value)} />
        <TextInput label="Göz Sayısı" value={form.cavityCount} readonly={readonly} type="number" onChange={(value) => updateField("cavityCount", value)} />
        <SelectInput label="Foam Tipi" value={form.foamType} readonly={readonly} options={FOAM_TYPES} onChange={(value) => updateField("foamType", value)} />
        <SelectInput label="Ürün Tipi" value={form.productType} readonly={readonly} options={PRODUCT_TYPES} onChange={(value) => updateField("productType", value)} />
        <Toggle label="Sağ/Sol Birleşik mi" checked={form.isRightLeftCombined} readonly={readonly} onChange={(value) => updateField("isRightLeftCombined", value)} />
        <Toggle label="Aktif/Pasif" checked={form.isActive} readonly={readonly} onChange={(value) => updateField("isActive", value)} />
      </div>
      <TextAreaInput label="Açıklama" value={form.description} readonly={readonly} onChange={(value) => updateField("description", value)} />
    </TabPanel>
  );
}

function TechnicalTab({ form, readonly, updateField }: MoldTabProps) {
  return (
    <TabPanel title="Teknik Parametreler" note="Kalıba ait üretim parametreleri. Product Master içinde tekrar girilmez.">
      <InfoBox>“X/Y koordinatları kalıba aittir. Product Master içinde tekrar girilmez.”</InfoBox>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="X Koordinatı" value={form.xCoordinate} readonly={readonly} type="number" onChange={(value) => updateField("xCoordinate", value)} />
        <TextInput label="Y Koordinatı" value={form.yCoordinate} readonly={readonly} type="number" onChange={(value) => updateField("yCoordinate", value)} />
        <TextInput label="Hedef Çift Gramajı" value={form.targetPairWeight} readonly={readonly} type="number" onChange={(value) => updateField("targetPairWeight", value)} />
        <TextInput label="Minimum Çift Gramajı" value={form.minimumPairWeight} readonly={readonly} type="number" onChange={(value) => updateField("minimumPairWeight", value)} />
        <TextInput label="Maksimum Çift Gramajı" value={form.maximumPairWeight} readonly={readonly} type="number" onChange={(value) => updateField("maximumPairWeight", value)} />
        <TextInput label="Hedef Yoğunluk" value={form.targetDensity} readonly={readonly} type="number" onChange={(value) => updateField("targetDensity", value)} />
        <TextInput label="Minimum Yoğunluk" value={form.minimumDensity} readonly={readonly} type="number" onChange={(value) => updateField("minimumDensity", value)} />
        <TextInput label="Maksimum Yoğunluk" value={form.maximumDensity} readonly={readonly} type="number" onChange={(value) => updateField("maximumDensity", value)} />
        <TextInput label="Standart Pişme Süresi" value={form.standardCuringTimeSeconds} readonly={readonly} type="number" onChange={(value) => updateField("standardCuringTimeSeconds", value)} />
        <TextInput label="Standart Kalıp Sıcaklığı" value={form.standardMoldTemperature} readonly={readonly} type="number" onChange={(value) => updateField("standardMoldTemperature", value)} />
        <TextInput label="Standart Çevrim Süresi" value={form.standardCycleTimeSeconds} readonly={readonly} type="number" onChange={(value) => updateField("standardCycleTimeSeconds", value)} />
        <TextInput label="Release Sıklığı" value={form.releaseFrequencyCycles} readonly={readonly} type="number" onChange={(value) => updateField("releaseFrequencyCycles", value)} />
        <TextInput label="Kalıp Ağırlığı" value={form.moldWeightKg} readonly={readonly} type="number" onChange={(value) => updateField("moldWeightKg", value)} />
      </div>
    </TabPanel>
  );
}

function StationTab({ form, readonly, updateField }: MoldTabProps) {
  const stationNumber = safeParsedNumber(form.currentStationNumber);
  const stationValid = !form.currentStationNumber || (stationNumber >= 1 && stationNumber <= 24);
  return (
    <TabPanel title="Makine ve İstasyon" note="24 istasyonlu FIXAR üretim yapısında kalıbın aktif konumu.">
      {!stationValid && <InfoBox tone="red">İstasyon 1-24 arasında olmalı.</InfoBox>}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Makine Adı" value={form.machineName} readonly={readonly} onChange={(value) => updateField("machineName", value)} />
        <TextInput label="Uyumlu Makine Kodu" value={form.compatibleMachineCode} readonly={readonly} onChange={(value) => updateField("compatibleMachineCode", value)} />
        <TextInput label="Mevcut İstasyon" value={form.currentStationNumber} readonly={readonly} type="number" onChange={(value) => updateField("currentStationNumber", value)} />
        <ReadOnlyInfo label="İstasyon Durumu" value={form.currentStationNumber ? "İstasyonda" : "Depoda"} />
      </div>
      <InfoBox>İstasyon atama işlemi liste üzerindeki “İstasyon Ata” aksiyonu ile backend’e ayrı endpoint üzerinden gönderilir.</InfoBox>
    </TabPanel>
  );
}

function OwnershipTab({ form, readonly, updateField }: MoldTabProps) {
  return (
    <TabPanel title="Depolama ve Sahiplik" note="Kalıp lokasyonu ve mülkiyet bilgisi.">
      {form.ownerType === "Customer" && !form.ownerCustomerName.trim() && <InfoBox tone="amber">Customer sahiplik seçildiğinde sahip müşteri bilgisi girilmelidir.</InfoBox>}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Depolama Alanı" value={form.storageLocation} readonly={readonly} onChange={(value) => updateField("storageLocation", value)} />
        <TextInput label="Raf Kodu" value={form.shelfCode} readonly={readonly} onChange={(value) => updateField("shelfCode", value)} />
        <SelectInput label="Sahiplik" value={form.ownerType} readonly={readonly} options={OWNER_TYPES} onChange={(value) => updateField("ownerType", value)} />
        <TextInput label="Sahip Müşteri" value={form.ownerCustomerName} readonly={readonly} onChange={(value) => updateField("ownerCustomerName", value)} />
      </div>
    </TabPanel>
  );
}

function MaintenanceTab({ form }: { form: MoldFormState }) {
  return (
    <TabPanel title="Bakım ve Temizlik" note="Son kayıtlar entity üzerinde tutulur; geçmiş tabloları ileride bağlanabilir.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <ReadOnlyInfo label="Son Temizlik Tarihi" value={formatDate(form.lastCleaningDate)} />
        <ReadOnlyInfo label="Sonraki Temizlik Tarihi" value={formatDate(form.nextCleaningDate)} />
        <ReadOnlyInfo label="Son Bakım Tarihi" value={formatDate(form.lastMaintenanceDate)} />
        <ReadOnlyInfo label="Sonraki Bakım Tarihi" value={formatDate(form.nextMaintenanceDate)} />
      </div>
      <InfoBox>Temizlik ve bakım kayıtları liste üzerindeki ayrı aksiyonlarla backend’e işlenir; başarılı işlem sonrası liste yenilenir.</InfoBox>
    </TabPanel>
  );
}

function CountersTab({ form, life }: { form: MoldFormState; life: { remaining: number | null; percent: number | null; expired: boolean } }) {
  return (
    <TabPanel title="Sayaçlar ve Ömür" note="Üretim çevrimleri ve tahmini kalıp ömrü readonly izlenir.">
      {life.expired && <InfoBox tone="red">Tahmini kalıp ömrü aşılmış veya limite ulaşmış görünüyor.</InfoBox>}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <MetricCard label="Toplam Çevrim" value={formatNumber(safeParsedNumber(form.totalCycleCount))} />
        <MetricCard label="Toplam Üretilen Çift" value={formatNumber(safeParsedNumber(form.totalProducedPairs))} />
        <MetricCard label="Tahmini Ömür Çevrimi" value={form.estimatedLifeCycles || "-"} />
        <MetricCard label="Kalan Ömür" value={life.remaining === null ? "-" : formatNumber(life.remaining)} tone={life.expired ? "red" : "emerald"} />
        <MetricCard label="Ömür Kullanım %" value={life.percent === null ? "-" : `%${formatNumber(life.percent)}`} tone={life.expired ? "red" : "cyan"} />
      </div>
      <InfoBox>Çevrim ekleme işlemi liste üzerindeki “Çevrim Ekle” aksiyonu ile yapılır.</InfoBox>
    </TabPanel>
  );
}

function DocumentsTab({ form, readonly, updateField }: MoldTabProps) {
  return (
    <TabPanel title="Dokümanlar ve QR" note="Backend dosya yükleme hazır olana kadar path/text metadata olarak tutulur.">
      <div className="grid gap-4 lg:grid-cols-[1fr_1.4fr]">
        <div className="overflow-hidden rounded-xl border border-white/10 bg-black/30">
          <div className="flex aspect-[4/3] items-center justify-center bg-white/[0.04]">
            {form.photoPath ? <p className="px-4 text-center text-sm font-black text-emerald-200">{form.photoPath}</p> : <p className="text-xs font-black uppercase tracking-[0.18em] text-zinc-500">Kalıp Fotoğrafı Önizleme</p>}
          </div>
          <div className="border-t border-white/10 px-3 py-2 text-xs font-bold text-zinc-300">Dosya yükleme endpoint’i hazır olunca burada gerçek önizleme kullanılacak.</div>
        </div>
        <div className="grid gap-4 md:grid-cols-2">
          <TextInput label="Kalıp Fotoğrafı" value={form.photoPath} readonly={readonly} onChange={(value) => updateField("photoPath", value)} />
          <TextInput label="Teknik Doküman" value={form.technicalDocumentPath} readonly={readonly} onChange={(value) => updateField("technicalDocumentPath", value)} />
          <TextInput label="CAD Dosyası" value={form.cadFilePath} readonly={readonly} onChange={(value) => updateField("cadFilePath", value)} />
          <TextInput label="QR Kod" value={form.qrCode} readonly={readonly} onChange={(value) => updateField("qrCode", value)} />
          <TextInput label="Barkod" value={form.barcode} readonly={readonly} onChange={(value) => updateField("barcode", value)} />
        </div>
      </div>
    </TabPanel>
  );
}

type MoldTabProps = {
  form: MoldFormState;
  readonly: boolean;
  updateField: <K extends keyof MoldFormState>(key: K, value: MoldFormState[K]) => void;
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

function FilterSelect({ label, value, options, onChange }: { label: string; value: string; options: string[]; onChange: (value: string) => void }) {
  return <SelectInput label={label} value={value} readonly={false} options={options} onChange={onChange} />;
}

function Toggle({ label, checked, readonly, onChange }: { label: string; checked: boolean; readonly: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="rounded-xl border border-white/10 bg-black/20 p-4">
      <span className="mb-3 block text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{label}</span>
      <button
        type="button"
        disabled={readonly}
        onClick={() => onChange(!checked)}
        className={`rounded-full px-4 py-2 text-sm font-black transition ${checked ? "bg-emerald-500 text-black" : "bg-zinc-700 text-zinc-200"} disabled:cursor-not-allowed disabled:opacity-70`}
      >
        {checked ? "Var / Aktif" : "Yok / Pasif"}
      </button>
    </label>
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

function InfoBox({ children, tone = "amber" }: { children: ReactNode; tone?: "amber" | "red" | "cyan" }) {
  const className = tone === "red" ? "border-red-400/30 bg-red-500/10 text-red-100" : tone === "cyan" ? "border-cyan-400/30 bg-cyan-500/10 text-cyan-100" : "border-amber-400/30 bg-amber-500/10 text-amber-100";
  return <div className={`rounded-xl border p-4 text-sm font-bold ${className}`}>{children}</div>;
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

function StatusBadge({ active }: { active: boolean }) {
  return <span className={`rounded-full px-3 py-1 text-xs font-black ${active ? "bg-emerald-500/15 text-emerald-200" : "bg-red-500/15 text-red-200"}`}>{active ? "Aktif" : "Pasif"}</span>;
}

function ActionButton({ label, tone, onClick }: { label: string; tone: "cyan" | "emerald" | "amber" | "blue" | "zinc" | "red"; onClick: () => void }) {
  const className = {
    cyan: "border-cyan-400/30 bg-cyan-400/10 text-cyan-100 hover:bg-cyan-400/20",
    emerald: "border-emerald-400/30 bg-emerald-400/10 text-emerald-100 hover:bg-emerald-400/20",
    amber: "border-amber-400/30 bg-amber-400/10 text-amber-100 hover:bg-amber-400/20",
    blue: "border-blue-400/30 bg-blue-400/10 text-blue-100 hover:bg-blue-400/20",
    zinc: "border-zinc-400/30 bg-zinc-400/10 text-zinc-100 hover:bg-zinc-400/20",
    red: "border-red-400/30 bg-red-400/10 text-red-100 hover:bg-red-400/20",
  }[tone];
  return <button onClick={onClick} className={`rounded-lg border px-3 py-2 text-xs font-black transition ${className}`}>{label}</button>;
}

function LoadingState() {
  return <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-sm font-bold text-zinc-400">Yükleniyor...</div>;
}

function moldToForm(mold: Mold | null): MoldFormState {
  if (!mold) return { ...emptyForm };
  return {
    productId: mold.productId || "",
    code: mold.code || "",
    name: mold.name || "",
    customerName: mold.customerName || "",
    productModel: mold.productModel || mold.productName || "",
    modelCode: mold.modelCode || "",
    size: mold.size || mold.sizeRange || "",
    sizeGroup: mold.sizeGroup || "",
    description: mold.description || "",
    moldType: mold.moldType || "Pair",
    cavityCount: String(mold.cavityCount && mold.cavityCount > 0 ? mold.cavityCount : 1),
    isRightLeftCombined: Boolean(mold.isRightLeftCombined),
    foamType: mold.foamType || "10100",
    productType: mold.productType || "Normal",
    xCoordinate: numberToString(mold.xCoordinate),
    yCoordinate: numberToString(mold.yCoordinate),
    targetPairWeight: numberToString(mold.targetPairWeight),
    minimumPairWeight: numberToString(mold.minimumPairWeight),
    maximumPairWeight: numberToString(mold.maximumPairWeight),
    targetDensity: numberToString(mold.targetDensity),
    minimumDensity: numberToString(mold.minimumDensity),
    maximumDensity: numberToString(mold.maximumDensity),
    standardCuringTimeSeconds: numberToString(mold.standardCuringTimeSeconds),
    standardMoldTemperature: numberToString(mold.standardMoldTemperature),
    standardCycleTimeSeconds: numberToString(mold.standardCycleTimeSeconds),
    releaseFrequencyCycles: numberToString(mold.releaseFrequencyCycles),
    moldWeightKg: numberToString(mold.moldWeightKg),
    machineName: mold.machineName || "",
    compatibleMachineCode: mold.compatibleMachineCode || "",
    currentStationNumber: numberToString(mold.currentStationNumber),
    storageLocation: mold.storageLocation || "",
    shelfCode: mold.shelfCode || "",
    totalCycleCount: numberToString(mold.totalCycleCount) || "0",
    totalProducedPairs: numberToString(mold.totalProducedPairs) || "0",
    lastCleaningDate: dateToInput(mold.lastCleaningDate),
    nextCleaningDate: dateToInput(mold.nextCleaningDate),
    lastMaintenanceDate: dateToInput(mold.lastMaintenanceDate),
    nextMaintenanceDate: dateToInput(mold.nextMaintenanceDate),
    estimatedLifeCycles: numberToString(mold.estimatedLifeCycles),
    photoPath: mold.photoPath || "",
    technicalDocumentPath: mold.technicalDocumentPath || "",
    cadFilePath: mold.cadFilePath || "",
    qrCode: mold.qrCode || "",
    barcode: mold.barcode || "",
    ownerType: mold.ownerType || "Fixar",
    ownerCustomerName: mold.ownerCustomerName || "",
    isActive: mold.isActive !== false,
  };
}

function validateForm(form: MoldFormState) {
  if (!form.code.trim()) return "Kalıp kodu zorunludur.";
  if (!form.name.trim()) return "Kalıp adı zorunludur.";
  if (!form.size.trim()) return "Numara zorunludur.";
  if (!FOAM_TYPES.includes(form.foamType)) return "Foam tipi sadece 10100 veya 10900 olabilir.";
  if (!MOLD_TYPES.includes(form.moldType)) return "Kalıp tipi geçersiz.";
  if (safeParsedNumber(form.cavityCount) <= 0) return "Göz sayısı 0'dan büyük olmalıdır.";
  if (isNegative(form.xCoordinate)) return "X koordinatı negatif olamaz.";
  if (isNegative(form.yCoordinate)) return "Y koordinatı negatif olamaz.";
  if (isNegative(form.targetPairWeight)) return "Hedef çift gramajı negatif olamaz.";
  if (isNegative(form.standardCuringTimeSeconds)) return "Standart pişme süresi negatif olamaz.";
  if (isNegative(form.standardCycleTimeSeconds)) return "Standart çevrim süresi negatif olamaz.";
  const station = safeParsedNumber(form.currentStationNumber);
  if (form.currentStationNumber && (station < 1 || station > 24)) return "Mevcut istasyon 1 ile 24 arasında olmalıdır.";
  return null;
}

function toMoldRequest(form: MoldFormState) {
  return {
    productId: form.productId || null,
    code: form.code.trim(),
    name: form.name.trim(),
    customerName: form.customerName || null,
    productModel: form.productModel || null,
    modelCode: form.modelCode || null,
    size: form.size.trim(),
    sizeGroup: form.sizeGroup || null,
    description: form.description || null,
    moldType: form.moldType,
    cavityCount: safeParsedNumber(form.cavityCount),
    isRightLeftCombined: form.isRightLeftCombined,
    foamType: form.foamType,
    productType: form.productType,
    xCoordinate: nullableNumber(form.xCoordinate),
    yCoordinate: nullableNumber(form.yCoordinate),
    targetPairWeight: nullableNumber(form.targetPairWeight),
    minimumPairWeight: nullableNumber(form.minimumPairWeight),
    maximumPairWeight: nullableNumber(form.maximumPairWeight),
    targetDensity: nullableNumber(form.targetDensity),
    minimumDensity: nullableNumber(form.minimumDensity),
    maximumDensity: nullableNumber(form.maximumDensity),
    standardCuringTimeSeconds: nullableNumber(form.standardCuringTimeSeconds),
    standardMoldTemperature: nullableNumber(form.standardMoldTemperature),
    standardCycleTimeSeconds: nullableNumber(form.standardCycleTimeSeconds),
    releaseFrequencyCycles: nullableNumber(form.releaseFrequencyCycles),
    moldWeightKg: nullableNumber(form.moldWeightKg),
    machineName: form.machineName || null,
    compatibleMachineCode: form.compatibleMachineCode || null,
    currentStationNumber: nullableNumber(form.currentStationNumber),
    storageLocation: form.storageLocation || null,
    shelfCode: form.shelfCode || null,
    totalCycleCount: nullableNumber(form.totalCycleCount),
    totalProducedPairs: nullableNumber(form.totalProducedPairs),
    lastCleaningDate: toIsoOrNull(form.lastCleaningDate),
    nextCleaningDate: toIsoOrNull(form.nextCleaningDate),
    lastMaintenanceDate: toIsoOrNull(form.lastMaintenanceDate),
    nextMaintenanceDate: toIsoOrNull(form.nextMaintenanceDate),
    estimatedLifeCycles: nullableNumber(form.estimatedLifeCycles),
    photoPath: form.photoPath || null,
    technicalDocumentPath: form.technicalDocumentPath || null,
    cadFilePath: form.cadFilePath || null,
    qrCode: form.qrCode || null,
    barcode: form.barcode || null,
    ownerType: form.ownerType,
    ownerCustomerName: form.ownerCustomerName || null,
    isActive: form.isActive,
  };
}

function calculateLife(form: MoldFormState) {
  const total = safeParsedNumber(form.totalCycleCount);
  const estimated = safeParsedNumber(form.estimatedLifeCycles);
  if (estimated <= 0) return { remaining: null, percent: null, expired: false };
  const remaining = Math.max(estimated - total, 0);
  const percent = Math.min((total / estimated) * 100, 999);
  return { remaining, percent, expired: total >= estimated };
}

function getMoldSize(mold: Mold) {
  return mold.size || mold.sizeRange || "";
}

function getMoldType(mold: Mold) {
  return mold.moldType || "Pair";
}

function getOwnerType(mold: Mold) {
  return mold.ownerType || "Fixar";
}

function isLifeExpired(mold: Mold) {
  return safeNumber(mold.estimatedLifeCycles) > 0 && safeNumber(mold.totalCycleCount) >= safeNumber(mold.estimatedLifeCycles);
}

function getMaintenanceState(mold: Mold) {
  if (isLifeExpired(mold)) return "Ömrü Dolan";
  if (isDateWithinDays(mold.nextMaintenanceDate, 14)) return "Bakım Yaklaşan";
  if (isDateWithinDays(mold.nextCleaningDate, 7)) return "Temizlik Yaklaşan";
  return "Normal";
}

function isDateWithinDays(value: string | null | undefined, days: number) {
  if (!value) return false;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return false;
  const now = new Date();
  const limit = new Date();
  limit.setDate(now.getDate() + days);
  return date >= now && date <= limit;
}

function extractArray<T>(result: unknown): T[] {
  if (Array.isArray(result)) return result as T[];
  if (isRecord(result) && Array.isArray((result as ApiResponse<T[]>).data)) return (result as ApiResponse<T[]>).data || [];
  return [];
}

function extractOne<T>(result: unknown): T | null {
  if (isRecord(result) && isRecord((result as ApiResponse<T>).data)) return (result as ApiResponse<T>).data || null;
  return isRecord(result) ? result as T : null;
}

async function readError(response: Response, fallback: string) {
  try {
    const result = await response.json() as ApiResponse<unknown>;
    return result.message || result.errorCode || result.errors?.join(" ") || fallback;
  } catch {
    return fallback;
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function normalizeText(value: string | number | null | undefined) {
  return String(value || "").trim().toLocaleLowerCase("tr-TR");
}

function safeNumber(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function safeParsedNumber(value: string) {
  if (!value.trim()) return 0;
  const parsed = Number(value.replace(",", "."));
  return Number.isFinite(parsed) ? parsed : 0;
}

function nullableNumber(value: string) {
  if (!value.trim()) return null;
  const parsed = Number(value.replace(",", "."));
  return Number.isFinite(parsed) ? parsed : null;
}

function isNegative(value: string) {
  return value.trim() ? safeParsedNumber(value) < 0 : false;
}

function numberToString(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? String(value) : "";
}

function formatNumber(value: number | null | undefined) {
  if (typeof value !== "number" || !Number.isFinite(value)) return "-";
  return value.toLocaleString("tr-TR", { maximumFractionDigits: 2 });
}

function formatSeconds(value: number | null | undefined) {
  if (typeof value !== "number" || !Number.isFinite(value)) return "-";
  return `${value.toLocaleString("tr-TR")} sn`;
}

function dateToInput(value: string | null | undefined) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return formatDateInput(date);
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

function toIsoOrNull(value: string) {
  if (!value) return null;
  const date = new Date(`${value}T00:00:00`);
  if (Number.isNaN(date.getTime())) return null;
  return date.toISOString();
}
