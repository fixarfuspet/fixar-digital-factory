"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";

type MovementType = "Giriş" | "Çıkış";
type StockDialogMode = "create" | "edit" | "delete";
type DashboardTone = "emerald" | "red" | "cyan" | "amber" | "zinc" | "blue";

type StockMovement = {
  id?: string;
  movementType?: string;
  quantity?: number;
  unitPrice?: number | null;
  sourceType?: string | null;
  sourceDocumentNo?: string | null;
  note?: string | null;
  createdAt?: string | null;
  movementDate?: string | null;
};

type StockItem = {
  id: string;
  name: string;
  code?: string | null;
  category: string;
  unit: string;
  currentQuantity: number;
  criticalQuantity: number;
  minimumQuantity?: number | null;
  maximumQuantity?: number | null;
  minQuantity?: number | null;
  maxQuantity?: number | null;
  lastPurchasePrice?: number | null;
  supplierName?: string | null;
  supplierCode?: string | null;
  warehouseName?: string | null;
  warehouseCode?: string | null;
  warehouseLocation?: string | null;
  shelfCode?: string | null;
  rackCode?: string | null;
  locationCode?: string | null;
  note?: string | null;
  isActive: boolean;
  isCritical: boolean;
  movements?: StockMovement[];
  movementHistory?: StockMovement[];
};

type StockFormState = {
  name: string;
  code: string;
  category: string;
  unit: string;
  currentQuantity: string;
  criticalQuantity: string;
  minimumQuantity: string;
  maximumQuantity: string;
  warehouseName: string;
  shelfCode: string;
  supplierName: string;
  lastPurchasePrice: string;
  note: string;
  isActive: boolean;
};

const API = "http://localhost:5000/api/v1";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60";

const emptyForm: StockFormState = {
  name: "",
  code: "",
  category: "",
  unit: "",
  currentQuantity: "",
  criticalQuantity: "",
  minimumQuantity: "",
  maximumQuantity: "",
  warehouseName: "",
  shelfCode: "",
  supplierName: "",
  lastPurchasePrice: "",
  note: "",
  isActive: true,
};

