"use client";

import Image from "next/image";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { safeResponseJson, authenticatedFetch, API_PROXY } from "../lib/api/client";

type DashboardTone = "emerald" | "cyan" | "amber" | "red";
type ProductDialogMode = "create" | "edit" | "detail" | null;
type ProductTab = "general" | "production" | "recipe" | "packaging" | "quality" | "cost" | "documents" | "variants" | "route" | "traceability" | "notes";

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

type TechnicalDocument = {
  key: string;
  label: string;
  fileName: string;
};

type CustomerVariant = {
  id: string;
  customerName: string;
  variantCode: string;
  fabricType: string;
  adhesiveType: string;
  hasDTFLabel: boolean;
  dtfCode: string;
  mold: string;
  number: string;
  color: string;
  averageWeight: string;
  targetDensity: string;
  standardCycleTime: string;
  recipeNote: string;
  isActive: boolean;
};

type RouteStep = {
  id: string;
  operation: string;
  standardTime: string;
  department: string;
  note: string;
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
  productImageName: string;
  productImageDataUrl: string;
  fabricType: string;
  adhesiveType: string;
  dtfCode: string;
  dtfName: string;
  dtfSize: string;
  dtfPosition: string;
  dtfPrintNote: string;
  dtfDescription: string;
  standardDailyCapacity: string;
  standardWasteRate: string;
  cuttingMachine: string;
  standardMold: string;
  moldImageName: string;
  moldImageDataUrl: string;
  moldWeight: string;
  moldXCoordinate: string;
  moldYCoordinate: string;
  defaultOperationNote: string;
  recipeLines: RecipeLine[];
  recipeNote: string;
  packagingType: string;
  packageContent: string;
  cartonContent: string;
  defaultPairQuantity: string;
  customerLabel: boolean;
  cartonNote: string;
  boxType: string;
  boxSize: string;
  boxWeight: string;
  innerBag: boolean;
  shrink: boolean;
  labelType: string;
  barcode: string;
  qrCode: string;
  packagingNote: string;
  minWeight: string;
  maxWeight: string;
  minDensity: string;
  maxDensity: string;
  acceptedWasteRate: string;
  shoreHardness: string;
  qualityNote: string;
  laborCost: string;
  electricityCost: string;
  packagingCost: string;
  overheadCost: string;
  technicalDocuments: TechnicalDocument[];
  customerVariants: CustomerVariant[];
  routeSteps: RouteStep[];
  qrTemplate: string;
  gtin: string;
  lotFormat: string;
  labelTemplate: string;
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

const API = API_PROXY;
const DETAILS_MARKER = "\n\n---FIXAR_PRODUCT_MASTER_JSON---\n";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60 disabled:cursor-not-allowed disabled:opacity-70";
const TABS: Array<{ id: ProductTab; label: string }> = [
  { id: "general", label: "Genel" },
  { id: "production", label: "Üretim" },
  { id: "recipe", label: "Reçete" },
  { id: "packaging", label: "Ambalaj" },
  { id: "quality", label: "Kalite" },
  { id: "cost", label: "Maliyet" },
  { id: "documents", label: "Teknik Dokümanlar" },
  { id: "variants", label: "Müşteri Varyantları" },
  { id: "route", label: "Üretim Rotası" },
  { id: "traceability", label: "QR ve Barkod" },
  { id: "notes", label: "Açıklamalar" },
];
const RECIPE_MATERIALS = ["Poliol", "İzosiyanat", "Crosskim", "Pigment", "Solvent", "Kalıp Ayırıcı", "Kumaş", "Yapışkan", "DTF", "İşçilik"];

const emptyRecipeLines: RecipeLine[] = RECIPE_MATERIALS.map((material) => ({
  material,
  quantity: "",
  unit: material === "DTF" || material === "İşçilik" ? "Adet" : "gr",
  unitPrice: "",
}));

const emptyTechnicalDocuments: TechnicalDocument[] = [
  { key: "productImage", label: "Ürün resmi", fileName: "" },
  { key: "moldImage", label: "Kalıp resmi", fileName: "" },
  { key: "technicalPdf", label: "Teknik PDF", fileName: "" },
  { key: "productionInstructionPdf", label: "Üretim Talimatı PDF", fileName: "" },
  { key: "qualityDocument", label: "Kalite Dokümanı", fileName: "" },
  { key: "dxf", label: "DXF", fileName: "" },
  { key: "cad", label: "CAD", fileName: "" },
  { key: "excel", label: "Excel", fileName: "" },
];

const emptyCustomerVariants: CustomerVariant[] = ["DOGO", "ICEMEN", "FLO", "USPA"].map((customerName, index) => ({
  id: `variant-${index + 1}`,
  customerName,
  variantCode: "",
  fabricType: "Yok",
  adhesiveType: "Yok",
  hasDTFLabel: false,
  dtfCode: "",
  mold: "",
  number: "",
  color: "",
  averageWeight: "",
  targetDensity: "",
  standardCycleTime: "",
  recipeNote: "",
  isActive: true,
}));

const emptyRouteSteps: RouteStep[] = ["Enjeksiyon", "Pişirme", "Kesim", "DTF", "Kalite", "Paketleme", "Depo"].map((operation, index) => ({
  id: `route-${index + 1}`,
  operation,
  standardTime: "",
  department: "",
  note: "",
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
  productImageName: "",
  productImageDataUrl: "",
  fabricType: "Yok",
  adhesiveType: "Yok",
  dtfCode: "",
  dtfName: "",
  dtfSize: "",
  dtfPosition: "",
  dtfPrintNote: "",
  dtfDescription: "",
  standardDailyCapacity: "",
  standardWasteRate: "",
  cuttingMachine: "Gezer Kafa",
  standardMold: "",
  moldImageName: "",
  moldImageDataUrl: "",
  moldWeight: "",
  moldXCoordinate: "",
  moldYCoordinate: "",
  defaultOperationNote: "",
  recipeLines: emptyRecipeLines,
  recipeNote: "",
  packagingType: "Çift",
  packageContent: "",
  cartonContent: "",
  defaultPairQuantity: "",
  customerLabel: false,
  cartonNote: "",
  boxType: "",
  boxSize: "",
  boxWeight: "",
  innerBag: false,
  shrink: false,
  labelType: "",
  barcode: "",
  qrCode: "",
  packagingNote: "",
  minWeight: "",
  maxWeight: "",
  minDensity: "",
  maxDensity: "",
  acceptedWasteRate: "",
  shoreHardness: "",
  qualityNote: "",
  laborCost: "",
  electricityCost: "",
  packagingCost: "",
  overheadCost: "",
  technicalDocuments: emptyTechnicalDocuments,
  customerVariants: emptyCustomerVariants,
  routeSteps: emptyRouteSteps,
  qrTemplate: "",
  gtin: "",
  lotFormat: "",
  labelTemplate: "",
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
      const response = await authenticatedFetch(API + "/products");

      if (!response.ok) {
        throw new Error("Ürün listesi alınamadı.");
      }

      const result: unknown = await safeResponseJson(response);
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
      const response = await authenticatedFetch(`${API}/products/${product.id}`);

      if (!response.ok) {
        throw new Error("Ürün detayı alınamadı.");
      }

      const result: unknown = await safeResponseJson(response);
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
    const timer = window.setTimeout(() => {
      setForm(toFormState(product));
      setActiveTab("general");
      setFormError(null);
    }, 0);
    return () => window.clearTimeout(timer);
  }, [product]);

  const totalGram = useMemo(() => calculateTotalGram(form.recipeLines), [form.recipeLines]);
  const rawMaterialCost = useMemo(() => calculateRawMaterialCost(form.recipeLines), [form.recipeLines]);
  const recipeCost = useMemo(() => calculateRecipeTotal(form.recipeLines), [form.recipeLines]);
  const packagingCost = safeParsedNumber(form.packagingCost);
  const estimatedPairCost = recipeCost + safeParsedNumber(form.laborCost) + safeParsedNumber(form.electricityCost) + packagingCost + safeParsedNumber(form.overheadCost);

  function updateField<K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function updateRecipeLine(index: number, key: keyof RecipeLine, value: string) {
    setForm((current) => ({
      ...current,
      recipeLines: current.recipeLines.map((line, lineIndex) => (lineIndex === index ? { ...line, [key]: value } : line)),
    }));
  }

  function updateTechnicalDocument(index: number, fileName: string) {
    setForm((current) => ({
      ...current,
      technicalDocuments: current.technicalDocuments.map((document, documentIndex) => (documentIndex === index ? { ...document, fileName } : document)),
    }));
  }

  function updateCustomerVariant(index: number, key: keyof CustomerVariant, value: string | boolean) {
    setForm((current) => ({
      ...current,
      customerVariants: current.customerVariants.map((variant, variantIndex) => (variantIndex === index ? { ...variant, [key]: value } : variant)),
    }));
  }

  function updateRouteStep(index: number, key: keyof RouteStep, value: string) {
    setForm((current) => ({
      ...current,
      routeSteps: current.routeSteps.map((step, stepIndex) => (stepIndex === index ? { ...step, [key]: value } : step)),
    }));
  }

  function handleImageUpload(kind: "product" | "mold", file: File | null) {
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = typeof reader.result === "string" ? reader.result : "";
      setForm((current) => ({
        ...current,
        ...(kind === "product"
          ? { productImageName: file.name, productImageDataUrl: dataUrl }
          : { moldImageName: file.name, moldImageDataUrl: dataUrl }),
      }));
    };
    reader.readAsDataURL(file);
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
      const response = await authenticatedFetch(isEdit && product ? `${API}/products/${product.id}` : API + "/products", {
        method: isEdit ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(toRequestBody(form)),
      });

      const result: ApiResponse<unknown> = await safeResponseJson(response).catch(() => ({}));

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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-2 backdrop-blur-sm sm:p-4">
      <div className="flex h-[94vh] w-full max-w-[100rem] flex-col overflow-hidden rounded-2xl border border-white/10 bg-[#080B10] shadow-2xl">
        <div className="space-y-5 border-b border-white/10 bg-white/[0.04] p-6">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.34em] text-emerald-300">PRODUCT MASTER</p>
              <h2 className="mt-2 text-3xl font-black text-white">
                {readonly ? "Ürün Detayı" : isEdit ? "Ürün Master Kartı Düzenle" : "Yeni Ürün Master Kartı"}
              </h2>
              <p className="mt-1 max-w-4xl text-sm text-zinc-400">
                Ürün ana verisi, üretim reçetesi, kalite toleransları, teknik dokümanları, rota ve QR izlenebilirlik verilerini tek kartta yönetin.
              </p>
            </div>
            <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white transition hover:bg-white/[0.12]">
              Kapat
            </button>
          </div>
          <div className="grid gap-4 xl:grid-cols-[260px_1fr]">
            <ProductMediaSummary form={form} />
            <ProductSummaryBadges form={form} />
          </div>
        </div>

        <div className="border-b border-white/10 px-5 pt-4">
          <div className="flex gap-2 overflow-x-auto pb-4">
            {TABS.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`whitespace-nowrap rounded-xl px-5 py-3 text-sm font-black transition sm:text-base ${
                  activeTab === tab.id ? "bg-emerald-500 text-black shadow-lg shadow-emerald-950/40" : "border border-white/10 bg-black/30 text-zinc-300 hover:bg-white/[0.08]"
                }`}
              >
                <span className="inline-flex items-center gap-2">
                  <TabIcon id={tab.id} />
                  {tab.label}
                </span>
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

          {activeTab === "general" && <GeneralTab form={form} readonly={readonly} updateField={updateField} onImageUpload={handleImageUpload} />}
          {activeTab === "production" && <ProductionTab form={form} readonly={readonly} updateField={updateField} onImageUpload={handleImageUpload} />}
          {activeTab === "recipe" && (
            <RecipeTab
              form={form}
              readonly={readonly}
              totalGram={totalGram}
              rawMaterialCost={rawMaterialCost}
              recipeCost={recipeCost}
              updateRecipeLine={updateRecipeLine}
              updateField={updateField}
            />
          )}
          {activeTab === "packaging" && <PackagingTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "quality" && <QualityTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "cost" && <CostTab form={form} readonly={readonly} rawMaterialCost={rawMaterialCost} estimatedPairCost={estimatedPairCost} updateField={updateField} />}
          {activeTab === "documents" && <TechnicalDocumentsTab form={form} readonly={readonly} updateTechnicalDocument={updateTechnicalDocument} onImageUpload={handleImageUpload} />}
          {activeTab === "variants" && <CustomerVariantsTab form={form} readonly={readonly} updateCustomerVariant={updateCustomerVariant} />}
          {activeTab === "route" && <ProductionRouteTab form={form} readonly={readonly} updateRouteStep={updateRouteStep} />}
          {activeTab === "traceability" && <TraceabilityTab form={form} readonly={readonly} updateField={updateField} />}
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

function ProductSummaryBadges({ form }: { form: ProductFormState }) {
  const badges = [
    { label: "Ürün Kodu", value: form.code || "-" },
    { label: "Ürün Adı", value: form.name || "-" },
    { label: "Müşteri", value: form.customerName || "-" },
    { label: "Foam Tipi", value: form.foamType || "-" },
    { label: "Ürün Tipi", value: form.productType || "-" },
    { label: "Numara", value: form.number || "-" },
    { label: "Kumaş", value: form.fabricType || "Yok" },
    { label: "Yapışkan", value: form.adhesiveType || "Yok" },
    { label: "DTF", value: form.hasDTFLabel ? "Var" : "Yok" },
    { label: "Ortalama Gramaj", value: form.averageWeight ? `${form.averageWeight} gr` : "-" },
    { label: "Yoğunluk", value: form.targetDensity || "-" },
    { label: "Durum", value: form.isActive ? "Aktif" : "Pasif" },
  ];

  return (
    <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-6">
      {badges.map((badge) => (
        <div key={badge.label} className="rounded-xl border border-white/10 bg-black/30 px-3 py-2">
          <p className="text-[10px] font-black uppercase tracking-[0.18em] text-zinc-500">{badge.label}</p>
          <p className="mt-1 truncate text-sm font-black text-white" title={badge.value}>
            {badge.value}
          </p>
        </div>
      ))}
    </div>
  );
}

function ProductMediaSummary({ form }: { form: ProductFormState }) {
  return (
    <div className="grid grid-cols-2 gap-3">
      <ImagePreview title="Ürün resmi" name={form.productImageName} dataUrl={form.productImageDataUrl} />
      <ImagePreview title="Kalıp resmi" name={form.moldImageName} dataUrl={form.moldImageDataUrl} />
    </div>
  );
}

function ImagePreview({ title, name, dataUrl }: { title: string; name: string; dataUrl: string }) {
  return (
    <div className="overflow-hidden rounded-xl border border-white/10 bg-black/30">
      <div className="flex aspect-[4/3] items-center justify-center bg-white/[0.04]">
        {dataUrl ? (
          <Image src={dataUrl} alt={title} width={640} height={480} unoptimized className="h-full w-full object-cover" />
        ) : (
          <div className="px-3 text-center">
            <p className="text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{title}</p>
            <p className="mt-1 text-xs text-zinc-600">Önizleme yok</p>
          </div>
        )}
      </div>
      <div className="border-t border-white/10 px-3 py-2">
        <p className="truncate text-xs font-bold text-zinc-300" title={name || title}>
          {name || title}
        </p>
      </div>
    </div>
  );
}

function TabIcon({ id }: { id: ProductTab }) {
  const paths: Record<ProductTab, string> = {
    general: "M4 5h16v14H4z M8 9h8 M8 13h5",
    production: "M5 17h14 M7 17V9l5-3 5 3v8 M10 17v-5h4v5",
    recipe: "M6 4h12v16H6z M9 8h6 M9 12h6 M9 16h3",
    packaging: "M4 8l8-4 8 4v8l-8 4-8-4z M12 12l8-4 M12 12v8 M12 12L4 8",
    quality: "M12 3l7 4v5c0 4-3 7-7 9-4-2-7-5-7-9V7z M9 12l2 2 4-5",
    cost: "M12 3v18 M7 7h7a3 3 0 010 6h-4a3 3 0 000 6h7",
    documents: "M7 3h7l4 4v14H7z M14 3v5h5 M9 13h6 M9 17h6",
    variants: "M7 7h6v6H7z M13 11h4v6h-6v-4",
    route: "M6 5h6v6H6z M12 8h4a3 3 0 010 6h-4 M12 19h6v-6h-6z",
    traceability: "M5 5h6v6H5z M13 5h6v6h-6z M5 13h6v6H5z M15 15h4v4h-4z",
    notes: "M6 4h12v16H6z M9 8h6 M9 12h6 M9 16h4",
  };

  return (
    <svg viewBox="0 0 24 24" className="h-4 w-4" aria-hidden="true">
      <path d={paths[id]} fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function GeneralTab({
  form,
  readonly,
  updateField,
  onImageUpload,
}: {
  form: ProductFormState;
  readonly: boolean;
  updateField: <K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) => void;
  onImageUpload: (kind: "product" | "mold", file: File | null) => void;
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
        <SelectInput label="Üretim Şekli" value={form.productionType} readonly={readonly} options={["FIXAR", "OEM", "Fason"]} onChange={(value) => updateField("productionType", value)} />
        <SelectInput label="Foam Tipi" value={form.foamType} readonly={readonly} options={["10100", "10900"]} onChange={(value) => updateField("foamType", value)} />
        <SelectInput label="Ürün Tipi" value={form.productType} readonly={readonly} options={["Normal", "Memory Foam"]} onChange={(value) => updateField("productType", value)} />
        <SelectInput label="Varsayılan Para Birimi" value={form.currency} readonly={readonly} options={["TRY", "USD", "EUR"]} onChange={(value) => updateField("currency", value)} />
        <SelectInput label="Birim" value={form.unit} readonly={readonly} options={["Çift", "Adet", "Kg"]} onChange={(value) => updateField("unit", value)} />
        <ToggleInput label="Aktif / Pasif" checked={form.isActive} readonly={readonly} trueText="Aktif" falseText="Pasif" onChange={(value) => updateField("isActive", value)} />
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <FileUploadBox
          title="Ürün Resmi Yükle"
          description="Ürün ana görselini sürükle bırak veya seç."
          fileName={form.productImageName}
          previewDataUrl={form.productImageDataUrl}
          readonly={readonly}
          accept="image/*"
          onFile={(file) => onImageUpload("product", file)}
        />
      </div>
      <TextAreaInput label="Açıklama" value={form.description} readonly={readonly} onChange={(value) => updateField("description", value)} />
    </TabPanel>
  );
}

function ProductionTab({
  form,
  readonly,
  updateField,
  onImageUpload,
}: {
  form: ProductFormState;
  readonly: boolean;
  updateField: <K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) => void;
  onImageUpload: (kind: "product" | "mold", file: File | null) => void;
}) {
  return (
    <TabPanel title="Üretim" note="Standart üretim parametreleri iş emirleri ve kapasite planlamasına temel olur.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <SelectInput label="Kumaş" value={form.fabricType} readonly={readonly} options={["Yok", "Interlok", "Lacoste", "Mesh", "Keçe", "Diğer"]} onChange={(value) => updateField("fabricType", value)} />
        <SelectInput label="Yapışkan" value={form.adhesiveType} readonly={readonly} options={["Yok", "Normal", "Polibond"]} onChange={(value) => updateField("adhesiveType", value)} />
        <ToggleInput label="DTF" checked={form.hasDTFLabel} readonly={readonly} trueText="Var" falseText="Yok" onChange={(value) => updateField("hasDTFLabel", value)} />
        <ToggleInput label="Polibond" checked={form.hasPolibond} readonly={readonly} trueText="Var" falseText="Yok" onChange={(value) => updateField("hasPolibond", value)} />
        <TextInput label="Ortalama Ürün Gramajı" value={form.averageWeight} readonly={readonly} type="number" onChange={(value) => updateField("averageWeight", value)} />
        <TextInput label="Hedef Yoğunluk" value={form.targetDensity} readonly={readonly} type="number" onChange={(value) => updateField("targetDensity", value)} />
        <TextInput label="Standart Pişme Süresi" value={form.standardCycleTime} readonly={readonly} type="number" onChange={(value) => updateField("standardCycleTime", value)} />
        <TextInput label="Standart Günlük Kapasite" value={form.standardDailyCapacity} readonly={readonly} type="number" onChange={(value) => updateField("standardDailyCapacity", value)} />
        <TextInput label="Standart Fire %" value={form.standardWasteRate} readonly={readonly} type="number" onChange={(value) => updateField("standardWasteRate", value)} />
        <SelectInput label="Kesim Makinesi" value={form.cuttingMachine} readonly={readonly} options={["Gezer Kafa", "Döner Kafa"]} onChange={(value) => updateField("cuttingMachine", value)} />
        <TextInput label="Standart Kalıp" value={form.standardMold} readonly={readonly} onChange={(value) => updateField("standardMold", value)} />
        <TextInput label="Kalıp Ağırlığı" value={form.moldWeight} readonly={readonly} type="number" onChange={(value) => updateField("moldWeight", value)} />
        <TextInput label="Kalıp X Koordinatı" value={form.moldXCoordinate} readonly={readonly} type="number" onChange={(value) => updateField("moldXCoordinate", value)} />
        <TextInput label="Kalıp Y Koordinatı" value={form.moldYCoordinate} readonly={readonly} type="number" onChange={(value) => updateField("moldYCoordinate", value)} />
      </div>
      {form.hasDTFLabel && (
        <div className="rounded-2xl border border-cyan-400/20 bg-cyan-500/10 p-4">
          <div className="mb-4">
            <h4 className="text-lg font-black text-white">DTF Logo Spesifikasyonu</h4>
            <p className="mt-1 text-sm text-cyan-100/80">DTF, ambalaj etiketi değil; kumaş üzerine sıcak transfer ile uygulanan müşteri logosudur.</p>
          </div>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <TextInput label="DTF Kodu" value={form.dtfCode} readonly={readonly} onChange={(value) => updateField("dtfCode", value)} />
            <TextInput label="DTF Adı" value={form.dtfName} readonly={readonly} onChange={(value) => updateField("dtfName", value)} />
            <TextInput label="DTF Ölçüsü" value={form.dtfSize} readonly={readonly} onChange={(value) => updateField("dtfSize", value)} />
            <TextInput label="DTF Konumu" value={form.dtfPosition} readonly={readonly} onChange={(value) => updateField("dtfPosition", value)} />
          </div>
          <TextAreaInput label="DTF Baskı Notu" value={form.dtfPrintNote} readonly={readonly} onChange={(value) => updateField("dtfPrintNote", value)} />
        </div>
      )}
      <FileUploadBox
        title="Kalıp Fotoğrafı"
        description="Kalıp görselini sürükle bırak veya seç."
        fileName={form.moldImageName}
        previewDataUrl={form.moldImageDataUrl}
        readonly={readonly}
        accept="image/*"
        onFile={(file) => onImageUpload("mold", file)}
      />
      <TextAreaInput label="DTF Açıklaması" value={form.dtfDescription} readonly={readonly || !form.hasDTFLabel} onChange={(value) => updateField("dtfDescription", value)} />
      <TextAreaInput label="Varsayılan Operasyon Notu" value={form.defaultOperationNote} readonly={readonly} onChange={(value) => updateField("defaultOperationNote", value)} />
    </TabPanel>
  );
}

function RecipeTab({
  form,
  readonly,
  totalGram,
  rawMaterialCost,
  recipeCost,
  updateRecipeLine,
  updateField,
}: {
  form: ProductFormState;
  readonly: boolean;
  totalGram: number;
  rawMaterialCost: number;
  recipeCost: number;
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
              <FragmentRow key={line.material} line={line}>
                <tr>
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
              </FragmentRow>
            ))}
          </tbody>
        </table>
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        <div className="rounded-xl border border-white/10 bg-black/25 p-4">
          <p className="text-xs font-black uppercase tracking-[0.18em] text-zinc-500">Toplam Gram</p>
          <p className="mt-2 text-2xl font-black text-white">{formatNumber(totalGram)} gr</p>
        </div>
        <div className="rounded-xl border border-emerald-400/30 bg-emerald-500/10 p-4">
          <p className="text-xs font-black uppercase tracking-[0.18em] text-emerald-200">Toplam Hammadde Maliyeti</p>
          <p className="mt-2 text-2xl font-black text-white">{formatCurrency(rawMaterialCost, form.currency)}</p>
        </div>
        <div className="rounded-xl border border-cyan-400/30 bg-cyan-500/10 p-4">
          <p className="text-xs font-black uppercase tracking-[0.18em] text-cyan-200">Toplam Reçete Maliyeti</p>
          <p className="mt-2 text-2xl font-black text-white">{formatCurrency(recipeCost, form.currency)}</p>
        </div>
      </div>
      <TextAreaInput label="Reçete Notu" value={form.recipeNote} readonly={readonly} onChange={(value) => updateField("recipeNote", value)} />
    </TabPanel>
  );
}

