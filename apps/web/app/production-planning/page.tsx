"use client";

import { useEffect, useState } from "react";
import AssignmentModal from "../components/production-planning/AssignmentModal";
import ManageAssignmentModal from "../components/production-planning/ManageAssignmentModal";
import { safeResponseJson } from "../lib/api/client";

type Station = {
  station: number;
  status: string;
  customer: string;
  product: string;
  mold: string;
  operator: string;
  produced: number;
};

type LastTurn = {
  time: string;
  turnCount: number;
  activeStationCount: number;
  totalAddedPairs: number;
};

const API = "/api/backend/api/v1";

const emptyStations: Station[] = Array.from({ length: 24 }, (_, i) => ({
  station: i + 1,
  status: "Boş",
  customer: "-",
  product: "-",
  mold: "-",
  operator: "-",
  produced: 0,
}));

export default function ProductionPlanningPage() {
  const [stations, setStations] = useState<Station[]>(emptyStations);
  const [selectedStation, setSelectedStation] = useState<number | null>(null);
  const [manageStation, setManageStation] = useState<number | null>(null);

  const [savingTurn, setSavingTurn] = useState(false);
  const [turnConfirmOpen, setTurnConfirmOpen] = useState(false);
  const [turnSuccessOpen, setTurnSuccessOpen] = useState(false);
  const [panelFlash, setPanelFlash] = useState(false);
  const [cooldown, setCooldown] = useState(0);
  const [lastTurn, setLastTurn] = useState<LastTurn | null>(null);
  const [recentTurns, setRecentTurns] = useState<LastTurn[]>([]);

  useEffect(() => {
    loadStations();
  }, []);

  useEffect(() => {
    if (cooldown <= 0) return;

    const timer = setInterval(() => {
      setCooldown((x) => Math.max(0, x - 1));
    }, 1000);

    return () => clearInterval(timer);
  }, [cooldown]);

  async function loadStations() {
    try {
      const response = await fetch(API + "/station-assignments/active");
      const result = await safeResponseJson<any[]>(response);

      const list = [...emptyStations];

      (result.data ?? []).forEach((x: any) => {
        const stationNumber = x.stationNumberSnapshot;
        const index = stationNumber - 1;

        if (index >= 0 && index < list.length) {
          list[index] = {
            station: stationNumber,
            status: x.status ?? "Üretimde",
            customer: x.customerName ?? "-",
            product: x.productName ?? "-",
            mold: x.moldName ?? "-",
            operator: x.operatorName ?? "-",
            produced: x.producedPairs ?? 0,
          };
        }
      });

      setStations(list);
    } catch {
      setStations(emptyStations);
    }
  }

  const runningCount = stations.filter((x) => x.status === "Üretimde").length;
  const activeCount = stations.filter(
    (x) => x.status === "Üretimde" || x.status === "Duraklatıldı"
  ).length;
  const emptyCount = 24 - activeCount;
  const totalProduced = stations.reduce((sum, x) => sum + x.produced, 0);

  function handleTurnClick() {
    if (runningCount === 0) {
      alert("Aktif üretimde istasyon yok.");
      return;
    }

    if (cooldown > 0) {
      alert("Çift kayıt olmaması için " + cooldown + " saniye bekle.");
      return;
    }

    setTurnConfirmOpen(true);
  }

  async function addOneTurn() {
    if (savingTurn || cooldown > 0) return;

    setSavingTurn(true);

    const response = await fetch(API + "/station-assignments/add-turn", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        turnCount: 1,
        note: "Operatör panelinden 1 tur eklendi",
      }),
    });

    if (!response.ok) {
      const text = await response.text();
      setSavingTurn(false);
      alert("Tur eklenemedi: " + text);
      return;
    }

    const result = await safeResponseJson<LastTurn>(response);

    if (!result.data) {
      setSavingTurn(false);
      alert("Tur sonucu alınamadı.");
      return;
    }

    const record: LastTurn = {
      time: new Date().toLocaleTimeString("tr-TR"),
      turnCount: result.data.turnCount,
      activeStationCount: result.data.activeStationCount,
      totalAddedPairs: result.data.totalAddedPairs,
    };

    setLastTurn(record);
    setRecentTurns((old) => [record, ...old].slice(0, 5));
    setTurnConfirmOpen(false);
    setTurnSuccessOpen(true);
    setPanelFlash(true);
    setCooldown(40);

    await loadStations();

    setTimeout(() => setPanelFlash(false), 900);
    setTimeout(() => setTurnSuccessOpen(false), 1600);
    setSavingTurn(false);
  }

  function handleStationClick(item: Station) {
    if (item.status === "Üretimde" || item.status === "Duraklatıldı") {
      setManageStation(item.station);
    } else {
      setSelectedStation(item.station);
    }
  }

  const turnButtonDisabled = savingTurn || cooldown > 0;

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen px-6 py-8">
        <div className="mx-auto max-w-7xl space-y-8">
          <header className="border-b border-white/10 pb-6">
            <p className="text-sm font-bold tracking-[0.4em] text-emerald-400">
              FIXAR OS
            </p>
            <h1 className="mt-2 text-4xl font-black">Üretim Planlama</h1>
            <p className="mt-2 text-sm text-zinc-400">
              PU enjeksiyon hattı · tur girişi · istasyon yönetimi
            </p>
          </header>

          <section
            className={
              "rounded-3xl border p-6 shadow-2xl transition-all duration-500 " +
              (panelFlash
                ? "border-emerald-300 bg-emerald-400/25 shadow-emerald-500/40"
                : "border-emerald-400/30 bg-emerald-400/10")
            }
          >
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
              <div className="lg:col-span-2">
                <p className="text-sm font-bold text-emerald-300">
                  PU ENJEKSİYON OPERATÖR PANELİ
                </p>
                <h2 className="mt-1 text-3xl font-black">Tur Yönetimi</h2>
                <p className="mt-2 text-sm text-zinc-300">
                  Tur ekle dediğinde aktif üretimdeki tüm istasyonlara otomatik dağıtılır.
                </p>

                <div className="mt-6 grid grid-cols-1 gap-4 md:grid-cols-3">
                  <div className="rounded-2xl bg-black/30 p-4">
                    <p className="text-xs text-zinc-400">Aktif İstasyon</p>
                    <p className="mt-1 text-3xl font-black text-emerald-300">
                      {runningCount}
                    </p>
                  </div>

                  <div className="rounded-2xl bg-black/30 p-4">
                    <p className="text-xs text-zinc-400">Bu Tur Eklenecek</p>
                    <p className="mt-1 text-3xl font-black text-white">
                      {runningCount} çift
                    </p>
                  </div>

                  <button
                    onClick={handleTurnClick}
                    disabled={turnButtonDisabled}
                    className={
                      "rounded-2xl px-6 py-6 text-2xl font-black text-black transition-all " +
                      (turnButtonDisabled
                        ? "cursor-not-allowed bg-blue-400 opacity-80"
                        : "bg-emerald-500 hover:scale-[1.02] hover:bg-emerald-400")
                    }
                  >
                    {savingTurn
                      ? "TUR EKLENİYOR..."
                      : cooldown > 0
                      ? "BEKLE " + cooldown + " SN"
                      : "TUR EKLE"}
                  </button>
                </div>

                {cooldown > 0 && (
                  <div className="mt-4 rounded-2xl border border-blue-400/30 bg-blue-400/10 p-4 text-sm font-bold text-blue-200">
                    Çift kayıt olmaması için Tur Ekle butonu {cooldown} saniye kilitli.
                  </div>
                )}
              </div>

              <div
                className={
                  "rounded-2xl border p-5 transition-all duration-500 " +
                  (panelFlash
                    ? "border-emerald-300 bg-emerald-400/20"
                    : "border-white/10 bg-black/25")
                }
              >
                <p className="text-sm font-bold text-emerald-300">✓ Son Tur</p>

                {lastTurn ? (
                  <div className="mt-3">
                    <p className="text-4xl font-black text-white">{lastTurn.time}</p>
                    <p className="mt-3 text-sm text-zinc-300">
                      {lastTurn.turnCount} tur · {lastTurn.activeStationCount} istasyon
                    </p>
                    <p className="mt-2 text-2xl font-black text-emerald-300">
                      +{lastTurn.totalAddedPairs} çift
                    </p>
                    <p className="mt-2 text-sm font-bold text-emerald-300">
                      Az önce işlendi
                    </p>
                  </div>
                ) : (
                  <p className="mt-3 text-sm text-zinc-400">Henüz tur eklenmedi.</p>
                )}

                {recentTurns.length > 0 && (
                  <div className="mt-5 space-y-2">
                    <p className="text-xs font-bold text-zinc-400">Son 5 Tur</p>
                    {recentTurns.map((x, i) => (
                      <div
                        key={i}
                        className="flex justify-between rounded-xl bg-black/30 px-3 py-2 text-xs"
                      >
                        <span>{x.time}</span>
                        <span>+{x.totalAddedPairs} çift</span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </section>

          <section className="grid grid-cols-1 gap-4 md:grid-cols-4">
            <SummaryCard title="Toplam İstasyon" value="24" note="PU enjeksiyon hattı" />
            <SummaryCard title="Üretimde" value={String(activeCount)} note="Aktif çalışan istasyon" />
            <SummaryCard title="Boş" value={String(emptyCount)} note="Atama bekliyor" />
            <SummaryCard title="Bugünkü Üretim" value={totalProduced.toLocaleString("tr-TR")} note="çift" />
          </section>

          <section className="rounded-3xl border border-white/10 bg-white/[0.06] p-6 shadow-2xl">
            <h2 className="mb-5 text-2xl font-black">Enjeksiyon İstasyonları</h2>

            <div className="grid grid-cols-1 gap-4 md:grid-cols-3 xl:grid-cols-4">
              {stations.map((item) => (
                <StationCard
                  key={item.station}
                  item={item}
                  onClick={() => handleStationClick(item)}
                />
              ))}
            </div>
          </section>
        </div>
      </div>

      {turnConfirmOpen && (
        <div className="fixed inset-0 z-[80] flex items-center justify-center bg-black/70 p-4 backdrop-blur-sm">
          <div className="w-full max-w-lg rounded-3xl border border-emerald-400/30 bg-[#0F1115] p-7 shadow-2xl">
            <p className="text-sm font-bold text-emerald-300">TUR EKLE ONAYI</p>
            <h2 className="mt-2 text-3xl font-black">1 Tur Eklenecek</h2>

            <div className="mt-6 grid grid-cols-2 gap-4">
              <div className="rounded-2xl bg-black/30 p-4">
                <p className="text-xs text-zinc-400">Aktif İstasyon</p>
                <p className="mt-1 text-2xl font-black">{runningCount}</p>
              </div>

              <div className="rounded-2xl bg-black/30 p-4">
                <p className="text-xs text-zinc-400">Toplam Üretim</p>
                <p className="mt-1 text-2xl font-black text-emerald-300">
                  +{runningCount} çift
                </p>
              </div>
            </div>

            <p className="mt-5 text-sm text-zinc-400">
              Bu işlem aktif üretimdeki tüm istasyonlara 1 çift üretim ekler.
              İşlemden sonra buton 40 saniye kilitlenir.
            </p>

            <div className="mt-7 flex justify-end gap-3">
              <button
                onClick={() => setTurnConfirmOpen(false)}
                disabled={savingTurn}
                className="rounded-xl bg-zinc-700 px-5 py-3 font-bold text-white disabled:opacity-50"
              >
                İptal
              </button>

              <button
                onClick={addOneTurn}
                disabled={savingTurn}
                className="rounded-xl bg-emerald-500 px-5 py-3 font-black text-black hover:bg-emerald-400 disabled:opacity-50"
              >
                {savingTurn ? "Tur Ekleniyor..." : "TURU BAŞLAT"}
              </button>
            </div>
          </div>
        </div>
      )}

      {turnSuccessOpen && lastTurn && (
        <div className="fixed inset-0 z-[90] flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm">
          <div className="w-full max-w-md rounded-3xl border border-emerald-400/30 bg-[#0F1115] p-8 text-center shadow-2xl">
            <div className="mx-auto flex h-24 w-24 animate-pulse items-center justify-center rounded-full bg-emerald-500 text-5xl font-black text-black">
              ✓
            </div>
            <h2 className="mt-5 text-4xl font-black">TUR EKLENDİ</h2>
            <p className="mt-3 text-xl font-bold text-emerald-300">
              +{lastTurn.totalAddedPairs} çift işlendi
            </p>
          </div>
        </div>
      )}

      <AssignmentModal
        open={selectedStation !== null}
        station={selectedStation}
        onClose={() => {
          setSelectedStation(null);
          loadStations();
        }}
      />

      <ManageAssignmentModal
        open={manageStation !== null}
        station={manageStation}
        onClose={() => {
          setManageStation(null);
          loadStations();
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

function StationCard({ item, onClick }: { item: Station; onClick: () => void }) {
  const isActive = item.status === "Üretimde";
  const isPaused = item.status === "Duraklatıldı";

  return (
    <div
      className={
        "rounded-2xl border p-5 shadow-lg transition-all " +
        (isActive
          ? "border-emerald-400/30 bg-emerald-400/15"
          : isPaused
          ? "border-yellow-400/30 bg-yellow-400/15"
          : "border-white/10 bg-black/20")
      }
    >
      <div className="mb-5 flex items-center justify-between">
        <h3 className="text-3xl font-black">{item.station}</h3>
        <span className="rounded-full bg-zinc-500/20 px-3 py-1 text-xs font-bold">
          {item.status}
        </span>
      </div>

      <Info label="Müşteri" value={item.customer} />
      <Info label="Ürün" value={item.product} />
      <Info label="Kalıp" value={item.mold} />
      <Info label="Operatör" value={item.operator} />
      <Info label="Üretilen" value={item.produced.toLocaleString("tr-TR") + " çift"} />

      <button
        onClick={onClick}
        className="mt-5 w-full rounded-xl bg-emerald-500 px-4 py-3 text-sm font-bold text-black hover:bg-emerald-400"
      >
        {isActive || isPaused ? "İşi Yönet" : "İş Ata"}
      </button>
    </div>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 border-b border-white/10 py-2 text-sm">
      <span className="text-zinc-400">{label}</span>
      <span className="text-right font-bold text-white">{value}</span>
    </div>
  );
}