export default function StocksPage() {
  const [stocks, setStocks] = useState<StockItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedStock, setSelectedStock] = useState<StockItem | null>(null);
  const [detailStock, setDetailStock] = useState<StockItem | null>(null);
  const [historyStock, setHistoryStock] = useState<StockItem | null>(null);
  const [movementType, setMovementType] = useState<MovementType>("Giriş");
  const [dialogMode, setDialogMode] = useState<StockDialogMode | null>(null);
  const [dialogStock, setDialogStock] = useState<StockItem | null>(null);
  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("Tümü");
  const [statusFilter, setStatusFilter] = useState("Tümü");

  useEffect(() => {
    loadStocks();
  }, []);

  async function loadStocks() {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(API + "/stocks");

      if (!response.ok) {
        throw new Error("Stok listesi alınamadı.");
      }

      const result = await response.json();
      setStocks(Array.isArray(result.data) ? result.data : []);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  function openMovement(item: StockItem, type: MovementType) {
    setSelectedStock(item);
    setMovementType(type);
  }

  function openStockDialog(mode: StockDialogMode, item: StockItem | null = null) {
    setDialogMode(mode);
    setDialogStock(item);
  }

  const categories = useMemo(() => {
    const values = stocks.map((item) => item.category).filter(Boolean);
    return ["Tümü", ...Array.from(new Set(values))];
  }, [stocks]);

  const filteredStocks = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase("tr-TR");

    return stocks.filter((item) => {
      const matchesSearch =
        !normalizedSearch ||
        [item.name, item.code, item.category, item.supplierName, getWarehouseText(item), getShelfText(item)]
          .filter(Boolean)
          .some((value) => String(value).toLocaleLowerCase("tr-TR").includes(normalizedSearch));

      const matchesCategory = categoryFilter === "Tümü" || item.category === categoryFilter;
      const matchesStatus =
        statusFilter === "Tümü" ||
        (statusFilter === "Kritik" && item.isCritical) ||
        (statusFilter === "Normal" && !item.isCritical) ||
        (statusFilter === "Pasif" && !item.isActive);

      return matchesSearch && matchesCategory && matchesStatus;
    });
  }, [categoryFilter, search, statusFilter, stocks]);

  const criticalStocks = stocks.filter((item) => item.isCritical);
  const activeStocks = stocks.filter((item) => item.isActive).length;
  const passiveStocks = stocks.length - activeStocks;
  const categoriesCount = categories.length > 0 ? Math.max(categories.length - 1, 0) : 0;
  const warehouseCount = new Set(stocks.map(getWarehouseText).filter((value) => value !== "-")).size;
  const supplierCount = new Set(stocks.map((item) => item.supplierName).filter(Boolean)).size;
  const totalQuantity = stocks.reduce((sum, item) => sum + safeNumber(item.currentQuantity), 0);
  const inventoryValue = stocks.reduce((sum, item) => {
    const price = item.lastPurchasePrice ?? 0;
    return sum + safeNumber(item.currentQuantity) * price;
  }, 0);
  const dashboardCards = [
    { title: "Stok Kartı", value: stocks.length.toLocaleString("tr-TR"), note: `${activeStocks} aktif · ${passiveStocks} pasif`, tone: "emerald" as DashboardTone },
    { title: "Kritik Stok", value: criticalStocks.length.toLocaleString("tr-TR"), note: "Minimum seviyenin altında", tone: "red" as DashboardTone },
    { title: "Toplam Miktar", value: formatNumber(totalQuantity), note: "Tüm birimler toplamı", tone: "cyan" as DashboardTone },
    { title: "Stok Değeri", value: formatCurrency(inventoryValue), note: "Son alış fiyatına göre ₺", tone: "amber" as DashboardTone },
    { title: "Kategori", value: categoriesCount.toLocaleString("tr-TR"), note: "Tanımlı stok grubu", tone: "blue" as DashboardTone },
    { title: "Depo / Tedarikçi", value: `${warehouseCount} / ${supplierCount}`, note: "Depo ve tedarikçi görünümü", tone: "zinc" as DashboardTone },
  ];

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.18),transparent_34%),radial-gradient(circle_at_bottom_right,rgba(14,165,233,0.13),transparent_32%)] px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.38em] text-emerald-300">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">Stok ve Depo Yönetimi</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                Hammadde, kumaş, sarf malzeme, tedarikçi ve raf bilgilerini tek ekrandan takip edin.
              </p>
            </div>

            <div className="flex flex-col gap-3 sm:flex-row">
              <button
                onClick={loadStocks}
                disabled={loading}
                className="rounded-xl border border-white/10 bg-white/[0.08] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:opacity-50"
              >
                {loading ? "Yenileniyor..." : "Yenile"}
              </button>
              <button
                onClick={() => openStockDialog("create")}
                className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400"
              >
                Stok Kartı Oluştur
              </button>
            </div>
          </header>

          <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-6">
            {dashboardCards.map((card) => (
              <DashboardCard key={card.title} title={card.title} value={card.value} note={card.note} tone={card.tone} />
            ))}
          </section>

          {criticalStocks.length > 0 && (
            <section className="rounded-2xl border border-red-400/30 bg-red-500/10 p-5 shadow-xl">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                <div>
                  <p className="text-xs font-black tracking-[0.22em] text-red-300">KRİTİK STOK UYARILARI</p>
                  <h2 className="mt-2 text-2xl font-black">{criticalStocks.length} kart takip gerektiriyor</h2>
                </div>
                <button
                  onClick={() => setStatusFilter("Kritik")}
                  className="rounded-xl bg-red-500 px-4 py-3 text-sm font-black text-white transition hover:bg-red-400"
                >
                  Kritik Kartları Göster
                </button>
              </div>
              <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-3">
                {criticalStocks.slice(0, 3).map((item) => (
                  <button
                    key={item.id}
                    onClick={() => setDetailStock(item)}
                    className="rounded-xl border border-red-300/20 bg-black/20 p-4 text-left transition hover:bg-black/35"
                  >
                    <p className="text-sm font-black">{item.name}</p>
                    <p className="mt-1 text-xs text-red-200">
                      {formatNumber(item.currentQuantity)} {item.unit} / Kritik: {formatNumber(item.criticalQuantity)} {item.unit}
                    </p>
                  </button>
                ))}
              </div>
            </section>
          )}

          <section className="rounded-2xl border border-white/10 bg-white/[0.06] p-5 shadow-2xl backdrop-blur">
            <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
              <div>
                <h2 className="text-2xl font-black">Stok Kartları</h2>
                <p className="mt-1 text-sm text-zinc-400">
                  {filteredStocks.length.toLocaleString("tr-TR")} kart listeleniyor.
                </p>
              </div>

              <div className="grid grid-cols-1 gap-3 md:grid-cols-3 xl:min-w-[760px]">
                <Field label="Arama">
                  <input
                    value={search}
                    onChange={(event) => setSearch(event.target.value)}
                    className={CONTROL_CLASS}
                    placeholder="Kod, stok adı, depo, tedarikçi"
                  />
                </Field>

                <Field label="Kategori">
                  <select value={categoryFilter} onChange={(event) => setCategoryFilter(event.target.value)} className={CONTROL_CLASS}>
                    {categories.map((category) => (
                      <option key={category} value={category}>
                        {category}
                      </option>
                    ))}
                  </select>
                </Field>

                <Field label="Durum">
                  <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} className={CONTROL_CLASS}>
                    <option value="Tümü">Tümü</option>
                    <option value="Kritik">Kritik</option>
                    <option value="Normal">Normal</option>
                    <option value="Pasif">Pasif</option>
                  </select>
                </Field>
              </div>
            </div>

            {error && <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-4 text-sm text-red-200">{error}</div>}
            {loading && <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-5 text-zinc-400">Stok kartları yükleniyor...</div>}

            {!loading && filteredStocks.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-6 text-zinc-300">
                Arama veya filtrelere uygun stok kartı bulunamadı.
              </div>
            )}

            {!loading && filteredStocks.length > 0 && (
              <div className="mt-5 grid grid-cols-1 gap-4 lg:grid-cols-2 2xl:grid-cols-3">
                {filteredStocks.map((item) => (
                  <StockCard
                    key={item.id}
                    item={item}
                    onDetail={() => setDetailStock(item)}
                    onHistory={() => setHistoryStock(item)}
                    onEdit={() => openStockDialog("edit", item)}
                    onDelete={() => openStockDialog("delete", item)}
                    onEntry={() => openMovement(item, "Giriş")}
                    onExit={() => openMovement(item, "Çıkış")}
                  />
                ))}
              </div>
            )}
          </section>
        </div>
      </div>

      <MovementModal
        stock={selectedStock}
        movementType={movementType}
        onClose={() => setSelectedStock(null)}
        onSaved={() => {
          setSelectedStock(null);
          loadStocks();
        }}
      />

      <StockDetailModal
        stock={detailStock}
        onClose={() => setDetailStock(null)}
        onEntry={(item) => openMovement(item, "Giriş")}
        onExit={(item) => openMovement(item, "Çıkış")}
        onEdit={(item) => openStockDialog("edit", item)}
        onHistory={(item) => setHistoryStock(item)}
      />

      <MovementHistoryModal stock={historyStock} onClose={() => setHistoryStock(null)} />

      <StockCreateModal
        open={dialogMode === "create"}
        onClose={() => {
          setDialogMode(null);
          setDialogStock(null);
        }}
      />

      <StockEditModal
        stock={dialogMode === "edit" ? dialogStock : null}
        onClose={() => {
          setDialogMode(null);
          setDialogStock(null);
        }}
      />

      <StockDeleteModal
        stock={dialogMode === "delete" ? dialogStock : null}
        onClose={() => {
          setDialogMode(null);
          setDialogStock(null);
        }}
      />
    </main>
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
      : tone === "blue"
      ? "text-sky-300"
      : tone === "zinc"
      ? "text-zinc-300"
      : "text-emerald-300";

  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.06] p-5 shadow-xl backdrop-blur xl:min-h-36">
      <p className="text-sm text-zinc-400">{title}</p>
      <h3 className="mt-2 break-words text-3xl font-black text-white">{value}</h3>
      <p className={`mt-2 text-xs font-bold ${color}`}>{note}</p>
    </div>
  );
}