function FragmentRow({ line, children }: { line: RecipeLine; children: ReactNode }) {
  return (
    <>
      {children}
      {line.material === "Crosskim" && (
        <tr>
          <td colSpan={5} className="px-3 pb-4 pt-0">
            <div className="rounded-xl border border-amber-400/30 bg-amber-500/10 p-3 text-sm font-bold text-amber-100">
              Crosskim doğrudan makineye verilmez. 180 kg Poliol kazanına ilave edilen katkıdır.
            </div>
          </td>
        </tr>
      )}
    </>
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
    <TabPanel title="Paketleme" note="OEM iç taban üretimi için paket, sağ-sol ve koli içeriği sevkiyat standardını belirler.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <SelectInput label="Paketleme Tipi" value={form.packagingType} readonly={readonly} options={["Çift", "Sağ/Sol Ayrı", "Müşteriye Özel"]} onChange={(value) => updateField("packagingType", value)} />
        <TextInput label="Bir Paket İçeriği" value={form.packageContent} readonly={readonly} onChange={(value) => updateField("packageContent", value)} />
        <TextInput label="Koli İçeriği" value={form.cartonContent} readonly={readonly} onChange={(value) => updateField("cartonContent", value)} />
        <TextInput label="Varsayılan Çift Adedi" value={form.defaultPairQuantity} readonly={readonly} onChange={(value) => updateField("defaultPairQuantity", value)} />
        <TextInput label="Koli Tipi" value={form.boxType} readonly={readonly} onChange={(value) => updateField("boxType", value)} />
        <ToggleInput label="Müşteri Etiketi" checked={form.customerLabel} readonly={readonly} trueText="Var" falseText="Yok" onChange={(value) => updateField("customerLabel", value)} />
      </div>
      <div className="rounded-xl border border-white/10 bg-black/20 p-4 text-sm text-zinc-300">
        <p className="font-black text-white">Örnekler</p>
        <p className="mt-2">Bir Paket İçeriği: 10 Sağ, 10 Sol veya 10 Çift</p>
        <p>Koli İçeriği: 200 Çift, 250 Çift veya 300 Çift</p>
      </div>
      <TextAreaInput label="Koli Notu" value={form.cartonNote} readonly={readonly} onChange={(value) => updateField("cartonNote", value)} />
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
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
        <TextInput label="Minimum Gramaj" value={form.minWeight} readonly={readonly} type="number" onChange={(value) => updateField("minWeight", value)} />
        <TextInput label="Maksimum Gramaj" value={form.maxWeight} readonly={readonly} type="number" onChange={(value) => updateField("maxWeight", value)} />
        <TextInput label="Minimum Yoğunluk" value={form.minDensity} readonly={readonly} type="number" onChange={(value) => updateField("minDensity", value)} />
        <TextInput label="Maksimum Yoğunluk" value={form.maxDensity} readonly={readonly} type="number" onChange={(value) => updateField("maxDensity", value)} />
        <TextInput label="Kabul Edilen Fire %" value={form.acceptedWasteRate} readonly={readonly} type="number" onChange={(value) => updateField("acceptedWasteRate", value)} />
        <TextInput label="Shore Sertliği" value={form.shoreHardness} readonly={readonly} type="number" onChange={(value) => updateField("shoreHardness", value)} />
      </div>
      <TextAreaInput label="Kalite Kontrol Notu" value={form.qualityNote} readonly={readonly} onChange={(value) => updateField("qualityNote", value)} />
    </TabPanel>
  );
}

