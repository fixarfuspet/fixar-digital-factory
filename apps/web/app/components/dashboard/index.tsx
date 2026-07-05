export { DashboardHeader } from "./DashboardHeader";

function Card({ title, value, note }: { title: string; value: string; note?: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/5 p-5 shadow-lg">
      <p className="text-sm text-white/60">{title}</p>
      <h3 className="mt-2 text-3xl font-bold text-white">{value}</h3>
      {note ? <p className="mt-2 text-xs text-emerald-300">{note}</p> : null}
    </div>
  );
}

export function KpiCardRow() {
  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
      <Card title="Günlük Üretim" value="2.880 çift" note="10900 Memory Foam" />
      <Card title="Aktif Sipariş" value="35.000 çift" note="Icemen + Dogo" />
      <Card title="Kritik Stok" value="3 uyarı" note="Poliol / Kumaş / Koli" />
      <Card title="Verimlilik" value="%87" note="Bugünkü hedefe göre" />
    </div>
  );
}

export function MachineStatusCard() {
  return <Card title="Makine Durumu" value="24 istasyon" note="22 aktif / 2 bakım bekliyor" />;
}

export function CriticalStockAlerts() {
  return <Card title="Kritik Stok Uyarıları" value="Poliol düşük" note="3 gün içinde sipariş önerilir" />;
}

export function ProductionChart() {
  return <Card title="Üretim Grafiği" value="Saatlik takip" note="Grafik modülü eklenecek" />;
}

export function RawMaterialStock() {
  return <Card title="Hammadde Stoku" value="10900: 420 kg" note="Yaklaşık 5.460 çift kapasite" />;
}

export function LatestActivities() {
  return <Card title="Son Aktiviteler" value="8 işlem" note="Son üretim: Icemen 10900" />;
}

export function AiAssistantCard() {
  return <Card title="AI Asistan" value="Hazır" note="CEO raporu oluşturabilir" />;
}

export function StationLineVisual() {
  return <Card title="İstasyon Hattı" value="1-24" note="Canlı istasyon görünümü eklenecek" />;
}