function StockCard({
  item,
  onDetail,
  onHistory,
  onEdit,
  onDelete,
  onEntry,
  onExit,
}: {
  item: StockItem;
  onDetail: () => void;
  onHistory: () => void;
  onEdit: () => void;
  onDelete: () => void;
  onEntry: () => void;
  onExit: () => void;
}) {
  const minimum = getMinimumQuantity(item);
  const maximum = getMaximumQuantity(item);
  const level = getStockLevel(item);

  return (
    <article
      className={
        "rounded-2xl border p-5 shadow-xl transition " +
        (item.isCritical ? "border-red-400/35 bg-red-500/10" : "border-white/10 bg-black/20 hover:border-emerald-400/30")
      }
    >
      <div className="flex items-start justify-between gap-4">
        <button onClick={onDetail} className="min-w-0 text-left">
          <p className="text-xs font-black uppercase tracking-[0.18em] text-emerald-300">{item.category || "Kategori Yok"}</p>
          <h3 className="mt-1 line-clamp-2 text-xl font-black text-white">{item.name}</h3>
          <p className="mt-1 text-xs text-zinc-400">{item.code || "Kod tanımsız"}</p>
        </button>

        <span
          className={
            "shrink-0 rounded-full px-3 py-1 text-xs font-black " +
            (item.isCritical ? "bg-red-500/20 text-red-200" : item.isActive ? "bg-emerald-500/20 text-emerald-200" : "bg-zinc-500/20 text-zinc-300")
          }
        >
          {item.isCritical ? "Kritik" : item.isActive ? "Aktif" : "Pasif"}
        </span>
      </div>

      <button onClick={onDetail} className="mt-4 w-full rounded-xl bg-white/[0.06] p-4 text-left transition hover:bg-white/[0.09]">
        <p className="text-xs text-zinc-400">Mevcut Stok</p>
        <p className="mt-1 text-4xl font-black">
          {formatNumber(item.currentQuantity)} <span className="text-lg text-zinc-400">{item.unit}</span>
        </p>
        <ProgressBar value={level} critical={item.isCritical} />
      </button>

      <div className="mt-4 grid grid-cols-2 gap-3">
        <Info label="Minimum" value={formatQuantity(minimum, item.unit)} />
        <Info label="Maksimum" value={formatQuantity(maximum, item.unit)} />
        <Info label="Depo" value={getWarehouseText(item)} />
        <Info label="Raf" value={getShelfText(item)} />
        <Info label="Tedarikçi" value={item.supplierName || "-"} />
        <Info label="Son Alış" value={formatPrice(item.lastPurchasePrice)} />
      </div>

      {item.note && <p className="mt-4 rounded-xl bg-black/20 p-3 text-sm text-zinc-400">{item.note}</p>}

      <div className="mt-5 grid grid-cols-2 gap-3">
        <button onClick={onEntry} className="rounded-xl bg-emerald-500 px-4 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
          Stok Girişi
        </button>
        <button onClick={onExit} className="rounded-xl bg-red-500 px-4 py-3 text-sm font-black text-white transition hover:bg-red-400">
          Stok Çıkışı
        </button>
      </div>

      <div className="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-4">
        <button onClick={onDetail} className="rounded-xl border border-white/10 bg-white/[0.06] px-3 py-2 text-xs font-bold text-zinc-200 transition hover:bg-white/[0.1]">
          Detay
        </button>
        <button onClick={onHistory} className="rounded-xl border border-white/10 bg-white/[0.06] px-3 py-2 text-xs font-bold text-zinc-200 transition hover:bg-white/[0.1]">
          Geçmiş
        </button>
        <button onClick={onEdit} className="rounded-xl border border-white/10 bg-white/[0.06] px-3 py-2 text-xs font-bold text-zinc-200 transition hover:bg-white/[0.1]">
          Düzenle
        </button>
        <button onClick={onDelete} className="rounded-xl border border-red-400/20 bg-red-500/10 px-3 py-2 text-xs font-bold text-red-200 transition hover:bg-red-500/20">
          Sil
        </button>
      </div>
    </article>
  );
}