function CostTab({
  form,
  readonly,
  rawMaterialCost,
  estimatedPairCost,
  updateField,
}: {
  form: ProductFormState;
  readonly: boolean;
  rawMaterialCost: number;
  estimatedPairCost: number;
  updateField: <K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) => void;
}) {
  return (
    <TabPanel title="Maliyet" note="Şimdilik frontend üzerinde hesaplanan tahmini çift maliyeti görünümü.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <CostCard title="Hammadde" value={formatCurrency(rawMaterialCost, form.currency)} note="Reçete grid toplamı" />
        <EditableCostCard title="İşçilik" value={form.laborCost} readonly={readonly} currency={form.currency} onChange={(value) => updateField("laborCost", value)} />
        <EditableCostCard title="Elektrik" value={form.electricityCost} readonly={readonly} currency={form.currency} onChange={(value) => updateField("electricityCost", value)} />
        <EditableCostCard title="Paketleme" value={form.packagingCost} readonly={readonly} currency={form.currency} onChange={(value) => updateField("packagingCost", value)} />
        <EditableCostCard title="Genel Gider" value={form.overheadCost} readonly={readonly} currency={form.currency} onChange={(value) => updateField("overheadCost", value)} />
      </div>
      <div className="rounded-2xl border border-emerald-400/30 bg-emerald-500/10 p-6 shadow-xl shadow-emerald-950/20">
        <p className="text-xs font-black uppercase tracking-[0.22em] text-emerald-200">Tahmini Çift Maliyeti</p>
        <p className="mt-3 text-4xl font-black text-white">{formatCurrency(estimatedPairCost, form.currency)}</p>
        <p className="mt-2 text-sm text-emerald-100/80">Hammadde + işçilik + elektrik + paketleme + genel gider toplamı</p>
      </div>
    </TabPanel>
  );
}

