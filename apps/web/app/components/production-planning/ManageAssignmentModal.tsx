"use client";

import { useEffectEvent,useEffect, useState } from "react";
import { safeResponseJson, authenticatedFetch, API_PROXY } from "../../lib/api/client";

type Props = {
  open: boolean;
  station: number | null;
  onClose: () => void;
};

type AssignmentDetail = {
  id: string;
  stationNumberSnapshot: number;
  status: string;
  operatorName: string;
  producedPairs: number;
  customerName: string;
  productName: string;
  moldName: string;
  quantityPairs: number;
  orderItemProducedPairs: number;
  remainingPairs: number;
  productionType: string;
  fabricColor: string;
  startedAt: string;
  note?: string;
};

type AddTurnResult = { turnCount: number; activeStationCount: number; totalAddedPairs: number };

const API = API_PROXY;

export default function ManageAssignmentModal({ open, station, onClose }: Props) {
  const [item, setItem] = useState<AssignmentDetail | null>(null);
  const [loading, setLoading] = useState(false);

  const [turnCount, setTurnCount] = useState("1");
  const [savingTurn, setSavingTurn] = useState(false);

  const [addPairs, setAddPairs] = useState("100");
  const [savingProduction, setSavingProduction] = useState(false);

  const [note, setNote] = useState("");
  const [message, setMessage] = useState("");

  const loadEffect = useEffectEvent(load);
  useEffect(() => {
    if (!open || !station) return;
    const timer = window.setTimeout(() => void loadEffect(), 0);
    return () => window.clearTimeout(timer);
  }, [open, station]);

  async function load() {
    setLoading(true);
    const response = await authenticatedFetch(API + "/station-assignments/station/" + station);
    const result = await safeResponseJson<AssignmentDetail>(response);
    setItem(result.data ?? null);
    setLoading(false);
  }

  async function post<T = unknown>(url: string, body: unknown) {
    const response = await authenticatedFetch(API + url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      const text = await response.text();
      alert("İşlem başarısız: " + text);
      return null;
    }

    const result = await safeResponseJson<T>(response);
    await load();
    return result;
  }

  async function addTurn() {
    if (savingTurn) return;

    const turns = Number(turnCount);

    if (!turns || turns <= 0) {
      alert("Geçerli tur adedi gir.");
      return;
    }

    const ok = confirm(
      turns.toLocaleString("tr-TR") +
        " tur eklensin mi?\n\nAktif üretimdeki tüm istasyonlara otomatik dağıtılacak."
    );

    if (!ok) return;

    setSavingTurn(true);
    setMessage("");

    const result = await post<AddTurnResult>("/station-assignments/add-turn", {
      turnCount: turns,
      note: note || "Tur üretimi",
    });

    if (result?.data) {
      setMessage(
        result.data.turnCount +
          " tur eklendi. " +
          result.data.activeStationCount +
          " aktif istasyona toplam +" +
          result.data.totalAddedPairs +
          " çift dağıtıldı."
      );
    }

    setSavingTurn(false);
  }

  async function addProduction() {
    if (!item || savingProduction) return;

    const qty = Number(addPairs);

    if (!qty || qty <= 0) {
      alert("Geçerli üretim adedi gir.");
      return;
    }

    const ok = confirm(qty.toLocaleString("tr-TR") + " çift manuel düzeltme olarak eklensin mi?");
    if (!ok) return;

    setSavingProduction(true);
    setMessage("");

    const result = await post("/station-assignments/add-production", {
      stationAssignmentId: item.id,
      producedPairs: qty,
    });

    if (result) {
      setMessage(qty.toLocaleString("tr-TR") + " çift manuel düzeltme eklendi.");
    }

    setSavingProduction(false);
  }

  async function pause() {
    if (!item) return;
    await post("/station-assignments/pause", {
      stationAssignmentId: item.id,
      note,
    });
  }

  async function resume() {
    if (!item) return;
    await post("/station-assignments/resume", {
      stationAssignmentId: item.id,
      note,
    });
  }

  async function finish() {
    if (!item) return;

    const ok = confirm("Bu işi bitirmek istediğine emin misin?");
    if (!ok) return;

    await post("/station-assignments/finish", {
      stationAssignmentId: item.id,
      note,
    });

    onClose();
  }

  if (!open) return null;

  const progress =
    item && item.quantityPairs > 0
      ? Math.min(100, Math.round((item.orderItemProducedPairs / item.quantityPairs) * 100))
      : 0;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4 backdrop-blur-sm">
      <div className="max-h-[92vh] w-full max-w-4xl overflow-y-auto rounded-3xl border border-white/10 bg-[#0F1115] p-8 shadow-2xl">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <p className="text-sm font-bold text-emerald-400">FIXAR OS</p>
            <h2 className="mt-1 text-3xl font-black text-white">
              İstasyon {station} İş Yönetimi
            </h2>
          </div>

          <button onClick={onClose} className="rounded-xl bg-zinc-800 px-4 py-2 text-white">
            Kapat
          </button>
        </div>

        {message && (
          <div className="mb-5 rounded-2xl border border-emerald-400/30 bg-emerald-400/10 p-4 font-bold text-emerald-300">
            {message}
          </div>
        )}

        {loading && <p className="text-zinc-400">Yükleniyor...</p>}

        {!loading && !item && (
          <div className="rounded-2xl border border-white/10 bg-black/20 p-6 text-white">
            Bu istasyonda aktif iş bulunamadı.
          </div>
        )}

        {item && (
          <div className="grid gap-5">
            <div className="rounded-3xl border border-emerald-400/30 bg-emerald-400/10 p-5">
              <p className="text-xs font-bold text-emerald-300">AKTİF ÜRETİM</p>
              <h3 className="mt-2 text-2xl font-black text-white">
                {item.customerName} · {item.productName} · {item.moldName}
              </h3>
              <p className="mt-2 text-sm text-emerald-300">
                Operatör: {item.operatorName} · Durum: {item.status}
              </p>
            </div>

            <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
              <Card title="Toplam" value={item.quantityPairs.toLocaleString("tr-TR") + " çift"} />
              <Card title="Üretilen" value={item.orderItemProducedPairs.toLocaleString("tr-TR") + " çift"} />
              <Card title="Kalan" value={item.remainingPairs.toLocaleString("tr-TR") + " çift"} />
              <Card title="İlerleme" value={"%" + progress} />
            </div>

            <div className="h-5 overflow-hidden rounded-full bg-zinc-800">
              <div className="h-full rounded-full bg-emerald-500" style={{ width: progress + "%" }} />
            </div>

            <div className="rounded-2xl border border-emerald-400/20 bg-emerald-400/10 p-5">
              <h3 className="mb-4 text-lg font-black text-emerald-300">Tur Ekle</h3>

              <p className="mb-4 text-sm text-zinc-300">
                Ana kullanım burasıdır. Tur eklendiğinde aktif üretimdeki tüm istasyonlara otomatik dağıtılır.
              </p>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                <input
                  value={turnCount}
                  onChange={(e) => setTurnCount(e.target.value)}
                  disabled={savingTurn}
                  className="rounded-xl border border-white/10 bg-black/30 p-3 text-white disabled:opacity-50"
                  placeholder="Tur adedi"
                />

                <button
                  onClick={addTurn}
                  disabled={savingTurn}
                  className="rounded-xl bg-emerald-500 px-5 py-3 font-bold text-black hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {savingTurn ? "Tur Ekleniyor..." : "Tur Ekle"}
                </button>

                <button
                  onClick={() => setTurnCount("1")}
                  disabled={savingTurn}
                  className="rounded-xl bg-zinc-700 px-5 py-3 font-bold text-white disabled:opacity-50"
                >
                  1 Tur Hazırla
                </button>
              </div>
            </div>

            <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
              <h3 className="mb-4 text-lg font-black text-emerald-300">Manuel Düzeltme</h3>

              <p className="mb-4 text-sm text-zinc-400">
                Sadece özel durumlarda kullan. Normal üretim girişi için Tur Ekle kullanılmalı.
              </p>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                <input
                  value={addPairs}
                  onChange={(e) => setAddPairs(e.target.value)}
                  disabled={savingProduction}
                  className="rounded-xl border border-white/10 bg-black/30 p-3 text-white disabled:opacity-50"
                  placeholder="Düzeltme çift"
                />

                <button
                  onClick={addProduction}
                  disabled={savingProduction}
                  className="rounded-xl bg-yellow-500 px-5 py-3 font-bold text-black hover:bg-yellow-400 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {savingProduction ? "Ekleniyor..." : "Manuel Düzeltme Ekle"}
                </button>

                <button
                  onClick={() => setAddPairs("100")}
                  disabled={savingProduction}
                  className="rounded-xl bg-zinc-700 px-5 py-3 font-bold text-white disabled:opacity-50"
                >
                  100 Çift Hazırla
                </button>
              </div>
            </div>

            <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
              <h3 className="mb-4 text-lg font-black text-emerald-300">Üretim Bilgisi</h3>

              <div className="grid grid-cols-2 gap-4 md:grid-cols-3">
                <Info label="Üretim Tipi" value={item.productionType ?? "-"} />
                <Info label="Kumaş Rengi" value={item.fabricColor ?? "-"} />
                <Info label="Başlangıç" value={new Date(item.startedAt).toLocaleString("tr-TR")} />
                <Info label="Bu Atamada Üretilen" value={item.producedPairs.toLocaleString("tr-TR") + " çift"} />
                <Info label="Kalıp" value={item.moldName} />
                <Info label="Durum" value={item.status} />
              </div>
            </div>

            <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
              <h3 className="mb-4 text-lg font-black text-emerald-300">Not</h3>

              <input
                value={note}
                onChange={(e) => setNote(e.target.value)}
                className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
                placeholder="İsteğe bağlı not"
              />
            </div>

            <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
              <button onClick={pause} className="rounded-xl bg-yellow-500 px-5 py-3 font-bold text-black">
                Duraklat
              </button>

              <button onClick={resume} className="rounded-xl bg-blue-500 px-5 py-3 font-bold text-white">
                Devam Et
              </button>

              <button className="rounded-xl bg-zinc-700 px-5 py-3 font-bold text-white">
                Kasa Oluştur
              </button>

              <button onClick={finish} className="rounded-xl bg-red-500 px-5 py-3 font-bold text-white">
                İşi Bitir
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function Card({ title, value }: { title: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
      <p className="text-sm text-zinc-400">{title}</p>
      <p className="mt-2 text-2xl font-black text-white">{value}</p>
    </div>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-zinc-500">{label}</p>
      <p className="text-sm font-bold text-white">{value}</p>
    </div>
  );
}
