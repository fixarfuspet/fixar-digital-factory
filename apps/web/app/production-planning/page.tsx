"use client";

import { useState } from "react";
import AssignmentModal from "../components/production-planning/AssignmentModal";

const sampleAssignments = [
  {
    station: 1,
    status: "Üretimde",
    customer: "Icemen",
    product: "10900 Memory Foam",
    mold: "ICE 39-45",
    operator: "Mahmut",
    produced: 1240,
  },
  {
    station: 2,
    status: "Üretimde",
    customer: "Dogo",
    product: "Comfy Light",
    mold: "CL 40-41",
    operator: "Erdem",
    produced: 860,
  },
];

export default function ProductionPlanningPage() {
  const [selectedStation, setSelectedStation] = useState<number | null>(null);

  const stations = Array.from({ length: 24 }, (_, i) => {
    const stationNumber = i + 1;
    const assigned = sampleAssignments.find((x) => x.station === stationNumber);

    return {
      station: stationNumber,
      status: assigned?.status ?? "Boş",
      customer: assigned?.customer ?? "-",
      product: assigned?.product ?? "-",
      mold: assigned?.mold ?? "-",
      operator: assigned?.operator ?? "-",
      produced: assigned?.produced ?? 0,
    };
  });

  return (
    <main className="min-h-screen bg-[#05070A] p-8 text-white">
      <div className="mx-auto max-w-7xl space-y-8">
        <header className="border-b border-white/10 pb-6">
          <p className="text-sm font-bold tracking-[0.4em] text-emerald-400">
            FIXAR OS
          </p>
          <h1 className="mt-2 text-4xl font-black">Üretim Planlama</h1>
          <p className="mt-2 text-sm text-zinc-400">
            24 istasyon · kalıp atama · sipariş takibi · operatör yönetimi
          </p>
        </header>

        <section className="grid grid-cols-1 gap-4 md:grid-cols-4">
          <SummaryCard title="Toplam İstasyon" value="24" note="PU enjeksiyon hattı" />
          <SummaryCard title="Üretimde" value="2" note="Aktif çalışan istasyon" />
          <SummaryCard title="Boş" value="22" note="Atama bekliyor" />
          <SummaryCard title="Bugünkü Üretim" value="2.100" note="çift" />
        </section>

        <section className="rounded-3xl border border-white/10 bg-white/[0.06] p-6">
          <h2 className="mb-5 text-2xl font-black">Enjeksiyon İstasyonları</h2>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-3 xl:grid-cols-4">
            {stations.map((item) => (
              <div
                key={item.station}
                className={
                  item.status === "Üretimde"
                    ? "rounded-2xl border border-emerald-400/30 bg-emerald-400/15 p-5"
                    : "rounded-2xl border border-white/10 bg-black/20 p-5"
                }
              >
                <div className="mb-5 flex items-center justify-between">
                  <h3 className="text-3xl font-black">{item.station}</h3>
                  <span>{item.status}</span>
                </div>

                <Info label="Müşteri" value={item.customer} />
                <Info label="Ürün" value={item.product} />
                <Info label="Kalıp" value={item.mold} />
                <Info label="Operatör" value={item.operator} />
                <Info label="Üretilen" value={item.produced.toLocaleString("tr-TR") + " çift"} />

                <button
                  onClick={() => setSelectedStation(item.station)}
                  className="mt-5 w-full rounded-xl bg-emerald-500 px-4 py-3 font-bold text-black"
                >
                  {item.status === "Üretimde" ? "İşi Yönet" : "İş Ata"}
                </button>
              </div>
            ))}
          </div>
        </section>
      </div>

      <AssignmentModal
        open={selectedStation !== null}
        station={selectedStation}
        onClose={() => setSelectedStation(null)}
      />
    </main>
  );
}

function SummaryCard({ title, value, note }: { title: string; value: string; note: string }) {
  return (
    <div className="rounded-3xl border border-white/10 bg-white/[0.06] p-6">
      <p className="text-sm text-zinc-400">{title}</p>
      <h3 className="mt-3 text-4xl font-black">{value}</h3>
      <p className="mt-3 text-xs text-emerald-300">{note}</p>
    </div>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between border-b border-white/10 py-2 text-sm">
      <span className="text-zinc-400">{label}</span>
      <span className="font-bold text-white">{value}</span>
    </div>
  );
}