function TechnicalDocumentsTab({
  form,
  readonly,
  updateTechnicalDocument,
  onImageUpload,
}: {
  form: ProductFormState;
  readonly: boolean;
  updateTechnicalDocument: (index: number, fileName: string) => void;
  onImageUpload: (kind: "product" | "mold", file: File | null) => void;
}) {
  return (
    <TabPanel title="Teknik Dokümanlar" note="Ürün, kalıp ve üretim dokümanları ileride doküman yönetimi ile eşleşecek şekilde tutulur.">
      <div className="grid gap-4 lg:grid-cols-2">
        <FileUploadBox
          title="Ürün resmi"
          description="Ürün görseli"
          fileName={form.productImageName}
          previewDataUrl={form.productImageDataUrl}
          readonly={readonly}
          accept="image/*"
          onFile={(file) => onImageUpload("product", file)}
        />
        <FileUploadBox
          title="Kalıp resmi"
          description="Kalıp görseli"
          fileName={form.moldImageName}
          previewDataUrl={form.moldImageDataUrl}
          readonly={readonly}
          accept="image/*"
          onFile={(file) => onImageUpload("mold", file)}
        />
      </div>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {form.technicalDocuments.map((document, index) => (
          <DocumentUploadCard key={document.key} document={document} readonly={readonly} onFileName={(fileName) => updateTechnicalDocument(index, fileName)} />
        ))}
      </div>
    </TabPanel>
  );
}

