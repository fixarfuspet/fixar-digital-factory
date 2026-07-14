"use client";

import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import ManageAssignmentModal from "../components/production-planning/ManageAssignmentModal";

type ApiResponse<T> = {
  data?: T;
  message?: string;
  errorCode?: string;
  success?: boolean;
};

type LiveStation = {
  stationNumber: number;
  assignmentId?: string | null;
  status: string;
  customerName?: string | null;
  productName?: string | null;
  productCode?: string | null;
  moldName?: string | null;
  moldCode?: string | null;
  operatorName?: string | null;
  producedPairs: number;
  goodPairs: number;
  firePairs: number;
  totalTurns: number;
  openDowntime: boolean;
  downtimeType?: string | null;
  downtimeStartedAt?: string | null;
  turnsSinceLastRelease: number;
  releaseFrequencyTurns?: number | null;
  releaseDue: boolean;
  lastReleaseAt?: string | null;
  lastTurnAt?: string | null;
  orderPlannedPairs: number;
  orderProducedPairs: number;
  orderRemainingPairs: number;
  startedAt?: string | null;
  pausedAt?: string | null;
  finishedAt?: string | null;
};

type LiveSummary = {
  totalStationCount: number;
  activeStationCount: number;
  emptyStationCount: number;
  pausedStationCount: number;
  openDowntimeCount: number;
  openFaultCount: number;
  activeJobsProducedPairs: number;
  dailyProducedPairs?: number;
  goodPairs: number;
  firePairs: number;
  firePercent: number;
  releaseDueStationCount: number;
  lastTurnAt?: string | null;
  lastTurnAddedPairs?: number | null;
  todayProducedPairs?: number | null;
  todayFirePairs?: number | null;
  todayTurnCount?: number | null;
  stations: LiveStation[];
};

type StationEvent = {
  id: string;
  eventType: string;
  eventTime: string;
  quantity?: number | null;
  reason?: string | null;
  note?: string | null;
  recordedBy?: string | null;
  stationNumber: number;
  operatorName?: string | null;
};

type Downtime = {
  id: string;
  downtimeType: string;
  reason?: string | null;
  note?: string | null;
  startedAt: string;
  endedAt?: string | null;
  durationMinutes?: number | null;
  isOpen: boolean;
};

const API =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ??
  "/api/backend/api/v1";

const FIRE_REASONS = [
  "Eksik Döküm",
  "Hava Kabarcığı",
  "Yırtık",
  "Kumaş Kayması",
  "Gramaj Hatası",
  "Yoğunluk Hatası",
  "Pişme Hatası",
  "Renk Hatası",
  "Kalıp Kaynaklı",
  "Operatör Kaynaklı",
  "Hammadde Kaynaklı",
  "Diğer",
];

const DOWNTIME_TYPES = [
  "Makine Arızası",
  "Kalıp Arızası",
  "Hammadde Bekleme",
  "Kumaş Bekleme",
  "Kalıp Değişimi",
  "Temizlik",
  "Bakım",
  "Elektrik Kesintisi",
  "Kompresör Arızası",
  "Operatör Molası",
  "Planlı Duruş",
  "Diğer",
];

const emptyStations: LiveStation[] = Array.from({ length: 24 }, (_, index) => ({
  stationNumber: index + 1,
  assignmentId: null,
  status: "Boş",
  producedPairs: 0,
  goodPairs: 0,
  firePairs: 0,
  totalTurns: 0,
  openDowntime: false,
  turnsSinceLastRelease: 0,
  releaseFrequencyTurns: null,
  releaseDue: false,
  orderPlannedPairs: 0,
  orderProducedPairs: 0,
  orderRemainingPairs: 0,
}));