function StockDetailModal({
  stock,
  onClose,
  onEntry,
  onExit,
  onEdit,
  onHistory,
}: {
  stock: StockItem | null;
  onClose: () => void;
  onEntry: (stock: StockItem) => void;
  onExit: (stock: StockItem) => void;
  onEdit: (stock: StockItem) => void;
  onHistory: (stock: StockItem) => void;
}) {
  if (!stock) return null;

  const movements = stock.movements ?? stock.movementHistory ?? [];

  return (
    <div className="fixed inset-0 z-40 bg-black/70 backdrop-blur-sm">
      <div className="ml-auto flex h-full w-full max-w-3xl flex-col overflow-y-auto border-l border-white/10 bg-[#0F1115] p-5 shadow-2xl sm:p-8">
        <div className="flex items-start justify-between gap-4 border-b border-white/10 pb-5">
          <div>
            <p className="text-xs font-black tracking-[0.28em] text-emerald-300">STOK DETAYI</p>
            <h2 className="mt-2 text-3xl font-black">{stock.name}</h2>
            <p className="mt-1 text-sm text-zinc-400">{stock.code || "Kod tanımsız"} · {stock.category}</p>
          </div>
          <button onClick={onClose} className="rounded-xl bg-white/[0.08] px-4 py-2 text-sm font-bold text-white transition hover:bg-white/[0.14]">
            Kapat
          </button>
        </div>

        <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-3">
          <SummaryMini label="Mevcut" value={`${formatNumber(stock.currentQuantity)} ${stock.unit}`} />
          <SummaryMini label="Minimum" value={formatQuantity(getMinimumQuantity(stock), stock.unit)} />
          <SummaryMini label="Maksimum" value={formatQuantity(getMaximumQuantity(stock), stock.unit)} />
        </div>

        <div className="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Info label="Depo" value={getWarehouseText(stock)} />
          <Info label="Raf / Lokasyon" value={getShelfText(stock)} />
          <Info label="Tedarikçi" value={stock.supplierName || "-"} />
          <Info label="Tedarikçi Kodu" value={stock.supplierCode || "-"} />
          <Info label="Son Alış Fiyatı" value={formatPrice(stock.lastPurchasePrice)} />
          <Info label="Kritik Stok" value={formatQuantity(stock.criticalQuantity, stock.unit)} />
        </div>

        {stock.note && (
          <div className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-4">
            <p className="text-xs text-zinc-500">Not</p>
            <p className="mt-1 text-sm text-zinc-300">{stock.note}</p>
          </div>
        )}

        <div className="mt-6 grid grid-cols-1 gap-3 sm:grid-cols-4">
          <button onClick={() => onEntry(stock)} className="rounded-xl bg-emerald-500 px-4 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
            Stok Girişi
          </button>
          <button onClick={() => onExit(stock)} className="rounded-xl bg-red-500 px-4 py-3 text-sm font-black text-white transition hover:bg-red-400">
            Stok Çıkışı
          </button>
          <button onClick={() => onEdit(stock)} className="rounded-xl border border-white/10 bg-white/[0.08] px-4 py-3 text-sm font-black text-white transition hover:bg-white/[0.14]">
            Kartı Düzenle
          </button>
          <button onClick={() => onHistory(stock)} className="rounded-xl border border-white/10 bg-white/[0.08] px-4 py-3 text-sm font-black text-white transition hover:bg-white/[0.14]">
            Geçmiş
          </button>
        </div>

        <MovementHistorySection stock={stock} movements={movements} compact={false} />
      </div>
    </div>
  );
}