function CustomerVariantsTab({
  form,
  readonly,
  updateCustomerVariant,
}: {
  form: ProductFormState;
  readonly: boolean;
  updateCustomerVariant: (index: number, key: keyof CustomerVariant, value: string | boolean) => void;
}) {
  return (
    <TabPanel title="Müşteri Varyantları" note="Her satır aynı ana ürünün müşteri, kumaş, DTF, yapışkan, kalıp ve reçete kombinasyonunu temsil eden üretim spesifikasyonudur.">
      <div className="rounded-xl border border-emerald-400/20 bg-emerald-500/10 p-4 text-sm text-emerald-100">
        Kumaş, DTF, kalıp, yapışkan, müşteri ve hammadde bilgileri ileride kendi ana kartlarından seçilecek. Seçim yapıldığında teknik bilgiler otomatik dolacak şekilde bu UI hazırlandı.
      </div>
      <div className="overflow-x-auto rounded-xl border border-white/10 bg-black/20">
        <table className="min-w-[1720px] w-full text-left text-sm">
          <thead>
            <tr className="border-b border-white/10 text-xs uppercase tracking-[0.18em] text-zinc-500">
              <th className="p-3">Müşteri</th>
              <th className="p-3">Varyant Kodu</th>
              <th className="p-3">Kumaş Tipi</th>
              <th className="p-3">Yapışkan Tipi</th>
              <th className="p-3">DTF</th>
              <th className="p-3">DTF Kodu</th>
              <th className="p-3">Kalıp</th>
              <th className="p-3">Numara</th>
              <th className="p-3">Renk</th>
              <th className="p-3">Gramaj</th>
              <th className="p-3">Yoğunluk</th>
              <th className="p-3">Pişme Süresi</th>
              <th className="p-3">Reçete Notu</th>
              <th className="p-3">Durum</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/10">
            {form.customerVariants.map((variant, index) => (
              <tr key={variant.id}>
                <td className="p-3">
                  <input value={variant.customerName} disabled={readonly} onChange={(event) => updateCustomerVariant(index, "customerName", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <input value={variant.variantCode} disabled={readonly} onChange={(event) => updateCustomerVariant(index, "variantCode", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <select value={variant.fabricType} disabled={readonly} onChange={(event) => updateCustomerVariant(index, "fabricType", event.target.value)} className={CONTROL_CLASS}>
                    {["Yok", "Interlok", "Lacoste", "Mesh", "Keçe", "Diğer"].map((option) => (
                      <option key={option}>{option}</option>
                    ))}
                  </select>
                </td>
                <td className="p-3">
                  <select value={variant.adhesiveType} disabled={readonly} onChange={(event) => updateCustomerVariant(index, "adhesiveType", event.target.value)} className={CONTROL_CLASS}>
                    {["Yok", "Normal", "Polibond"].map((option) => (
                      <option key={option}>{option}</option>
                    ))}
                  </select>
                </td>
                <td className="p-3">
                  <button
                    type="button"
                    disabled={readonly}
                    onClick={() => updateCustomerVariant(index, "hasDTFLabel", !variant.hasDTFLabel)}
                    className={`rounded-xl px-4 py-3 text-sm font-black transition disabled:cursor-not-allowed ${
                      variant.hasDTFLabel ? "bg-cyan-500/15 text-cyan-200" : "bg-white/[0.06] text-zinc-300"
                    }`}
                  >
                    {variant.hasDTFLabel ? "Var" : "Yok"}
                  </button>
                </td>
                <td className="p-3">
                  <input value={variant.dtfCode} disabled={readonly || !variant.hasDTFLabel} onChange={(event) => updateCustomerVariant(index, "dtfCode", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <input value={variant.mold} disabled={readonly} onChange={(event) => updateCustomerVariant(index, "mold", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <input value={variant.number} disabled={readonly} onChange={(event) => updateCustomerVariant(index, "number", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <input value={variant.color} disabled={readonly} onChange={(event) => updateCustomerVariant(index, "color", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <input value={variant.averageWeight} type="number" step="0.01" disabled={readonly} onChange={(event) => updateCustomerVariant(index, "averageWeight", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <input value={variant.targetDensity} type="number" step="0.01" disabled={readonly} onChange={(event) => updateCustomerVariant(index, "targetDensity", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <input value={variant.standardCycleTime} type="number" step="0.01" disabled={readonly} onChange={(event) => updateCustomerVariant(index, "standardCycleTime", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <input value={variant.recipeNote} disabled={readonly} onChange={(event) => updateCustomerVariant(index, "recipeNote", event.target.value)} className={CONTROL_CLASS} />
                </td>
                <td className="p-3">
                  <button
                    type="button"
                    disabled={readonly}
                    onClick={() => updateCustomerVariant(index, "isActive", !variant.isActive)}
                    className={`rounded-xl px-4 py-3 text-sm font-black transition disabled:cursor-not-allowed ${
                      variant.isActive ? "bg-emerald-500/15 text-emerald-200" : "bg-red-500/15 text-red-200"
                    }`}
                  >
                    {variant.isActive ? "Aktif" : "Pasif"}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </TabPanel>
  );
}

function ProductionRouteTab({
  form,
  readonly,
  updateRouteStep,
}: {
  form: ProductFormState;
  readonly: boolean;
  updateRouteStep: (index: number, key: keyof RouteStep, value: string) => void;
}) {
  return (
    <TabPanel title="Üretim Rotası" note="Operasyon sırası iş emri akışına temel olacak şekilde tutulur.">
      <div className="space-y-3">
        {form.routeSteps.map((step, index) => (
          <div key={step.id} className="grid gap-3 rounded-xl border border-white/10 bg-black/20 p-4 lg:grid-cols-[44px_1.2fr_1fr_1fr_1.6fr] lg:items-center">
            <div className="flex h-11 w-11 items-center justify-center rounded-full border border-emerald-400/30 bg-emerald-500/10 text-sm font-black text-emerald-200">
              {index + 1}
            </div>
            <TextInput label="Operasyon" value={step.operation} readonly={readonly} onChange={(value) => updateRouteStep(index, "operation", value)} />
            <TextInput label="Standart Süre" value={step.standardTime} readonly={readonly} onChange={(value) => updateRouteStep(index, "standardTime", value)} />
            <TextInput label="Sorumlu Departman" value={step.department} readonly={readonly} onChange={(value) => updateRouteStep(index, "department", value)} />
            <TextInput label="Operasyon Notu" value={step.note} readonly={readonly} onChange={(value) => updateRouteStep(index, "note", value)} />
          </div>
        ))}
      </div>
    </TabPanel>
  );
}

function TraceabilityTab({
  form,
  readonly,
  updateField,
}: {
  form: ProductFormState;
  readonly: boolean;
  updateField: <K extends keyof ProductFormState>(key: K, value: ProductFormState[K]) => void;
}) {
  return (
    <TabPanel title="QR ve Barkod" note="QR şablonu, GTIN, lot formatı ve etiket şablonu sevkiyat ve izlenebilirlik süreçlerini besler.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <TextInput label="QR Şablonu" value={form.qrTemplate} readonly={readonly} onChange={(value) => updateField("qrTemplate", value)} />
        <TextInput label="Barkod" value={form.barcode} readonly={readonly} onChange={(value) => updateField("barcode", value)} />
        <TextInput label="GTIN" value={form.gtin} readonly={readonly} onChange={(value) => updateField("gtin", value)} />
        <TextInput label="Lot Formatı" value={form.lotFormat} readonly={readonly} onChange={(value) => updateField("lotFormat", value)} />
        <TextInput label="Etiket Şablonu" value={form.labelTemplate} readonly={readonly} onChange={(value) => updateField("labelTemplate", value)} />
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

function FileUploadBox({
  title,
  description,
  fileName,
  previewDataUrl,
  readonly,
  accept,
  onFile,
}: {
  title: string;
  description: string;
  fileName: string;
  previewDataUrl?: string;
  readonly: boolean;
  accept: string;
  onFile: (file: File | null) => void;
}) {
  return (
    <label className={`block rounded-2xl border border-dashed border-white/15 bg-black/25 p-4 transition ${readonly ? "cursor-not-allowed opacity-75" : "cursor-pointer hover:border-emerald-400/50 hover:bg-emerald-500/5"}`}>
      <input
        type="file"
        accept={accept}
        disabled={readonly}
        className="hidden"
        onChange={(event) => onFile(event.target.files?.[0] ?? null)}
      />
      <div className="grid gap-4 sm:grid-cols-[140px_1fr] sm:items-center">
        <div className="flex aspect-[4/3] items-center justify-center overflow-hidden rounded-xl border border-white/10 bg-white/[0.04]">
          {previewDataUrl ? (
            <Image src={previewDataUrl} alt={title} width={640} height={480} unoptimized className="h-full w-full object-cover" />
          ) : (
            <span className="px-3 text-center text-xs font-black uppercase tracking-[0.18em] text-zinc-500">Önizleme</span>
          )}
        </div>
        <div>
          <p className="text-base font-black text-white">{title}</p>
          <p className="mt-1 text-sm text-zinc-400">{description}</p>
          <p className="mt-3 rounded-xl border border-white/10 bg-black/30 px-3 py-2 text-sm font-bold text-zinc-300">
            {fileName || "Dosyayı buraya bırakın veya seçin"}
          </p>
        </div>
      </div>
    </label>
  );
}

function DocumentUploadCard({
  document,
  readonly,
  onFileName,
}: {
  document: TechnicalDocument;
  readonly: boolean;
  onFileName: (fileName: string) => void;
}) {
  return (
    <label className={`block rounded-2xl border border-white/10 bg-black/25 p-4 transition ${readonly ? "cursor-not-allowed opacity-75" : "cursor-pointer hover:border-emerald-400/40 hover:bg-white/[0.04]"}`}>
      <input
        type="file"
        disabled={readonly}
        className="hidden"
        onChange={(event) => onFileName(event.target.files?.[0]?.name ?? "")}
      />
      <p className="text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{document.label}</p>
      <p className="mt-3 min-h-6 truncate text-sm font-black text-white" title={document.fileName || document.label}>
        {document.fileName || "Dosya seçilmedi"}
      </p>
      <p className="mt-2 text-xs text-zinc-500">Frontend state içinde tutulur.</p>
    </label>
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
  const displayProductType = normalizeOption(parsed.details.displayProductType || product.productType || "Normal", ["Normal", "Memory Foam"], "Normal");
  const fabricType = normalizeOption(parsed.details.fabricType || (product.isFabric ? "Interlok" : "Yok"), ["Yok", "Interlok", "Lacoste", "Mesh", "Keçe", "Diğer"], "Yok");
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
    productionType: normalizeOption(parsed.details.productionType, ["FIXAR", "OEM", "Fason"], "FIXAR"),
    unit: normalizeOption(parsed.details.unit, ["Çift", "Adet", "Kg"], "Çift"),
    fabricType,
    adhesiveType: normalizeOption(adhesiveType, ["Yok", "Normal", "Polibond"], "Yok"),
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
    productionType: normalizeOption(form.productionType, ["FIXAR", "OEM", "Fason"], "FIXAR"),
    displayProductType: normalizeOption(form.productType, ["Normal", "Memory Foam"], "Normal"),
    currency: form.currency,
    unit: normalizeOption(form.unit, ["Çift", "Adet", "Kg"], "Çift"),
    productImageName: form.productImageName,
    productImageDataUrl: form.productImageDataUrl,
    fabricType: form.fabricType,
    adhesiveType: form.adhesiveType,
    dtfCode: form.dtfCode,
    dtfName: form.dtfName,
    dtfSize: form.dtfSize,
    dtfPosition: form.dtfPosition,
    dtfPrintNote: form.dtfPrintNote,
    dtfDescription: form.dtfDescription,
    standardDailyCapacity: form.standardDailyCapacity,
    standardWasteRate: form.standardWasteRate,
    cuttingMachine: form.cuttingMachine,
    standardMold: form.standardMold,
    moldImageName: form.moldImageName,
    moldImageDataUrl: form.moldImageDataUrl,
    moldWeight: form.moldWeight,
    moldXCoordinate: form.moldXCoordinate,
    moldYCoordinate: form.moldYCoordinate,
    defaultOperationNote: form.defaultOperationNote,
    recipeLines: form.recipeLines,
    recipeNote: form.recipeNote,
    packagingType: form.packagingType,
    packageContent: form.packageContent,
    cartonContent: form.cartonContent,
    defaultPairQuantity: form.defaultPairQuantity,
    customerLabel: form.customerLabel,
    cartonNote: form.cartonNote,
    boxType: form.boxType,
    boxSize: form.boxSize,
    boxWeight: form.boxWeight,
    innerBag: form.innerBag,
    shrink: form.shrink,
    labelType: form.labelType,
    barcode: form.barcode,
    qrCode: form.qrCode,
    packagingNote: form.packagingNote,
    minWeight: form.minWeight,
    maxWeight: form.maxWeight,
    minDensity: form.minDensity,
    maxDensity: form.maxDensity,
    acceptedWasteRate: form.acceptedWasteRate,
    shoreHardness: form.shoreHardness,
    qualityNote: form.qualityNote,
    laborCost: form.laborCost,
    electricityCost: form.electricityCost,
    packagingCost: form.packagingCost,
    overheadCost: form.overheadCost,
    technicalDocuments: form.technicalDocuments,
    customerVariants: form.customerVariants,
    routeSteps: form.routeSteps,
    qrTemplate: form.qrTemplate,
    gtin: form.gtin,
    lotFormat: form.lotFormat,
    labelTemplate: form.labelTemplate,
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
    const technicalDocuments = normalizeTechnicalDocuments(parsed.technicalDocuments);
    const customerVariants = normalizeCustomerVariants(parsed.customerVariants);
    const routeSteps = normalizeRouteSteps(parsed.routeSteps);

    return {
      description: visibleDescription,
      details: {
        ...cloneDetails(emptyDetails),
        ...parsed,
        recipeLines: migratedRecipeLines,
        technicalDocuments,
        customerVariants,
        routeSteps,
        displayProductType: typeof parsed.displayProductType === "string" ? parsed.displayProductType : "",
      },
    };
  } catch {
    return { description: visibleDescription, details: cloneDetails(emptyDetails) };
  }
}

function normalizeRecipeLines(lines: unknown[]) {
  const normalized = RECIPE_MATERIALS.map((material) => {
    const existing = lines.find((line) => {
      if (typeof line !== "object" || line === null) return false;
      const lineMaterial = (line as { material?: unknown }).material;
      if (lineMaterial === material) return true;
      return material === "DTF" && lineMaterial === "DTF Etiket";
    }) as Partial<RecipeLine> | undefined;
    return {
      material,
      quantity: typeof existing?.quantity === "string" ? existing.quantity : "",
      unit: typeof existing?.unit === "string" ? existing.unit : material === "Koli" || material === "Etiket" || material === "DTF" ? "Adet" : "gr",
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

function normalizeTechnicalDocuments(value: unknown) {
  if (!Array.isArray(value)) return emptyTechnicalDocuments.map((document) => ({ ...document }));

  return emptyTechnicalDocuments.map((document) => {
    const existing = value.find((item) => typeof item === "object" && item !== null && (item as { key?: unknown }).key === document.key) as Partial<TechnicalDocument> | undefined;
    return {
      ...document,
      fileName: typeof existing?.fileName === "string" ? existing.fileName : "",
    };
  });
}

function normalizeCustomerVariants(value: unknown) {
  if (!Array.isArray(value)) return emptyCustomerVariants.map((variant) => ({ ...variant }));

  const normalized = value
    .filter((item): item is Partial<CustomerVariant> => typeof item === "object" && item !== null)
    .map((variant, index) => {
      const legacyVariant = variant as Partial<CustomerVariant> & { code?: unknown; description?: unknown };

      return {
      id: typeof variant.id === "string" ? variant.id : `variant-${index + 1}`,
      customerName: typeof variant.customerName === "string" ? variant.customerName : "",
      variantCode: typeof variant.variantCode === "string" ? variant.variantCode : typeof legacyVariant.code === "string" ? legacyVariant.code : "",
      fabricType: normalizeOption(variant.fabricType, ["Yok", "Interlok", "Lacoste", "Mesh", "Keçe", "Diğer"], "Yok"),
      adhesiveType: normalizeOption(variant.adhesiveType, ["Yok", "Normal", "Polibond"], "Yok"),
      hasDTFLabel: typeof variant.hasDTFLabel === "boolean" ? variant.hasDTFLabel : false,
      dtfCode: typeof variant.dtfCode === "string" ? variant.dtfCode : "",
      mold: typeof variant.mold === "string" ? variant.mold : "",
      number: typeof variant.number === "string" ? variant.number : "",
      color: typeof variant.color === "string" ? variant.color : "",
      averageWeight: typeof variant.averageWeight === "string" ? variant.averageWeight : "",
      targetDensity: typeof variant.targetDensity === "string" ? variant.targetDensity : "",
      standardCycleTime: typeof variant.standardCycleTime === "string" ? variant.standardCycleTime : "",
      recipeNote: typeof variant.recipeNote === "string" ? variant.recipeNote : typeof legacyVariant.description === "string" ? legacyVariant.description : "",
      isActive: typeof variant.isActive === "boolean" ? variant.isActive : true,
      };
    });

  return normalized.length > 0 ? normalized : emptyCustomerVariants.map((variant) => ({ ...variant }));
}

function normalizeRouteSteps(value: unknown) {
  if (!Array.isArray(value)) return emptyRouteSteps.map((step) => ({ ...step }));

  const normalized = value
    .filter((item): item is Partial<RouteStep> => typeof item === "object" && item !== null)
    .map((step, index) => ({
      id: typeof step.id === "string" ? step.id : `route-${index + 1}`,
      operation: typeof step.operation === "string" ? step.operation : "",
      standardTime: typeof step.standardTime === "string" ? step.standardTime : "",
      department: typeof step.department === "string" ? step.department : "",
      note: typeof step.note === "string" ? step.note : "",
    }));

  return normalized.length > 0 ? normalized : emptyRouteSteps.map((step) => ({ ...step }));
}

function cloneDetails(details: ProductMasterDetails): ProductMasterDetails {
  return {
    ...details,
    recipeLines: details.recipeLines.map((line) => ({ ...line })),
    technicalDocuments: details.technicalDocuments.map((document) => ({ ...document })),
    customerVariants: details.customerVariants.map((variant) => ({ ...variant })),
    routeSteps: details.routeSteps.map((step) => ({ ...step })),
  };
}

function cloneForm(form: ProductFormState): ProductFormState {
  return {
    ...form,
    recipeLines: form.recipeLines.map((line) => ({ ...line })),
    technicalDocuments: form.technicalDocuments.map((document) => ({ ...document })),
    customerVariants: form.customerVariants.map((variant) => ({ ...variant })),
    routeSteps: form.routeSteps.map((step) => ({ ...step })),
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

function normalizeOption(value: string | undefined | null, options: string[], fallback: string) {
  if (!value) return fallback;
  return options.includes(value) ? value : fallback;
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

function toInputNumber(value?: number | null) {
  return typeof value === "number" && Number.isFinite(value) ? String(value) : "";
}

function calculateLineTotal(line: RecipeLine) {
  return safeParsedNumber(line.quantity) * safeParsedNumber(line.unitPrice);
}

function calculateTotalGram(lines: RecipeLine[]) {
  return lines
    .filter((line) => line.unit.toLocaleLowerCase("tr-TR") === "gr")
    .reduce((sum, line) => sum + safeParsedNumber(line.quantity), 0);
}

function calculateRawMaterialCost(lines: RecipeLine[]) {
  return lines
    .filter((line) => !["Koli", "Diğer"].includes(line.material))
    .reduce((sum, line) => sum + calculateLineTotal(line), 0);
}

function calculateRecipeTotal(lines: RecipeLine[]) {
  return lines.reduce((sum, line) => sum + calculateLineTotal(line), 0);
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
