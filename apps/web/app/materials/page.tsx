"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import { safeResponseJson } from "../lib/api/client";

type DashboardTone = "emerald" | "cyan" | "amber" | "red" | "blue" | "violet" | "zinc";
type DialogMode = "create" | "edit" | "detail" | null;
type MaterialTab = "general" | "stock" | "technical" | "typeDetails" | "safety";

type Material = {
  id: string;
  code?: string | null;
  name?: string | null;
  category?: string | null;
  subCategory?: string | null;
  description?: string | null;
  materialType?: string | null;
  unit?: string | null;
  defaultSupplierId?: string | null;
  defaultSupplierName?: string | null;
  currency?: string | null;
  lastPurchasePrice?: number | null;
  minimumStock?: number | null;
  maximumStock?: number | null;
  criticalStock?: number | null;
  warehouseName?: string | null;
  locationCode?: string | null;
  lotTrackingEnabled?: boolean | null;
  expiryTrackingEnabled?: boolean | null;
  technicalSpecification?: string | null;
  safetyInformation?: string | null;
  chemicalRole?: string | null;
  density?: number | null;
  mixingRatio?: number | null;
  containerWeight?: number | null;
  addedToPoliolBatch?: boolean | null;
  crosskimApplicationNote?: string | null;
  fabricType?: string | null;
  fabricWeightGsm?: number | null;
  fabricColor?: string | null;
  fabricWidth?: number | null;
  fabricRollLength?: number | null;
  adhesiveType?: string | null;
  customerName?: string | null;
  dtfCode?: string | null;
  dtfName?: string | null;
  dtfWidth?: number | null;
  dtfHeight?: number | null;
  applicationPosition?: string | null;
  applicationNote?: string | null;
  packagingType?: string | null;
  boxPairCapacity?: number | null;
  boxDimensions?: string | null;
  boxWeight?: number | null;
  isActive?: boolean | null;
  createdAt?: string | null;
  updatedAt?: string | null;
};

type MaterialExtra = {
  supplierCode: string;
  barcode: string;
  qrCode: string;
  technicalNote: string;
  msdsFileName: string;
  technicalPdfFileName: string;
  labAnalysisNote: string;
  storageInstruction: string;
  usageInstruction: string;
  qualityNote: string;
  generalNote: string;
  priceHistoryNote: string;
  purchaseHistoryNote: string;
  usageHistoryNote: string;
  recipeUsageNote: string;
  alternativeMaterialsNote: string;
  aiPurchaseSuggestionNote: string;
  auxiliaryDescription: string;
  auxiliaryUsageNote: string;
};

type MaterialFormState = MaterialExtra & {
  code: string;
  name: string;
  category: string;
  subCategory: string;
  description: string;
  materialType: string;
  unit: string;
  defaultSupplierId: string;
  defaultSupplierName: string;
  currency: string;
  lastPurchasePrice: string;
  minimumStock: string;
  maximumStock: string;
  criticalStock: string;
  warehouseName: string;
  locationCode: string;
  lotTrackingEnabled: boolean;
  expiryTrackingEnabled: boolean;
  technicalSpecification: string;
  safetyInformation: string;
  chemicalRole: string;
  density: string;
  mixingRatio: string;
  containerWeight: string;
  addedToPoliolBatch: boolean;
  crosskimApplicationNote: string;
  fabricType: string;
  fabricWeightGsm: string;
  fabricColor: string;
  fabricWidth: string;
  fabricRollLength: string;
  adhesiveType: string;
  customerName: string;
  dtfCode: string;
  dtfName: string;
  dtfWidth: string;
  dtfHeight: string;
  applicationPosition: string;
  applicationNote: string;
  packagingType: string;
  boxPairCapacity: string;
  boxDimensions: string;
  boxWeight: string;
  isActive: boolean;
};

type ApiResponse<T> = {
  data?: T;
  message?: string;
  errors?: string[];
  errorCode?: string;
  success?: boolean;
};

const API = "/api/backend/api/v1";
const EXTRA_MARKER = "\n\n---FIXAR_MATERIAL_MASTER_JSON---\n";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60 disabled:cursor-not-allowed disabled:opacity-70";
const MATERIAL_TYPES = ["Chemical", "Fabric", "Adhesive", "DTF", "Packaging", "Auxiliary"];
const UNITS = ["kg", "gr", "metre", "m2", "adet", "çift", "litre"];
const CHEMICAL_ROLES = ["Poliol", "Izosiyanat", "Crosskim", "Pigment", "Solvent", "Kalıp Ayırıcı", "Diğer"];
const FABRIC_TYPES = ["Interlok", "Lacoste", "Mesh", "Keçe", "Diğer"];
const ADHESIVE_TYPES = ["Normal", "Polibond", "Diğer"];
const PACKAGING_TYPES = ["Koli", "Poşet", "Etiket", "Shrink", "Diğer"];
const TABS: Array<{ id: MaterialTab; label: string }> = [
  { id: "general", label: "Genel Bilgiler" },
  { id: "stock", label: "Stok ve Tedarik" },
  { id: "technical", label: "Teknik Bilgiler" },
  { id: "typeDetails", label: "Malzeme Tipi Detayları" },
  { id: "safety", label: "Güvenlik ve Açıklamalar" },
];

const emptyExtra: MaterialExtra = {
  supplierCode: "",
  barcode: "",
  qrCode: "",
  technicalNote: "",
  msdsFileName: "",
  technicalPdfFileName: "",
  labAnalysisNote: "",
  storageInstruction: "",
  usageInstruction: "",
  qualityNote: "",
  generalNote: "",
  priceHistoryNote: "",
  purchaseHistoryNote: "",
  usageHistoryNote: "",
  recipeUsageNote: "",
  alternativeMaterialsNote: "",
  aiPurchaseSuggestionNote: "",
  auxiliaryDescription: "",
  auxiliaryUsageNote: "",
};

const emptyForm: MaterialFormState = {
  code: "",
  name: "",
  category: "",
  subCategory: "",
  description: "",
  materialType: "Chemical",
  unit: "kg",
  defaultSupplierId: "",
  defaultSupplierName: "",
  currency: "TRY",
  lastPurchasePrice: "",
  minimumStock: "",
  maximumStock: "",
  criticalStock: "",
  warehouseName: "",
  locationCode: "",
  lotTrackingEnabled: false,
  expiryTrackingEnabled: false,
  technicalSpecification: "",
  safetyInformation: "",
  chemicalRole: "Poliol",
  density: "",
  mixingRatio: "",
  containerWeight: "",
  addedToPoliolBatch: false,
  crosskimApplicationNote: "",
  fabricType: "Interlok",
  fabricWeightGsm: "",
  fabricColor: "",
  fabricWidth: "",
  fabricRollLength: "",
  adhesiveType: "Normal",
  customerName: "",
  dtfCode: "",
  dtfName: "",
  dtfWidth: "",
  dtfHeight: "",
  applicationPosition: "",
  applicationNote: "",
  packagingType: "Koli",
  boxPairCapacity: "",
  boxDimensions: "",
  boxWeight: "",
  isActive: true,
  ...emptyExtra,
};

