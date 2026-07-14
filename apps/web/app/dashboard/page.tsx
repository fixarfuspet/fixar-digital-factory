import { requireSession } from "../lib/auth/session";
import { LogoutButton } from "../components/auth/LogoutButton";
import Link from "next/link";
import { can } from "../lib/auth/permissions";

const injectionStations = Array.from({ length: 24 }, (_, i) => {
  const data = [
    ["Icemen", "10900", "39-45 Siyah", "Güner Ayakkabı", "08:10", "120 çift"],
    ["Dogo", "Comfy Light", "40-41 Beyaz", "Işıklı Pabuç", "08:25", "96 çift"],
    ["Icemen", "Model 2", "42-48 Koyu Gri", "Güner Ayakkabı", "09:00", "110 çift"],
    ["Korayspor", "Memory Foam", "36-40 Mavi", "Korayspor", "09:20", "84 çift"],
  ];

  const item = data[i % data.length];

  return {
    no: i + 1,
    customer: item[0],
    product: item[1],
    variant: item[2],
    factory: item[3],
    start: item[4],
    quantity: item[5],
    status: i === 6 || i === 17 ? "bakim" : i === 11 ? "bekleme" : "aktif",
  };
});

const presses = [
  {
    name: "Gezer Kafa Kesim Presi",
    operator: "Ramazan",
    today: "1.240 çift",
    customer: "Icemen",
    product: "10900 Kumaşlı",
    current: "39-45 Siyah kesiliyor",
    pending: "740 çift bekleyen kesim",
    dailyCuts: [
      { customer: "Icemen", product: "10900 Kumaşlı", quantity: "620 çift" },
      { customer: "Dogo", product: "Comfy Light Beyaz", quantity: "410 çift" },
      { customer: "Korayspor", product: "Memory Foam", quantity: "210 çift" },
    ],
  },
  {
    name: "Döner Kafa Kesim Presi",
    operator: "Erdem",
    today: "980 çift",
    customer: "Dogo",
    product: "Comfy Light",
    current: "40-41 Beyaz kesiliyor",
    pending: "520 çift bekleyen kesim",
    dailyCuts: [
      { customer: "Dogo", product: "Comfy Light", quantity: "430 çift" },
      { customer: "Icemen", product: "Model 2", quantity: "350 çift" },
      { customer: "Korayspor", product: "Memory Foam", quantity: "200 çift" },
    ],
  },
];

const hourlyProduction = [
  { hour: "08:00", injection: 220, cutting: 160 },
  { hour: "09:00", injection: 310, cutting: 230 },
  { hour: "10:00", injection: 360, cutting: 280 },
  { hour: "11:00", injection: 420, cutting: 310 },
  { hour: "12:00", injection: 280, cutting: 250 },
  { hour: "13:00", injection: 390, cutting: 330 },
  { hour: "14:00", injection: 460, cutting: 370 },
  { hour: "15:00", injection: 440, cutting: 350 },
];

const orders = [
  { customer: "Icemen", product: "10900 Kumaşlı", total: 50000, done: 18400, due: "12 gün" },
  { customer: "Dogo", product: "Comfy Light Beyaz", total: 20000, done: 8200, due: "7 gün" },
  { customer: "Korayspor", product: "Memory Foam", total: 10000, done: 3600, due: "5 gün" },
];

const rawMaterials = [
  { name: "Poliol", stock: "420 kg", status: "3 gün yeter", level: 42 },
  { name: "İzosiyanat", stock: "390 kg", status: "4 gün yeter", level: 55 },
  { name: "Kumaş", stock: "1.850 m", status: "yeterli", level: 72 },
  { name: "Koli", stock: "940 adet", status: "sipariş önerilir", level: 35 },
];

const liveLogs = [
  "15:20 - Dogo Comfy Light 40-41 Beyaz kesime alındı.",
  "14:55 - 12. istasyon beklemeye geçti.",
  "14:30 - Icemen 10900 39-45 Siyah üretimi devam ediyor.",
  "13:45 - Poliol stok seviyesi kritik sınıra yaklaşıyor.",
];

function KpiCard({ title, value, note }: { title: string; value: string; note: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.06] p-5 shadow-xl backdrop-blur">
      <p className="text-sm text-zinc-400">{title}</p>
      <h3 className="mt-2 text-3xl font-black text-white">{value}</h3>
      <p className="mt-2 text-xs text-emerald-300">{note}</p>
    </div>
  );
}

function ProgressBar({ value }: { value: number }) {
  return (
    <div className="mt-3 h-3 rounded-full bg-white/10">
      <div className="h-3 rounded-full bg-emerald-400" style={{ width: value + "%" }} />
    </div>
  );
}

