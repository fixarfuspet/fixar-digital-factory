"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import { safeResponseJson } from "../lib/api/client";

type DashboardTone = "emerald" | "cyan" | "amber" | "red" | "blue" | "violet";
type DialogMode = "create" | "edit" | "detail" | null;
type RecipeTab = "general" | "materials" | "production" | "cost" | "revision" | "notes";

type Product = {
  id: string;
  code?: string | null;
  name?: string | null;
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

type ProductDetails = {
  number?: string;
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
};

type RecipeLine = {
  key: string;
  order: number;
  role: string;
  materialId: string;
  quantity: string;
  wasteRate: string;
  note: string;
};

type RecipeRecord = {
  id: string;
  code: string;
  name: string;
  productId: string;
  version: string;
  revisionNo: string;
  isActive: boolean;
  isDefault: boolean;
  outputQuantity: string;
  outputUnit: string;
  startDate: string;
  endDate: string;
  lines: RecipeLine[];
  standardWeight: string;
  standardDensity: string;
  standardCycleTime: string;
  standardWasteRate: string;
  standardMold: string;
  standardMachine: string;
  standardDailyCapacity: string;
  cuttingMachine: string;
  operationNote: string;
  laborCost: string;
  electricityCost: string;
  packagingCost: string;
  overheadCost: string;
  revisionDescription: string;
  updatedBy: string;
  revisionDate: string;
  productionNote: string;
  qualityNote: string;
  customerNote: string;
  generalNote: string;
  updatedAt: string;
};

type ApiResponse<T> = {
  data?: T;
  message?: string;
  errorCode?: string;
};

const API = "/api/backend/api/v1";
const PRODUCT_MARKER = "\n\n---FIXAR_PRODUCT_MASTER_JSON---\n";
const RECIPE_MARKER = "\n\n---FIXAR_RECIPE_UI_JSON---\n";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60 disabled:cursor-not-allowed disabled:opacity-70";
const RECIPE_ROLES = ["Poliol", "İzosiyanat", "Crosskim", "Pigment", "Solvent", "Kalıp Ayırıcı", "Kumaş", "Yapışkan", "DTF", "İşçilik", "Diğer"];
const TABS: Array<{ id: RecipeTab; label: string }> = [
  { id: "general", label: "1 Genel" },
  { id: "materials", label: "2 Hammadde Listesi" },
  { id: "production", label: "3 Üretim Parametreleri" },
  { id: "cost", label: "4 Maliyet" },
  { id: "revision", label: "5 Revizyon" },
  { id: "notes", label: "6 Açıklamalar" },
];
const AUTH_REDIRECT = "AUTH_REDIRECT";

async function fetchMasterData(url: string): Promise<unknown> {
  const response = await fetch(url, { cache: "no-store" });
  if (response.status === 401) {
    window.location.assign("/");
    throw new Error(AUTH_REDIRECT);
  }
  const contentType = response.headers.get("content-type")?.toLowerCase() ?? "";
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  if (!contentType.includes("json")) throw new Error(`Beklenmeyen yanıt türü: ${contentType || "boş"}`);
  try {
    return await safeResponseJson(response) as unknown;
  } catch (error) {
    throw new Error("API yanıtı ayrıştırılamadı.", { cause: error });
  }
}

function isAuthRedirect(reason: unknown) {
  return reason instanceof Error && reason.message === AUTH_REDIRECT;
}

export default function RecipesPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [materials, setMaterials] = useState<Material[]>([]);
  const [recipes, setRecipes] = useState<RecipeRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [dialogMode, setDialogMode] = useState<DialogMode>(null);
  const [dialogRecipe, setDialogRecipe] = useState<RecipeRecord | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  async function loadData() {
    setLoading(true);
    setError(null);
    const [productsResult, materialsResult, recipesResult] = await Promise.allSettled([
      fetchMasterData(API + "/products"),
      fetchMasterData(API + "/materials"),
      fetchMasterData(API + "/recipes?includeItems=true"),
    ]);
    const errors: string[] = [];
    const productList = productsResult.status === "fulfilled"
      ? extractProducts(productsResult.value).filter((product) => product.isActive !== false)
      : [];
    const materialList = materialsResult.status === "fulfilled"
      ? extractMaterials(materialsResult.value).filter((material) => material.isActive !== false)
      : [];

    if (productsResult.status === "rejected" && !isAuthRedirect(productsResult.reason)) {
      console.error("Ürün listesi yüklenemedi.", productsResult.reason);
      errors.push("Ürün listesi yüklenemedi.");
    }
    if (materialsResult.status === "rejected" && !isAuthRedirect(materialsResult.reason)) {
      console.error("Malzeme listesi yüklenemedi.", materialsResult.reason);
      errors.push("Malzeme listesi yüklenemedi.");
    }
    if (recipesResult.status === "rejected" && !isAuthRedirect(recipesResult.reason)) {
      console.error("Reçete listesi yüklenemedi.", recipesResult.reason);
      errors.push("Reçete listesi yüklenemedi.");
    }

    setProducts(productList);
    setMaterials(materialList);
    setRecipes(recipesResult.status === "fulfilled" ? extractRecipes(recipesResult.value, materialList) : []);
    setError(errors.length ? errors.join(" ") : null);
    setLoading(false);
  }

  function openDialog(mode: DialogMode, recipe: RecipeRecord | null = null) {
    setSuccessMessage(null);
    setDialogRecipe(recipe);
    setDialogMode(mode);
  }

  function closeDialog() {
    setDialogMode(null);
    setDialogRecipe(null);
  }

  function handleSaved(recipe: RecipeRecord, message: string) {
    setRecipes((current) => [recipe, ...current.filter((item) => item.id !== recipe.id)]);
    closeDialog();
    setSuccessMessage(message);
    loadData();
  }

  const filteredRecipes = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase("tr-TR");
    if (!normalizedSearch) return recipes;

    return recipes.filter((recipe) => {
      const product = products.find((item) => item.id === recipe.productId);
      return [recipe.code, recipe.name, product?.code, product?.name, product?.foamType, recipe.version, recipe.revisionNo]
        .filter(Boolean)
        .some((value) => String(value).toLocaleLowerCase("tr-TR").includes(normalizedSearch));
    });
  }, [products, recipes, search]);

  const activeCount = recipes.filter((recipe) => recipe.isActive).length;
  const memoryFoamCount = recipes.filter((recipe) => products.find((product) => product.id === recipe.productId)?.productType === "Memory Foam").length;
  const normalPuCount = recipes.filter((recipe) => {
    const product = products.find((item) => item.id === recipe.productId);
    return (product?.productType || "Normal") === "Normal" && product?.foamType === "10100";
  }).length;
  const totalMaterialCount = recipes.reduce((sum, recipe) => sum + recipe.lines.filter((line) => line.materialId).length, 0);
  const averageCost = recipes.length
    ? recipes.reduce((sum, recipe) => sum + calculateRecipeTotals(recipe.lines, materials).totalCost, 0) / recipes.length
    : 0;
  const dashboardCards = [
    { title: "Toplam Reçete", value: recipes.length.toLocaleString("tr-TR"), note: "Veritabanı reçete kaydı", tone: "emerald" as DashboardTone },
    { title: "Aktif Reçete", value: activeCount.toLocaleString("tr-TR"), note: "Üretime açık", tone: "cyan" as DashboardTone },
    { title: "Memory Foam", value: memoryFoamCount.toLocaleString("tr-TR"), note: "Ürün tipi", tone: "amber" as DashboardTone },
    { title: "Normal PU", value: normalPuCount.toLocaleString("tr-TR"), note: "10100 normal ürün", tone: "blue" as DashboardTone },
    { title: "Toplam Hammadde", value: totalMaterialCount.toLocaleString("tr-TR"), note: "Seçili malzeme satırı", tone: "violet" as DashboardTone },
    { title: "Ortalama Maliyet", value: formatMoney(averageCost, "TRY"), note: "Tahmini çift maliyeti", tone: "red" as DashboardTone },
  ];

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.18),transparent_34%),radial-gradient(circle_at_bottom_right,rgba(14,165,233,0.13),transparent_32%)] px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.38em] text-emerald-300">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">Reçete / Ürün Ağacı</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                Ürün Kartı ve Malzeme Kartı bilgilerini birleştirerek üretim, maliyet, stok ve satın alma süreçlerini besleyen reçete ana verisini yönetin.
              </p>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <button
                onClick={loadData}
                disabled={loading}
                className="rounded-xl border border-white/10 bg-white/[0.08] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:opacity-50"
              >
                {loading ? "Yenileniyor..." : "Listeyi Yenile"}
              </button>
              <button onClick={() => openDialog("create")} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
                + Yeni Reçete
              </button>
            </div>
          </header>

          {successMessage && <div className="rounded-xl border border-emerald-400/30 bg-emerald-500/10 p-4 text-sm font-bold text-emerald-100">{successMessage}</div>}

          <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-6">
            {dashboardCards.map((card) => (
              <DashboardCard key={card.title} title={card.title} value={card.value} note={card.note} tone={card.tone} />
            ))}
          </section>

          <section className="rounded-2xl border border-white/10 bg-white/[0.06] p-5 shadow-2xl backdrop-blur">
            <div className="flex flex-col gap-4 border-b border-white/10 pb-5 xl:flex-row xl:items-end xl:justify-between">
              <div>
                <h2 className="text-2xl font-black">Reçete Listesi</h2>
                <p className="mt-1 text-sm text-zinc-400">{filteredRecipes.length.toLocaleString("tr-TR")} reçete listeleniyor.</p>
              </div>
              <div className="w-full xl:max-w-md">
                <Field label="Arama">
                  <input value={search} onChange={(event) => setSearch(event.target.value)} className={CONTROL_CLASS} placeholder="Ürün, kod, foam, versiyon, revizyon" />
                </Field>
              </div>
            </div>

            {loading && <LoadingState />}

            {!loading && error && (
              <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-5 text-sm text-red-100">
                <p className="font-black">Reçete ana verileri yüklenemedi.</p>
                <p className="mt-1 text-red-200">{error}</p>
              </div>
            )}

            {!loading && !error && filteredRecipes.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-zinc-300">
                Henüz reçete kaydı bulunmuyor. Ürün Kartı ve Malzeme Kartı seçimleriyle yeni reçete oluşturun.
              </div>
            )}

            {!loading && !error && filteredRecipes.length > 0 && (
              <div className="mt-5 overflow-x-auto">
                <table className="min-w-[1040px] w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-white/10 text-xs uppercase tracking-[0.18em] text-zinc-500">
                          <th className="py-3 pr-4">Reçete Kodu</th>
                          <th className="py-3 pr-4">Reçete Adı</th>
                          <th className="py-3 pr-4">Ürün</th>
                          <th className="py-3 pr-4">Versiyon</th>
                          <th className="py-3 pr-4">Malzeme</th>
                          <th className="py-3 pr-4">Çıktı</th>
                          <th className="py-3 pr-4">Varsayılan</th>
                          <th className="py-3 pr-4">Toplam Gram</th>
                          <th className="py-3 pr-4">Malzeme Maliyeti</th>
                          <th className="py-3 pr-4">Durum</th>
                          <th className="py-3 text-right">İşlemler</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/10">
                    {filteredRecipes.map((recipe) => {
                      const product = products.find((item) => item.id === recipe.productId);
                      const totals = calculateRecipeTotals(recipe.lines, materials);

                      return (
                        <tr key={recipe.id} className="align-middle text-zinc-200 transition hover:bg-white/[0.04]">
                          <td className="py-4 pr-4 font-mono text-xs text-emerald-200">{recipe.code || "-"}</td>
                          <td className="py-4 pr-4 font-black text-white">{recipe.name || "-"}</td>
                          <td className="py-4 pr-4">{[product?.code, product?.name].filter(Boolean).join(" - ") || "-"}</td>
                          <td className="py-4 pr-4">{recipe.version || "-"}</td>
                          <td className="py-4 pr-4">{recipe.lines.filter((line) => line.materialId).length.toLocaleString("tr-TR")}</td>
                          <td className="py-4 pr-4">{formatNumber(safeParsedNumber(recipe.outputQuantity))} {recipe.outputUnit || "Çift"}</td>
                          <td className="py-4 pr-4">{recipe.isDefault ? "Evet" : "Hayır"}</td>
                          <td className="py-4 pr-4">{formatNumber(totals.totalGram)} gr</td>
                          <td className="py-4 pr-4">{formatCurrencyTotals(totals.totalsByCurrency)}</td>
                          <td className="py-4 pr-4"><StatusBadge active={recipe.isActive} /></td>
                          <td className="py-4">
                            <div className="flex justify-end gap-2">
                              <button onClick={() => openDialog("detail", recipe)} className="rounded-lg border border-cyan-400/30 bg-cyan-400/10 px-3 py-2 text-xs font-black text-cyan-100 transition hover:bg-cyan-400/20">Detay</button>
                              <button onClick={() => openDialog("edit", recipe)} className="rounded-lg border border-emerald-400/30 bg-emerald-400/10 px-3 py-2 text-xs font-black text-emerald-100 transition hover:bg-emerald-400/20">Düzenle</button>
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
        <RecipeModal
          mode={dialogMode}
          recipe={dialogRecipe}
          products={products}
          materials={materials}
          onClose={closeDialog}
          onSaved={handleSaved}
        />
      )}
    </main>
  );
}

function RecipeModal({
  mode,
  recipe,
  products,
  materials,
  onClose,
  onSaved,
}: {
  mode: DialogMode;
  recipe: RecipeRecord | null;
  products: Product[];
  materials: Material[];
  onClose: () => void;
  onSaved: (recipe: RecipeRecord, message: string) => void;
}) {
  const [activeTab, setActiveTab] = useState<RecipeTab>("general");
  const [form, setForm] = useState<RecipeRecord>(() => recipe ? cloneRecipe(recipe) : createEmptyRecipe());
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const readonly = mode === "detail";
  const product = products.find((item) => item.id === form.productId);
  const productDetails = parseProductDetails(product?.description);
  const totals = calculateRecipeTotals(form.lines, materials);
  const currency = getRecipeCurrency(form, materials);
  const estimatedPairCost = totals.totalCost + safeParsedNumber(form.laborCost) + safeParsedNumber(form.electricityCost) + safeParsedNumber(form.packagingCost) + safeParsedNumber(form.overheadCost);

  function updateForm<K extends keyof RecipeRecord>(key: K, value: RecipeRecord[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function selectProduct(productId: string) {
    const selected = products.find((item) => item.id === productId);
    const details = parseProductDetails(selected?.description);

    setForm((current) => ({
      ...current,
      productId,
      standardWeight: toInputNumber(selected?.averageWeight),
      standardDensity: toInputNumber(selected?.targetDensity),
      standardCycleTime: toInputNumber(selected?.standardCycleTime),
      standardWasteRate: details.standardWasteRate || current.standardWasteRate,
      standardMold: details.standardMold || current.standardMold,
      standardDailyCapacity: details.standardDailyCapacity || current.standardDailyCapacity,
      cuttingMachine: details.cuttingMachine || current.cuttingMachine,
      operationNote: details.defaultOperationNote || current.operationNote,
    }));
  }

  function updateLine(key: string, field: keyof RecipeLine, value: string) {
    setForm((current) => ({
      ...current,
      lines: current.lines.map((line) => (line.key === key ? { ...line, [field]: field === "order" ? Number(value) : value } : line)),
    }));
  }

  async function saveRecipe() {
    setError(null);

    if (!form.productId) {
      setError("Ürün Kartı seçmelisiniz.");
      setActiveTab("general");
      return;
    }

    if (!form.code.trim()) {
      setError("Reçete kodu zorunludur.");
      setActiveTab("general");
      return;
    }

    if (!form.name.trim()) {
      setError("Reçete adı zorunludur.");
      setActiveTab("general");
      return;
    }

    if (safeParsedNumber(form.version) <= 0) {
      setError("Versiyon pozitif sayı olmalıdır.");
      setActiveTab("general");
      return;
    }

    if (form.lines.some((line) => !line.materialId && line.quantity.trim())) {
      setError("Miktar girilen her satırda Malzeme Kartı seçilmelidir.");
      setActiveTab("materials");
      return;
    }

    const selectedLines = form.lines.filter((line) => line.materialId && line.quantity.trim());
    if (selectedLines.length === 0) {
      setError("Reçetede en az bir malzeme bulunmalıdır.");
      setActiveTab("materials");
      return;
    }

    setSaving(true);
    try {
      const response = await fetch(mode === "edit" && recipe ? `${API}/recipes/${recipe.id}` : `${API}/recipes`, {
        method: mode === "edit" ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(toRecipeRequest(form, selectedLines, materials)),
      });
      const result = await safeResponseJson(response) as ApiResponse<unknown>;
      if (!response.ok) throw new Error(result.message || "Reçete kaydedilemedi.");
      const saved = recipeFromApi(result.data, materials);
      if (!saved) throw new Error("API reçete cevabı okunamadı.");
      onSaved(saved, result.message || (mode === "edit" ? "Reçete güncellendi." : "Reçete oluşturuldu."));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Reçete kaydedilirken beklenmeyen bir hata oluştu.");
    } finally {
      setSaving(false);
    }
  }

  async function runRecipeAction(action: "activate" | "deactivate" | "set-default" | "duplicate") {
    if (!recipe) return;
    setError(null);
    setSaving(true);

    try {
      const endpoint = `${API}/recipes/${recipe.id}/${action}`;
      let body: unknown = undefined;
      if (action === "duplicate") {
        const nextVersion = Math.max(1, Number(form.version) || 1) + 1;
        body = {
          code: `${form.code}-KOPYA-${Date.now().toString().slice(-4)}`,
          name: `${form.name} Kopya`,
          version: nextVersion,
        };
      }

      const response = await fetch(endpoint, {
        method: "POST",
        headers: body ? { "Content-Type": "application/json" } : undefined,
        body: body ? JSON.stringify(body) : undefined,
      });
      const result = await safeResponseJson(response) as ApiResponse<unknown>;
      if (!response.ok) throw new Error(result.message || "İşlem tamamlanamadı.");
      const updated = recipeFromApi(result.data, materials);
      if (!updated) throw new Error("API reçete cevabı okunamadı.");
      onSaved(updated, result.message || "İşlem tamamlandı.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "İşlem sırasında beklenmeyen bir hata oluştu.");
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
              <p className="text-xs font-black tracking-[0.34em] text-emerald-300">REÇETE / ÜRÜN AĞACI</p>
              <h2 className="mt-2 text-2xl font-black text-white">{readonly ? "Reçete Detayı" : mode === "edit" ? "Reçete Düzenle" : "Yeni Reçete"}</h2>
              <p className="mt-1 text-sm text-zinc-400">Ürün Kartı ve Malzeme Kartı seçimleri üzerinden üretim reçetesini yönetin.</p>
            </div>
            <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white transition hover:bg-white/[0.12]">Kapat</button>
          </div>
          <RecipeSummary product={product} details={productDetails} form={form} totals={totals} currency={currency} />
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

          {activeTab === "general" && <GeneralTab product={product} details={productDetails} form={form} products={products} readonly={readonly} selectProduct={selectProduct} updateForm={updateForm} />}
          {activeTab === "materials" && <MaterialsTab form={form} materials={materials} readonly={readonly} updateLine={updateLine} totals={totals} currency={currency} />}
          {activeTab === "production" && <ProductionTab form={form} readonly={readonly} updateForm={updateForm} />}
          {activeTab === "cost" && <CostTab form={form} readonly={readonly} updateForm={updateForm} rawMaterialCost={totals.totalCost} currency={currency} estimatedPairCost={estimatedPairCost} />}
          {activeTab === "revision" && <RevisionTab form={form} readonly={readonly} updateForm={updateForm} />}
          {activeTab === "notes" && <NotesTab form={form} readonly={readonly} updateForm={updateForm} />}
        </div>

        <div className="flex flex-col gap-3 border-t border-white/10 bg-black/30 p-5 sm:flex-row sm:justify-end">
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.12]">
            {readonly ? "Kapat" : "Vazgeç"}
          </button>
          {!readonly && (
            <button disabled={saving} onClick={saveRecipe} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400 disabled:opacity-50">
              {saving ? "Kaydediliyor..." : "Kaydet"}
            </button>
          )}
          {mode === "edit" && recipe && (
            <>
              <button disabled={saving} onClick={() => runRecipeAction(form.isActive ? "deactivate" : "activate")} className="rounded-xl border border-amber-400/30 bg-amber-500/10 px-5 py-3 text-sm font-black text-amber-100 transition hover:bg-amber-500/20 disabled:opacity-50">
                {form.isActive ? "Pasif Yap" : "Aktif Yap"}
              </button>
              <button disabled={saving || !form.isActive} onClick={() => runRecipeAction("set-default")} className="rounded-xl border border-cyan-400/30 bg-cyan-500/10 px-5 py-3 text-sm font-black text-cyan-100 transition hover:bg-cyan-500/20 disabled:opacity-50">
                Varsayılan Yap
              </button>
              <button disabled={saving} onClick={() => runRecipeAction("duplicate")} className="rounded-xl border border-violet-400/30 bg-violet-500/10 px-5 py-3 text-sm font-black text-violet-100 transition hover:bg-violet-500/20 disabled:opacity-50">
                Kopyala
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

function RecipeSummary({ product, details, form, totals, currency }: { product: Product | undefined; details: ProductDetails; form: RecipeRecord; totals: RecipeTotals; currency: string }) {
  const items = [
    ["Ürün", product?.name || "-"],
    ["Kod", product?.code || "-"],
    ["Foam", product?.foamType || "-"],
    ["Versiyon", form.version || "-"],
    ["Revizyon", form.revisionNo || "-"],
    ["Numara", details.number || "-"],
    ["Kumaş", details.fabricType || (product?.isFabric ? "Var" : "Yok")],
    ["DTF", product?.hasDTFLabel ? "Var" : "Yok"],
    ["Yapışkan", details.adhesiveType || (product?.isAdhesive ? "Var" : "Yok")],
    ["Toplam Gram", `${formatNumber(totals.totalGram)} gr`],
    ["Toplam Maliyet", formatMoney(totals.totalCost, currency)],
    ["Durum", form.isActive ? "Aktif" : "Pasif"],
  ];

  return (
    <div className="mt-5 grid gap-2 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-6">
      {items.map(([label, value]) => (
        <div key={label} className="rounded-xl border border-white/10 bg-black/30 px-3 py-2">
          <p className="text-[10px] font-black uppercase tracking-[0.18em] text-zinc-500">{label}</p>
          <p className="mt-1 truncate text-sm font-black text-white" title={value}>{value}</p>
        </div>
      ))}
    </div>
  );
}

function GeneralTab({
  product,
  details,
  form,
  products,
  readonly,
  selectProduct,
  updateForm,
}: {
  product: Product | undefined;
  details: ProductDetails;
  form: RecipeRecord;
  products: Product[];
  readonly: boolean;
  selectProduct: (productId: string) => void;
  updateForm: <K extends keyof RecipeRecord>(key: K, value: RecipeRecord[K]) => void;
}) {
  return (
    <TabPanel title="Genel" note="Ürün Kartı seçimi reçetenin ürün teknik bilgisini otomatik doldurur.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Reçete Kodu" value={form.code} readonly={readonly} onChange={(value) => updateForm("code", value)} />
        <TextInput label="Reçete Adı" value={form.name} readonly={readonly} onChange={(value) => updateForm("name", value)} />
        <Field label="Ürün Kartı seç">
          <select value={form.productId} disabled={readonly} onChange={(event) => selectProduct(event.target.value)} className={CONTROL_CLASS}>
            <option value="">Ürün ara / seç</option>
            {products.map((item) => (
              <option key={item.id} value={item.id}>{[item.code, item.name, item.foamType].filter(Boolean).join(" - ")}</option>
            ))}
          </select>
        </Field>
        <ReadOnlyInfo label="Ürün kodu" value={product?.code || "-"} />
        <ReadOnlyInfo label="Ürün adı" value={product?.name || "-"} />
        <ReadOnlyInfo label="Foam" value={product?.foamType || "-"} />
        <ReadOnlyInfo label="Numara" value={details.number || "-"} />
        <ReadOnlyInfo label="Kumaş" value={details.fabricType || (product?.isFabric ? "Var" : "Yok")} />
        <ReadOnlyInfo label="DTF" value={product?.hasDTFLabel ? "Var" : "Yok"} />
        <ReadOnlyInfo label="Yapışkan" value={details.adhesiveType || (product?.isAdhesive ? "Var" : "Yok")} />
        <ReadOnlyInfo label="Gramaj" value={formatNumber(product?.averageWeight)} />
        <ReadOnlyInfo label="Yoğunluk" value={formatNumber(product?.targetDensity)} />
        <TextInput label="Versiyon" value={form.version} readonly={readonly} type="number" onChange={(value) => updateForm("version", value)} />
        <TextInput label="Revizyon" value={form.revisionNo} readonly={readonly} onChange={(value) => updateForm("revisionNo", value)} />
        <TextInput label="Çıktı Miktarı" value={form.outputQuantity} readonly={readonly} type="number" onChange={(value) => updateForm("outputQuantity", value)} />
        <Field label="Çıktı Birimi">
          <select value={form.outputUnit} disabled={readonly} onChange={(event) => updateForm("outputUnit", event.target.value)} className={CONTROL_CLASS}>
            <option value="Çift">Çift</option>
            <option value="Adet">Adet</option>
            <option value="Kg">Kg</option>
            <option value="gr">gr</option>
          </select>
        </Field>
        <ToggleInput label="Aktif" checked={form.isActive} readonly={readonly} trueText="Aktif" falseText="Pasif" onChange={(value) => updateForm("isActive", value)} />
        <ToggleInput label="Varsayılan" checked={form.isDefault} readonly={readonly} trueText="Varsayılan" falseText="Standart değil" onChange={(value) => updateForm("isDefault", value)} />
        <TextInput label="Başlangıç tarihi" value={form.startDate} readonly={readonly} type="date" onChange={(value) => updateForm("startDate", value)} />
        <TextInput label="Bitiş tarihi" value={form.endDate} readonly={readonly} type="date" onChange={(value) => updateForm("endDate", value)} />
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <ImagePreview title="Ürün resmi" name={details.productImageName || ""} dataUrl={details.productImageDataUrl || ""} />
        <ImagePreview title="Kalıp resmi" name={details.moldImageName || ""} dataUrl={details.moldImageDataUrl || ""} />
      </div>
    </TabPanel>
  );
}

function MaterialsTab({
  form,
  materials,
  readonly,
  updateLine,
  totals,
  currency,
}: {
  form: RecipeRecord;
  materials: Material[];
  readonly: boolean;
  updateLine: (key: string, field: keyof RecipeLine, value: string) => void;
  totals: RecipeTotals;
  currency: string;
}) {
  return (
    <TabPanel title="Hammadde Listesi" note="Malzeme Kartı seçimi kod, birim, fiyat, para birimi ve tipi otomatik getirir.">
      <div className="overflow-x-auto rounded-xl border border-white/10 bg-black/20">
        <table className="min-w-[1240px] w-full text-left text-sm">
          <thead>
            <tr className="border-b border-white/10 text-xs uppercase tracking-[0.16em] text-zinc-500">
              <th className="p-3">Sıra</th>
              <th className="p-3">Malzeme</th>
              <th className="p-3">Kod</th>
              <th className="p-3">Tip</th>
              <th className="p-3">Miktar</th>
              <th className="p-3">Birim</th>
              <th className="p-3">Son Alış</th>
              <th className="p-3">Toplam</th>
              <th className="p-3">Fire %</th>
              <th className="p-3">Net Kullanım</th>
              <th className="p-3">Açıklama</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/10">
            {form.lines.map((line) => {
              const material = materials.find((item) => item.id === line.materialId);
              const quantity = safeParsedNumber(line.quantity);
              const wasteRate = safeParsedNumber(line.wasteRate);
              const netUsage = quantity + quantity * wasteRate / 100;
              const lineTotal = netUsage * safeNumber(material?.lastPurchasePrice);

              return (
                <FragmentRow key={line.key} line={line}>
                  <tr>
                    <td className="p-3 font-black text-white">{line.order}</td>
                    <td className="p-3">
                      <select value={line.materialId} disabled={readonly} onChange={(event) => updateLine(line.key, "materialId", event.target.value)} className={CONTROL_CLASS}>
                        <option value="">{line.role} için malzeme seç</option>
                        {materials.map((item) => (
                          <option key={item.id} value={item.id}>{formatMaterialOption(item)}</option>
                        ))}
                      </select>
                    </td>
                    <td className="p-3 font-mono text-xs text-emerald-200">{material?.code || "-"}</td>
                    <td className="p-3">{material?.materialType || "-"}</td>
                    <td className="p-3"><input value={line.quantity} type="number" step="0.01" disabled={readonly} onChange={(event) => updateLine(line.key, "quantity", event.target.value)} className={CONTROL_CLASS} /></td>
                    <td className="p-3">{material?.unit || "-"}</td>
                    <td className="p-3">{formatMoney(safeNumber(material?.lastPurchasePrice), material?.currency || currency)}</td>
                    <td className="p-3 font-black text-emerald-200">{formatMoney(lineTotal, material?.currency || currency)}</td>
                    <td className="p-3"><input value={line.wasteRate} type="number" step="0.01" disabled={readonly} onChange={(event) => updateLine(line.key, "wasteRate", event.target.value)} className={CONTROL_CLASS} /></td>
                    <td className="p-3">{formatNumber(netUsage)}</td>
                    <td className="p-3"><input value={line.note} disabled={readonly} onChange={(event) => updateLine(line.key, "note", event.target.value)} className={CONTROL_CLASS} /></td>
                  </tr>
                </FragmentRow>
              );
            })}
          </tbody>
        </table>
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        <MetricCard label="Toplam Gram" value={`${formatNumber(totals.totalGram)} gr`} />
        <MetricCard label="Toplam Hammadde" value={formatMoney(totals.rawMaterialCost, currency)} tone="emerald" />
        <MetricCard label="Toplam Maliyet" value={formatMoney(totals.totalCost, currency)} tone="cyan" />
      </div>
    </TabPanel>
  );
}

function FragmentRow({ line, children }: { line: RecipeLine; children: ReactNode }) {
  return (
    <>
      {children}
      {line.role === "Crosskim" && (
        <tr>
          <td colSpan={11} className="px-3 pb-4 pt-0">
            <div className="rounded-xl border border-amber-400/30 bg-amber-500/10 p-3 text-sm font-bold text-amber-100">
              Crosskim doğrudan makineye verilmez. 180 kg Poliol kazanına ilave edilen katkıdır.
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

function ProductionTab({ form, readonly, updateForm }: RecipeFormTabProps) {
  return (
    <TabPanel title="Üretim Parametreleri" note="Ürün Kartı seçimi bu alanları otomatik başlatır; reçete özelinde revize edilebilir.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Standart Gramaj" value={form.standardWeight} readonly={readonly} type="number" onChange={(value) => updateForm("standardWeight", value)} />
        <TextInput label="Standart Yoğunluk" value={form.standardDensity} readonly={readonly} type="number" onChange={(value) => updateForm("standardDensity", value)} />
        <TextInput label="Standart Pişme" value={form.standardCycleTime} readonly={readonly} type="number" onChange={(value) => updateForm("standardCycleTime", value)} />
        <TextInput label="Standart Fire" value={form.standardWasteRate} readonly={readonly} type="number" onChange={(value) => updateForm("standardWasteRate", value)} />
        <TextInput label="Standart Kalıp" value={form.standardMold} readonly={readonly} onChange={(value) => updateForm("standardMold", value)} />
        <TextInput label="Standart Makine" value={form.standardMachine} readonly={readonly} onChange={(value) => updateForm("standardMachine", value)} />
        <TextInput label="Standart Günlük Kapasite" value={form.standardDailyCapacity} readonly={readonly} type="number" onChange={(value) => updateForm("standardDailyCapacity", value)} />
        <TextInput label="Kesim Makinesi" value={form.cuttingMachine} readonly={readonly} onChange={(value) => updateForm("cuttingMachine", value)} />
      </div>
      <TextAreaInput label="Operasyon Notu" value={form.operationNote} readonly={readonly} onChange={(value) => updateForm("operationNote", value)} />
    </TabPanel>
  );
}

function CostTab({ form, readonly, updateForm, rawMaterialCost, currency, estimatedPairCost }: RecipeFormTabProps & { rawMaterialCost: number; currency: string; estimatedPairCost: number }) {
  return (
    <TabPanel title="Maliyet" note="Şimdilik frontend hesaplar; Malzeme Kartı son alış fiyatları altyapısı hazırdır.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <CostCard title="Hammadde" value={formatMoney(rawMaterialCost, currency)} note="Malzeme son alış fiyatlarından" />
        <EditableCostCard title="İşçilik" value={form.laborCost} readonly={readonly} currency={currency} onChange={(value) => updateForm("laborCost", value)} />
        <EditableCostCard title="Elektrik" value={form.electricityCost} readonly={readonly} currency={currency} onChange={(value) => updateForm("electricityCost", value)} />
        <EditableCostCard title="Paketleme" value={form.packagingCost} readonly={readonly} currency={currency} onChange={(value) => updateForm("packagingCost", value)} />
        <EditableCostCard title="Genel Gider" value={form.overheadCost} readonly={readonly} currency={currency} onChange={(value) => updateForm("overheadCost", value)} />
      </div>
      <div className="rounded-2xl border border-emerald-400/30 bg-emerald-500/10 p-6 shadow-xl shadow-emerald-950/20">
        <p className="text-xs font-black uppercase tracking-[0.22em] text-emerald-200">Tahmini Çift Maliyeti</p>
        <p className="mt-3 text-4xl font-black text-white">{formatMoney(estimatedPairCost, currency)}</p>
      </div>
    </TabPanel>
  );
}

function RevisionTab({ form, readonly, updateForm }: RecipeFormTabProps) {
  return (
    <TabPanel title="Revizyon" note="Reçete değişiklik geçmişi backend hazır olana kadar frontend state içinde tutulur.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <TextInput label="Versiyon" value={form.version} readonly={readonly} onChange={(value) => updateForm("version", value)} />
        <TextInput label="Revizyon No" value={form.revisionNo} readonly={readonly} onChange={(value) => updateForm("revisionNo", value)} />
        <TextInput label="Kim Güncelledi" value={form.updatedBy} readonly={readonly} onChange={(value) => updateForm("updatedBy", value)} />
        <TextInput label="Tarih" value={form.revisionDate} readonly={readonly} type="date" onChange={(value) => updateForm("revisionDate", value)} />
      </div>
      <TextAreaInput label="Değişiklik Açıklaması" value={form.revisionDescription} readonly={readonly} onChange={(value) => updateForm("revisionDescription", value)} />
    </TabPanel>
  );
}

function NotesTab({ form, readonly, updateForm }: RecipeFormTabProps) {
  return (
    <TabPanel title="Açıklamalar" note="Üretim, kalite, müşteri ve genel notlar.">
      <div className="grid gap-4 lg:grid-cols-2">
        <TextAreaInput label="Üretim Notu" value={form.productionNote} readonly={readonly} onChange={(value) => updateForm("productionNote", value)} />
        <TextAreaInput label="Kalite Notu" value={form.qualityNote} readonly={readonly} onChange={(value) => updateForm("qualityNote", value)} />
        <TextAreaInput label="Müşteri Notu" value={form.customerNote} readonly={readonly} onChange={(value) => updateForm("customerNote", value)} />
        <TextAreaInput label="Genel Not" value={form.generalNote} readonly={readonly} onChange={(value) => updateForm("generalNote", value)} />
      </div>
    </TabPanel>
  );
}

type RecipeFormTabProps = {
  form: RecipeRecord;
  readonly: boolean;
  updateForm: <K extends keyof RecipeRecord>(key: K, value: RecipeRecord[K]) => void;
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

function ToggleInput({ label, checked, readonly, trueText, falseText, onChange }: { label: string; checked: boolean; readonly: boolean; trueText: string; falseText: string; onChange: (value: boolean) => void }) {
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

function ReadOnlyInfo({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs font-black uppercase tracking-[0.16em] text-zinc-500">{label}</p>
      <p className="mt-2 text-sm font-black text-white">{value}</p>
    </div>
  );
}

function ImagePreview({ title, name, dataUrl }: { title: string; name: string; dataUrl: string }) {
  return (
    <div className="overflow-hidden rounded-xl border border-white/10 bg-black/30">
      <div className="flex aspect-[4/3] items-center justify-center bg-white/[0.04]">
        {dataUrl ? <img src={dataUrl} alt={title} className="h-full w-full object-cover" /> : <p className="text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{title}</p>}
      </div>
      <div className="border-t border-white/10 px-3 py-2 text-xs font-bold text-zinc-300">{name || "Önizleme yok"}</div>
    </div>
  );
}

function MetricCard({ label, value, tone = "zinc" }: { label: string; value: string; tone?: "emerald" | "cyan" | "zinc" }) {
  const color = tone === "emerald" ? "border-emerald-400/30 bg-emerald-500/10 text-emerald-200" : tone === "cyan" ? "border-cyan-400/30 bg-cyan-500/10 text-cyan-200" : "border-white/10 bg-black/25 text-zinc-300";
  return (
    <div className={`rounded-xl border p-4 ${color}`}>
      <p className="text-xs font-black uppercase tracking-[0.18em] opacity-80">{label}</p>
      <p className="mt-2 text-2xl font-black text-white">{value}</p>
    </div>
  );
}

function CostCard({ title, value, note }: { title: string; value: string; note: string }) {
  return (
    <article className="rounded-2xl border border-white/10 bg-black/25 p-5">
      <p className="text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{title}</p>
      <p className="mt-3 text-2xl font-black text-white">{value}</p>
      <p className="mt-2 text-sm text-zinc-400">{note}</p>
    </article>
  );
}

function EditableCostCard({ title, value, readonly, currency, onChange }: { title: string; value: string; readonly: boolean; currency: string; onChange: (value: string) => void }) {
  return (
    <article className="rounded-2xl border border-white/10 bg-black/25 p-5">
      <p className="text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{title}</p>
      <input value={value} type="number" step="0.01" disabled={readonly} onChange={(event) => onChange(event.target.value)} className={`${CONTROL_CLASS} mt-3`} />
      <p className="mt-2 text-sm text-zinc-400">{formatMoney(safeParsedNumber(value), currency)}</p>
    </article>
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
  }[tone];

  return (
    <article className={`rounded-2xl border p-5 shadow-xl ${toneClass}`}>
      <p className="text-xs font-black uppercase tracking-[0.18em] opacity-80">{title}</p>
      <p className="mt-3 text-2xl font-black text-white">{value}</p>
      <p className="mt-2 text-sm opacity-80">{note}</p>
    </article>
  );
}

function StatusBadge({ active }: { active: boolean }) {
  return <span className={`rounded-full px-3 py-1 text-xs font-black ${active ? "bg-emerald-500/15 text-emerald-200" : "bg-red-500/15 text-red-200"}`}>{active ? "Aktif" : "Pasif"}</span>;
}

function LoadingState() {
  return <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-sm font-bold text-zinc-400">Yükleniyor...</div>;
}

type RecipeTotals = {
  totalGram: number;
  rawMaterialCost: number;
  totalCost: number;
  totalsByCurrency: Record<string, number>;
};

function createEmptyRecipe(): RecipeRecord {
  return {
    id: `recipe-${Date.now()}-${Math.random().toString(36).slice(2)}`,
    code: "",
    name: "",
    productId: "",
    version: "1",
    revisionNo: "R0",
    isActive: true,
    isDefault: false,
    outputQuantity: "1",
    outputUnit: "Çift",
    startDate: formatDateInput(new Date()),
    endDate: "",
    lines: RECIPE_ROLES.map((role, index) => ({
      key: `${role}-${index}`,
      order: index + 1,
      role,
      materialId: "",
      quantity: "",
      wasteRate: "0",
      note: "",
    })),
    standardWeight: "",
    standardDensity: "",
    standardCycleTime: "",
    standardWasteRate: "",
    standardMold: "",
    standardMachine: "",
    standardDailyCapacity: "",
    cuttingMachine: "",
    operationNote: "",
    laborCost: "",
    electricityCost: "",
    packagingCost: "",
    overheadCost: "",
    revisionDescription: "",
    updatedBy: "",
    revisionDate: formatDateInput(new Date()),
    productionNote: "",
    qualityNote: "",
    customerNote: "",
    generalNote: "",
    updatedAt: new Date().toISOString(),
  };
}

function cloneRecipe(recipe: RecipeRecord): RecipeRecord {
  return {
    ...recipe,
    lines: recipe.lines.map((line) => ({ ...line })),
  };
}

function calculateRecipeTotals(lines: RecipeLine[], materials: Material[]): RecipeTotals {
  return lines.reduce(
    (totals, line) => {
      const material = materials.find((item) => item.id === line.materialId);
      const quantity = safeParsedNumber(line.quantity);
      const wasteRate = safeParsedNumber(line.wasteRate);
      const netUsage = quantity + quantity * wasteRate / 100;
      const lineCost = netUsage * safeNumber(material?.lastPurchasePrice);
      const currency = material?.currency || "TRY";
      const isGram = (material?.unit || "").toLocaleLowerCase("tr-TR") === "gr";

      const totalsByCurrency = {
        ...totals.totalsByCurrency,
        [currency]: safeNumber(totals.totalsByCurrency[currency]) + lineCost,
      };

      return {
        totalGram: totals.totalGram + (isGram ? netUsage : 0),
        rawMaterialCost: totals.rawMaterialCost + lineCost,
        totalCost: totals.totalCost + lineCost,
        totalsByCurrency,
      };
    },
    { totalGram: 0, rawMaterialCost: 0, totalCost: 0, totalsByCurrency: {} as Record<string, number> }
  );
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

function extractRecipes(result: unknown, materials: Material[]): RecipeRecord[] {
  if (Array.isArray(result)) return result.map((item) => recipeFromApi(item, materials)).filter(isRecipeRecord);
  if (isRecord(result) && Array.isArray((result as ApiResponse<unknown[]>).data)) {
    return (result as ApiResponse<unknown[]>).data!.map((item) => recipeFromApi(item, materials)).filter(isRecipeRecord);
  }
  return [];
}

function isProduct(value: unknown): value is Product {
  return isRecord(value) && typeof value.id === "string";
}

function isMaterial(value: unknown): value is Material {
  return isRecord(value) && typeof value.id === "string";
}

function isRecipeRecord(value: RecipeRecord | null): value is RecipeRecord {
  return value !== null;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function recipeFromApi(value: unknown, materials: Material[]): RecipeRecord | null {
  if (!isRecord(value)) return null;

  const id = readString(value, "id");
  if (!id) return null;

  const notes = readString(value, "notes");
  const meta = parseRecipeMeta(notes);
  const items = readArray(value, "items");
  const lines = mergeApiItemsWithDefaultLines(items, materials);
  const effectiveFrom = readString(value, "effectiveFrom");
  const effectiveTo = readString(value, "effectiveTo");
  const updatedAt = readString(value, "updatedAt") || new Date().toISOString();

  return {
    ...createEmptyRecipe(),
    ...meta,
    id,
    code: readString(value, "code"),
    name: readString(value, "name"),
    productId: readString(value, "productId"),
    version: String(readNumber(value, "version") || 1),
    isActive: readBoolean(value, "isActive", true),
    isDefault: readBoolean(value, "isDefault", false),
    outputQuantity: String(readNumber(value, "outputQuantity") || 1),
    outputUnit: readString(value, "outputUnit") || "Çift",
    startDate: toDateInput(effectiveFrom),
    endDate: toDateInput(effectiveTo),
    lines,
    generalNote: meta.generalNote || stripRecipeMeta(notes),
    updatedAt,
  };
}

function mergeApiItemsWithDefaultLines(items: unknown[], materials: Material[]) {
  const baseLines = RECIPE_ROLES.map((role, index) => ({
    key: `${role}-${index}`,
    order: index + 1,
    role,
    materialId: "",
    quantity: "",
    wasteRate: "0",
    note: "",
  }));

  items.forEach((item, index) => {
    if (!isRecord(item)) return;
    const sequence = readNumber(item, "sequence") || index + 1;
    const materialId = readString(item, "materialId");
    const material = materials.find((entry) => entry.id === materialId);
    const role = material?.materialType || readString(item, "materialType") || RECIPE_ROLES[index] || "Diğer";
    const line = {
      key: readString(item, "id") || `api-${materialId}-${index}`,
      order: sequence,
      role,
      materialId,
      quantity: String(readNumber(item, "quantity") || ""),
      wasteRate: String(readNumber(item, "wastePercent") || 0),
      note: readString(item, "notes"),
    };

    if (index < baseLines.length) {
      baseLines[index] = line;
    } else {
      baseLines.push(line);
    }
  });

  return baseLines.sort((left, right) => left.order - right.order);
}

function toRecipeRequest(form: RecipeRecord, selectedLines: RecipeLine[], materials: Material[]) {
  return {
    code: form.code.trim(),
    name: form.name.trim(),
    productId: form.productId,
    version: Math.max(1, Math.trunc(safeParsedNumber(form.version))),
    description: form.productionNote || null,
    outputQuantity: safeParsedNumber(form.outputQuantity) || 1,
    outputUnit: form.outputUnit.trim() || "Çift",
    isActive: form.isActive,
    isDefault: form.isDefault,
    effectiveFrom: form.startDate ? new Date(`${form.startDate}T00:00:00.000Z`).toISOString() : null,
    effectiveTo: form.endDate ? new Date(`${form.endDate}T00:00:00.000Z`).toISOString() : null,
    notes: buildRecipeNotes(form),
    items: selectedLines.map((line, index) => {
      const material = materials.find((item) => item.id === line.materialId);
      return {
        materialId: line.materialId,
        quantity: safeParsedNumber(line.quantity),
        unit: material?.unit || null,
        wastePercent: safeParsedNumber(line.wasteRate),
        isOptional: false,
        sequence: line.order || index + 1,
        notes: line.note || null,
      };
    }),
  };
}

function buildRecipeNotes(form: RecipeRecord) {
  const meta = {
    revisionNo: form.revisionNo,
    standardWeight: form.standardWeight,
    standardDensity: form.standardDensity,
    standardCycleTime: form.standardCycleTime,
    standardWasteRate: form.standardWasteRate,
    standardMold: form.standardMold,
    standardMachine: form.standardMachine,
    standardDailyCapacity: form.standardDailyCapacity,
    cuttingMachine: form.cuttingMachine,
    operationNote: form.operationNote,
    laborCost: form.laborCost,
    electricityCost: form.electricityCost,
    packagingCost: form.packagingCost,
    overheadCost: form.overheadCost,
    revisionDescription: form.revisionDescription,
    updatedBy: form.updatedBy,
    revisionDate: form.revisionDate,
    productionNote: form.productionNote,
    qualityNote: form.qualityNote,
    customerNote: form.customerNote,
    generalNote: form.generalNote,
  };

  return `${form.generalNote || ""}${RECIPE_MARKER}${JSON.stringify(meta)}`;
}

function parseRecipeMeta(notes: string): Partial<RecipeRecord> {
  const markerIndex = notes.indexOf(RECIPE_MARKER);
  if (markerIndex === -1) return {};

  try {
    return JSON.parse(notes.slice(markerIndex + RECIPE_MARKER.length)) as Partial<RecipeRecord>;
  } catch {
    return {};
  }
}

function stripRecipeMeta(notes: string) {
  const markerIndex = notes.indexOf(RECIPE_MARKER);
  return markerIndex === -1 ? notes : notes.slice(0, markerIndex);
}

function readString(value: Record<string, unknown>, key: string) {
  const raw = value[key] ?? value[toPascalCase(key)];
  return typeof raw === "string" ? raw : "";
}

function readNumber(value: Record<string, unknown>, key: string) {
  const raw = value[key] ?? value[toPascalCase(key)];
  if (typeof raw === "number" && Number.isFinite(raw)) return raw;
  if (typeof raw === "string" && raw.trim()) {
    const parsed = Number(raw.replace(",", "."));
    return Number.isFinite(parsed) ? parsed : 0;
  }
  return 0;
}

function readBoolean(value: Record<string, unknown>, key: string, fallback: boolean) {
  const raw = value[key] ?? value[toPascalCase(key)];
  return typeof raw === "boolean" ? raw : fallback;
}

function readArray(value: Record<string, unknown>, key: string) {
  const raw = value[key] ?? value[toPascalCase(key)];
  return Array.isArray(raw) ? raw : [];
}

function toPascalCase(value: string) {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function formatMaterialOption(material: Material) {
  return `${[material.code, material.name].filter(Boolean).join(" - ") || material.id} | ${material.materialType || "-"} | ${material.unit || "-"} | ${formatMoney(safeNumber(material.lastPurchasePrice), material.currency || "TRY")}`;
}

function getRecipeCurrency(recipe: RecipeRecord, materials: Material[]) {
  const material = recipe.lines.map((line) => materials.find((item) => item.id === line.materialId)).find(Boolean);
  return material?.currency || "TRY";
}

function safeNumber(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function safeParsedNumber(value: string) {
  if (!value.trim()) return 0;
  const parsed = Number(value.replace(",", "."));
  return Number.isFinite(parsed) ? parsed : 0;
}

function toInputNumber(value?: number | null) {
  return typeof value === "number" && Number.isFinite(value) ? String(value) : "";
}

function formatNumber(value?: number | null) {
  if (typeof value !== "number" || !Number.isFinite(value)) return "-";
  return value.toLocaleString("tr-TR", { maximumFractionDigits: 2 });
}

function formatMoney(value: number, currency: string) {
  return value.toLocaleString("tr-TR", { style: "currency", currency: currency || "TRY", maximumFractionDigits: 2 });
}

function formatCurrencyTotals(totalsByCurrency: Record<string, number>) {
  const entries = Object.entries(totalsByCurrency).filter(([, value]) => value > 0);
  if (entries.length === 0) return formatMoney(0, "TRY");
  return entries.map(([currency, value]) => formatMoney(value, currency)).join(" / ");
}

function formatDateInput(value: Date) {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, "0");
  const day = String(value.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleDateString("tr-TR");
}

function toDateInput(value: string) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return formatDateInput(date);
}