function MovementHistoryModal({ stock, onClose }: { stock: StockItem | null; onClose: () => void }) {
  if (!stock) return null;

  const movements = stock.movements ?? stock.movementHistory ?? [];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4 backdrop-blur-sm">
      <div className="max-h-[92vh] w-full max-w-3xl overflow-y-auto rounded-2xl border border-white/10 bg-[#0F1115] p-5 shadow-2xl sm:p-8">
        <ModalHeader title="Hareket Geçmişi" subtitle={`${stock.name} · ${stock.code || "Kod tanımsız"}`} onClose={onClose} />
        <MovementHistorySection stock={stock} movements={movements} compact={false} />
      </div>
    </div>
  );
}

function StockCreateModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  if (!open) return null;

  return (
    <StockFormModal
      key="create"
      title="Stok Kartı Oluştur"
      subtitle="Yeni stok kartı bilgilerini hazırlayın."
      initialStock={null}
      submitLabel="Kaydet"
      onClose={onClose}
    />
  );
}

function StockEditModal({ stock, onClose }: { stock: StockItem | null; onClose: () => void }) {
  if (!stock) return null;

  return (
    <StockFormModal
      key={`edit-${stock.id}`}
      title="Stok Kartı Düzenle"
      subtitle={`${stock.name} kart bilgilerini güncelleyin.`}
      initialStock={stock}
      submitLabel="Değişiklikleri Kaydet"
      onClose={onClose}
    />
  );
}

function StockDeleteModal({ stock, onClose }: { stock: StockItem | null; onClose: () => void }) {
  if (!stock) return null;

  function unavailableDelete() {
    alert(
      "Silme ekranı hazır, ancak verilen mevcut API listesinde stok kartı silme endpointi yok.\n\n" +
        "Backend tarafına istek gönderilmedi."
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4 backdrop-blur-sm">
      <div className="w-full max-w-xl rounded-2xl border border-white/10 bg-[#0F1115] p-5 shadow-2xl sm:p-8">
        <ModalHeader title="Stok Kartı Sil" subtitle={stock.code || "Kod tanımsız"} onClose={onClose} />
        <div className="rounded-2xl border border-red-400/30 bg-red-500/10 p-5">
          <p className="text-sm text-red-100">
            <strong>{stock.name}</strong> stok kartı silinmek üzere seçildi. Bu işlem mevcut API sınırları nedeniyle yalnızca arayüzde onaylanabilir.
          </p>
          <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Info label="Mevcut Stok" value={`${formatNumber(stock.currentQuantity)} ${stock.unit}`} />
            <Info label="Depo" value={getWarehouseText(stock)} />
          </div>
        </div>
        <ModalActions onClose={onClose} onSubmit={unavailableDelete} submitLabel="Sil" submitTone="danger" />
      </div>
    </div>
  );
}

function StockFormModal({
  title,
  subtitle,
  initialStock,
  submitLabel,
  onClose,
}: {
  title: string;
  subtitle: string;
  initialStock: StockItem | null;
  submitLabel: string;
  onClose: () => void;
}) {
  const [form, setForm] = useState<StockFormState>(() => toStockForm(initialStock));

  function updateForm(key: keyof StockFormState, value: string | boolean) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function saveStockCard() {
  if (initialStock) {
    alert("Düzenleme için PUT endpointi henüz bağlı değil. Şimdilik sadece yeni stok kartı oluşturma aktif.");
    return;
  }

  if (!form.name.trim()) {
    alert("Stok adı zorunludur.");
    return;
  }

  const response = await fetch(API + "/stocks", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      name: form.name.trim(),
      code: form.code.trim() || null,
      category: form.category.trim() || "Genel",
      unit: form.unit.trim() || "kg",
      currentQuantity: Number(form.currentQuantity || 0),
      criticalQuantity: Number(form.criticalQuantity || 0),
      minimumQuantity: form.minimumQuantity ? Number(form.minimumQuantity) : null,
      maximumQuantity: form.maximumQuantity ? Number(form.maximumQuantity) : null,
      lastPurchasePrice: form.lastPurchasePrice ? Number(form.lastPurchasePrice) : null,
      currency: "TRY",
      vatRate: null,
      supplierName: form.supplierName.trim() || null,
      supplierCode: null,
      leadTimeDays: null,
      warehouseName: form.warehouseName.trim() || null,
      locationCode: form.shelfCode.trim() || null,
      lotNumber: null,
      expiryDate: null,
      recipeUsageAmount: null,
      wasteRate: null,
      safetyInfo: null,
      note: form.note.trim() || null,
    }),
  });

  const resultText = await response.text();

  if (!response.ok) {
    alert("Stok kartı oluşturulamadı: " + resultText);
    return;
  }

  alert("Stok kartı oluşturuldu.");
  onClose();
  window.location.reload();
}


  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4 backdrop-blur-sm">
      <div className="max-h-[92vh] w-full max-w-3xl overflow-y-auto rounded-2xl border border-white/10 bg-[#0F1115] p-5 shadow-2xl sm:p-8">
        <ModalHeader
          title={title}
          subtitle={subtitle}
          onClose={onClose}
        />

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <StockFormFields
            form={form}
            updateForm={updateForm}
          />
        </div>

        <ModalActions
          onClose={onClose}
          onSubmit={saveStockCard}
          submitLabel={submitLabel}
          submitTone="success"
        />
      </div>
    </div>
  );
}