export default function LiveProductionPage() {
  const router = useRouter();
  const [summary, setSummary] = useState<LiveSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [savingTurn, setSavingTurn] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [manageStation, setManageStation] = useState<number | null>(null);
  const [panelStation, setPanelStation] = useState<LiveStation | null>(null);

  const loadSummary = useCallback(async () => {
    setError(null);
    try {
      const response = await fetch(`${API}/station-assignments/live-summary`, { cache: "no-store" });
      const result = (await response.json()) as ApiResponse<LiveSummary>;

      if (!response.ok) {
        throw new Error(result.message ?? "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.");
      }

      setSummary(result.data ?? null);
    } catch (requestError) {
      console.error("Canlı üretim özeti alınamadı.", requestError);
      setError("İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.");
      setSummary(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadSummary();
  }, [loadSummary]);

  const stations = useMemo(() => normalizeStations(summary?.stations), [summary]);
  const activeStations = stations.filter((station) => station.status === "Üretimde");
  const turnPairs = activeStations.length;
  const canAddTurn = activeStations.length > 0 && !savingTurn;

  async function addTurn() {
    if (!canAddTurn) return;

    setSavingTurn(true);
    setError(null);
    setSuccess(null);

    try {
      const response = await fetch(`${API}/station-assignments/add-turn`, {
        method: "POST",
        headers: { "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() },
        body: JSON.stringify({
          turnCount: 1,
          note: "Canlı üretim ekranından 1 tur eklendi",
          requestId: crypto.randomUUID(),
        }),
      });
      const result = (await response.json()) as ApiResponse<{
        activeStationCount: number;
        skippedStationCount: number;
        totalAddedPairs: number;
        releaseDueStations: Array<{ stationNumber: number }>;
      }>;

      if (!response.ok) {
        throw new Error(result.message ?? "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.");
      }

      setSuccess(
        `Tur eklendi. ${result.data?.totalAddedPairs ?? 0} çift işlendi, ${
          result.data?.skippedStationCount ?? 0
        } istasyon atlandı.`
      );
      setConfirmOpen(false);
      await loadSummary();
    } catch (requestError) {
      console.error("Tur eklenemedi.", requestError);
      setError(requestError instanceof Error ? requestError.message : "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.");
    } finally {
      setSavingTurn(false);
    }
  }

  const selectedStation = panelStation
    ? stations.find((station) => station.stationNumber === panelStation.stationNumber) ?? panelStation
    : null;

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-sm font-bold tracking-[0.36em] text-emerald-400">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">Canlı Üretim</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                Üretim Planlama kayıtlarından anlık izleme, fire, duruş ve kalıp ayırıcı yönetimi.
              </p>
            </div>

            <button
              onClick={() => router.push("/production-planning")}
              className="rounded-xl border border-white/10 bg-white/[0.07] px-5 py-3 text-sm font-bold text-white transition hover:border-emerald-400/40 hover:bg-emerald-400/10"
            >
              Üretim Planlama
            </button>
          </header>

          <LiveProductionErrorBanner error={error} success={success} />

          <LiveProductionSummary loading={loading} summary={summary} stations={stations} />

          <TurnManagementPanel
            activeStationCount={activeStations.length}
            turnPairs={turnPairs}
            disabled={!canAddTurn}
            saving={savingTurn}
            onAddTurn={() => setConfirmOpen(true)}
          />

          <section className="rounded-3xl border border-white/10 bg-white/[0.055] p-4 shadow-2xl sm:p-6">
            <div className="mb-5 flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
              <div>
                <h2 className="text-2xl font-black">24 Enjeksiyon İstasyonu</h2>
                <p className="mt-1 text-sm text-zinc-400">Ana durum kaynağı: StationAssignment.Status</p>
              </div>
              <button
                onClick={loadSummary}
                className="rounded-xl border border-white/10 bg-black/30 px-4 py-2 text-sm font-bold text-white transition hover:border-emerald-400/40"
              >
                Yenile
              </button>
            </div>

            {loading ? (
              <div className="rounded-2xl border border-white/10 bg-black/20 p-10 text-center text-zinc-400">
                Canlı üretim verileri yükleniyor...
              </div>
            ) : (
              <StationGrid
                stations={stations}
                onAssign={() => router.push("/production-planning")}
                onOpenPanel={setPanelStation}
                onQuality={(station) => router.push(`/quality-control?stationAssignmentId=${station.assignmentId}`)}
                onCutting={(station) => router.push(`/cutting?stationAssignmentId=${station.assignmentId}`)}
              />
            )}
          </section>
        </div>
      </div>

      {confirmOpen && (
        <ConfirmTurnModal
          activeStationCount={activeStations.length}
          turnPairs={turnPairs}
          saving={savingTurn}
          onClose={() => setConfirmOpen(false)}
          onConfirm={addTurn}
        />
      )}

      {selectedStation && (
        <StationOperationPanel
          station={selectedStation}
          onClose={() => setPanelStation(null)}
          onRefresh={loadSummary}
          onManage={() => setManageStation(selectedStation.stationNumber)}
          onMessage={(message) => setSuccess(message)}
          onError={(message) => setError(message)}
          onPlanning={() => router.push("/production-planning")}
        />
      )}

      <ManageAssignmentModal
        open={manageStation !== null}
        station={manageStation}
        onClose={() => {
          setManageStation(null);
          loadSummary();
        }}
      />
    </main>
  );
}

function normalizeStations(stations?: LiveStation[] | null) {
  const byNumber = new Map((stations ?? []).map((station) => [station.stationNumber, station]));
  return emptyStations.map((station) => byNumber.get(station.stationNumber) ?? station);
}

function LiveProductionErrorBanner({ error, success }: { error: string | null; success: string | null }) {
  if (!error && !success) return null;

  return (
    <div
      className={
        "rounded-2xl border px-5 py-4 text-sm font-bold " +
        (error
          ? "border-red-400/30 bg-red-500/10 text-red-200"
          : "border-emerald-400/30 bg-emerald-500/10 text-emerald-200")
      }
    >
      {error ?? success}
    </div>
  );
}

function LiveProductionSummary({
  loading,
  summary,
  stations,
}: {
  loading: boolean;
  summary: LiveSummary | null;
  stations: LiveStation[];
}) {
  const cards = [
    { title: "Üretimde", value: summary?.activeStationCount ?? 0, note: "Tur alacak istasyon" },
    { title: "Boş", value: summary?.emptyStationCount ?? 0, note: "İş atama bekliyor" },
    { title: "Duraklatılmış", value: summary?.pausedStationCount ?? 0, note: "Tur eklenmez" },
    { title: "Açık Duruş", value: summary?.openDowntimeCount ?? 0, note: "Müdahale bekliyor" },
    { title: "Açık İşlerin Üretimi", value: summary?.activeJobsProducedPairs ?? 0, note: "Toplam çift" },
    { title: "Sağlam Üretim", value: summary?.goodPairs ?? 0, note: "Üretim - fire" },
    { title: "Fire", value: summary?.firePairs ?? 0, note: "% " + formatValue(summary?.firePercent ?? 0) },
    { title: "Kalıp Ayırıcı Gereken", value: summary?.releaseDueStationCount ?? 0, note: "Uyarıdaki istasyon" },
    {
      title: "Son Tur",
      value: summary?.lastTurnAddedPairs ? `+${summary.lastTurnAddedPairs}` : "Yok",
      note: summary?.lastTurnAt ? formatTime(summary.lastTurnAt) : "Henüz kayıt yok",
    },
    { title: "Toplam İstasyon", value: stations.length, note: "PU enjeksiyon hattı" },
  ];

  return (
    <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
      {cards.map((card) => (
        <div key={card.title} className="rounded-2xl border border-white/10 bg-white/[0.06] p-5 shadow-xl">
          <p className="text-sm text-zinc-400">{card.title}</p>
          <h3 className="mt-3 text-3xl font-black text-white">{loading ? "..." : formatValue(card.value)}</h3>
          <p className="mt-2 text-xs font-bold text-emerald-300">{card.note}</p>
        </div>
      ))}
    </section>
  );
}

function TurnManagementPanel({
  activeStationCount,
  turnPairs,
  disabled,
  saving,
  onAddTurn,
}: {
  activeStationCount: number;
  turnPairs: number;
  disabled: boolean;
  saving: boolean;
  onAddTurn: () => void;
}) {
  return (
    <section className="rounded-3xl border border-emerald-400/25 bg-emerald-400/10 p-5 shadow-2xl sm:p-6">
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[1fr_auto] lg:items-center">
        <div>
          <p className="text-sm font-black tracking-[0.24em] text-emerald-300">TUR YÖNETİMİ</p>
          <h2 className="mt-2 text-3xl font-black">TUR EKLE</h2>
          <p className="mt-2 text-sm text-zinc-300">
            Açık duruşu olmayan üretimdeki istasyonlara mevcut Üretim Planlama endpointi üzerinden 1 tur ekler.
          </p>
          <div className="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Metric label="Aktif istasyon" value={`${activeStationCount} istasyon`} />
            <Metric label="Bu tur eklenecek" value={`${turnPairs} çift`} />
          </div>
        </div>

        <button
          onClick={onAddTurn}
          disabled={disabled}
          className="min-h-28 rounded-2xl bg-emerald-500 px-8 py-6 text-3xl font-black text-black transition hover:scale-[1.01] hover:bg-emerald-400 disabled:cursor-not-allowed disabled:bg-zinc-700 disabled:text-zinc-400"
        >
          {saving ? "İŞLENİYOR..." : "TUR EKLE"}
        </button>
      </div>
    </section>
  );
}

function StationGrid({
  stations,
  onAssign,
  onOpenPanel,
  onQuality,
  onCutting,
}: {
  stations: LiveStation[];
  onAssign: () => void;
  onOpenPanel: (station: LiveStation) => void;
  onQuality: (station: LiveStation) => void;
  onCutting: (station: LiveStation) => void;
}) {
  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
      {stations.map((station) => (
        <StationCard
          key={station.stationNumber}
          station={station}
          onAssign={onAssign}
          onOpenPanel={() => onOpenPanel(station)}
          onQuality={() => onQuality(station)}
          onCutting={() => onCutting(station)}
        />
      ))}
    </div>
  );
}

function StationCard({
  station,
  onAssign,
  onOpenPanel,
  onQuality,
  onCutting,
}: {
  station: LiveStation;
  onAssign: () => void;
  onOpenPanel: () => void;
  onQuality: () => void;
  onCutting: () => void;
}) {
  const status = translateStatus(station.status);
  const isEmpty = status === "Boş";
  const isPaused = status === "Duraklatıldı";

  return (
    <article
      className={
        "rounded-2xl border p-5 shadow-xl transition " +
        (isEmpty
          ? "border-white/10 bg-black/25"
          : station.releaseDue
          ? "border-orange-400/40 bg-orange-400/10"
          : isPaused
          ? "border-yellow-400/30 bg-yellow-400/10"
          : "border-emerald-400/30 bg-emerald-400/12")
      }
    >
      <div className="mb-4 flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-bold text-zinc-500">İstasyon</p>
          <h3 className="text-4xl font-black">{station.stationNumber}</h3>
        </div>
        <span className={"rounded-full px-3 py-1 text-xs font-black " + statusClass(status)}>
          {status}
        </span>
      </div>

      {isEmpty ? (
        <div className="rounded-2xl border border-dashed border-white/10 bg-black/20 p-5 text-center">
          <p className="text-lg font-black text-zinc-200">Boş</p>
          <p className="mt-2 text-sm text-zinc-500">Bu istasyonda aktif iş yok.</p>
          <button
            onClick={onAssign}
            className="mt-5 w-full rounded-xl bg-emerald-500 px-4 py-3 text-sm font-black text-black transition hover:bg-emerald-400"
          >
            İş Ata
          </button>
        </div>
      ) : (
        <>
          <div className="space-y-2">
            <Info label="Müşteri" value={station.customerName} />
            <Info label="Ürün" value={[station.productCode, station.productName].filter(Boolean).join(" - ")} />
            <Info label="Kalıp" value={[station.moldCode, station.moldName].filter(Boolean).join(" - ")} />
            <Info label="Operatör" value={station.operatorName} />
            <Info label="Üretilen" value={`${station.producedPairs} çift`} />
            <Info label="Sağlam" value={`${station.goodPairs} çift`} />
            <Info label="Fire" value={`${station.firePairs} çift`} />
            <Info label="Toplam Tur" value={String(station.totalTurns)} />
            <Info
              label="Kalıp Ayırıcı"
              value={
                station.releaseFrequencyTurns
                  ? `${station.turnsSinceLastRelease} / ${station.releaseFrequencyTurns} tur`
                  : "Sıklık tanımlı değil"
              }
            />
            {station.openDowntime && <Info label="Açık Duruş" value={station.downtimeType ?? "Duruş"} />}
          </div>

          {station.releaseDue && (
            <p className="mt-4 rounded-xl border border-orange-400/30 bg-orange-400/10 p-3 text-xs font-black text-orange-200">
              Kalıp Ayırıcı Gerekli
            </p>
          )}

          <div className="mt-5 grid grid-cols-1 gap-2 sm:grid-cols-3">
            <button
              onClick={onOpenPanel}
              className="rounded-xl bg-emerald-500 px-4 py-3 text-sm font-black text-black transition hover:bg-emerald-400"
            >
              İşlem Paneli
            </button>
            <button
              onClick={onQuality}
              className="rounded-xl border border-white/10 bg-white/[0.07] px-4 py-3 text-sm font-black text-white transition hover:border-emerald-400/50"
            >
              Kalite Kontrolü Aç
            </button>
            <button
              onClick={onCutting}
              className="rounded-xl border border-white/10 bg-white/[0.07] px-4 py-3 text-sm font-black text-white transition hover:border-cyan-400/50"
            >
              Kesime Gönder
            </button>
          </div>
        </>
      )}
    </article>
  );
}

function StationOperationPanel({
  station,
  onClose,
  onRefresh,
  onManage,
  onPlanning,
  onMessage,
  onError,
}: {
  station: LiveStation;
  onClose: () => void;
  onRefresh: () => Promise<void>;
  onManage: () => void;
  onPlanning: () => void;
  onMessage: (message: string) => void;
  onError: (message: string) => void;
}) {
  const [tab, setTab] = useState("Özet");
  const [events, setEvents] = useState<StationEvent[]>([]);
  const [downtimes, setDowntimes] = useState<Downtime[]>([]);
  const [saving, setSaving] = useState(false);
  const [firePairs, setFirePairs] = useState("1");
  const [fireReason, setFireReason] = useState(FIRE_REASONS[0]);
  const [fireDescription, setFireDescription] = useState("");
  const [fireNote, setFireNote] = useState("");
  const [downtimeType, setDowntimeType] = useState(DOWNTIME_TYPES[0]);
  const [downtimeReason, setDowntimeReason] = useState("");
  const [downtimeNote, setDowntimeNote] = useState("");
  const [releaseNote, setReleaseNote] = useState("");

  const assignmentId = station.assignmentId;

  const loadPanelData = useCallback(async () => {
    if (!assignmentId) return;

    try {
      const [eventsResponse, downtimesResponse] = await Promise.all([
        fetch(`${API}/station-assignments/${assignmentId}/events`),
        fetch(`${API}/station-assignments/${assignmentId}/downtimes`),
      ]);
      const eventsResult = (await eventsResponse.json()) as ApiResponse<StationEvent[]>;
      const downtimesResult = (await downtimesResponse.json()) as ApiResponse<Downtime[]>;
      setEvents(eventsResult.data ?? []);
      setDowntimes(downtimesResult.data ?? []);
    } catch (requestError) {
      console.error("İstasyon geçmişi alınamadı.", requestError);
    }
  }, [assignmentId]);

  useEffect(() => {
    loadPanelData();
  }, [loadPanelData]);

  const openDowntime = downtimes.find((item) => item.isOpen);

  async function postAction(path: string, body: unknown, successMessage: string) {
    if (!assignmentId) return;

    setSaving(true);
    try {
      const response = await fetch(`${API}${path}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      const result = (await response.json()) as ApiResponse<unknown>;

      if (!response.ok) {
        throw new Error(result.message ?? "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.");
      }

      onMessage(successMessage);
      await onRefresh();
      await loadPanelData();
    } catch (requestError) {
      console.error("İstasyon işlemi başarısız.", requestError);
      onError(requestError instanceof Error ? requestError.message : "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.");
    } finally {
      setSaving(false);
    }
  }

  async function saveFire() {
    const count = Number(firePairs);
    if (!Number.isFinite(count) || count <= 0) {
      onError("Fire çift adedi 0'dan büyük olmalıdır.");
      return;
    }

    if (!window.confirm(`${count} çift fire kaydı oluşturulsun mu?`)) return;

    await postAction(
      `/station-assignments/${assignmentId}/fires`,
      { firePairs: count, reasonType: fireReason, reason: fireDescription, note: fireNote },
      "Fire kaydı oluşturuldu."
    );
    setFirePairs("1");
    setFireDescription("");
    setFireNote("");
  }

  async function startDowntime() {
    await postAction(
      `/station-assignments/${assignmentId}/downtimes/start`,
      { downtimeType, reason: downtimeReason, note: downtimeNote },
      "Duruş başlatıldı."
    );
  }

  async function finishDowntime() {
    if (!openDowntime) return;
    await postAction(
      `/station-assignments/${assignmentId}/downtimes/${openDowntime.id}/finish`,
      { note: downtimeNote },
      "Duruş bitirildi."
    );
  }

  async function applyRelease() {
    await postAction(
      `/station-assignments/${assignmentId}/release-applied`,
      { note: releaseNote },
      "Kalıp ayırıcı uygulandı."
    );
    setReleaseNote("");
  }

  return (
    <div className="fixed inset-0 z-[90] overflow-y-auto bg-black/75 p-4 backdrop-blur-sm">
      <div className="mx-auto my-6 w-full max-w-5xl rounded-3xl border border-white/10 bg-[#0F1115] shadow-2xl">
        <div className="flex flex-col gap-4 border-b border-white/10 p-6 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <p className="text-sm font-black tracking-[0.22em] text-emerald-300">İSTASYON İŞLEM PANELİ</p>
            <h2 className="mt-2 text-3xl font-black">İstasyon {station.stationNumber}</h2>
            <p className="mt-2 text-sm text-zinc-400">
              {[station.customerName, station.productName, station.moldName].filter(Boolean).join(" · ")}
            </p>
          </div>
          <button onClick={onClose} className="rounded-xl bg-zinc-800 px-4 py-2 text-sm font-bold text-white">
            Kapat
          </button>
        </div>

        <div className="flex gap-2 overflow-x-auto border-b border-white/10 p-4">
          {["Özet", "Fire Bildir", "Duruş / Arıza", "Kalıp Ayırıcı", "Geçmiş", "Üretim Planlama’ya Git"].map((item) => (
            <button
              key={item}
              onClick={() => (item === "Üretim Planlama’ya Git" ? onPlanning() : setTab(item))}
              className={
                "shrink-0 rounded-xl px-4 py-2 text-sm font-bold transition " +
                (tab === item ? "bg-emerald-500 text-black" : "bg-white/[0.06] text-zinc-300 hover:bg-white/[0.1]")
              }
            >
              {item}
            </button>
          ))}
        </div>

        <div className="p-6">
          {tab === "Özet" && (
            <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
              <Metric label="Üretilen" value={`${station.producedPairs} çift`} />
              <Metric label="Sağlam" value={`${station.goodPairs} çift`} />
              <Metric label="Fire" value={`${station.firePairs} çift`} />
              <Metric label="Toplam Tur" value={String(station.totalTurns)} />
              <Metric
                label="Kalıp Ayırıcı"
                value={station.releaseFrequencyTurns ? `${station.turnsSinceLastRelease} / ${station.releaseFrequencyTurns}` : "Tanımsız"}
              />
              <Metric label="Açık Duruş" value={station.openDowntime ? station.downtimeType ?? "Var" : "Yok"} />
              <button onClick={onManage} className="rounded-2xl bg-emerald-500 p-4 text-sm font-black text-black">
                Mevcut İşi Yönet
              </button>
            </div>
          )}

          {tab === "Fire Bildir" && (
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <Field label="Fire çift adedi">
                <input value={firePairs} onChange={(event) => setFirePairs(event.target.value)} className={CONTROL_CLASS} type="number" min="1" />
              </Field>
              <Field label="Fire nedeni">
                <select value={fireReason} onChange={(event) => setFireReason(event.target.value)} className={CONTROL_CLASS}>
                  {FIRE_REASONS.map((reason) => (
                    <option key={reason}>{reason}</option>
                  ))}
                </select>
              </Field>
              <Field label="Açıklama">
                <textarea value={fireDescription} onChange={(event) => setFireDescription(event.target.value)} className={CONTROL_CLASS} />
              </Field>
              <Field label="Not">
                <textarea value={fireNote} onChange={(event) => setFireNote(event.target.value)} className={CONTROL_CLASS} />
              </Field>
              <button onClick={saveFire} disabled={saving} className="rounded-xl bg-red-500 px-5 py-3 font-black text-white disabled:opacity-60">
                Fire Kaydet
              </button>
            </div>
          )}

          {tab === "Duruş / Arıza" && (
            <div className="space-y-4">
              {openDowntime ? (
                <div className="rounded-2xl border border-yellow-400/30 bg-yellow-400/10 p-5">
                  <p className="font-black text-yellow-200">{openDowntime.downtimeType}</p>
                  <p className="mt-2 text-sm text-zinc-300">Başlangıç: {formatDateTime(openDowntime.startedAt)}</p>
                  <p className="mt-1 text-sm text-zinc-300">Geçen süre: {elapsedMinutes(openDowntime.startedAt)} dk</p>
                  <textarea
                    value={downtimeNote}
                    onChange={(event) => setDowntimeNote(event.target.value)}
                    placeholder="Kapanış notu"
                    className={CONTROL_CLASS + " mt-4"}
                  />
                  <button onClick={finishDowntime} disabled={saving} className="mt-4 rounded-xl bg-emerald-500 px-5 py-3 font-black text-black disabled:opacity-60">
                    Duruşu Bitir
                  </button>
                </div>
              ) : (
                <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                  <Field label="Duruş türü">
                    <select value={downtimeType} onChange={(event) => setDowntimeType(event.target.value)} className={CONTROL_CLASS}>
                      {DOWNTIME_TYPES.map((item) => (
                        <option key={item}>{item}</option>
                      ))}
                    </select>
                  </Field>
                  <Field label="Açıklama">
                    <textarea value={downtimeReason} onChange={(event) => setDowntimeReason(event.target.value)} className={CONTROL_CLASS} />
                  </Field>
                  <Field label="Not">
                    <textarea value={downtimeNote} onChange={(event) => setDowntimeNote(event.target.value)} className={CONTROL_CLASS} />
                  </Field>
                  <button onClick={startDowntime} disabled={saving} className="rounded-xl bg-yellow-400 px-5 py-3 font-black text-black disabled:opacity-60">
                    Duruşu Başlat
                  </button>
                </div>
              )}
            </div>
          )}

          {tab === "Kalıp Ayırıcı" && (
            <div className="space-y-4">
              <div className="rounded-2xl border border-white/10 bg-black/25 p-5">
                <p className="text-sm text-zinc-400">Kalıp Ayırıcı</p>
                <p className="mt-2 text-3xl font-black">
                  {station.releaseFrequencyTurns
                    ? `${station.turnsSinceLastRelease} / ${station.releaseFrequencyTurns} tur`
                    : "Kalıp ayırıcı sıklığı kalıp kartında tanımlı değil."}
                </p>
                {station.releaseDue && <p className="mt-3 font-black text-orange-300">Kalıp Ayırıcı Gerekli</p>}
              </div>
              <textarea value={releaseNote} onChange={(event) => setReleaseNote(event.target.value)} placeholder="Not" className={CONTROL_CLASS} />
              <button onClick={applyRelease} disabled={saving} className="rounded-xl bg-emerald-500 px-5 py-3 font-black text-black disabled:opacity-60">
                Kalıp Ayırıcı Uygulandı
              </button>
            </div>
          )}

          {tab === "Geçmiş" && (
            <div className="space-y-3">
              {events.length === 0 ? (
                <p className="text-sm text-zinc-400">Henüz olay kaydı yok.</p>
              ) : (
                events.map((event) => (
                  <div key={event.id} className="rounded-2xl border border-white/10 bg-black/25 p-4">
                    <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                      <p className="font-black text-white">{event.eventType}</p>
                      <p className="text-xs text-zinc-500">{formatDateTime(event.eventTime)}</p>
                    </div>
                    <p className="mt-2 text-sm text-zinc-400">
                      {[event.quantity ? `${event.quantity} çift` : null, event.reason, event.note].filter(Boolean).join(" · ")}
                    </p>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function ConfirmTurnModal({
  activeStationCount,
  turnPairs,
  saving,
  onClose,
  onConfirm,
}: {
  activeStationCount: number;
  turnPairs: number;
  saving: boolean;
  onClose: () => void;
  onConfirm: () => void;
}) {
  return (
    <div className="fixed inset-0 z-[80] flex items-center justify-center bg-black/75 p-4 backdrop-blur-sm">
      <div className="w-full max-w-lg rounded-3xl border border-emerald-400/30 bg-[#0F1115] p-7 shadow-2xl">
        <p className="text-sm font-black tracking-[0.2em] text-emerald-300">TUR EKLE ONAYI</p>
        <h2 className="mt-2 text-3xl font-black">1 Tur Eklenecek</h2>
        <p className="mt-3 text-sm text-zinc-400">Açık duruşu olan istasyonlar bu işlemde atlanır.</p>
        <div className="mt-6 grid grid-cols-2 gap-4">
          <Metric label="Aktif istasyon" value={`${activeStationCount} istasyon`} />
          <Metric label="Eklenecek üretim" value={`+${turnPairs} çift`} />
        </div>
        <div className="mt-7 flex justify-end gap-3">
          <button onClick={onClose} disabled={saving} className="rounded-xl bg-zinc-700 px-5 py-3 font-bold text-white disabled:opacity-50">
            Vazgeç
          </button>
          <button onClick={onConfirm} disabled={saving} className="rounded-xl bg-emerald-500 px-5 py-3 font-black text-black hover:bg-emerald-400 disabled:opacity-50">
            {saving ? "Tur Ekleniyor..." : "Onayla"}
          </button>
        </div>
      </div>
    </div>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/25 p-4">
      <p className="text-xs text-zinc-400">{label}</p>
      <p className="mt-1 text-2xl font-black text-white">{value}</p>
    </div>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-bold text-zinc-300">{label}</span>
      {children}
    </label>
  );
}

function Info({ label, value }: { label: string; value?: string | number | null }) {
  if (value === null || value === undefined || value === "") return null;
  return (
    <div className="flex justify-between gap-4 border-b border-white/10 py-2 text-sm">
      <span className="text-zinc-400">{label}</span>
      <span className="text-right font-bold text-white">{value}</span>
    </div>
  );
}

const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-sm text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60 disabled:cursor-not-allowed disabled:opacity-60";

function translateStatus(status?: string | null) {
  switch (status) {
    case "Üretimde":
      return "Üretimde";
    case "Duraklatıldı":
      return "Duraklatıldı";
    case "Tamamlandı":
      return "Tamamlandı";
    case "Boş":
    default:
      return "Boş";
  }
}

function statusClass(status: string) {
  if (status === "Üretimde") return "bg-emerald-400 text-black";
  if (status === "Duraklatıldı") return "bg-yellow-400 text-black";
  if (status === "Tamamlandı") return "bg-blue-400 text-black";
  return "bg-zinc-700 text-zinc-200";
}

function formatTime(value?: string | null) {
  if (!value) return "Yok";
  return new Intl.DateTimeFormat("tr-TR", { hour: "2-digit", minute: "2-digit" }).format(new Date(value));
}

function formatDateTime(value?: string | null) {
  if (!value) return "Yok";
  return new Intl.DateTimeFormat("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

function elapsedMinutes(value: string) {
  return Math.max(0, Math.floor((Date.now() - new Date(value).getTime()) / 60000));
}

function formatValue(value: string | number) {
  if (typeof value === "number") return value.toLocaleString("tr-TR");
  return value;
}
