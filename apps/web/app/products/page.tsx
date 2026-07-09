"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";

type DashboardTone = "emerald" | "cyan" | "amber" | "red";
type ProductDialogMode = "create" | "edit" | "detail" | null;
type ProductTab = "general" | "production" | "recipe" | "packaging" | "quality" | "cost" | "notes";

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
  hasPolibond?: boolean | null;
  averageWeight?: number | null;
  targetDensity?: number | null;
  standardCycleTime?: number | null;
  defaultBoxQuantity?: number | null;
  isActive?: boolean | null;
  createdAt?: string | null;
  updatedAt?: string | null;
};

type RecipeLine = {
  material: string;
  quantity: string;
  unit: string;
  unitPrice: string;
};

type ProductMasterDetails = {
  brand: string;
  model: string;
  number: string;
  color: string;
  productionType: string;
  displayProductType: string;
  currency: string;
  unit: string;
  fabricType: string;
  adhesiveType: string;
  dtfCode: string;
  dtfDescription: string;
  standardDailyCapacity: string;
  standardWasteRate: string;
  cuttingMachine: string;
  standardMold: string;
  defaultOperationNote: string;
  recipeLines: RecipeLine[];
  recipeNote: string;
  boxType: string;
  boxSize: string;
  boxWeight: string;
  innerBag: boolean;
  shrink: boolean;
  labelType: string;
  barcode: string;
  packagingNote: string;
  minWeight: string;
  maxWeight: string;
  minDensity: string;
  maxDensity: string;
  acceptedWasteRate: string;
  qualityNote: string;
  laborCost: string;
  electricityCost: string;
  overheadCost: string;
  productionNote: string;
  qualityControlNote: string;
  shippingNote: string;
};

type ProductFormState = ProductMasterDetails & {
  code: string;
  name: string;
  customerName: string;
  category: string;
  modelCode: string;
  description: string;
  foamType: string;
  productType: string;
  isFabric: boolean;
  isAdhesive: boolean;
  hasDTFLabel: boolean;
  hasPolibond: boolean;
  averageWeight: string;
  targetDensity: string;
  standardCycleTime: string;
  defaultBoxQuantity: string;
  isActive: boolean;
};

type ApiResponse<T> = {
  data?: T;
  message?: string;
  errors?: string[];
  errorCode?: string;
  success?: boolean;
};

const API = "http://localhost:5000/api/v1";
const DETAILS_MARKER = "\n\n---FIXAR_PRODUCT_MASTER_JSON---\n";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60 disabled:cursor-not-allowed disabled:opacity-70";
const TABS: Array<{ id: ProductTab; label: string }> = [
  { id: "general", label: "1 Genel Bilgiler" },
  { id: "production", label: "2 Üretim" },
  { id: "recipe", label: "3 Reçete" },
  { id: "packaging", label: "4 Ambalaj" },
  { id: "quality", label: "5 Kalite" },
  { id: "cost", label: "6 Maliyet" },
  { id: "notes", label: "7 Açıklamalar" },
];
const RECIPE_MATERIALS = ["Poliol", "İzosiyanat", "Crosskim", "Pigment", "Solvent", "Kalıp Ayırıcı", "Kumaş", "Yapışkan", "DTF", "Koli", "Etiket", "Diğer"];

const emptyRecipeLines: RecipeLine[] = RECIPE_MATERIALS.map((material) => ({
  material,
  quantity: "",
  unit: material === "Koli" || material === "Etiket" ? "Adet" : "gr",
  unitPrice: "",
}));

const emptyDetails: ProductMasterDetails = {
  brand: "",
  model: "",
  number: "",
  color: "",
  productionType: "FIXAR",
  displayProductType: "Normal",
  currency: "TRY",
  unit: "Çift",
  fabricType: "Yok",
  adhesiveType: "Yok",
  dtfCode: "",
  dtfDescription: "",
  standardDailyCapacity: "",
  standardWasteRate: "",
  cuttingMachine: "Gezer Kafa",
  standardMold: "",
  defaultOperationNote: "",
  recipeLines: emptyRecipeLines,
  recipeNote: "",
  boxType: "",
  boxSize: "",
  boxWeight: "",
  innerBag: false,
  shrink: false,
  labelType: "",
  barcode: "",
  packagingNote: "",
  minWeight: "",
  maxWeight: "",
  minDensity: "",
  maxDensity: "",
  acceptedWasteRate: "",
  qualityNote: "",
  laborCost: "",
  electricityCost: "",
  overheadCost: "",
  productionNote: "",
  qualityControlNote: "",
  shippingNote: "",
};