function StockFormFields({
  form,
  updateForm,
}: {
  form: StockFormState;
  updateForm: (key: keyof StockFormState, value: string | boolean) => void;
}) {
  return (
    <>
      <Field label="Stok Adı">
        <input value={form.name} onChange={(event) => updateForm("name", event.target.value)} className={CONTROL_CLASS} placeholder="Örn: Poliol" />
      </Field>
      <Field label="Stok Kodu">
        <input value={form.code} onChange={(event) => updateForm("code", event.target.value)} className={CONTROL_CLASS} placeholder="Örn: RM-001" />
      </Field>
      <Field label="Kategori">
        <input value={form.category} onChange={(event) => updateForm("category", event.target.value)} className={CONTROL_CLASS} placeholder="Hammadde" />
      </Field>
      <Field label="Birim">
        <input value={form.unit} onChange={(event) => updateForm("unit", event.target.value)} className={CONTROL_CLASS} placeholder="kg, adet, metre" />
      </Field>
      <Field label="Mevcut Stok">
        <input value={form.currentQuantity} onChange={(event) => updateForm("currentQuantity", event.target.value)} className={CONTROL_CLASS} placeholder="0" />
      </Field>
      <Field label="Kritik Stok">
        <input value={form.criticalQuantity} onChange={(event) => updateForm("criticalQuantity", event.target.value)} className={CONTROL_CLASS} placeholder="0" />
      </Field>
      <Field label="Minimum Stok">
        <input value={form.minimumQuantity} onChange={(event) => updateForm("minimumQuantity", event.target.value)} className={CONTROL_CLASS} placeholder="0" />
      </Field>
      <Field label="Maksimum Stok">
        <input value={form.maximumQuantity} onChange={(event) => updateForm("maximumQuantity", event.target.value)} className={CONTROL_CLASS} placeholder="0" />
      </Field>
      <Field label="Depo">
        <input value={form.warehouseName} onChange={(event) => updateForm("warehouseName", event.target.value)} className={CONTROL_CLASS} placeholder="Ana Depo" />
      </Field>
      <Field label="Raf / Lokasyon">
        <input value={form.shelfCode} onChange={(event) => updateForm("shelfCode", event.target.value)} className={CONTROL_CLASS} placeholder="A-01-03" />
      </Field>
      <Field label="Tedarikçi">
        <input value={form.supplierName} onChange={(event) => updateForm("supplierName", event.target.value)} className={CONTROL_CLASS} placeholder="Tedarikçi adı" />
      </Field>
      <Field label="Son Alış Fiyatı">
        <input value={form.lastPurchasePrice} onChange={(event) => updateForm("lastPurchasePrice", event.target.value)} className={CONTROL_CLASS} placeholder="0.00" />
      </Field>
      <label className="md:col-span-2">
        <p className="mb-2 text-sm font-bold text-zinc-300">Not</p>
        <textarea
          value={form.note}
          onChange={(event) => updateForm("note", event.target.value)}
          className={`${CONTROL_CLASS} min-h-24`}
          placeholder="İsteğe bağlı açıklama"
        />
      </label>
      <label className="flex items-center gap-3 rounded-xl border border-white/10 bg-black/20 p-4 md:col-span-2">
        <input type="checkbox" checked={form.isActive} onChange={(event) => updateForm("isActive", event.target.checked)} />
        <span className="text-sm font-bold text-zinc-200">Stok kartı aktif</span>
      </label>
    </>
  );
}

function MovementHistorySection({
  stock,
  movements,
  compact,
}: {
  stock: StockItem;
  movements: StockMovement[];
  compact: boolean;
}) {
  return (
    <section className={`${compact ? "" : "mt-7"} rounded-2xl border border-white/10 bg-white/[0.04] p-5`}>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="text-xl font-black">Hareket Geçmişi</h3>
          <p className="mt-1 text-sm text-zinc-400">Stok kartı içinde dönen hareket kayıtları burada listelenir.</p>
        </div>
        <span className="w-fit rounded-full bg-white/[0.08] px-3 py-1 text-xs font-bold text-zinc-300">{movements.length} kayıt</span>
      </div>

      {movements.length === 0 ? (
        <div className="mt-4 rounded-xl border border-white/10 bg-black/20 p-4 text-sm text-zinc-400">
          Bu stok kartı için hareket geçmişi verisi bulunmuyor.
        </div>
      ) : (
        <div className="mt-4 space-y-3">
          {movements.map((movement, index) => (
            <MovementHistoryRow key={movement.id ?? String(index)} movement={movement} unit={stock.unit} />
          ))}
        </div>
      )}
    </section>
  );
}