export default function MaterialsPage() {
  const [materials, setMaterials] = useState<Material[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState("Tümü");
  const [statusFilter, setStatusFilter] = useState("Tümü");
  const [stockFilter, setStockFilter] = useState("Tümü");
  const [lotFilter, setLotFilter] = useState("Tümü");
  const [dialogMode, setDialogMode] = useState<DialogMode>(null);
  const [dialogMaterial, setDialogMaterial] = useState<Material | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);

  useEffect(() => {
    loadMaterials();
  }, []);

  async function loadMaterials() {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(API + "/materials");

      if (!response.ok) {
        throw new Error("Malzeme listesi alınamadı.");
      }

      const result: unknown = await safeResponseJson(response);
      setMaterials(extractMaterials(result));
    } catch (err) {
      setMaterials([]);
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  async function openDialog(mode: DialogMode, material: Material | null = null) {
    setSuccessMessage(null);
    setDetailError(null);

    if (mode === "create" || !material) {
      setDialogMaterial(null);
      setDialogMode(mode);
      return;
    }

    setDialogMaterial(material);
    setDialogMode(mode);
    setDetailLoading(true);

    try {
      const response = await fetch(`${API}/materials/${material.id}`);

      if (!response.ok) {
        throw new Error("Malzeme detayı alınamadı.");
      }

      const result: unknown = await safeResponseJson(response);
      setDialogMaterial(extractMaterial(result) ?? material);
    } catch (err) {
      setDetailError(err instanceof Error ? err.message : "Malzeme detayı yüklenemedi.");
    } finally {
      setDetailLoading(false);
    }
  }

  function closeDialog() {
    setDialogMode(null);
    setDialogMaterial(null);
    setDetailError(null);
    setDetailLoading(false);
  }

  function handleSaved(message: string) {
    closeDialog();
    setSuccessMessage(message);
    loadMaterials();
  }

  const filteredMaterials = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase("tr-TR");

    return materials.filter((material) => {
      const extras = parseExtra(material.technicalSpecification).extra;
      const matchesSearch =
        !normalizedSearch ||
        [
          material.code,
          material.name,
          material.category,
          material.subCategory,
          material.defaultSupplierName,
          extras.supplierCode,
          material.warehouseName,
          material.locationCode,
        ]
          .filter(Boolean)
          .some((value) => String(value).toLocaleLowerCase("tr-TR").includes(normalizedSearch));

      const matchesType = typeFilter === "Tümü" || material.materialType === typeFilter;
      const matchesStatus =
        statusFilter === "Tümü" ||
        (statusFilter === "Aktif" && material.isActive !== false) ||
        (statusFilter === "Pasif" && material.isActive === false);
      const matchesStock =
        stockFilter === "Tümü" ||
        (stockFilter === "Kritik" && isCriticalMaterial(material)) ||
        (stockFilter === "Normal" && !isCriticalMaterial(material));
      const matchesLot =
        lotFilter === "Tümü" ||
        (lotFilter === "Var" && material.lotTrackingEnabled === true) ||
        (lotFilter === "Yok" && material.lotTrackingEnabled !== true);

      return matchesSearch && matchesType && matchesStatus && matchesStock && matchesLot;
    });
  }, [lotFilter, materials, search, statusFilter, stockFilter, typeFilter]);

  const activeCount = materials.filter((material) => material.isActive !== false).length;
  const dashboardCards = [
    { title: "Toplam Malzeme", value: materials.length.toLocaleString("tr-TR"), note: "Ana kart sayısı", tone: "emerald" as DashboardTone },
    { title: "Aktif Malzeme", value: activeCount.toLocaleString("tr-TR"), note: "Kullanıma açık", tone: "cyan" as DashboardTone },
    { title: "Kimyasal", value: countByType(materials, "Chemical"), note: "Poliol, izosiyanat ve katkılar", tone: "amber" as DashboardTone },
    { title: "Kumaş", value: countByType(materials, "Fabric"), note: "Interlok, lacoste, mesh", tone: "blue" as DashboardTone },
    { title: "Yapışkan", value: countByType(materials, "Adhesive"), note: "Normal ve Polibond", tone: "violet" as DashboardTone },
    { title: "DTF", value: countByType(materials, "DTF"), note: "Sıcak transfer logo", tone: "cyan" as DashboardTone },
    { title: "Ambalaj", value: countByType(materials, "Packaging"), note: "Koli, poşet, shrink", tone: "zinc" as DashboardTone },
    { title: "Kritik Stok", value: materials.filter(isCriticalMaterial).length.toLocaleString("tr-TR"), note: "Kritik seviyesi tanımlı", tone: "red" as DashboardTone },
  ];

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.18),transparent_34%),radial-gradient(circle_at_bottom_right,rgba(14,165,233,0.13),transparent_32%)] px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.38em] text-emerald-300">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">Hammadde ve Malzeme Master</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                Satın alma, stok, reçete, maliyet, üretim ve kalite modüllerinin kullanacağı tekil malzeme ana kartlarını yönetin.
              </p>
            </div>

            <div className="flex flex-col gap-3 sm:flex-row">
              <button
                onClick={() => {
                  setSuccessMessage(null);
                  loadMaterials();
                }}
                disabled={loading}
                className="rounded-xl border border-white/10 bg-white/[0.08] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:opacity-50"
              >
                {loading ? "Yenileniyor..." : "Yenile"}
              </button>
              <button onClick={() => openDialog("create")} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
                + Yeni Malzeme
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
            <div className="flex flex-col gap-4 border-b border-white/10 pb-5">
              <div className="flex flex-col gap-3 xl:flex-row xl:items-end xl:justify-between">
                <div>
                  <h2 className="text-2xl font-black">Malzeme Listesi</h2>
                  <p className="mt-1 text-sm text-zinc-400">
                    {filteredMaterials.length.toLocaleString("tr-TR")} malzeme listeleniyor.
                  </p>
                </div>
                <div className="w-full xl:max-w-lg">
                  <Field label="Arama">
                    <input
                      value={search}
                      onChange={(event) => setSearch(event.target.value)}
                      className={CONTROL_CLASS}
                      placeholder="Kod, ad, kategori, tedarikçi, depo, raf"
                    />
                  </Field>
                </div>
              </div>

              <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <Field label="Malzeme Tipi">
                  <select value={typeFilter} onChange={(event) => setTypeFilter(event.target.value)} className={CONTROL_CLASS}>
                    <option>Tümü</option>
                    {MATERIAL_TYPES.map((type) => (
                      <option key={type}>{type}</option>
                    ))}
                  </select>
                </Field>
                <Field label="Aktif / Pasif">
                  <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} className={CONTROL_CLASS}>
                    <option>Tümü</option>
                    <option>Aktif</option>
                    <option>Pasif</option>
                  </select>
                </Field>
                <Field label="Kritik / Normal">
                  <select value={stockFilter} onChange={(event) => setStockFilter(event.target.value)} className={CONTROL_CLASS}>
                    <option>Tümü</option>
                    <option>Kritik</option>
                    <option>Normal</option>
                  </select>
                </Field>
                <Field label="Lot Takibi">
                  <select value={lotFilter} onChange={(event) => setLotFilter(event.target.value)} className={CONTROL_CLASS}>
                    <option>Tümü</option>
                    <option>Var</option>
                    <option>Yok</option>
                  </select>
                </Field>
              </div>
            </div>

            {loading && <LoadingState />}

            {!loading && error && (
              <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-5 text-sm text-red-100">
                <p className="font-black">Malzeme listesi yüklenemedi.</p>
                <p className="mt-1 text-red-200">{error}</p>
              </div>
            )}

            {!loading && !error && filteredMaterials.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-zinc-300">
                {materials.length === 0 ? "Henüz malzeme kaydı bulunmuyor." : "Filtreye uygun malzeme kaydı bulunamadı."}
              </div>
            )}

            {!loading && !error && filteredMaterials.length > 0 && (
              <div className="mt-5 overflow-x-auto">
                <table className="min-w-[1320px] w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-white/10 text-xs uppercase tracking-[0.18em] text-zinc-500">
                      <th className="py-3 pr-4">Kod</th>
                      <th className="py-3 pr-4">Malzeme Adı</th>
                      <th className="py-3 pr-4">Tip</th>
                      <th className="py-3 pr-4">Kategori</th>
                      <th className="py-3 pr-4">Birim</th>
                      <th className="py-3 pr-4">Son Alış Fiyatı</th>
                      <th className="py-3 pr-4">Para Birimi</th>
                      <th className="py-3 pr-4">Minimum Stok</th>
                      <th className="py-3 pr-4">Kritik Stok</th>
                      <th className="py-3 pr-4">Maksimum Stok</th>
                      <th className="py-3 pr-4">Tedarikçi</th>
                      <th className="py-3 pr-4">Depo / Raf</th>
                      <th className="py-3 pr-4">Durum</th>
                      <th className="py-3 text-right">İşlemler</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/10">
                    {filteredMaterials.map((material) => (
                      <tr key={material.id} className="align-middle text-zinc-200 transition hover:bg-white/[0.04]">
                        <td className="py-4 pr-4 font-mono text-xs text-emerald-200">{material.code || "-"}</td>
                        <td className="py-4 pr-4">
                          <p className="font-black text-white">{material.name || "-"}</p>
                          <p className="mt-1 text-xs text-zinc-500">{material.subCategory || material.description || "-"}</p>
                        </td>
                        <td className="py-4 pr-4">
                          <Badge>{material.materialType || "-"}</Badge>
                        </td>
                        <td className="py-4 pr-4">{material.category || "-"}</td>
                        <td className="py-4 pr-4">{material.unit || "-"}</td>
                        <td className="py-4 pr-4">{formatNumber(material.lastPurchasePrice)}</td>
                        <td className="py-4 pr-4">{material.currency || "-"}</td>
                        <td className="py-4 pr-4">{formatNumber(material.minimumStock)}</td>
                        <td className="py-4 pr-4">{formatNumber(material.criticalStock)}</td>
                        <td className="py-4 pr-4">{formatNumber(material.maximumStock)}</td>
                        <td className="py-4 pr-4">{material.defaultSupplierName || "-"}</td>
                        <td className="py-4 pr-4">{[material.warehouseName, material.locationCode].filter(Boolean).join(" / ") || "-"}</td>
                        <td className="py-4 pr-4">
                          <StatusBadge active={material.isActive !== false} />
                        </td>
                        <td className="py-4">
                          <div className="flex justify-end gap-2">
                            <a href={`/material-lots?materialId=${material.id}`} className="rounded-lg border border-amber-400/30 bg-amber-400/10 px-3 py-2 text-xs font-black text-amber-100">Lotlar</a>
                            <a href={`/material-containers?materialId=${material.id}`} className="rounded-lg border border-violet-400/30 bg-violet-400/10 px-3 py-2 text-xs font-black text-violet-100">Containerlar</a>
                            <button
                              onClick={() => openDialog("detail", material)}
                              className="rounded-lg border border-cyan-400/30 bg-cyan-400/10 px-3 py-2 text-xs font-black text-cyan-100 transition hover:bg-cyan-400/20"
                            >
                              Detay
                            </button>
                            <button
                              onClick={() => openDialog("edit", material)}
                              className="rounded-lg border border-emerald-400/30 bg-emerald-400/10 px-3 py-2 text-xs font-black text-emerald-100 transition hover:bg-emerald-400/20"
                            >
                              Düzenle
                            </button>
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
        <MaterialModal
          mode={dialogMode}
          material={dialogMaterial}
          loading={detailLoading}
          error={detailError}
          onClose={closeDialog}
          onSaved={handleSaved}
        />
      )}
    </main>
  );
}

function MaterialModal({
  mode,
  material,
  loading,
  error,
  onClose,
  onSaved,
}: {
  mode: DialogMode;
  material: Material | null;
  loading: boolean;
  error: string | null;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const [activeTab, setActiveTab] = useState<MaterialTab>("general");
  const [form, setForm] = useState<MaterialFormState>(() => toFormState(material));
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const readonly = mode === "detail";
  const isEdit = mode === "edit";

  useEffect(() => {
    setForm(toFormState(material));
    setActiveTab("general");
    setFormError(null);
  }, [material]);

  function updateField<K extends keyof MaterialFormState>(key: K, value: MaterialFormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function handleSubmit() {
    setFormError(null);

    if (!form.code.trim()) {
      setFormError("Malzeme kodu zorunludur.");
      setActiveTab("general");
      return;
    }

    if (!form.name.trim()) {
      setFormError("Malzeme adı zorunludur.");
      setActiveTab("general");
      return;
    }

    if (!form.materialType.trim() || !form.unit.trim()) {
      setFormError("Malzeme tipi ve birim zorunludur.");
      setActiveTab("general");
      return;
    }

    setSaving(true);

    try {
      const response = await fetch(isEdit && material ? `${API}/materials/${material.id}` : API + "/materials", {
        method: isEdit ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(toRequestBody(form)),
      });
      const result: ApiResponse<unknown> = await safeResponseJson(response).catch(() => ({}));

      if (!response.ok) {
        throw new Error(extractErrorMessage(result) || "Malzeme kaydedilemedi.");
      }

      onSaved(isEdit ? "Malzeme güncellendi." : "Malzeme oluşturuldu.");
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
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
              <p className="text-xs font-black tracking-[0.34em] text-emerald-300">MATERIAL MASTER</p>
              <h2 className="mt-2 text-2xl font-black text-white">
                {readonly ? "Malzeme Detayı" : isEdit ? "Malzeme Kartı Düzenle" : "Yeni Malzeme Kartı"}
              </h2>
              <p className="mt-1 text-sm text-zinc-400">
                Tekil ana kart mantığıyla satın alma, stok, reçete, maliyet, üretim ve kalite verilerini merkezileştirin.
              </p>
            </div>
            <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white transition hover:bg-white/[0.12]">
              Kapat
            </button>
          </div>
          <MaterialSummary form={form} />
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
          {loading && <LoadingState compact />}

          {error && (
            <div className="mb-5 rounded-xl border border-red-400/30 bg-red-500/10 p-4 text-sm text-red-100">
              {error}
            </div>
          )}

          {formError && (
            <div className="mb-5 rounded-xl border border-red-400/30 bg-red-500/10 p-4 text-sm font-bold text-red-100">
              {formError}
            </div>
          )}

          {activeTab === "general" && <GeneralTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "stock" && <StockSupplyTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "technical" && <TechnicalTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "typeDetails" && <TypeDetailsTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "safety" && <SafetyTab form={form} readonly={readonly} updateField={updateField} />}
        </div>

        <div className="flex flex-col gap-3 border-t border-white/10 bg-black/30 p-5 sm:flex-row sm:justify-end">
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.12]">
            {readonly ? "Kapat" : "Vazgeç"}
          </button>
          {!readonly && (
            <button
              onClick={handleSubmit}
              disabled={saving}
              className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {saving ? "Kaydediliyor..." : "Kaydet"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function MaterialSummary({ form }: { form: MaterialFormState }) {
  const items = [
    ["Kod", form.code || "-"],
    ["Malzeme", form.name || "-"],
    ["Tip", form.materialType || "-"],
    ["Birim", form.unit || "-"],
    ["Tedarikçi", form.defaultSupplierName || "-"],
    ["Depo / Raf", [form.warehouseName, form.locationCode].filter(Boolean).join(" / ") || "-"],
    ["Lot", form.lotTrackingEnabled ? "Var" : "Yok"],
    ["Durum", form.isActive ? "Aktif" : "Pasif"],
  ];

  return (
    <div className="mt-5 grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
      {items.map(([label, value]) => (
        <div key={label} className="rounded-xl border border-white/10 bg-black/30 px-3 py-2">
          <p className="text-[10px] font-black uppercase tracking-[0.18em] text-zinc-500">{label}</p>
          <p className="mt-1 truncate text-sm font-black text-white" title={value}>
            {value}
          </p>
        </div>
      ))}
    </div>
  );
}

function GeneralTab({
  form,
  readonly,
  updateField,
}: {
  form: MaterialFormState;
  readonly: boolean;
  updateField: <K extends keyof MaterialFormState>(key: K, value: MaterialFormState[K]) => void;
}) {
  return (
    <TabPanel title="Genel Bilgiler" note="Malzemenin tüm modüllerde kullanılacak ana kimliği.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Malzeme Kodu" value={form.code} readonly={readonly} onChange={(value) => updateField("code", value)} required />
        <TextInput label="Malzeme Adı" value={form.name} readonly={readonly} onChange={(value) => updateField("name", value)} required />
        <TextInput label="Kategori" value={form.category} readonly={readonly} onChange={(value) => updateField("category", value)} />
        <TextInput label="Alt Kategori" value={form.subCategory} readonly={readonly} onChange={(value) => updateField("subCategory", value)} />
        <SelectInput label="Malzeme Tipi" value={form.materialType} readonly={readonly} options={MATERIAL_TYPES} onChange={(value) => updateField("materialType", value)} />
        <SelectInput label="Birim" value={form.unit} readonly={readonly} options={UNITS} onChange={(value) => updateField("unit", value)} />
        <ToggleInput label="Aktif / Pasif" checked={form.isActive} readonly={readonly} trueText="Aktif" falseText="Pasif" onChange={(value) => updateField("isActive", value)} />
      </div>
      <TextAreaInput label="Açıklama" value={form.description} readonly={readonly} onChange={(value) => updateField("description", value)} />
    </TabPanel>
  );
}

function StockSupplyTab({
  form,
  readonly,
  updateField,
}: {
  form: MaterialFormState;
  readonly: boolean;
  updateField: <K extends keyof MaterialFormState>(key: K, value: MaterialFormState[K]) => void;
}) {
  return (
    <TabPanel title="Stok ve Tedarik" note="Tedarikçi, fiyat, stok eşikleri ve izlenebilirlik bilgileri.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Varsayılan Tedarikçi" value={form.defaultSupplierName} readonly={readonly} onChange={(value) => updateField("defaultSupplierName", value)} />
        <TextInput label="Tedarikçi Kodu" value={form.supplierCode} readonly={readonly} onChange={(value) => updateField("supplierCode", value)} />
        <SelectInput label="Para Birimi" value={form.currency} readonly={readonly} options={["TRY", "USD", "EUR"]} onChange={(value) => updateField("currency", value)} />
        <TextInput label="Son Alış Fiyatı" value={form.lastPurchasePrice} readonly={readonly} type="number" onChange={(value) => updateField("lastPurchasePrice", value)} />
        <TextInput label="Minimum Stok" value={form.minimumStock} readonly={readonly} type="number" onChange={(value) => updateField("minimumStock", value)} />
        <TextInput label="Kritik Stok" value={form.criticalStock} readonly={readonly} type="number" onChange={(value) => updateField("criticalStock", value)} />
        <TextInput label="Maksimum Stok" value={form.maximumStock} readonly={readonly} type="number" onChange={(value) => updateField("maximumStock", value)} />
        <TextInput label="Depo" value={form.warehouseName} readonly={readonly} onChange={(value) => updateField("warehouseName", value)} />
        <TextInput label="Raf / Lokasyon" value={form.locationCode} readonly={readonly} onChange={(value) => updateField("locationCode", value)} />
        <ToggleInput label="Lot Takibi" checked={form.lotTrackingEnabled} readonly={readonly} trueText="Var" falseText="Yok" onChange={(value) => updateField("lotTrackingEnabled", value)} />
        <ToggleInput label="Son Kullanma Tarihi Takibi" checked={form.expiryTrackingEnabled} readonly={readonly} trueText="Var" falseText="Yok" onChange={(value) => updateField("expiryTrackingEnabled", value)} />
      </div>
    </TabPanel>
  );
}

function TechnicalTab({
  form,
  readonly,
  updateField,
}: {
  form: MaterialFormState;
  readonly: boolean;
  updateField: <K extends keyof MaterialFormState>(key: K, value: MaterialFormState[K]) => void;
}) {
  return (
    <TabPanel title="Teknik Bilgiler" note="Reçete, üretim ve kalite süreçlerinde kullanılacak ortak teknik veriler.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <TextInput label="Yoğunluk" value={form.density} readonly={readonly} type="number" onChange={(value) => updateField("density", value)} />
        <TextInput label="Karışım Oranı" value={form.mixingRatio} readonly={readonly} type="number" onChange={(value) => updateField("mixingRatio", value)} />
        <TextInput label="Ambalaj / Varil Ağırlığı" value={form.containerWeight} readonly={readonly} type="number" onChange={(value) => updateField("containerWeight", value)} />
      </div>
      <TextAreaInput label="Teknik Spesifikasyon" value={form.technicalSpecification} readonly={readonly} onChange={(value) => updateField("technicalSpecification", value)} />
      <TextAreaInput label="Teknik Not" value={form.technicalNote} readonly={readonly} onChange={(value) => updateField("technicalNote", value)} />
      <details className="rounded-2xl border border-white/10 bg-black/20 p-4">
        <summary className="cursor-pointer text-sm font-black text-emerald-200">Doküman ve laboratuvar altyapısı</summary>
        <div className="mt-4 grid gap-4 md:grid-cols-2">
          <TextInput label="MSDS / Güvenlik PDF" value={form.msdsFileName} readonly={readonly} onChange={(value) => updateField("msdsFileName", value)} />
          <TextInput label="Teknik PDF" value={form.technicalPdfFileName} readonly={readonly} onChange={(value) => updateField("technicalPdfFileName", value)} />
        </div>
        <TextAreaInput label="Laboratuvar Analizleri" value={form.labAnalysisNote} readonly={readonly} onChange={(value) => updateField("labAnalysisNote", value)} />
      </details>
    </TabPanel>
  );
}

function TypeDetailsTab({
  form,
  readonly,
  updateField,
}: {
  form: MaterialFormState;
  readonly: boolean;
  updateField: <K extends keyof MaterialFormState>(key: K, value: MaterialFormState[K]) => void;
}) {
  return (
    <TabPanel title="Malzeme Tipi Detayları" note="Sadece seçilen malzeme tipine ait uzmanlık alanları gösterilir.">
      {form.materialType === "Chemical" && <ChemicalFields form={form} readonly={readonly} updateField={updateField} />}
      {form.materialType === "Fabric" && <FabricFields form={form} readonly={readonly} updateField={updateField} />}
      {form.materialType === "Adhesive" && <AdhesiveFields form={form} readonly={readonly} updateField={updateField} />}
      {form.materialType === "DTF" && <DtfFields form={form} readonly={readonly} updateField={updateField} />}
      {form.materialType === "Packaging" && <PackagingFields form={form} readonly={readonly} updateField={updateField} />}
      {form.materialType === "Auxiliary" && <AuxiliaryFields form={form} readonly={readonly} updateField={updateField} />}
    </TabPanel>
  );
}

function ChemicalFields({ form, readonly, updateField }: FieldGroupProps) {
  return (
    <div className="space-y-4">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <SelectInput label="Chemical Role" value={form.chemicalRole} readonly={readonly} options={CHEMICAL_ROLES} onChange={(value) => updateField("chemicalRole", value)} />
        <TextInput label="Yoğunluk" value={form.density} readonly={readonly} type="number" onChange={(value) => updateField("density", value)} />
        <TextInput label="Karışım Oranı" value={form.mixingRatio} readonly={readonly} type="number" onChange={(value) => updateField("mixingRatio", value)} />
        <TextInput label="Ambalaj / Varil Ağırlığı" value={form.containerWeight} readonly={readonly} type="number" onChange={(value) => updateField("containerWeight", value)} />
        <ToggleInput label="Poliol Kazanına Eklenir mi" checked={form.addedToPoliolBatch} readonly={readonly} trueText="Evet" falseText="Hayır" onChange={(value) => updateField("addedToPoliolBatch", value)} />
      </div>
      {form.chemicalRole === "Crosskim" && (
        <div className="rounded-xl border border-amber-400/30 bg-amber-500/10 p-4 text-sm text-amber-100">
          Crosskim doğrudan makineye verilmez. 180 kg Poliol kazanına ilave edilen katkıdır.
        </div>
      )}
      <TextAreaInput label="Crosskim Uygulama Notu" value={form.crosskimApplicationNote} readonly={readonly} onChange={(value) => updateField("crosskimApplicationNote", value)} />
    </div>
  );
}

function FabricFields({ form, readonly, updateField }: FieldGroupProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
      <SelectInput label="Fabric Type" value={form.fabricType} readonly={readonly} options={FABRIC_TYPES} onChange={(value) => updateField("fabricType", value)} />
      <TextInput label="Kumaş Gramajı (gsm)" value={form.fabricWeightGsm} readonly={readonly} type="number" onChange={(value) => updateField("fabricWeightGsm", value)} />
      <TextInput label="Renk" value={form.fabricColor} readonly={readonly} onChange={(value) => updateField("fabricColor", value)} />
      <TextInput label="En" value={form.fabricWidth} readonly={readonly} type="number" onChange={(value) => updateField("fabricWidth", value)} />
      <TextInput label="Rulo Uzunluğu" value={form.fabricRollLength} readonly={readonly} type="number" onChange={(value) => updateField("fabricRollLength", value)} />
    </div>
  );
}

function AdhesiveFields({ form, readonly, updateField }: FieldGroupProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      <SelectInput label="Adhesive Type" value={form.adhesiveType} readonly={readonly} options={ADHESIVE_TYPES} onChange={(value) => updateField("adhesiveType", value)} />
    </div>
  );
}

function DtfFields({ form, readonly, updateField }: FieldGroupProps) {
  return (
    <div className="space-y-4">
      <div className="rounded-xl border border-cyan-400/30 bg-cyan-500/10 p-4 text-sm text-cyan-100">
        DTF bir ambalaj etiketi değildir. Kumaş üzerine sıcak transfer ile uygulanan müşteri logosudur.
      </div>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Müşteri" value={form.customerName} readonly={readonly} onChange={(value) => updateField("customerName", value)} />
        <TextInput label="DTF Kodu" value={form.dtfCode} readonly={readonly} onChange={(value) => updateField("dtfCode", value)} />
        <TextInput label="DTF Adı" value={form.dtfName} readonly={readonly} onChange={(value) => updateField("dtfName", value)} />
        <TextInput label="Genişlik" value={form.dtfWidth} readonly={readonly} type="number" onChange={(value) => updateField("dtfWidth", value)} />
        <TextInput label="Yükseklik" value={form.dtfHeight} readonly={readonly} type="number" onChange={(value) => updateField("dtfHeight", value)} />
        <TextInput label="Uygulama Konumu" value={form.applicationPosition} readonly={readonly} onChange={(value) => updateField("applicationPosition", value)} />
      </div>
      <TextAreaInput label="Uygulama Notu" value={form.applicationNote} readonly={readonly} onChange={(value) => updateField("applicationNote", value)} />
    </div>
  );
}

function PackagingFields({ form, readonly, updateField }: FieldGroupProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <SelectInput label="Packaging Type" value={form.packagingType} readonly={readonly} options={PACKAGING_TYPES} onChange={(value) => updateField("packagingType", value)} />
      <TextInput label="Koli Çift Kapasitesi" value={form.boxPairCapacity} readonly={readonly} type="number" onChange={(value) => updateField("boxPairCapacity", value)} />
      <TextInput label="Koli Ölçüsü" value={form.boxDimensions} readonly={readonly} onChange={(value) => updateField("boxDimensions", value)} />
      <TextInput label="Koli Ağırlığı" value={form.boxWeight} readonly={readonly} type="number" onChange={(value) => updateField("boxWeight", value)} />
    </div>
  );
}

function AuxiliaryFields({ form, readonly, updateField }: FieldGroupProps) {
  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <TextAreaInput label="Yardımcı Malzeme Açıklaması" value={form.auxiliaryDescription} readonly={readonly} onChange={(value) => updateField("auxiliaryDescription", value)} />
      <TextAreaInput label="Kullanım Notu" value={form.auxiliaryUsageNote} readonly={readonly} onChange={(value) => updateField("auxiliaryUsageNote", value)} />
    </div>
  );
}

function SafetyTab({
  form,
  readonly,
  updateField,
}: {
  form: MaterialFormState;
  readonly: boolean;
  updateField: <K extends keyof MaterialFormState>(key: K, value: MaterialFormState[K]) => void;
}) {
  return (
    <TabPanel title="Güvenlik ve Açıklamalar" note="Depolama, kullanım, kalite ve genel açıklama notları.">
      <div className="grid gap-4 lg:grid-cols-2">
        <TextAreaInput label="Güvenlik Bilgisi" value={form.safetyInformation} readonly={readonly} onChange={(value) => updateField("safetyInformation", value)} />
        <TextAreaInput label="Depolama Talimatı" value={form.storageInstruction} readonly={readonly} onChange={(value) => updateField("storageInstruction", value)} />
        <TextAreaInput label="Kullanım Talimatı" value={form.usageInstruction} readonly={readonly} onChange={(value) => updateField("usageInstruction", value)} />
        <TextAreaInput label="Kalite Notu" value={form.qualityNote} readonly={readonly} onChange={(value) => updateField("qualityNote", value)} />
      </div>
      <TextAreaInput label="Genel Not" value={form.generalNote} readonly={readonly} onChange={(value) => updateField("generalNote", value)} />
      <FutureIntegrationPanel form={form} readonly={readonly} updateField={updateField} />
    </TabPanel>
  );
}

function FutureIntegrationPanel({
  form,
  readonly,
  updateField,
}: {
  form: MaterialFormState;
  readonly: boolean;
  updateField: <K extends keyof MaterialFormState>(key: K, value: MaterialFormState[K]) => void;
}) {
  return (
    <details className="rounded-2xl border border-emerald-400/20 bg-emerald-500/10 p-4">
      <summary className="cursor-pointer text-sm font-black text-emerald-100">ERP bağlantıları ve gelecek modül altyapısı</summary>
      <p className="mt-3 text-sm text-emerald-100/80">
        Bu alanlar Product, Purchase Orders, Stocks, Production Orders, Recipes, Quality ve Cost Analysis modülleri bağlandığında aynı ana kart verisini kullanacak şekilde tutulur.
      </p>
      <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <TextInput label="Barkod" value={form.barcode} readonly={readonly} onChange={(value) => updateField("barcode", value)} />
        <TextInput label="QR" value={form.qrCode} readonly={readonly} onChange={(value) => updateField("qrCode", value)} />
        <TextAreaInput label="Fiyat Geçmişi Notu" value={form.priceHistoryNote} readonly={readonly} onChange={(value) => updateField("priceHistoryNote", value)} />
        <TextAreaInput label="Satın Alma Geçmişi Notu" value={form.purchaseHistoryNote} readonly={readonly} onChange={(value) => updateField("purchaseHistoryNote", value)} />
        <TextAreaInput label="Kullanım Geçmişi Notu" value={form.usageHistoryNote} readonly={readonly} onChange={(value) => updateField("usageHistoryNote", value)} />
        <TextAreaInput label="Kullanıldığı Ürün Reçeteleri" value={form.recipeUsageNote} readonly={readonly} onChange={(value) => updateField("recipeUsageNote", value)} />
        <TextAreaInput label="Alternatif Malzemeler" value={form.alternativeMaterialsNote} readonly={readonly} onChange={(value) => updateField("alternativeMaterialsNote", value)} />
        <TextAreaInput label="AI Satın Alma Önerisi Notu" value={form.aiPurchaseSuggestionNote} readonly={readonly} onChange={(value) => updateField("aiPurchaseSuggestionNote", value)} />
      </div>
    </details>
  );
}

type FieldGroupProps = {
  form: MaterialFormState;
  readonly: boolean;
  updateField: <K extends keyof MaterialFormState>(key: K, value: MaterialFormState[K]) => void;
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

function TextInput({
  label,
  value,
  readonly,
  onChange,
  type = "text",
  required = false,
}: {
  label: string;
  value: string;
  readonly: boolean;
  onChange: (value: string) => void;
  type?: string;
  required?: boolean;
}) {
  return (
    <Field label={required ? `${label} *` : label}>
      <input
        value={value}
        type={type}
        step={type === "number" ? "0.01" : undefined}
        readOnly={readonly}
        disabled={readonly}
        onChange={(event) => onChange(event.target.value)}
        className={CONTROL_CLASS}
        placeholder={readonly ? "-" : label}
      />
    </Field>
  );
}

function TextAreaInput({
  label,
  value,
  readonly,
  onChange,
}: {
  label: string;
  value: string;
  readonly: boolean;
  onChange: (value: string) => void;
}) {
  return (
    <Field label={label}>
      <textarea
        value={value}
        readOnly={readonly}
        disabled={readonly}
        rows={4}
        onChange={(event) => onChange(event.target.value)}
        className={`${CONTROL_CLASS} min-h-28 resize-y`}
        placeholder={readonly ? "-" : label}
      />
    </Field>
  );
}

function SelectInput({
  label,
  value,
  readonly,
  options,
  onChange,
}: {
  label: string;
  value: string;
  readonly: boolean;
  options: string[];
  onChange: (value: string) => void;
}) {
  return (
    <Field label={label}>
      <select value={value} disabled={readonly} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS}>
        {options.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
    </Field>
  );
}

function ToggleInput({
  label,
  checked,
  readonly,
  trueText,
  falseText,
  onChange,
}: {
  label: string;
  checked: boolean;
  readonly: boolean;
  trueText: string;
  falseText: string;
  onChange: (value: boolean) => void;
}) {
  return (
    <Field label={label}>
      <button
        type="button"
        disabled={readonly}
        onClick={() => onChange(!checked)}
        className={`flex w-full items-center justify-between rounded-xl border p-3 text-left text-sm font-black transition disabled:cursor-not-allowed ${
          checked ? "border-emerald-400/40 bg-emerald-500/15 text-emerald-100" : "border-white/10 bg-black/30 text-zinc-300"
        }`}
      >
        <span>{checked ? trueText : falseText}</span>
        <span className={`h-6 w-11 rounded-full p-1 transition ${checked ? "bg-emerald-400" : "bg-zinc-700"}`}>
          <span className={`block h-4 w-4 rounded-full bg-white transition ${checked ? "translate-x-5" : ""}`} />
        </span>
      </button>
    </Field>
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
      <p className="text-xs font-black uppercase tracking-[0.22em] opacity-80">{title}</p>
      <p className="mt-3 text-3xl font-black text-white">{value}</p>
      <p className="mt-2 text-sm opacity-80">{note}</p>
    </article>
  );
}

function LoadingState({ compact = false }: { compact?: boolean }) {
  return (
    <div className={`${compact ? "py-8" : "mt-5 py-12"} flex items-center justify-center text-sm font-bold text-zinc-400`}>
      <span className="mr-3 h-3 w-3 animate-pulse rounded-full bg-emerald-400" />
      Yükleniyor...
    </div>
  );
}

function Badge({ children }: { children: ReactNode }) {
  return <span className="rounded-full border border-white/10 bg-white/[0.06] px-3 py-1 text-xs font-black text-zinc-200">{children}</span>;
}

function StatusBadge({ active }: { active: boolean }) {
  return (
    <span className={`rounded-full px-3 py-1 text-xs font-black ${active ? "bg-emerald-500/15 text-emerald-200" : "bg-red-500/15 text-red-200"}`}>
      {active ? "Aktif" : "Pasif"}
    </span>
  );
}

function toFormState(material: Material | null): MaterialFormState {
  if (!material) return { ...emptyForm };

  const parsedTechnical = parseExtra(material.technicalSpecification);
  const parsedSafety = parseExtra(material.safetyInformation);
  const extra = { ...emptyExtra, ...parsedTechnical.extra, ...parsedSafety.extra };

  return {
    ...emptyForm,
    ...extra,
    code: material.code ?? "",
    name: material.name ?? "",
    category: material.category ?? "",
    subCategory: material.subCategory ?? "",
    description: material.description ?? "",
    materialType: normalizeOption(material.materialType, MATERIAL_TYPES, "Chemical"),
    unit: normalizeOption(material.unit, UNITS, "kg"),
    defaultSupplierId: material.defaultSupplierId ?? "",
    defaultSupplierName: material.defaultSupplierName ?? "",
    currency: material.currency ?? "TRY",
    lastPurchasePrice: toInputNumber(material.lastPurchasePrice),
    minimumStock: toInputNumber(material.minimumStock),
    maximumStock: toInputNumber(material.maximumStock),
    criticalStock: toInputNumber(material.criticalStock),
    warehouseName: material.warehouseName ?? "",
    locationCode: material.locationCode ?? "",
    lotTrackingEnabled: Boolean(material.lotTrackingEnabled),
    expiryTrackingEnabled: Boolean(material.expiryTrackingEnabled),
    technicalSpecification: parsedTechnical.visible,
    safetyInformation: parsedSafety.visible,
    chemicalRole: normalizeOption(material.chemicalRole, CHEMICAL_ROLES, "Poliol"),
    density: toInputNumber(material.density),
    mixingRatio: toInputNumber(material.mixingRatio),
    containerWeight: toInputNumber(material.containerWeight),
    addedToPoliolBatch: Boolean(material.addedToPoliolBatch),
    crosskimApplicationNote: material.crosskimApplicationNote ?? "",
    fabricType: normalizeOption(material.fabricType, FABRIC_TYPES, "Interlok"),
    fabricWeightGsm: toInputNumber(material.fabricWeightGsm),
    fabricColor: material.fabricColor ?? "",
    fabricWidth: toInputNumber(material.fabricWidth),
    fabricRollLength: toInputNumber(material.fabricRollLength),
    adhesiveType: normalizeOption(material.adhesiveType, ADHESIVE_TYPES, "Normal"),
    customerName: material.customerName ?? "",
    dtfCode: material.dtfCode ?? "",
    dtfName: material.dtfName ?? "",
    dtfWidth: toInputNumber(material.dtfWidth),
    dtfHeight: toInputNumber(material.dtfHeight),
    applicationPosition: material.applicationPosition ?? "",
    applicationNote: material.applicationNote ?? "",
    packagingType: normalizeOption(material.packagingType, PACKAGING_TYPES, "Koli"),
    boxPairCapacity: toInputNumber(material.boxPairCapacity),
    boxDimensions: material.boxDimensions ?? "",
    boxWeight: toInputNumber(material.boxWeight),
    isActive: material.isActive !== false,
  };
}

function toRequestBody(form: MaterialFormState) {
  const technicalExtra = {
    supplierCode: form.supplierCode,
    barcode: form.barcode,
    qrCode: form.qrCode,
    technicalNote: form.technicalNote,
    msdsFileName: form.msdsFileName,
    technicalPdfFileName: form.technicalPdfFileName,
    labAnalysisNote: form.labAnalysisNote,
    auxiliaryDescription: form.auxiliaryDescription,
    auxiliaryUsageNote: form.auxiliaryUsageNote,
  };
  const safetyExtra = {
    storageInstruction: form.storageInstruction,
    usageInstruction: form.usageInstruction,
    qualityNote: form.qualityNote,
    generalNote: form.generalNote,
    priceHistoryNote: form.priceHistoryNote,
    purchaseHistoryNote: form.purchaseHistoryNote,
    usageHistoryNote: form.usageHistoryNote,
    recipeUsageNote: form.recipeUsageNote,
    alternativeMaterialsNote: form.alternativeMaterialsNote,
    aiPurchaseSuggestionNote: form.aiPurchaseSuggestionNote,
  };

  return {
    code: form.code.trim(),
    name: form.name.trim(),
    category: emptyToNull(form.category),
    subCategory: emptyToNull(form.subCategory),
    description: emptyToNull(form.description),
    materialType: form.materialType,
    unit: form.unit,
    defaultSupplierId: emptyToNull(form.defaultSupplierId),
    defaultSupplierName: emptyToNull(form.defaultSupplierName),
    currency: emptyToNull(form.currency) ?? "TRY",
    lastPurchasePrice: parseOptionalNumber(form.lastPurchasePrice),
    minimumStock: parseOptionalNumber(form.minimumStock),
    maximumStock: parseOptionalNumber(form.maximumStock),
    criticalStock: parseOptionalNumber(form.criticalStock),
    warehouseName: emptyToNull(form.warehouseName),
    locationCode: emptyToNull(form.locationCode),
    lotTrackingEnabled: form.lotTrackingEnabled,
    expiryTrackingEnabled: form.expiryTrackingEnabled,
    technicalSpecification: buildExtraText(form.technicalSpecification, technicalExtra),
    safetyInformation: buildExtraText(form.safetyInformation, safetyExtra),
    chemicalRole: form.materialType === "Chemical" ? form.chemicalRole : null,
    density: parseOptionalNumber(form.density),
    mixingRatio: parseOptionalNumber(form.mixingRatio),
    containerWeight: parseOptionalNumber(form.containerWeight),
    addedToPoliolBatch: form.materialType === "Chemical" && form.addedToPoliolBatch,
    crosskimApplicationNote: form.materialType === "Chemical" ? emptyToNull(form.crosskimApplicationNote) : null,
    fabricType: form.materialType === "Fabric" ? form.fabricType : null,
    fabricWeightGsm: form.materialType === "Fabric" ? parseOptionalNumber(form.fabricWeightGsm) : null,
    fabricColor: form.materialType === "Fabric" ? emptyToNull(form.fabricColor) : null,
    fabricWidth: form.materialType === "Fabric" ? parseOptionalNumber(form.fabricWidth) : null,
    fabricRollLength: form.materialType === "Fabric" ? parseOptionalNumber(form.fabricRollLength) : null,
    adhesiveType: form.materialType === "Adhesive" ? form.adhesiveType : null,
    customerName: form.materialType === "DTF" ? emptyToNull(form.customerName) : null,
    dtfCode: form.materialType === "DTF" ? emptyToNull(form.dtfCode) : null,
    dtfName: form.materialType === "DTF" ? emptyToNull(form.dtfName) : null,
    dtfWidth: form.materialType === "DTF" ? parseOptionalNumber(form.dtfWidth) : null,
    dtfHeight: form.materialType === "DTF" ? parseOptionalNumber(form.dtfHeight) : null,
    applicationPosition: form.materialType === "DTF" ? emptyToNull(form.applicationPosition) : null,
    applicationNote: form.materialType === "DTF" ? emptyToNull(form.applicationNote) : null,
    packagingType: form.materialType === "Packaging" ? form.packagingType : null,
    boxPairCapacity: form.materialType === "Packaging" ? parseOptionalInteger(form.boxPairCapacity) : null,
    boxDimensions: form.materialType === "Packaging" ? emptyToNull(form.boxDimensions) : null,
    boxWeight: form.materialType === "Packaging" ? parseOptionalNumber(form.boxWeight) : null,
    isActive: form.isActive,
  };
}

function extractMaterials(result: unknown): Material[] {
  if (Array.isArray(result)) return result as Material[];

  if (result && typeof result === "object" && "data" in result) {
    const data = (result as { data?: unknown }).data;
    return Array.isArray(data) ? (data as Material[]) : [];
  }

  return [];
}

function extractMaterial(result: unknown): Material | null {
  if (!result || typeof result !== "object") return null;

  if ("data" in result) {
    const data = (result as { data?: unknown }).data;
    return data && typeof data === "object" ? (data as Material) : null;
  }

  return result as Material;
}

function extractErrorMessage(result: ApiResponse<unknown>) {
  if (Array.isArray(result.errors) && result.errors.length > 0) return result.errors.join(", ");
  if (typeof result.message === "string" && result.message.trim()) return result.message;
  if (typeof result.errorCode === "string" && result.errorCode.trim()) return result.errorCode;
  return "";
}

function buildExtraText(visible: string, extra: Partial<MaterialExtra>) {
  return `${visible.trim()}${EXTRA_MARKER}${JSON.stringify(extra)}`;
}

function parseExtra(value?: string | null): { visible: string; extra: Partial<MaterialExtra> } {
  const raw = value ?? "";
  const markerIndex = raw.indexOf(EXTRA_MARKER);

  if (markerIndex === -1) {
    return { visible: raw, extra: {} };
  }

  try {
    return {
      visible: raw.slice(0, markerIndex).trim(),
      extra: JSON.parse(raw.slice(markerIndex + EXTRA_MARKER.length)) as Partial<MaterialExtra>,
    };
  } catch {
    return { visible: raw.slice(0, markerIndex).trim(), extra: {} };
  }
}

function normalizeOption(value: string | null | undefined, options: string[], fallback: string) {
  return value && options.includes(value) ? value : fallback;
}

function emptyToNull(value: string) {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

function parseOptionalNumber(value: string) {
  if (!value.trim()) return null;
  const parsed = Number(value.replace(",", "."));
  return Number.isFinite(parsed) ? parsed : null;
}

function parseOptionalInteger(value: string) {
  if (!value.trim()) return null;
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : null;
}

function toInputNumber(value?: number | null) {
  return typeof value === "number" && Number.isFinite(value) ? String(value) : "";
}

function formatNumber(value?: number | null) {
  if (typeof value !== "number" || !Number.isFinite(value)) return "-";
  return value.toLocaleString("tr-TR", { maximumFractionDigits: 2 });
}

function countByType(materials: Material[], type: string) {
  return materials.filter((material) => material.materialType === type).length.toLocaleString("tr-TR");
}

function isCriticalMaterial(material: Material) {
  return typeof material.criticalStock === "number" && material.criticalStock > 0;
}
