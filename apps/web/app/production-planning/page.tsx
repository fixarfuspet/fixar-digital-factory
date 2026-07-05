"use client";

import { useEffect, useState } from "react";
type Station = {
  station: number;
  status: string;
  customer: string;
  product: string;
  mold: string;
  operator: string;
  produced: number;
};

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

  const [stations, setStations] = useState<Station>([] as any);

  useEffect(() => {
    loadStations();
  }, []);

  async function loadStations() {
    try {
      const response = await fetch(
        "http://localhost:5000/api/stationassignments/active"
      );

      if (!response.ok) {
        setStations(emptyStations);
        return;
      }

      const data = await response.json();

      const list = [...emptyStations];

      data.forEach((x: any) => {
        const index = x.stationNumberSnapshot - 1;

        if (index >= 0 && index < list.length) {
          list[index] = {
            station: x.stationNumberSnapshot,
            status: x.status,
            customer: x.customerName,
            product: x.productName,
            mold: x.moldName,
            operator: x.operatorName ?? "-",
            produced: x.producedPairs,
          };
        }
      });

      setStations(list);
    } catch {
      setStations(emptyStations);
    }
  }
  



  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.18),transparent_35%),radial-gradient(circle_at_bottom_right,rgba(59,130,246,0.14),transparent_30%)] px-6 py-8">
        <div className="mx-auto max-w-7xl space-y-8">
          <header className="border-b border-white/10 pb-6">
            <p className="text-sm font-bold tracking-[0.4em] text-emerald-400">
              FIXAR OS
            </p>
            <h1 className="mt-2 text-4xl font-black">
              Üretim Planlama
            </h1>
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

          <section className="rounded-3xl border border-white/10 bg-white/[0.06] p-6 shadow-2xl">
            <div className="mb-5 flex items-center justify-between">
              <div>
                <h2 className="text-2xl font-black">
                  Enjeksiyon İstasyonları
                </h2>
                <p className="text-sm text-zinc-400">
                  Her istasyona sipariş kalemi, kalıp ve operatör atanacak.
                </p>
              </div>
              <span className="rounded-full bg-emerald-500/15 px-4 py-2 text-xs font-bold text-emerald-300">
                Canlı planlama ekranı
              </span>
            </div>

            <div className="grid grid-cols-1 gap-4 md:grid-cols-3 xl:grid-cols-4">
              {stations.map((item) => (
                <StationCard key={item.station} item={item} />
              ))}
            </div>
          </section>
        </div>
      </div>
    </main>
  );
}

function SummaryCard({
  title,
  value,
  note,
}: {
  title: string;
  value: string;
  note: string;
}) {
  return (
    <div className="rounded-3xl border border-white/10 bg-white/[0.06] p-6 shadow-2xl backdrop-blur">
      <p className="text-sm text-zinc-400">{title}</p>
      <h3 className="mt-3 text-4xl font-black text-white">{value}</h3>
      <p className="mt-3 text-xs text-emerald-300">{note}</p>
    </div>
  );
}

function StationCard({
  item,
}: {
  item: {
    station: number;
    status: string;
    customer: string;
    product: string;
    mold: string;
    operator: string;
    produced: number;
  };
}) {
  const isActive = item.status === "Üretimde";

  return (
    <div
      className={
        "rounded-2xl border p-5 shadow-lg " +
        (isActive
          ? "border-emerald-400/30 bg-emerald-400/15"
          : "border-white/10 bg-black/20")
      }
    >
      <div className="mb-5 flex items-center justify-between">
        <div>
          <p className="text-xs text-zinc-400">İstasyon</p>
          <h3 className="text-3xl font-black">{item.station}</h3>
        </div>

        <span
          className={
            "rounded-full px-3 py-1 text-xs font-bold " +
            (isActive
              ? "bg-emerald-500/20 text-emerald-300"
              : "bg-zinc-500/20 text-zinc-300")
          }
        >
          {item.status}
        </span>
      </div>

      <div className="space-y-2 text-sm">
        <Info label="Müşteri" value={item.customer} />
        <Info label="Ürün" value={item.product} />
        <Info label="Kalıp" value={item.mold} />
        <Info label="Operatör" value={item.operator} />
        <div className="flex justify-between gap-4 border-b border-white/10 pb-2">
  <span className="text-zinc-400">Üretilen</span>
  <span className="text-right font-bold text-white">
    {item.produced.toLocaleString("tr-TR")} çift
  </span>
</div>
      </div>

      <button className="mt-5 w-full rounded-xl bg-emerald-500 px-4 py-3 text-sm font-bold text-black hover:bg-emerald-400">
        {isActive ? "İşi Yönet" : "İş Ata"}
      </button>
    </div>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 border-b border-white/10 pb-2">
      <span className="text-zinc-400">{label}</span>
      <span className="text-right font-bold text-white">{value}</span>
    </div>
  );
}