function MovementHistoryRow({ movement, unit }: { movement: StockMovement; unit: string }) {
  const isExit = movement.movementType === "Çıkış";

  return (
    <div className="rounded-xl border border-white/10 bg-black/20 p-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className={isExit ? "text-sm font-black text-red-200" : "text-sm font-black text-emerald-200"}>{movement.movementType || "Hareket"}</p>
          <p className="text-xs text-zinc-500">{formatDate(movement.createdAt ?? movement.movementDate)}</p>
        </div>
        <p className={isExit ? "text-sm font-black text-red-300" : "text-sm font-black text-emerald-300"}>
          {formatNumber(movement.quantity ?? 0)} {unit}
        </p>
      </div>
      <p className="mt-2 text-xs text-zinc-400">
        {movement.sourceType || "-"} · {movement.sourceDocumentNo || "Belge yok"} · {formatPrice(movement.unitPrice)}
      </p>
      {movement.note && <p className="mt-2 text-sm text-zinc-300">{movement.note}</p>}
    </div>
  );
}

function ModalHeader({ title, subtitle, onClose }: { title: string; subtitle: string; onClose: () => void }) {
  return (
    <div className="mb-6 flex items-start justify-between gap-4">
      <div>
        <p className="text-xs font-black tracking-[0.28em] text-emerald-300">FIXAR OS</p>
        <h2 className="mt-2 text-3xl font-black">{title}</h2>
        <p className="mt-1 text-sm text-zinc-400">{subtitle}</p>
      </div>
      <button onClick={onClose} className="rounded-xl bg-zinc-800 px-4 py-2 text-sm font-bold text-white transition hover:bg-zinc-700">
        Kapat
      </button>
    </div>
  );
}

function ModalActions({
  onClose,
  onSubmit,
  submitLabel,
  submitTone,
}: {
  onClose: () => void;
  onSubmit: () => void;
  submitLabel: string;
  submitTone: "success" | "danger";
}) {
  return (
    <div className="mt-7 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
      <button onClick={onClose} className="rounded-xl bg-zinc-700 px-5 py-3 font-bold text-white transition hover:bg-zinc-600">
        Vazgeç
      </button>
      <button
        onClick={onSubmit}
        className={
          "rounded-xl px-5 py-3 font-black transition " +
          (submitTone === "danger" ? "bg-red-500 text-white hover:bg-red-400" : "bg-emerald-500 text-black hover:bg-emerald-400")
        }
      >
        {submitLabel}
      </button>
    </div>
  );
}

function MovementModal({
  stock,
  movementType,
  onClose,
  onSaved,
}: {
  stock: StockItem | null;
  movementType: MovementType;
  onClose: () => void;
  onSaved: () => void;
}) {
  if (!stock) return null;

  return <MovementModalContent key={`${stock.id}-${movementType}`} stock={stock} movementType={movementType} onClose={onClose} onSaved={onSaved} />;
}

function MovementModalContent({
  stock,
  movementType,
  onClose,
  onSaved,
}: {
  stock: StockItem;
  movementType: MovementType;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [quantity, setQuantity] = useState("");
  const [unitPrice, setUnitPrice] = useState("");
  const [documentNo, setDocumentNo] = useState("");
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);

  async function saveMovement() {
    if (saving) return;

    const qty = Number(quantity);
    const price = unitPrice ? Number(unitPrice) : null;

    if (!qty || qty <= 0) {
      alert("Geçerli miktar gir.");
      return;
    }

    const ok = confirm(
      stock.name +
        "\n\n" +
        movementType +
        " yapılacak: " +
        qty +
        " " +
        stock.unit +
        "\n\nOnaylıyor musun?"
    );

    if (!ok) return;

    setSaving(true);

    const response = await fetch(API + "/stocks/movement", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        stockItemId: stock.id,
        movementType,
        quantity: qty,
        unitPrice: price,
        sourceType: movementType === "Giriş" ? "Satın Alma" : "Üretim",
        sourceDocumentNo: documentNo || null,
        note: note || null,
      }),
    });

    setSaving(false);

    if (!response.ok) {
      const text = await response.text();
      alert("İşlem başarısız: " + text);
      return;
    }

    alert("Stok hareketi kaydedildi.");
    onSaved();
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4 backdrop-blur-sm">
      <div className="w-full max-w-2xl rounded-2xl border border-white/10 bg-[#0F1115] p-5 shadow-2xl sm:p-8">
        <div className="mb-6 flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-black tracking-[0.28em] text-emerald-300">FIXAR OS</p>
            <h2 className="mt-2 text-3xl font-black">{movementType === "Giriş" ? "Stok Girişi" : "Stok Çıkışı"}</h2>
          </div>

          <button onClick={onClose} className="rounded-xl bg-zinc-800 px-4 py-2 text-sm font-bold text-white transition hover:bg-zinc-700">
            Kapat
          </button>
        </div>

        <div className="mb-5 rounded-2xl border border-emerald-400/30 bg-emerald-400/10 p-5">
          <p className="text-xs font-black tracking-[0.18em] text-emerald-300">STOK KARTI</p>
          <h3 className="mt-1 text-2xl font-black">{stock.name}</h3>
          <p className="mt-2 text-sm text-zinc-300">
            Mevcut: {formatNumber(stock.currentQuantity)} {stock.unit}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <Field label={"Miktar (" + stock.unit + ")"}>
            <input value={quantity} onChange={(e) => setQuantity(e.target.value)} className={CONTROL_CLASS} placeholder="Örn: 1000" />
          </Field>

          <Field label="Birim Fiyat">
            <input value={unitPrice} onChange={(e) => setUnitPrice(e.target.value)} className={CONTROL_CLASS} placeholder="Örn: 3.75" />
          </Field>

          <Field label="Belge No">
            <input value={documentNo} onChange={(e) => setDocumentNo(e.target.value)} className={CONTROL_CLASS} placeholder="Fatura / irsaliye / fiş no" />
          </Field>

          <Field label="Not">
            <input value={note} onChange={(e) => setNote(e.target.value)} className={CONTROL_CLASS} placeholder="İsteğe bağlı not" />
          </Field>
        </div>

        <div className="mt-7 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <button onClick={onClose} disabled={saving} className="rounded-xl bg-zinc-700 px-5 py-3 font-bold text-white transition hover:bg-zinc-600 disabled:opacity-50">
            Vazgeç
          </button>

          <button
            onClick={saveMovement}
            disabled={saving}
            className={
              "rounded-xl px-5 py-3 font-black transition disabled:opacity-50 " +
              (movementType === "Giriş" ? "bg-emerald-500 text-black hover:bg-emerald-400" : "bg-red-500 text-white hover:bg-red-400")
            }
          >
            {saving ? "Kaydediliyor..." : "Kaydet"}
          </button>
        </div>
      </div>
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

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-white/10 bg-black/20 p-3">
      <p className="text-xs text-zinc-500">{label}</p>
      <p className="mt-1 break-words text-sm font-bold text-white">{value}</p>
    </div>
  );
}