const emptyForm: ProductFormState = {
  code: "",
  name: "",
  customerName: "",
  category: "",
  modelCode: "",
  description: "",
  foamType: "10100",
  productType: "Normal",
  isFabric: false,
  isAdhesive: false,
  hasDTFLabel: false,
  hasPolibond: false,
  averageWeight: "",
  targetDensity: "",
  standardCycleTime: "",
  defaultBoxQuantity: "",
  isActive: true,
  ...emptyDetails,
};

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [dialogMode, setDialogMode] = useState<ProductDialogMode>(null);
  const [dialogProduct, setDialogProduct] = useState<Product | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);

  useEffect(() => {
    loadProducts();
  }, []);

  async function loadProducts() {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(API + "/products");

      if (!response.ok) {
        throw new Error("Ürün listesi alınamadı.");
      }

      const result: unknown = await response.json();
      setProducts(extractProducts(result));
    } catch (err) {
      setProducts([]);
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  async function openDialog(mode: ProductDialogMode, product: Product | null = null) {
    setSuccessMessage(null);
    setDetailError(null);

    if (mode === "create" || !product) {
      setDialogProduct(null);
      setDialogMode(mode);
      return;
    }

    setDialogMode(mode);
    setDialogProduct(product);
    setDetailLoading(true);

    try {
      const response = await fetch(`${API}/products/${product.id}`);

      if (!response.ok) {
        throw new Error("Ürün detayı alınamadı.");
      }

      const result: unknown = await response.json();
      setDialogProduct(extractProduct(result) ?? product);
    } catch (err) {
      setDetailError(err instanceof Error ? err.message : "Ürün detayı yüklenirken hata oluştu.");
    } finally {
      setDetailLoading(false);
    }
  }

  function closeDialog() {
    setDialogMode(null);
    setDialogProduct(null);
    setDetailError(null);
    setDetailLoading(false);
  }

  function handleSaved(message: string) {
    closeDialog();
    setSuccessMessage(message);
    loadProducts();
  }

  const filteredProducts = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase("tr-TR");

    if (!normalizedSearch) return products;

    return products.filter((product) => {
      const details = parseDescription(product.description).details;
      return [product.code, product.name, product.customerName, product.category, product.modelCode, details.brand, details.model, details.number, details.color]
        .filter(Boolean)
        .some((value) => String(value).toLocaleLowerCase("tr-TR").includes(normalizedSearch));
    });
  }, [products, search]);

  const activeCount = products.filter((product) => product.isActive !== false).length;
  const memoryFoamCount = products.filter((product) => {
    const details = parseDescription(product.description).details;
    return (details.displayProductType || product.productType) === "Memory Foam";
  }).length;
  const normalPuCount = products.filter((product) => {
    const details = parseDescription(product.description).details;
    return (details.displayProductType || product.productType || "Normal") === "Normal" && product.foamType === "10100";
  }).length;
  const dashboardCards = [
    { title: "Toplam Ürün", value: products.length.toLocaleString("tr-TR"), note: "Master kart sayısı", tone: "emerald" as DashboardTone },
    { title: "Aktif Ürün", value: activeCount.toLocaleString("tr-TR"), note: "Üretime açık", tone: "cyan" as DashboardTone },
    { title: "Memory Foam", value: memoryFoamCount.toLocaleString("tr-TR"), note: "Özel ürün tipi", tone: "amber" as DashboardTone },
    { title: "Normal PU", value: normalPuCount.toLocaleString("tr-TR"), note: "10100 normal ürün", tone: "red" as DashboardTone },
  ];

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.18),transparent_34%),radial-gradient(circle_at_bottom_right,rgba(14,165,233,0.13),transparent_32%)] px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.38em] text-emerald-300">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">Ürün Master Kartı</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                İş emirleri, üretim, maliyet, satın alma, stok, kalite, QR izlenebilirlik ve raporlama için ana ürün verilerini yönetin.
              </p>
            </div>

            <div className="flex flex-col gap-3 sm:flex-row">
              <button
                onClick={loadProducts}
                disabled={loading}
                className="rounded-xl border border-white/10 bg-white/[0.08] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:opacity-50"
              >
                {loading ? "Yenileniyor..." : "Yenile"}
              </button>
              <button onClick={() => openDialog("create")} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
                + Yeni Ürün
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
                <h2 className="text-2xl font-black">Ürün Kayıtları</h2>
                <p className="mt-1 text-sm text-zinc-400">
                  {filteredProducts.length.toLocaleString("tr-TR")} ürün listeleniyor.
                </p>
              </div>
              <div className="w-full xl:max-w-md">
                <Field label="Arama">
                  <input
                    value={search}
                    onChange={(event) => setSearch(event.target.value)}
                    className={CONTROL_CLASS}
                    placeholder="Kod, ürün, müşteri, marka, model, numara"
                  />
                </Field>
              </div>
            </div>

            {loading && <LoadingState />}

            {!loading && error && (
              <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-5 text-sm text-red-100">
                <p className="font-black">Ürün listesi yüklenemedi.</p>
                <p className="mt-1 text-red-200">{error}</p>
              </div>
            )}

            {!loading && !error && filteredProducts.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-zinc-300">
                {products.length === 0 ? "Henüz ürün master kartı bulunmuyor." : "Aramaya uygun ürün kaydı bulunamadı."}
              </div>
            )}

            {!loading && !error && filteredProducts.length > 0 && (
              <div className="mt-5 overflow-x-auto">
                <table className="min-w-[1180px] w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-white/10 text-xs uppercase tracking-[0.18em] text-zinc-500">
                      <th className="py-3 pr-4">Kod</th>
                      <th className="py-3 pr-4">Ürün</th>
                      <th className="py-3 pr-4">Müşteri</th>
                      <th className="py-3 pr-4">Foam</th>
                      <th className="py-3 pr-4">Numara</th>
                      <th className="py-3 pr-4">Kumaş</th>
                      <th className="py-3 pr-4">Yapışkan</th>
                      <th className="py-3 pr-4">DTF</th>
                      <th className="py-3 pr-4">Gramaj</th>
                      <th className="py-3 pr-4">Yoğunluk</th>
                      <th className="py-3 pr-4">Durum</th>
                      <th className="py-3 text-right">İşlemler</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/10">
                    {filteredProducts.map((product) => {
                      const details = parseDescription(product.description).details;

                      return (
                        <tr key={product.id} className="align-middle text-zinc-200 transition hover:bg-white/[0.04]">
                          <td className="py-4 pr-4 font-mono text-xs text-emerald-200">{product.code || "-"}</td>
                          <td className="py-4 pr-4">
                            <p className="font-black text-white">{product.name || "-"}</p>
                            <p className="mt-1 text-xs text-zinc-500">{details.brand || product.modelCode || "-"}</p>
                          </td>
                          <td className="py-4 pr-4">{product.customerName || "-"}</td>
                          <td className="py-4 pr-4">
                            <Badge>{product.foamType || "-"}</Badge>
                          </td>
                          <td className="py-4 pr-4">{details.number || "-"}</td>
                          <td className="py-4 pr-4">{details.fabricType || "Yok"}</td>
                          <td className="py-4 pr-4">{details.adhesiveType || "Yok"}</td>
                          <td className="py-4 pr-4">{product.hasDTFLabel ? "Var" : "Yok"}</td>
                          <td className="py-4 pr-4">{formatNumber(product.averageWeight)}</td>
                          <td className="py-4 pr-4">{formatNumber(product.targetDensity)}</td>
                          <td className="py-4 pr-4">
                            <StatusBadge active={product.isActive !== false} />
                          </td>
                          <td className="py-4">
                            <div className="flex justify-end gap-2">
                              <button
                                onClick={() => openDialog("detail", product)}
                                className="rounded-lg border border-cyan-400/30 bg-cyan-400/10 px-3 py-2 text-xs font-black text-cyan-100 transition hover:bg-cyan-400/20"
                              >
                                Detay
                              </button>
                              <button
                                onClick={() => openDialog("edit", product)}
                                className="rounded-lg border border-emerald-400/30 bg-emerald-400/10 px-3 py-2 text-xs font-black text-emerald-100 transition hover:bg-emerald-400/20"
                              >
                                Düzenle
                              </button>
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
        <ProductMasterModal
          mode={dialogMode}
          product={dialogProduct}
          loading={detailLoading}
          error={detailError}
          onClose={closeDialog}
          onSaved={handleSaved}
        />
      )}
    </main>
  );
}

function ProductMasterModal({
  mode,
  product,
  loading,
  error,
  onClose,
  onSaved,
}: {
  mode: ProductDialogMode;
  product: Product | null;
  loading: boolean;
  error: string | null;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const [activeTab, setActiveTab] = useState<ProductTab>("general");
  const [form, setForm] = useState<ProductFormState>(() => toFormState(product));
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const readonly = mode === "detail";
  const isEdit = mode === "edit";

  useEffect(() => {
    setForm(toFormState(product));
    setActiveTab("general");
    setFormError(null);
  }, [product]);

  const rawMaterialCost = useMemo(() => calculateRecipeTotal(form.recipeLines), [form.recipeLines]);
  const packagingCost = useMemo(() => calculatePackagingCost(form.recipeLines), [form.recipeLines]);
  const estimatedPairCost = rawMaterialCost + packagingCost + safeParsedNumber(form.laborCost) + safeParsedNumber(form.electricityCost) + safeParsedNumber(form.overheadCost);

  function updateField<K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function updateRecipeLine(index: number, key: keyof RecipeLine, value: string) {
    setForm((current) => ({
      ...current,
      recipeLines: current.recipeLines.map((line, lineIndex) => (lineIndex === index ? { ...line, [key]: value } : line)),
    }));
  }

  async function handleSubmit() {
    setFormError(null);

    if (!form.code.trim()) {
      setFormError("Ürün kodu zorunludur.");
      setActiveTab("general");
      return;
    }

    if (!form.name.trim()) {
      setFormError("Ürün adı zorunludur.");
      setActiveTab("general");
      return;
    }

    setSaving(true);

    try {
      const response = await fetch(isEdit && product ? `${API}/products/${product.id}` : API + "/products", {
        method: isEdit ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(toRequestBody(form)),
      });

      const result: ApiResponse<unknown> = await response.json().catch(() => ({}));

      if (!response.ok) {
        throw new Error(extractErrorMessage(result) || "Ürün kaydedilemedi.");
      }

      onSaved(isEdit ? "Ürün master kartı güncellendi." : "Ürün master kartı oluşturuldu.");
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-3 backdrop-blur-sm sm:p-5">
      <div className="flex max-h-[94vh] w-full max-w-7xl flex-col overflow-hidden rounded-2xl border border-white/10 bg-[#080B10] shadow-2xl">
        <div className="flex flex-col gap-4 border-b border-white/10 bg-white/[0.04] p-5 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <p className="text-xs font-black tracking-[0.34em] text-emerald-300">PRODUCT MASTER</p>
            <h2 className="mt-2 text-2xl font-black text-white">
              {readonly ? "Ürün Detayı" : isEdit ? "Ürün Master Kartı Düzenle" : "Yeni Ürün Master Kartı"}
            </h2>
            <p className="mt-1 text-sm text-zinc-400">
              Ürün ana verisi, üretim reçetesi, kalite toleransları ve maliyet varsayımlarını tek kartta yönetin.
            </p>
          </div>
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white transition hover:bg-white/[0.12]">
            Kapat
          </button>
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
          {activeTab === "production" && <ProductionTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "recipe" && <RecipeTab form={form} readonly={readonly} rawMaterialCost={rawMaterialCost} updateRecipeLine={updateRecipeLine} updateField={updateField} />}
          {activeTab === "packaging" && <PackagingTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "quality" && <QualityTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "cost" && <CostTab form={form} readonly={readonly} rawMaterialCost={rawMaterialCost} packagingCost={packagingCost} estimatedPairCost={estimatedPairCost} updateField={updateField} />}
          {activeTab === "notes" && <NotesTab form={form} readonly={readonly} updateField={updateField} />}
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

function GeneralTab({
  form,
  readonly,
  updateField,
}: {
  form: ProductFormState;
  readonly: boolean;
  updateField: <K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) => void;
}) {
  return (
    <TabPanel title="Genel Bilgiler" note="Ürünün tüm FIXAR OS modüllerinde kullanılacak ana kimliği.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Ürün Kodu" value={form.code} readonly={readonly} onChange={(value) => updateField("code", value)} required />
        <TextInput label="Ürün Adı" value={form.name} readonly={readonly} onChange={(value) => updateField("name", value)} required />
        <TextInput label="Marka" value={form.brand} readonly={readonly} onChange={(value) => updateField("brand", value)} />
        <TextInput label="Müşteri" value={form.customerName} readonly={readonly} onChange={(value) => updateField("customerName", value)} />
        <TextInput label="Kategori" value={form.category} readonly={readonly} onChange={(value) => updateField("category", value)} />
        <TextInput label="Model" value={form.model} readonly={readonly} onChange={(value) => updateField("model", value)} />
        <TextInput label="Model Kodu" value={form.modelCode} readonly={readonly} onChange={(value) => updateField("modelCode", value)} />
        <TextInput label="Numara" value={form.number} readonly={readonly} onChange={(value) => updateField("number", value)} />
        <TextInput label="Renk" value={form.color} readonly={readonly} onChange={(value) => updateField("color", value)} />
        <SelectInput label="Üretim Şekli" value={form.productionType} readonly={readonly} options={["OEM", "FIXAR", "Fason"]} onChange={(value) => updateField("productionType", value)} />
        <SelectInput label="Foam Tipi" value={form.foamType} readonly={readonly} options={["10100", "10900"]} onChange={(value) => updateField("foamType", value)} />
        <SelectInput label="Ürün Tipi" value={form.productType} readonly={readonly} options={["Normal", "Memory Foam", "Kids", "Ortopedik"]} onChange={(value) => updateField("productType", value)} />
        <SelectInput label="Varsayılan Para Birimi" value={form.currency} readonly={readonly} options={["TRY", "USD", "EUR"]} onChange={(value) => updateField("currency", value)} />
        <SelectInput label="Birim" value={form.unit} readonly={readonly} options={["Çift"]} onChange={(value) => updateField("unit", value)} />
        <ToggleInput label="Aktif / Pasif" checked={form.isActive} readonly={readonly} trueText="Aktif" falseText="Pasif" onChange={(value) => updateField("isActive", value)} />
      </div>
      <TextAreaInput label="Açıklama" value={form.description} readonly={readonly} onChange={(value) => updateField("description", value)} />
    </TabPanel>
  );
}

function ProductionTab({
  form,
  readonly,
  updateField,
}: {
  form: ProductFormState;
  readonly: boolean;
  updateField: <K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) => void;
}) {
  return (
    <TabPanel title="Üretim" note="Standart üretim parametreleri iş emirleri ve kapasite planlamasına temel olur.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <SelectInput label="Kumaş" value={form.fabricType} readonly={readonly} options={["Yok", "Interlok", "Lacoste", "Mesh", "Polar", "Keçe", "Diğer"]} onChange={(value) => updateField("fabricType", value)} />
        <SelectInput label="Yapışkan" value={form.adhesiveType} readonly={readonly} options={["Yok", "Normal", "Polibond"]} onChange={(value) => updateField("adhesiveType", value)} />
        <ToggleInput label="DTF" checked={form.hasDTFLabel} readonly={readonly} trueText="Var" falseText="Yok" onChange={(value) => updateField("hasDTFLabel", value)} />
        <ToggleInput label="Polibond" checked={form.hasPolibond} readonly={readonly} trueText="Var" falseText="Yok" onChange={(value) => updateField("hasPolibond", value)} />
        <TextInput label="DTF Kodu" value={form.dtfCode} readonly={readonly || !form.hasDTFLabel} onChange={(value) => updateField("dtfCode", value)} />
        <TextInput label="Ortalama Gramaj" value={form.averageWeight} readonly={readonly} type="number" onChange={(value) => updateField("averageWeight", value)} />
        <TextInput label="Hedef Yoğunluk" value={form.targetDensity} readonly={readonly} type="number" onChange={(value) => updateField("targetDensity", value)} />
        <TextInput label="Standart Pişme Süresi" value={form.standardCycleTime} readonly={readonly} type="number" onChange={(value) => updateField("standardCycleTime", value)} />
        <TextInput label="Standart Günlük Kapasite" value={form.standardDailyCapacity} readonly={readonly} type="number" onChange={(value) => updateField("standardDailyCapacity", value)} />
        <TextInput label="Standart Fire %" value={form.standardWasteRate} readonly={readonly} type="number" onChange={(value) => updateField("standardWasteRate", value)} />
        <SelectInput label="Kesim Makinesi" value={form.cuttingMachine} readonly={readonly} options={["Gezer Kafa", "Döner Kafa"]} onChange={(value) => updateField("cuttingMachine", value)} />
        <TextInput label="Standart Kalıp" value={form.standardMold} readonly={readonly} onChange={(value) => updateField("standardMold", value)} />
      </div>
      <TextAreaInput label="DTF Açıklaması" value={form.dtfDescription} readonly={readonly || !form.hasDTFLabel} onChange={(value) => updateField("dtfDescription", value)} />
      <TextAreaInput label="Varsayılan Operasyon Notu" value={form.defaultOperationNote} readonly={readonly} onChange={(value) => updateField("defaultOperationNote", value)} />
    </TabPanel>
  );
}

function RecipeTab({
  form,
  readonly,
  rawMaterialCost,
  updateRecipeLine,
  updateField,
}: {
  form: ProductFormState;
  readonly: boolean;
  rawMaterialCost: number;
  updateRecipeLine: (index: number, key: keyof RecipeLine, value: string) => void;
  updateField: <K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) => void;
}) {
  return (
    <TabPanel title="Reçete" note="Reçete grid’i 1 çift ürün için miktar ve birim fiyat varsayımlarını tutar.">
      <div className="overflow-x-auto rounded-xl border border-white/10 bg-black/20">
        <table className="min-w-[760px] w-full text-left text-sm">
          <thead>
            <tr className="border-b border-white/10 text-xs uppercase tracking-[0.18em] text-zinc-500">
              <th className="p-3">Hammadde</th>
              <th className="p-3">Miktar</th>
              <th className="p-3">Birim</th>
              <th className="p-3">Birim Fiyat</th>
              <th className="p-3 text-right">Toplam</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/10">
            {form.recipeLines.map((line, index) => (
              <tr key={line.material}>
                <td className="p-3 font-black text-white">{line.material}</td>
                <td className="p-3">
                  <input value={line.quantity} type="number" step="0.01" disabled={readonly} onChange={(event) => updateRecipeLine(index, "quantity", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <input value={line.unit} disabled={readonly} onChange={(event) => updateRecipeLine(index, "unit", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <input value={line.unitPrice} type="number" step="0.01" disabled={readonly} onChange={(event) => updateRecipeLine(index, "unitPrice", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3 text-right font-black text-emerald-200">{formatCurrency(calculateLineTotal(line), form.currency)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="grid gap-4 lg:grid-cols-[1fr_320px]">
        <div className="rounded-xl border border-amber-400/30 bg-amber-500/10 p-4 text-sm text-amber-100">
          <p className="font-black">Not</p>
          <p className="mt-1">Crosskim doğrudan makineye verilmez.</p>
          <p>180 kg poliol kazanına ilave edilir.</p>
        </div>
        <div className="rounded-xl border border-emerald-400/30 bg-emerald-500/10 p-4 text-right">
          <p className="text-xs font-black uppercase tracking-[0.18em] text-emerald-200">Toplam Hammadde Maliyeti</p>
          <p className="mt-2 text-3xl font-black text-white">{formatCurrency(rawMaterialCost, form.currency)}</p>
        </div>
      </div>
      <TextAreaInput label="Reçete Notu" value={form.recipeNote} readonly={readonly} onChange={(value) => updateField("recipeNote", value)} />
    </TabPanel>
  );
}

function PackagingTab({
  form,
  readonly,
  updateField,
}: {
  form: ProductFormState;
  readonly: boolean;
  updateField: <K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) => void;
}) {
  return (
    <TabPanel title="Ambalaj" note="Koli, paketleme, etiket ve barkod bilgileri sevkiyat ile QR izlenebilirliği besler.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Varsayılan Koli Adedi" value={form.defaultBoxQuantity} readonly={readonly} type="number" onChange={(value) => updateField("defaultBoxQuantity", value)} />
        <TextInput label="Koli Tipi" value={form.boxType} readonly={readonly} onChange={(value) => updateField("boxType", value)} />
        <TextInput label="Koli Ölçüsü" value={form.boxSize} readonly={readonly} onChange={(value) => updateField("boxSize", value)} />
        <TextInput label="Koli Ağırlığı" value={form.boxWeight} readonly={readonly} type="number" onChange={(value) => updateField("boxWeight", value)} />
        <ToggleInput label="İç Poşet" checked={form.innerBag} readonly={readonly} trueText="Var" falseText="Yok" onChange={(value) => updateField("innerBag", value)} />
        <ToggleInput label="Shrink" checked={form.shrink} readonly={readonly} trueText="Var" falseText="Yok" onChange={(value) => updateField("shrink", value)} />
        <TextInput label="Etiket Tipi" value={form.labelType} readonly={readonly} onChange={(value) => updateField("labelType", value)} />
        <TextInput label="Barkod" value={form.barcode} readonly={readonly} onChange={(value) => updateField("barcode", value)} />
      </div>
      <TextAreaInput label="Paketleme Notu" value={form.packagingNote} readonly={readonly} onChange={(value) => updateField("packagingNote", value)} />
    </TabPanel>
  );
}

function QualityTab({
  form,
  readonly,
  updateField,
}: {
  form: ProductFormState;
  readonly: boolean;
  updateField: <K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) => void;
}) {
  return (
    <TabPanel title="Kalite" note="Kalite kontrol toleransları, üretim kabul ve raporlama süreçlerinde kullanılır.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <TextInput label="Minimum Gramaj" value={form.minWeight} readonly={readonly} type="number" onChange={(value) => updateField("minWeight", value)} />
        <TextInput label="Maksimum Gramaj" value={form.maxWeight} readonly={readonly} type="number" onChange={(value) => updateField("maxWeight", value)} />
        <TextInput label="Minimum Yoğunluk" value={form.minDensity} readonly={readonly} type="number" onChange={(value) => updateField("minDensity", value)} />
        <TextInput label="Maksimum Yoğunluk" value={form.maxDensity} readonly={readonly} type="number" onChange={(value) => updateField("maxDensity", value)} />
        <TextInput label="Kabul Edilen Fire %" value={form.acceptedWasteRate} readonly={readonly} type="number" onChange={(value) => updateField("acceptedWasteRate", value)} />
      </div>
      <TextAreaInput label="Kalite Notu" value={form.qualityNote} readonly={readonly} onChange={(value) => updateField("qualityNote", value)} />
    </TabPanel>
  );
}

function CostTab({
  form,
  readonly,
  rawMaterialCost,
  packagingCost,
  estimatedPairCost,
  updateField,
}: {
  form: ProductFormState;
  readonly: boolean;
  rawMaterialCost: number;
  packagingCost: number;
  estimatedPairCost: number;
  updateField: <K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) => void;
}) {
  return (
    <TabPanel title="Maliyet" note="Şimdilik frontend üzerinde hesaplanan tahmini çift maliyeti görünümü.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <CostCard title="Hammadde" value={formatCurrency(rawMaterialCost, form.currency)} note="Reçete grid toplamı" />
        <EditableCostCard title="İşçilik" value={form.laborCost} readonly={readonly} currency={form.currency} onChange={(value) => updateField("laborCost", value)} />
        <EditableCostCard title="Elektrik" value={form.electricityCost} readonly={readonly} currency={form.currency} onChange={(value) => updateField("electricityCost", value)} />
        <CostCard title="Paketleme" value={formatCurrency(packagingCost, form.currency)} note="Koli, etiket ve diğer satırlar" />
        <EditableCostCard title="Genel Gider" value={form.overheadCost} readonly={readonly} currency={form.currency} onChange={(value) => updateField("overheadCost", value)} />
        <CostCard title="Tahmini Çift Maliyeti" value={formatCurrency(estimatedPairCost, form.currency)} note="Hammadde + işçilik + elektrik + paketleme + gider" strong />
      </div>
    </TabPanel>
  );
}

function NotesTab({
  form,
  readonly,
  updateField,
}: {
  form: ProductFormState;
  readonly: boolean;
  updateField: <K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) => void;
}) {
  return (
    <TabPanel title="Açıklamalar" note="Üretim, kalite ve sevkiyat ekipleri için operasyonel notlar.">
      <div className="grid gap-4 lg:grid-cols-3">
        <TextAreaInput label="Üretim Notu" value={form.productionNote} readonly={readonly} onChange={(value) => updateField("productionNote", value)} />
        <TextAreaInput label="Kalite Notu" value={form.qualityControlNote} readonly={readonly} onChange={(value) => updateField("qualityControlNote", value)} />
        <TextAreaInput label="Sevkiyat Notu" value={form.shippingNote} readonly={readonly} onChange={(value) => updateField("shippingNote", value)} />
      </div>
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
        onChange={(event) => onChange(event.target.value)}
        rows={4}
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

function CostCard({ title, value, note, strong = false }: { title: string; value: string; note: string; strong?: boolean }) {
  return (
    <article className={`rounded-2xl border p-5 ${strong ? "border-emerald-400/30 bg-emerald-500/10" : "border-white/10 bg-black/25"}`}>
      <p className="text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{title}</p>
      <p className="mt-3 text-2xl font-black text-white">{value}</p>
      <p className="mt-2 text-sm text-zinc-400">{note}</p>
    </article>
  );
}

function EditableCostCard({
  title,
  value,
  readonly,
  currency,
  onChange,
}: {
  title: string;
  value: string;
  readonly: boolean;
  currency: string;
  onChange: (value: string) => void;
}) {
  return (
    <article className="rounded-2xl border border-white/10 bg-black/25 p-5">
      <p className="text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{title}</p>
      <input value={value} type="number" step="0.01" disabled={readonly} onChange={(event) => onChange(event.target.value)} className={`${CONTROL_CLASS} mt-3`} />
      <p className="mt-2 text-sm text-zinc-400">{formatCurrency(safeParsedNumber(value), currency)}</p>
    </article>
  );
}

function DashboardCard({ title, value, note, tone }: { title: string; value: string; note: string; tone: DashboardTone }) {
  const toneClass = {
    emerald: "border-emerald-400/25 bg-emerald-500/10 text-emerald-200",
    cyan: "border-cyan-400/25 bg-cyan-500/10 text-cyan-200",
    amber: "border-amber-400/25 bg-amber-500/10 text-amber-200",
    red: "border-red-400/25 bg-red-500/10 text-red-200",
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

function toFormState(product: Product | null): ProductFormState {
  if (!product) return cloneForm(emptyForm);

  const parsed = parseDescription(product.description);
  const displayProductType = parsed.details.displayProductType || product.productType || "Normal";
  const fabricType = parsed.details.fabricType || (product.isFabric ? "Interlok" : "Yok");
  const adhesiveType = parsed.details.adhesiveType || (product.isAdhesive ? "Normal" : "Yok");

  return {
    ...cloneForm(emptyForm),
    ...parsed.details,
    code: product.code ?? "",
    name: product.name ?? "",
    customerName: product.customerName ?? "",
    category: product.category ?? "",
    modelCode: product.modelCode ?? "",
    description: parsed.description,
    foamType: product.foamType ?? "10100",
    productType: displayProductType,
    displayProductType,
    fabricType,
    adhesiveType,
    isFabric: fabricType !== "Yok",
    isAdhesive: adhesiveType !== "Yok",
    hasDTFLabel: Boolean(product.hasDTFLabel),
    hasPolibond: Boolean(product.hasPolibond),
    averageWeight: toInputNumber(product.averageWeight),
    targetDensity: toInputNumber(product.targetDensity),
    standardCycleTime: toInputNumber(product.standardCycleTime),
    defaultBoxQuantity: toInputNumber(product.defaultBoxQuantity),
    isActive: product.isActive !== false,
  };
}

function toRequestBody(form: ProductFormState) {
  return {
    code: form.code.trim(),
    name: form.name.trim(),
    customerName: emptyToNull(form.customerName),
    category: emptyToNull(form.category),
    modelCode: emptyToNull(form.modelCode),
    description: buildDescription(form),
    foamType: form.foamType,
    productType: form.productType === "Memory Foam" ? "Memory Foam" : "Normal",
    isFabric: form.fabricType !== "Yok",
    isAdhesive: form.adhesiveType !== "Yok",
    hasDTFLabel: form.hasDTFLabel,
    hasPolibond: form.hasPolibond,
    averageWeight: parseOptionalNumber(form.averageWeight),
    targetDensity: parseOptionalNumber(form.targetDensity),
    standardCycleTime: parseOptionalNumber(form.standardCycleTime),
    defaultBoxQuantity: parseOptionalInteger(form.defaultBoxQuantity),
    isActive: form.isActive,
  };
}

function buildDescription(form: ProductFormState) {
  const details: ProductMasterDetails = {
    brand: form.brand,
    model: form.model,
    number: form.number,
    color: form.color,
    productionType: form.productionType,
    displayProductType: form.productType,
    currency: form.currency,
    unit: form.unit,
    fabricType: form.fabricType,
    adhesiveType: form.adhesiveType,
    dtfCode: form.dtfCode,
    dtfDescription: form.dtfDescription,
    standardDailyCapacity: form.standardDailyCapacity,
    standardWasteRate: form.standardWasteRate,
    cuttingMachine: form.cuttingMachine,
    standardMold: form.standardMold,
    defaultOperationNote: form.defaultOperationNote,
    recipeLines: form.recipeLines,
    recipeNote: form.recipeNote,
    boxType: form.boxType,
    boxSize: form.boxSize,
    boxWeight: form.boxWeight,
    innerBag: form.innerBag,
    shrink: form.shrink,
    labelType: form.labelType,
    barcode: form.barcode,
    packagingNote: form.packagingNote,
    minWeight: form.minWeight,
    maxWeight: form.maxWeight,
    minDensity: form.minDensity,
    maxDensity: form.maxDensity,
    acceptedWasteRate: form.acceptedWasteRate,
    qualityNote: form.qualityNote,
    laborCost: form.laborCost,
    electricityCost: form.electricityCost,
    overheadCost: form.overheadCost,
    productionNote: form.productionNote,
    qualityControlNote: form.qualityControlNote,
    shippingNote: form.shippingNote,
  };

  return `${form.description.trim()}${DETAILS_MARKER}${JSON.stringify(details)}`;
}

function parseDescription(rawDescription?: string | null): { description: string; details: ProductMasterDetails } {
  const raw = rawDescription ?? "";
  const markerIndex = raw.indexOf(DETAILS_MARKER);

  if (markerIndex === -1) {
    return { description: raw, details: cloneDetails(emptyDetails) };
  }

  const visibleDescription = raw.slice(0, markerIndex).trim();
  const detailsRaw = raw.slice(markerIndex + DETAILS_MARKER.length);

  try {
    const parsed = JSON.parse(detailsRaw) as Partial<ProductMasterDetails> & Record<string, unknown>;
    const migratedRecipeLines =
      Array.isArray(parsed.recipeLines) && parsed.recipeLines.length > 0
        ? normalizeRecipeLines(parsed.recipeLines)
        : migrateLegacyRecipeLines(parsed);

    return {
      description: visibleDescription,
      details: {
        ...cloneDetails(emptyDetails),
        ...parsed,
        recipeLines: migratedRecipeLines,
        displayProductType: typeof parsed.displayProductType === "string" ? parsed.displayProductType : "",
      },
    };
  } catch {
    return { description: visibleDescription, details: cloneDetails(emptyDetails) };
  }
}

function normalizeRecipeLines(lines: unknown[]) {
  const normalized = RECIPE_MATERIALS.map((material) => {
    const existing = lines.find((line) => typeof line === "object" && line !== null && (line as { material?: unknown }).material === material) as Partial<RecipeLine> | undefined;
    return {
      material,
      quantity: typeof existing?.quantity === "string" ? existing.quantity : "",
      unit: typeof existing?.unit === "string" ? existing.unit : material === "Koli" || material === "Etiket" ? "Adet" : "gr",
      unitPrice: typeof existing?.unitPrice === "string" ? existing.unitPrice : "",
    };
  });

  return normalized;
}

function migrateLegacyRecipeLines(parsed: Record<string, unknown>) {
  const legacyQuantityKeys: Record<string, string> = {
    Poliol: "recipePolyol",
    İzosiyanat: "recipeIsocyanate",
    Crosskim: "recipeCrosskim",
    Pigment: "recipePigment",
    Solvent: "recipeSolvent",
    "Kalıp Ayırıcı": "recipeMoldRelease",
  };
  const legacyPriceKeys: Record<string, string> = {
    Kumaş: "fabricCost",
    Yapışkan: "adhesiveCost",
    DTF: "dtfLabelCost",
    Koli: "boxCost",
    Diğer: "otherCost",
  };

  return emptyRecipeLines.map((line) => ({
    ...line,
    quantity: typeof parsed[legacyQuantityKeys[line.material]] === "string" ? String(parsed[legacyQuantityKeys[line.material]]) : "",
    unitPrice: typeof parsed[legacyPriceKeys[line.material]] === "string" ? String(parsed[legacyPriceKeys[line.material]]) : "",
  }));
}

function cloneDetails(details: ProductMasterDetails): ProductMasterDetails {
  return {
    ...details,
    recipeLines: details.recipeLines.map((line) => ({ ...line })),
  };
}

function cloneForm(form: ProductFormState): ProductFormState {
  return {
    ...form,
    recipeLines: form.recipeLines.map((line) => ({ ...line })),
  };
}

function extractProducts(result: unknown): Product[] {
  if (Array.isArray(result)) return result as Product[];

  if (result && typeof result === "object" && "data" in result) {
    const data = (result as { data?: unknown }).data;
    return Array.isArray(data) ? (data as Product[]) : [];
  }

  return [];
}

function extractProduct(result: unknown): Product | null {
  if (!result || typeof result !== "object") return null;

  if ("data" in result) {
    const data = (result as { data?: unknown }).data;
    return data && typeof data === "object" ? (data as Product) : null;
  }

  return result as Product;
}

function extractErrorMessage(result: ApiResponse<unknown>) {
  if (Array.isArray(result.errors) && result.errors.length > 0) return result.errors.join(", ");
  if (typeof result.message === "string" && result.message.trim()) return result.message;
  if (typeof result.errorCode === "string" && result.errorCode.trim()) return result.errorCode;
  return "";
}

function emptyToNull(value: string) {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

function parseOptionalNumber(value: string) {
  if (!value.trim()) return null;
  const parsed = safeParsedNumber(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function parseOptionalInteger(value: string) {
  if (!value.trim()) return null;
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : null;
}

function safeParsedNumber(value: string) {
  if (!value.trim()) return 0;
  const parsed = Number(value.replace(",", "."));
  return Number.isFinite(parsed) ? parsed : 0;
}

function safeNumber(value?: number | null) {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function toInputNumber(value?: number | null) {
  return typeof value === "number" && Number.isFinite(value) ? String(value) : "";
}

function calculateLineTotal(line: RecipeLine) {
  return safeParsedNumber(line.quantity) * safeParsedNumber(line.unitPrice);
}

function calculateRecipeTotal(lines: RecipeLine[]) {
  return lines.reduce((sum, line) => sum + calculateLineTotal(line), 0);
}

function calculatePackagingCost(lines: RecipeLine[]) {
  return lines
    .filter((line) => ["Koli", "Etiket", "Diğer"].includes(line.material))
    .reduce((sum, line) => sum + calculateLineTotal(line), 0);
}

function formatNumber(value?: number | null) {
  if (typeof value !== "number" || !Number.isFinite(value)) return "-";
  return value.toLocaleString("tr-TR", { maximumFractionDigits: 2 });
}

function formatCurrency(value: number, currency: string) {
  return value.toLocaleString("tr-TR", {
    style: "currency",
    currency: currency || "TRY",
    maximumFractionDigits: 2,
  });
}
