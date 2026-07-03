import {
  AiAssistantCard,
  CriticalStockAlerts,
  DashboardHeader,
  KpiCardRow,
  LatestActivities,
  MachineStatusCard,
  ProductionChart,
  RawMaterialStock,
  StationLineVisual,
} from "../components/dashboard";
import { overviewCards, topKpiCards } from "../lib/dashboard-data";
import { requireSession } from "../lib/auth/session";

export default async function DashboardPage() {
  // Authoritative check: verifies the JWT against the real backend
  // (`/api/v1/auth/me`). Redirects to /login if there is no valid session.
  const user = await requireSession();

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <section className="bg-[radial-gradient(ellipse_at_top,_rgba(16,185,129,0.08),_transparent_55%)] px-4 py-12 sm:px-8 lg:px-12">
        <div className="mx-auto flex max-w-7xl flex-col gap-8">
          <DashboardHeader user={user} />

          <div>
            <p className="mb-3 text-xs font-semibold uppercase tracking-widest text-zinc-500">
              Top KPI Cards
            </p>
            <KpiCardRow cards={overviewCards} />
          </div>

          <div>
            <p className="mb-3 text-xs font-semibold uppercase tracking-widest text-zinc-500">
              Performance KPIs
            </p>
            <KpiCardRow cards={topKpiCards} />
          </div>

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
            <div className="lg:col-span-2">
              <StationLineVisual />
            </div>
            <MachineStatusCard />
          </div>

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
            <div className="lg:col-span-2">
              <ProductionChart />
            </div>
            <AiAssistantCard />
          </div>

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
            <RawMaterialStock />
            <CriticalStockAlerts />
            <LatestActivities />
          </div>

          <p className="pb-4 text-center text-xs text-zinc-600">
            FIXAR OS · Executive Dashboard · Authenticated session, widget data is still a static demo
          </p>
        </div>
      </section>
    </main>
  );
}