function SummaryMini({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.06] p-4">
      <p className="text-xs text-zinc-500">{label}</p>
      <p className="mt-1 text-lg font-black text-white">{value}</p>
    </div>
  );
}

function ProgressBar({ value, critical }: { value: number; critical: boolean }) {
  return (
    <div className="mt-3 h-2 rounded-full bg-black/35">
      <div className={`h-2 rounded-full ${critical ? "bg-red-400" : "bg-emerald-400"}`} style={{ width: `${value}%` }} />
    </div>
  );
}

function toStockForm(stock: StockItem | null): StockFormState {
  if (!stock) return emptyForm;

  const warehouseText = getWarehouseText(stock);
  const shelfText = getShelfText(stock);

  return {
    name: stock.name || "",
    code: stock.code || "",
    category: stock.category || "",
    unit: stock.unit || "",
    currentQuantity: String(stock.currentQuantity ?? ""),
    criticalQuantity: String(stock.criticalQuantity ?? ""),
    minimumQuantity: String(getMinimumQuantity(stock) ?? ""),
    maximumQuantity: String(getMaximumQuantity(stock) ?? ""),
    warehouseName: warehouseText === "-" ? "" : warehouseText,
    shelfCode: shelfText === "-" ? "" : shelfText,
    supplierName: stock.supplierName || "",
    lastPurchasePrice: stock.lastPurchasePrice === null || stock.lastPurchasePrice === undefined ? "" : String(stock.lastPurchasePrice),
    note: stock.note || "",
    isActive: stock.isActive,
  };
}

function safeNumber(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function getMinimumQuantity(item: StockItem) {
  return item.minimumQuantity ?? item.minQuantity ?? item.criticalQuantity;
}

function getMaximumQuantity(item: StockItem) {
  return item.maximumQuantity ?? item.maxQuantity ?? null;
}

function getStockLevel(item: StockItem) {
  const maximum = getMaximumQuantity(item);
  const quantity = safeNumber(item.currentQuantity);

  if (!maximum || maximum <= 0) {
    const critical = safeNumber(item.criticalQuantity);
    if (!critical) return quantity > 0 ? 100 : 0;
    return Math.min(100, Math.round((quantity / Math.max(critical * 2, 1)) * 100));
  }

  return Math.max(0, Math.min(100, Math.round((quantity / maximum) * 100)));
}

function getWarehouseText(item: StockItem) {
  return item.warehouseName || item.warehouseCode || item.warehouseLocation || "-";
}

function getShelfText(item: StockItem) {
  return item.shelfCode || item.rackCode || item.locationCode || "-";
}

function formatNumber(value: number) {
  return safeNumber(value).toLocaleString("tr-TR");
}

function formatQuantity(value: number | null | undefined, unit: string) {
  if (value === null || value === undefined) return "-";
  return `${formatNumber(value)} ${unit}`;
}

function formatCurrency(value: number) {
  return value.toLocaleString("tr-TR", {
    maximumFractionDigits: 0,
    style: "currency",
    currency: "TRY",
  });
}

function formatPrice(value: number | null | undefined) {
  if (value === null || value === undefined) return "-";
  return value.toLocaleString("tr-TR", {
    maximumFractionDigits: 2,
    style: "currency",
    currency: "TRY",
  });
}

function formatDate(value: string | null | undefined) {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString("tr-TR");
}