export default async function DashboardPage() {
  const user = await requireSession();

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.20),transparent_35%),radial-gradient(circle_at_bottom_right,rgba(59,130,246,0.14),transparent_30%)] px-6 py-8">
        <div className="mx-auto max-w-7xl space-y-8">
          <header className="flex items-center justify-between border-b border-white/10 pb-6">
            <div>
              <p className="text-sm font-bold tracking-[0.4em] text-emerald-400">FIXAR OS</p>
              <h1 className="mt-2 text-4xl font-black">Canlı Fabrika Dashboard</h1>
              <p className="mt-2 text-sm text-zinc-400">
                Enjeksiyon · Kesim · Stok · Sipariş · Makine canlı takip ekranı
              </p>
            </div>

            <div className="flex items-center gap-4">
              <nav className="flex gap-2">
                <Link href="/customers" className="rounded-xl border border-white/10 bg-white/5 px-3 py-2 text-sm font-bold">Müşteriler</Link>
                <Link href="/orders" className="rounded-xl border border-white/10 bg-white/5 px-3 py-2 text-sm font-bold">Satış Siparişleri</Link>
                <Link href="/traceability" className="rounded-xl border border-violet-400/30 bg-violet-500/10 px-3 py-2 text-sm font-bold text-violet-200">QR İzlenebilirlik</Link>
                {can(user.roles, "costs") && <Link href="/work-order-costs" className="rounded-xl border border-amber-400/30 bg-amber-500/10 px-3 py-2 text-sm font-bold text-amber-200">İş Emri Maliyetleri</Link>}
                {can(user.roles, "costs") && <Link href="/cost-settings" className="rounded-xl border border-white/10 bg-white/5 px-3 py-2 text-sm font-bold">Maliyet Ayarları</Link>}
                {can(user.roles, "costs") && <Link href="/exchange-rates" className="rounded-xl border border-white/10 bg-white/5 px-3 py-2 text-sm font-bold">Döviz Kurları</Link>}
              </nav>
              <div className="rounded-2xl border border-white/10 bg-white/[0.06] px-4 py-3 text-right">
                <p className="text-sm font-semibold">{user.email}</p>
                <p className="text-xs text-zinc-400">Yetkili kullanıcı</p>
              </div>
              <LogoutButton />
            </div>
          </header>

          <section className="grid grid-cols-1 gap-4 md:grid-cols-4">
            <KpiCard title="Bugünkü Üretim" value="2.880 çift" note="10900 + Comfy Light" />
            <KpiCard title="Kesilen Ürün" value="2.220 çift" note="2 pres aktif" />
            <KpiCard title="Aktif Sipariş" value="35.000 çift" note="Icemen + Dogo" />
            <KpiCard title="Kritik Stok" value="3 uyarı" note="Poliol / Kumaş / Koli" />
          </section>

          <section className="rounded-3xl border border-white/10 bg-white/[0.06] p-5">
            <div className="mb-4 flex items-center justify-between">
              <div>
                <h2 className="text-2xl font-black">Poliüretan Enjeksiyon</h2>
                <p className="text-sm text-zinc-400">
                  24 istasyon canlı üretim durumu — müşteri, ürün, numara ve renk bilgisi
                </p>
              </div>
              <span className="rounded-full bg-emerald-500/15 px-4 py-2 text-xs font-bold text-emerald-300">
                22 aktif · 1 bekleme · 2 bakım
              </span>
            </div>

            <div className="grid grid-cols-2 gap-2 md:grid-cols-4 xl:grid-cols-8">
              {injectionStations.map((s) => (
                <div
                  key={s.no}
                  className={
                    "rounded-xl border p-3 shadow-lg " +
                    (s.status === "aktif"
                      ? "border-emerald-400/30 bg-emerald-400/15"
                      : s.status === "bakim"
                      ? "border-red-400/30 bg-red-400/15"
                      : "border-yellow-400/30 bg-yellow-400/15")
                  }
                >
                  <div className="flex items-center justify-between">
                    <span className="text-xl font-black">{s.no}</span>
                    <span className="rounded-full bg-black/30 px-2 py-1 text-[9px] font-bold">
                      {s.status === "aktif" ? "AKTİF" : s.status === "bakim" ? "BAKIM" : "BEKLEME"}
                    </span>
                  </div>
                  <p className="mt-2 text-sm font-bold">{s.customer}</p>
                  <p className="text-xs text-zinc-300">{s.product}</p>
                  <p className="text-[11px] text-zinc-400">{s.factory}</p>
                  <p className="mt-1 text-xs font-semibold text-emerald-200">{s.variant}</p>
                  <p className="mt-1 text-[11px] text-zinc-400">Başlangıç: {s.start}</p>
                  <p className="text-[11px] text-zinc-400">Üretim: {s.quantity}</p>
                </div>
              ))}
            </div>
          </section>

          <section className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            {presses.map((press) => (
              <div key={press.name} className="rounded-3xl border border-white/10 bg-white/[0.06] p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <h2 className="text-2xl font-black">{press.name}</h2>
                    <p className="text-sm text-zinc-400">Canlı kesim presi takibi</p>
                  </div>
                  <span className="rounded-full bg-emerald-500/15 px-4 py-2 text-xs font-bold text-emerald-300">
                    ÇALIŞIYOR
                  </span>
                </div>

                <div className="mt-6 grid grid-cols-2 gap-4">
                  <KpiCard title="Operatör" value={press.operator} note="Bugünkü vardiya" />
                  <KpiCard title="Bugünkü Kesim" value={press.today} note={press.pending} />
                </div>

                <div className="mt-5 grid grid-cols-1 gap-4 xl:grid-cols-2">
                  <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
                    <p className="text-sm text-zinc-400">Şu an kesilen ürün</p>
                    <h3 className="mt-2 text-2xl font-black">{press.customer}</h3>
                    <p className="mt-1 text-sm text-zinc-300">{press.product}</p>
                    <p className="mt-3 text-sm font-bold text-emerald-300">{press.current}</p>
                  </div>

                  <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
                    <p className="text-sm text-zinc-400">Günlük kesim özeti</p>
                    <div className="mt-3 space-y-3">
                      {press.dailyCuts.map((cut) => (
                        <div key={cut.customer + cut.product} className="flex justify-between border-b border-white/10 pb-2">
                          <div>
                            <p className="text-sm font-bold">{cut.customer}</p>
                            <p className="text-xs text-zinc-400">{cut.product}</p>
                          </div>
                          <p className="text-sm font-black text-emerald-300">{cut.quantity}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </section>

          <section className="grid grid-cols-1 gap-6 lg:grid-cols-3">
            <div className="rounded-3xl border border-white/10 bg-white/[0.06] p-6 lg:col-span-2">
              <h2 className="text-2xl font-black">Saatlik Üretim Grafiği</h2>
              <p className="text-sm text-zinc-400">Enjeksiyon ve kesim adetleri</p>
              <div className="mt-6 flex h-64 items-end gap-4">
                {hourlyProduction.map((item) => (
                  <div key={item.hour} className="flex flex-1 flex-col items-center gap-2">
                    <div className="flex h-52 w-full items-end gap-1">
                      <div className="w-1/2 rounded-t-xl bg-emerald-400" style={{ height: item.injection / 5 + "%" }} />
                      <div className="w-1/2 rounded-t-xl bg-cyan-400" style={{ height: item.cutting / 5 + "%" }} />
                    </div>
                    <p className="text-xs text-zinc-400">{item.hour}</p>
                  </div>
                ))}
              </div>
              <div className="mt-4 flex gap-4 text-xs text-zinc-400">
                <span className="text-emerald-300">■ Enjeksiyon</span>
                <span className="text-cyan-300">■ Kesim</span>
              </div>
            </div>

            <div className="rounded-3xl border border-white/10 bg-white/[0.06] p-6">
              <h2 className="text-2xl font-black">Canlı Olaylar</h2>
              <div className="mt-5 space-y-3">
                {liveLogs.map((log) => (
                  <div key={log} className="rounded-2xl border border-white/10 bg-black/20 p-4 text-sm text-zinc-300">
                    {log}
                  </div>
                ))}
              </div>
            </div>
          </section>

          <section className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <div className="rounded-3xl border border-white/10 bg-white/[0.06] p-6">
              <h2 className="text-2xl font-black">Sipariş İlerleme</h2>
              <div className="mt-5 space-y-5">
                {orders.map((order) => {
                  const percent = Math.round((order.done / order.total) * 100);
                  return (
                    <div key={order.customer + order.product} className="rounded-2xl border border-white/10 bg-black/20 p-5">
                      <div className="flex justify-between">
                        <div>
                          <p className="text-lg font-black">{order.customer}</p>
                          <p className="text-sm text-zinc-400">{order.product}</p>
                        </div>
                        <p className="text-lg font-black text-emerald-300">%{percent}</p>
                      </div>
                      <ProgressBar value={percent} />
                      <p className="mt-3 text-xs text-zinc-400">
                        {order.done.toLocaleString("tr-TR")} / {order.total.toLocaleString("tr-TR")} çift · Termin: {order.due}
                      </p>
                    </div>
                  );
                })}
              </div>
            </div>

            <div className="rounded-3xl border border-white/10 bg-white/[0.06] p-6">
              <h2 className="text-2xl font-black">Hammadde Stokları</h2>
              <div className="mt-5 space-y-5">
                {rawMaterials.map((material) => (
                  <div key={material.name} className="rounded-2xl border border-white/10 bg-black/20 p-5">
                    <div className="flex justify-between">
                      <div>
                        <p className="text-lg font-black">{material.name}</p>
                        <p className="text-sm text-zinc-400">{material.stock}</p>
                      </div>
                      <p className="text-sm font-bold text-emerald-300">{material.status}</p>
                    </div>
                    <ProgressBar value={material.level} />
                  </div>
                ))}
              </div>
            </div>
          </section>
        </div>
      </div>
    </main>
  );
}
