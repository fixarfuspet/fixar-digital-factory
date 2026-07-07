"use client";

import { useEffect, useState } from "react";

type StockItem = {
  id: string;
  name: string;
  code?: string;
  category: string;
  unit: string;
  currentQuantity: number;
  criticalQuantity: number;
  lastPurchasePrice?: number | null;
  supplierName?: string | null;
  note?: string | null;
  isActive: boolean;
  isCritical: boolean;
};

const API = "http://localhost:5000/api/v1";

export default function StocksPage() {
  const [stocks, setStocks] = useState<StockItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedStock, setSelectedStock] = useState<StockItem | null>(null);
  const [movementType, setMovementType] = useState<"Giriş" | "Çıkış">("Giriş");

  useEffect(() => {
    loadStocks();
  }, []);

  async function loadStocks() {
    setLoading(true);
    const response = await fetch(API + "/stocks");
    const result = await response.json();
    setStocks(result.data ?? []);
    setLoading(false);
  }

  function openMovement(item: StockItem, type: "Giriş" | "Çıkış") {
    setSelectedStock(item);
    setMovementType(type);
  }

  const totalItems = stocks.length;
  const criticalItems = stocks.filter((x) => x.isCritical).length;
  const rawMaterials = stocks.filter((x) => x.category === "Hammadde").length;

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen px-6 py-8">
        <div className="mx-auto max-w-7xl space-y-8">
          <header className="border-b border-white/10 pb-6">
            <p className="text-sm font-bold tracking-[0.4em] text-emerald-400">
              FIXAR OS
            </p>
            <h1 className="mt-2 text-4xl font-black">Stok / Depo</h1>
            <p className="mt-2 text-sm text-zinc-400">
              Hammadde · kumaş · sarf malzeme · kritik stok takibi
            </p>
          </header>

          <section className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <SummaryCard title="Stok Kartı" value={String(totalItems)} note="Tanımlı ürün" />
            <SummaryCard title="Kritik Stok" value={String(criticalItems)} note="Acil takip gerekli" />
            <SummaryCard title="Hammadde" value={String(rawMaterials)} note="PU üretim girdileri" />
          </section>

          <section className="rounded-3xl border border-white/10 bg-white/[0.06] p-6 shadow-2xl">
            <div className="mb-5 flex items-center justify-between">
              <h2 className="text-2xl font-black">Stok Kartları</h2>

              <button
                onClick={loadStocks}
                className="rounded-xl bg-emerald-500 px-5 py-3 font-bold text-black hover:bg-emerald-400"
              >
                Yenile
              </button>
            </div>

            {loading && <p className="text-zinc-400">Yükleniyor...</p>}

            {!loading && stocks.length === 0 && (
              <div className="rounded-2xl border border-white/10 bg-black/20 p-6 text-zinc-300">
                Henüz stok kartı yok.
              </div>
            )}

            {!loading && stocks.length > 0 && (
              <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
                {stocks.map((item) => (
                  <StockCard
                    key={item.id}
                    item={item}
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
    </main>
  );
}

function SummaryCard({ title, value, note }: { title: string; value: string; note: string }) {
  return (
    <div className="rounded-3xl border border-white/10 bg-white/[0.06] p-6 shadow-2xl">
      <p className="text-sm text-zinc-400">{title}</p>
      <h3 className="mt-3 text-4xl font-black">{value}</h3>
      <p className="mt-3 text-xs text-emerald-300">{note}</p>
    </div>
  );
}

function StockCard({
  item,
  onEntry,
  onExit,
}: {
  item: StockItem;
  onEntry: () => void;
  onExit: () => void;
}) {
  return (
    <div
      className={
        "rounded-2xl border p-5 shadow-lg " +
        (item.isCritical
          ? "border-red-400/40 bg-red-400/10"
          : "border-emerald-400/30 bg-emerald-400/10")
      }
    >
      <div className="mb-4 flex items-start justify-between gap-4">
        <div>
          <p className="text-xs font-bold text-emerald-300">{item.category}</p>
          <h3 className="mt-1 text-xl font-black text-white">{item.name}</h3>
          <p className="mt-1 text-xs text-zinc-400">{item.code || "-"}</p>
        </div>

        <span
          className={
            "rounded-full px-3 py-1 text-xs font-bold " +
            (item.isCritical
              ? "bg-red-500/20 text-red-300"
              : "bg-emerald-500/20 text-emerald-300")
          }
        >
          {item.isCritical ? "Kritik" : "Normal"}
        </span>
      </div>

      <div className="rounded-2xl bg-black/25 p-4">
        <p className="text-xs text-zinc-400">Mevcut Stok</p>
        <p className="mt-1 text-4xl font-black">
          {item.currentQuantity.toLocaleString("tr-TR")}{" "}
          <span className="text-lg text-zinc-400">{item.unit}</span>
        </p>
      </div>

      <div className="mt-4 grid grid-cols-2 gap-3">
        <Info label="Kritik Seviye" value={item.criticalQuantity.toString() + " " + item.unit} />
        <Info label="Tedarikçi" value={item.supplierName || "-"} />
        <Info
          label="Son Alış"
          value={
            item.lastPurchasePrice !== null && item.lastPurchasePrice !== undefined
              ? item.lastPurchasePrice.toString() + " €"
              : "-"
          }
        />
        <Info label="Durum" value={item.isActive ? "Aktif" : "Pasif"} />
      </div>

      {item.note && <p className="mt-4 text-sm text-zinc-400">{item.note}</p>}

      <div className="mt-5 grid grid-cols-2 gap-3">
        <button
          onClick={onEntry}
          className="rounded-xl bg-emerald-500 px-4 py-3 text-sm font-black text-black hover:bg-emerald-400"
        >
          Stok Girişi
        </button>

        <button
          onClick={onExit}
          className="rounded-xl bg-red-500 px-4 py-3 text-sm font-black text-white hover:bg-red-400"
        >
          Stok Çıkışı
        </button>
      </div>
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
  movementType: "Giriş" | "Çıkış";
  onClose: () => void;
  onSaved: () => void;
}) {
  const [quantity, setQuantity] = useState("");
  const [unitPrice, setUnitPrice] = useState("");
  const [documentNo, setDocumentNo] = useState("");
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!stock) return;
    setQuantity("");
    setUnitPrice("");
    setDocumentNo("");
    setNote("");
  }, [stock]);

  if (!stock) return null;

  async function saveMovement() {
    if (!stock || saving) return;

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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4 backdrop-blur-sm">
      <div className="w-full max-w-2xl rounded-3xl border border-white/10 bg-[#0F1115] p-8 shadow-2xl">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <p className="text-sm font-bold text-emerald-400">FIXAR OS</p>
            <h2 className="mt-1 text-3xl font-black">
              {movementType === "Giriş" ? "Stok Girişi" : "Stok Çıkışı"}
            </h2>
          </div>

          <button onClick={onClose} className="rounded-xl bg-zinc-800 px-4 py-2 text-white">
            Kapat
          </button>
        </div>

        <div className="mb-5 rounded-2xl border border-emerald-400/30 bg-emerald-400/10 p-5">
          <p className="text-xs font-bold text-emerald-300">STOK KARTI</p>
          <h3 className="mt-1 text-2xl font-black">{stock.name}</h3>
          <p className="mt-2 text-sm text-zinc-300">
            Mevcut: {stock.currentQuantity.toLocaleString("tr-TR")} {stock.unit}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <Field label={"Miktar (" + stock.unit + ")"}>
            <input
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
              placeholder="Örn: 1000"
            />
          </Field>

          <Field label="Birim Fiyat">
            <input
              value={unitPrice}
              onChange={(e) => setUnitPrice(e.target.value)}
              className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
              placeholder="Örn: 3.75"
            />
          </Field>

          <Field label="Belge No">
            <input
              value={documentNo}
              onChange={(e) => setDocumentNo(e.target.value)}
              className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
              placeholder="Fatura / irsaliye / fiş no"
            />
          </Field>

          <Field label="Not">
            <input
              value={note}
              onChange={(e) => setNote(e.target.value)}
              className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
              placeholder="İsteğe bağlı not"
            />
          </Field>
        </div>

        <div className="mt-7 flex justify-end gap-3">
          <button
            onClick={onClose}
            disabled={saving}
            className="rounded-xl bg-zinc-700 px-5 py-3 font-bold text-white disabled:opacity-50"
          >
            Vazgeç
          </button>

          <button
            onClick={saveMovement}
            disabled={saving}
            className={
              "rounded-xl px-5 py-3 font-black disabled:opacity-50 " +
              (movementType === "Giriş"
                ? "bg-emerald-500 text-black hover:bg-emerald-400"
                : "bg-red-500 text-white hover:bg-red-400")
            }
          >
            {saving ? "Kaydediliyor..." : "Kaydet"}
          </button>
        </div>
      </div>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block">
      <p className="mb-2 text-sm font-bold text-zinc-300">{label}</p>
      {children}
    </label>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl bg-black/20 p-3">
      <p className="text-xs text-zinc-500">{label}</p>
      <p className="mt-1 text-sm font-bold text-white">{value}</p>
    </div>
  );
}