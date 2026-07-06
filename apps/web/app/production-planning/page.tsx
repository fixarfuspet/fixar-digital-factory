"use client";

import { useEffect, useState } from "react";
import AssignmentModal from "../components/production-planning/AssignmentModal";

type Station = {
  station: number;
  status: string;
  customer: string;
  product: string;
  mold: string;
  operator: string;
  produced: number;
};

const API = "http://localhost:5000/api/v1";

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

  useEffect(() => {
    loadStations();
  }, []);

  async function loadStations() {
    const response = await fetch(API + "/station-assignments/active");
    const result = await response.json();

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
  }

  const activeCount = stations.filter((x) => x.status === "Üretimde").length;
  const emptyCount = 24 - activeCount;
  const totalProduced = stations.reduce((sum, x) => sum + x.produced, 0);

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen px-6 py-8">
        <div className="mx-auto max-w-7xl space-y-8">
          <header className="border-b border-white/10 pb-6">
            <p className="text-sm font-bold tracking-[0.4em] text-emerald-400">FIXAR OS</p>
            <h1 className="mt-2 text-4xl font-black">Üretim Planlama</h1>
            <p className="mt-2 text-sm text-zinc-400">
              24 istasyon · kalıp atama · sipariş takibi · operatör yönetimi
            </p>
          </header>

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
                <StationCard key={item.station} item={item} onClick={() => setSelectedStation(item.station)} />
              ))}
            </div>
          </section>
        </div>
      </div>

      <AssignmentModal
        open={selectedStation !== null}
        station={selectedStation}
        onClose={() => {
          setSelectedStation(null);
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

  return (
    <div className={"rounded-2xl border p-5 shadow-lg " + (isActive ? "border-emerald-400/30 bg-emerald-400/15" : "border-white/10 bg-black/20")}>
      <div className="mb-5 flex items-center justify-between">
        <h3 className="text-3xl font-black">{item.station}</h3>
        <span className="rounded-full bg-zinc-500/20 px-3 py-1 text-xs font-bold">{item.status}</span>
      </div>

      <Info label="Müşteri" value={item.customer} />
      <Info label="Ürün" value={item.product} />
      <Info label="Kalıp" value={item.mold} />
      <Info label="Operatör" value={item.operator} />
      <Info label="Üretilen" value={item.produced.toLocaleString("tr-TR") + " çift"} />

      <button onClick={onClick} className="mt-5 w-full rounded-xl bg-emerald-500 px-4 py-3 text-sm font-bold text-black">
        {isActive ? "İşi Yönet" : "İş Ata"}
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