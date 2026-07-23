"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import { safeResponseJson, authenticatedFetch, API_PROXY } from "../lib/api/client";

type DashboardTone = "emerald" | "cyan" | "amber" | "red" | "blue" | "violet" | "zinc";
type DialogMode = "create" | "edit" | "detail" | null;
type MachineTab = "general" | "capacity" | "status" | "maintenance" | "performance" | "counters" | "notes";
type ActionMode = "start" | "stop" | "maintenance" | "cleaning" | "calibration" | "production" | null;

type ApiResponse<T> = {
  data?: T;
  message?: string;
  errorCode?: string;
  errors?: string[];
  success?: boolean;
};

type Machine = {
  id: string;
  code?: string | null;
  name?: string | null;
  description?: string | null;
  machineType?: string | null;
  model?: string | null;
  manufacturer?: string | null;
  serialNumber?: string | null;
  year?: number | null;
  stationCount?: number | null;
  defaultCycleTimeSeconds?: number | null;
  maximumDailyCapacity?: number | null;
  workingHoursPerDay?: number | null;
  energyConsumption?: number | null;
  location?: string | null;
  currentStatus?: string | null;
  currentWorkOrderId?: string | null;
  currentOperatorName?: string | null;
  lastMaintenanceDate?: string | null;
  nextMaintenanceDate?: string | null;
  lastCleaningDate?: string | null;
  nextCleaningDate?: string | null;
  lastCalibrationDate?: string | null;
  nextCalibrationDate?: string | null;
  totalRunningHours?: number | null;
  totalProducedPairs?: number | null;
  availabilityPercent?: number | null;
  performancePercent?: number | null;
  qualityPercent?: number | null;
  oee?: number | null;
  notes?: string | null;
  isActive?: boolean | null;
  createdAt?: string | null;
  updatedAt?: string | null;
};

type MachineFormState = {
  code: string;
  name: string;
  description: string;
  machineType: string;
  model: string;
  manufacturer: string;
  serialNumber: string;
  year: string;
  stationCount: string;
  defaultCycleTimeSeconds: string;
  maximumDailyCapacity: string;
  workingHoursPerDay: string;
  energyConsumption: string;
  location: string;
  currentStatus: string;
  currentWorkOrderId: string;
  currentOperatorName: string;
  lastMaintenanceDate: string;
  nextMaintenanceDate: string;
  lastCleaningDate: string;
  nextCleaningDate: string;
  lastCalibrationDate: string;
  nextCalibrationDate: string;
  totalRunningHours: string;
  totalProducedPairs: string;
  availabilityPercent: string;
  performancePercent: string;
  qualityPercent: string;
  oee: string;
  notes: string;
  isActive: boolean;
};

const API = API_PROXY;
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60 disabled:cursor-not-allowed disabled:opacity-70";
const MACHINE_TYPES = ["Injection", "Cutting", "Packaging", "DTF", "Warehouse", "Quality"];
const MACHINE_STATUSES = ["Idle", "Running", "Maintenance", "Stopped"];
const ACTIVE_FILTERS = ["Tümü", "Aktif", "Pasif"];
const DATE_FILTERS = ["Tümü", "Yaklaşan", "Normal"];
const TABS: Array<{ id: MachineTab; label: string }> = [
  { id: "general", label: "1 Genel Bilgiler" },
  { id: "capacity", label: "2 Teknik Kapasite" },
  { id: "status", label: "3 Durum ve Atamalar" },
  { id: "maintenance", label: "4 Bakım / Temizlik / Kalibrasyon" },
  { id: "performance", label: "5 Performans ve OEE" },
  { id: "counters", label: "6 Sayaçlar" },
  { id: "notes", label: "7 Notlar" },
];

const emptyForm: MachineFormState = {
  code: "",
  name: "",
  description: "",
  machineType: "Injection",
  model: "",
  manufacturer: "",
  serialNumber: "",
  year: "",
  stationCount: "",
  defaultCycleTimeSeconds: "",
  maximumDailyCapacity: "",
  workingHoursPerDay: "",
  energyConsumption: "",
  location: "",
  currentStatus: "Idle",
  currentWorkOrderId: "",
  currentOperatorName: "",
  lastMaintenanceDate: "",
  nextMaintenanceDate: "",
  lastCleaningDate: "",
  nextCleaningDate: "",
  lastCalibrationDate: "",
  nextCalibrationDate: "",
  totalRunningHours: "0",
  totalProducedPairs: "0",
  availabilityPercent: "",
  performancePercent: "",
  qualityPercent: "",
  oee: "",
  notes: "",
  isActive: true,
};

export default function MachinesPage() {
  const [machines, setMachines] = useState<Machine[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState("Tümü");
  const [statusFilter, setStatusFilter] = useState("Tümü");
  const [activeFilter, setActiveFilter] = useState("Tümü");
  const [maintenanceFilter, setMaintenanceFilter] = useState("Tümü");
  const [calibrationFilter, setCalibrationFilter] = useState("Tümü");
  const [dialogMode, setDialogMode] = useState<DialogMode>(null);
  const [selectedMachine, setSelectedMachine] = useState<Machine | null>(null);
  const [actionMode, setActionMode] = useState<ActionMode>(null);
  const [actionMachine, setActionMachine] = useState<Machine | null>(null);

  useEffect(() => {
    loadMachines();
  }, []);

  async function loadMachines() {
    setLoading(true);
    setError(null);

    try {
      const response = await authenticatedFetch(API + "/machines");
      if (!response.ok) {
        throw new Error(await readError(response, "Makine listesi alınamadı."));
      }

      setMachines(extractArray<Machine>(await safeResponseJson(response)));
    } catch (err) {
      setMachines([]);
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  function openDialog(mode: DialogMode, machine: Machine | null = null) {
    setSuccessMessage(null);
    setSelectedMachine(machine);
    setDialogMode(mode);
  }

  function closeDialog() {
    setDialogMode(null);
    setSelectedMachine(null);
  }

  function openAction(mode: ActionMode, machine: Machine) {
    setSuccessMessage(null);
    setActionMachine(machine);
    setActionMode(mode);
  }

  function closeAction() {
    setActionMode(null);
    setActionMachine(null);
  }

  async function handleActionSuccess(message: string, machineId?: string) {
    await loadMachines();
    if (machineId && selectedMachine?.id === machineId) {
      const response = await authenticatedFetch(`${API}/machines/${machineId}`);
      if (response.ok) {
        setSelectedMachine(extractOne<Machine>(await safeResponseJson(response)));
      }
    }
    closeAction();
    setSuccessMessage(message);
  }

  const filteredMachines = useMemo(() => {
    const term = normalizeText(search);
    return machines.filter((machine) => {
      const haystack = [
        machine.code,
        machine.name,
        machine.machineType,
        machine.model,
        machine.manufacturer,
        machine.serialNumber,
        machine.location,
        machine.currentOperatorName,
      ].join(" ");

      return (
        (!term || normalizeText(haystack).includes(term)) &&
        (typeFilter === "Tümü" || getMachineType(machine) === typeFilter) &&
        (statusFilter === "Tümü" || getMachineStatus(machine) === statusFilter) &&
        (activeFilter === "Tümü" || (activeFilter === "Aktif" ? machine.isActive !== false : machine.isActive === false)) &&
        (maintenanceFilter === "Tümü" || (maintenanceFilter === "Yaklaşan" ? isDateWithinDays(machine.nextMaintenanceDate, 14) : !isDateWithinDays(machine.nextMaintenanceDate, 14))) &&
        (calibrationFilter === "Tümü" || (calibrationFilter === "Yaklaşan" ? isDateWithinDays(machine.nextCalibrationDate, 14) : !isDateWithinDays(machine.nextCalibrationDate, 14)))
      );
    });
  }, [activeFilter, calibrationFilter, machines, maintenanceFilter, search, statusFilter, typeFilter]);

  const averageOee = calculateAverage(machines.map((machine) => safeNumber(machine.oee)).filter((value) => value > 0));
  const dashboardCards = [
    { title: "Toplam Makine", value: machines.length, note: "Machine Master", tone: "emerald" as DashboardTone },
    { title: "Aktif Makine", value: machines.filter((machine) => machine.isActive !== false).length, note: "Kullanılabilir", tone: "cyan" as DashboardTone },
    { title: "Çalışıyor", value: machines.filter((machine) => getMachineStatus(machine) === "Running").length, note: "Canlı üretim", tone: "emerald" as DashboardTone },
    { title: "Boşta", value: machines.filter((machine) => getMachineStatus(machine) === "Idle").length, note: "Hazır", tone: "zinc" as DashboardTone },
    { title: "Bakımda", value: machines.filter((machine) => getMachineStatus(machine) === "Maintenance").length, note: "Bakım süreci", tone: "amber" as DashboardTone },
    { title: "Durduruldu", value: machines.filter((machine) => getMachineStatus(machine) === "Stopped").length, note: "Plan dışı duruş", tone: "red" as DashboardTone },
    { title: "Toplam Üretilen Çift", value: machines.reduce((sum, machine) => sum + safeNumber(machine.totalProducedPairs), 0), note: "Tüm makineler", tone: "blue" as DashboardTone },
    { title: "Ortalama OEE", value: averageOee, note: "Yüzde", tone: getOeeTone(averageOee) },
  ];

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.16),transparent_34%),radial-gradient(circle_at_bottom_right,rgba(14,165,233,0.12),transparent_32%)] px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.38em] text-emerald-300">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">Machine Master</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                İş emri, canlı üretim, Mold Master, bakım, kapasite planlama, OEE ve dashboard için tek makine ana veri kaynağı.
              </p>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <button onClick={loadMachines} disabled={loading} className="rounded-xl border border-white/10 bg-white/[0.08] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:opacity-50">
                {loading ? "Yenileniyor..." : "Listeyi Yenile"}
              </button>
              <button onClick={() => openDialog("create")} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
                + Yeni Makine
              </button>
            </div>
          </header>

          {successMessage && <div className="rounded-xl border border-emerald-400/30 bg-emerald-500/10 p-4 text-sm font-bold text-emerald-100">{successMessage}</div>}

          <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {dashboardCards.map((card) => (
              <DashboardCard key={card.title} title={card.title} value={formatDashboardValue(card.value, card.title.includes("OEE"))} note={card.note} tone={card.tone} />
            ))}
          </section>

          <section className="rounded-2xl border border-white/10 bg-white/[0.06] p-5 shadow-2xl backdrop-blur">
            <div className="flex flex-col gap-4 border-b border-white/10 pb-5">
              <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
                <div>
                  <h2 className="text-2xl font-black">Makine Listesi</h2>
                  <p className="mt-1 text-sm text-zinc-400">{filteredMachines.length.toLocaleString("tr-TR")} makine listeleniyor.</p>
                </div>
                <div className="w-full xl:max-w-md">
                  <Field label="Arama">
                    <input value={search} onChange={(event) => setSearch(event.target.value)} className={CONTROL_CLASS} placeholder="Kod, makine, tip, model, operatör" />
                  </Field>
                </div>
              </div>

              <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-5">
                <FilterSelect label="Makine Tipi" value={typeFilter} options={["Tümü", ...MACHINE_TYPES]} onChange={setTypeFilter} />
                <FilterSelect label="Durum" value={statusFilter} options={["Tümü", ...MACHINE_STATUSES]} onChange={setStatusFilter} />
                <FilterSelect label="Aktif/Pasif" value={activeFilter} options={ACTIVE_FILTERS} onChange={setActiveFilter} />
                <FilterSelect label="Bakım" value={maintenanceFilter} options={DATE_FILTERS} onChange={setMaintenanceFilter} />
                <FilterSelect label="Kalibrasyon" value={calibrationFilter} options={DATE_FILTERS} onChange={setCalibrationFilter} />
              </div>
            </div>

            {loading && <LoadingState />}

            {!loading && error && (
              <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-5 text-sm text-red-100">
                <p className="font-black">Makine verileri yüklenemedi.</p>
                <p className="mt-1 text-red-200">{error}</p>
              </div>
            )}

            {!loading && !error && filteredMachines.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-zinc-300">
                Makine kaydı bulunamadı.
              </div>
            )}

            {!loading && !error && filteredMachines.length > 0 && (
              <div className="mt-5 overflow-x-auto">
                <table className="min-w-[1560px] w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-white/10 text-xs uppercase tracking-[0.16em] text-zinc-500">
                      <th className="py-3 pr-4">Kod</th>
                      <th className="py-3 pr-4">Makine</th>
                      <th className="py-3 pr-4">Tip</th>
                      <th className="py-3 pr-4">Model</th>
                      <th className="py-3 pr-4">Üretici</th>
                      <th className="py-3 pr-4">İstasyon Sayısı</th>
                      <th className="py-3 pr-4">Durum</th>
                      <th className="py-3 pr-4">Operatör</th>
                      <th className="py-3 pr-4">Günlük Kapasite</th>
                      <th className="py-3 pr-4">Toplam Çalışma Saati</th>
                      <th className="py-3 pr-4">Toplam Üretilen Çift</th>
                      <th className="py-3 pr-4">OEE</th>
                      <th className="py-3 pr-4">Son Bakım</th>
                      <th className="py-3 pr-4">Sonraki Bakım</th>
                      <th className="py-3 pr-4">Lokasyon</th>
                      <th className="py-3 pr-4">Aktiflik</th>
                      <th className="py-3 text-right">İşlemler</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/10">
                    {filteredMachines.map((machine) => (
                      <tr key={machine.id} className="align-middle text-zinc-200 transition hover:bg-white/[0.04]">
                        <td className="py-4 pr-4 font-mono text-xs text-emerald-200">{machine.code || "-"}</td>
                        <td className="py-4 pr-4 font-black text-white">{machine.name || "-"}</td>
                        <td className="py-4 pr-4">{getMachineType(machine)}</td>
                        <td className="py-4 pr-4">{machine.model || "-"}</td>
                        <td className="py-4 pr-4">{machine.manufacturer || "-"}</td>
                        <td className="py-4 pr-4">{formatNumber(machine.stationCount)}</td>
                        <td className="py-4 pr-4"><MachineStatusBadge status={getMachineStatus(machine)} /></td>
                        <td className="py-4 pr-4">{machine.currentOperatorName || "-"}</td>
                        <td className="py-4 pr-4">{formatNumber(machine.maximumDailyCapacity)}</td>
                        <td className="py-4 pr-4">{formatNumber(machine.totalRunningHours)}</td>
                        <td className="py-4 pr-4">{formatNumber(machine.totalProducedPairs)}</td>
                        <td className="py-4 pr-4"><OeeBadge value={safeNumber(machine.oee)} /></td>
                        <td className="py-4 pr-4">{formatDate(machine.lastMaintenanceDate)}</td>
                        <td className="py-4 pr-4">{formatDate(machine.nextMaintenanceDate)}</td>
                        <td className="py-4 pr-4">{machine.location || "-"}</td>
                        <td className="py-4 pr-4"><ActiveBadge active={machine.isActive !== false} /></td>
                        <td className="py-4">
                          <div className="flex min-w-[500px] flex-wrap justify-end gap-2">
                            <ActionButton label="Detay" tone="cyan" onClick={() => openDialog("detail", machine)} />
                            <ActionButton label="Düzenle" tone="emerald" onClick={() => openDialog("edit", machine)} />
                            <ActionButton label="Başlat" tone="emerald" onClick={() => openAction("start", machine)} />
                            <ActionButton label="Durdur" tone="amber" onClick={() => openAction("stop", machine)} />
                            <ActionButton label="Bakım Kaydı" tone="red" onClick={() => openAction("maintenance", machine)} />
                            <ActionButton label="Temizlik Kaydı" tone="zinc" onClick={() => openAction("cleaning", machine)} />
                            <ActionButton label="Kalibrasyon Kaydı" tone="blue" onClick={() => openAction("calibration", machine)} />
                            <ActionButton label="Üretim Kaydı" tone="violet" onClick={() => openAction("production", machine)} />
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      </div>

      {dialogMode && (
        <MachineModal
          mode={dialogMode}
          machine={selectedMachine}
          onClose={closeDialog}
          onSaved={async (message) => {
            await loadMachines();
            closeDialog();
            setSuccessMessage(message);
          }}
        />
      )}

      {actionMode && actionMachine && (
        <MachineActionModal
          mode={actionMode}
          machine={actionMachine}
          onClose={closeAction}
          onSuccess={(message) => handleActionSuccess(message, actionMachine.id)}
        />
      )}
    </main>
  );
}

function MachineModal({
  mode,
  machine,
  onClose,
  onSaved,
}: {
  mode: DialogMode;
  machine: Machine | null;
  onClose: () => void;
  onSaved: (message: string) => Promise<void>;
}) {
  const [activeTab, setActiveTab] = useState<MachineTab>("general");
  const [form, setForm] = useState<MachineFormState>(() => machineToForm(machine));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const readonly = mode === "detail";
  const computedOee = calculateOeeFromForm(form);

  function updateField<K extends keyof MachineFormState>(key: K, value: MachineFormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function saveMachine() {
    setError(null);
    const validation = validateForm(form);
    if (validation) {
      setError(validation);
      setActiveTab("general");
      return;
    }

    setSaving(true);
    try {
      const response = await authenticatedFetch(mode === "edit" && machine ? `${API}/machines/${machine.id}` : `${API}/machines`, {
        method: mode === "edit" ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(toMachineRequest(form)),
      });

      if (!response.ok) {
        throw new Error(await readError(response, "Makine kaydedilemedi."));
      }

      await onSaved(mode === "edit" ? "Makine güncellendi." : "Makine oluşturuldu.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-3 backdrop-blur-sm sm:p-5">
      <div className="flex max-h-[94vh] w-full max-w-7xl flex-col overflow-hidden rounded-2xl border border-white/10 bg-[#080B10] shadow-2xl">
        <div className="border-b border-white/10 bg-white/[0.04] p-5">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.34em] text-emerald-300">MACHINE MASTER</p>
              <h2 className="mt-2 text-2xl font-black text-white">{readonly ? "Makine Detayı" : mode === "edit" ? "Makine Düzenle" : "Yeni Makine"}</h2>
              <p className="mt-1 text-sm text-zinc-400">Makine bilgisi bir kez tanımlanır; iş emirleri ve üretim modülleri buradan seçim yapar.</p>
            </div>
            <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white transition hover:bg-white/[0.12]">Kapat</button>
          </div>
          <MachineSummary form={form} computedOee={computedOee} />
        </div>

        <div className="border-b border-white/10 px-5 pt-4">
          <div className="flex gap-2 overflow-x-auto pb-4">
            {TABS.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`whitespace-nowrap rounded-xl px-4 py-2 text-sm font-black transition ${
                  activeTab === tab.id ? "bg-emerald-500 text-black" : "border border-white/10 bg-black/30 text-zinc-300 hover:bg-white/[0.08]"
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>
        </div>

        <div className="overflow-y-auto p-5">
          {error && <div className="mb-5 rounded-xl border border-red-400/30 bg-red-500/10 p-4 text-sm font-bold text-red-100">{error}</div>}

          {activeTab === "general" && <GeneralTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "capacity" && <CapacityTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "status" && <StatusTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "maintenance" && <MaintenanceTab form={form} />}
          {activeTab === "performance" && <PerformanceTab form={form} computedOee={computedOee} />}
          {activeTab === "counters" && <CountersTab form={form} />}
          {activeTab === "notes" && <NotesTab form={form} readonly={readonly} updateField={updateField} />}
        </div>

        <div className="flex flex-col gap-3 border-t border-white/10 bg-black/30 p-5 sm:flex-row sm:justify-end">
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.12]">
            {readonly ? "Kapat" : "Vazgeç"}
          </button>
          {!readonly && (
            <button onClick={saveMachine} disabled={saving} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400 disabled:opacity-60">
              {saving ? "Kaydediliyor..." : "Kaydet"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function MachineActionModal({ mode, machine, onClose, onSuccess }: { mode: ActionMode; machine: Machine; onClose: () => void; onSuccess: (message: string) => void }) {
  const [operatorName, setOperatorName] = useState(machine.currentOperatorName || "");
  const [workOrderId, setWorkOrderId] = useState(machine.currentWorkOrderId || "");
  const [producedPairs, setProducedPairs] = useState("");
  const [runningHours, setRunningHours] = useState("");
  const [availabilityPercent, setAvailabilityPercent] = useState(numberToString(machine.availabilityPercent));
  const [performancePercent, setPerformancePercent] = useState(numberToString(machine.performancePercent));
  const [qualityPercent, setQualityPercent] = useState(numberToString(machine.qualityPercent));
  const [eventDate, setEventDate] = useState(formatDateInput(new Date()));
  const [nextDate, setNextDate] = useState("");
  const [note, setNote] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function submit() {
    setError(null);
    setSaving(true);

    try {
      let url = "";
      let body: Record<string, unknown> = {};
      let message = "";

      if (mode === "start") {
        url = `${API}/machines/${machine.id}/start`;
        body = { currentWorkOrderId: workOrderId || null, currentOperatorName: operatorName || null };
        message = "Makine çalıştırıldı.";
      }

      if (mode === "stop") {
        url = `${API}/machines/${machine.id}/stop`;
        body = { note };
        message = "Makine durduruldu.";
      }

      if (mode === "maintenance") {
        url = `${API}/machines/${machine.id}/maintenance`;
        body = { maintenanceDate: toIsoOrNull(eventDate), nextMaintenanceDate: toIsoOrNull(nextDate), note };
        message = "Bakım kaydı işlendi.";
      }

      if (mode === "cleaning") {
        url = `${API}/machines/${machine.id}/cleaning`;
        body = { cleaningDate: toIsoOrNull(eventDate), nextCleaningDate: toIsoOrNull(nextDate), note };
        message = "Temizlik kaydı işlendi.";
      }

      if (mode === "calibration") {
        url = `${API}/machines/${machine.id}/calibration`;
        body = { calibrationDate: toIsoOrNull(eventDate), nextCalibrationDate: toIsoOrNull(nextDate), note };
        message = "Kalibrasyon kaydı işlendi.";
      }

      if (mode === "production") {
        const pairs = safeParsedNumber(producedPairs);
        const hours = safeParsedNumber(runningHours);
        if (pairs < 0) throw new Error("Üretilen çift negatif olamaz.");
        if (hours < 0) throw new Error("Çalışma saati negatif olamaz.");
        url = `${API}/machines/${machine.id}/production`;
        body = {
          producedPairs: pairs,
          runningHours: hours,
          availabilityPercent: nullableNumber(availabilityPercent),
          performancePercent: nullableNumber(performancePercent),
          qualityPercent: nullableNumber(qualityPercent),
          oee: calculateOee(nullableNumber(availabilityPercent), nullableNumber(performancePercent), nullableNumber(qualityPercent)),
          note,
        };
        message = "Üretim kaydı işlendi.";
      }

      const response = await authenticatedFetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });

      if (!response.ok) {
        throw new Error(await readError(response, "İşlem tamamlanamadı."));
      }

      onSuccess(message);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setSaving(false);
    }
  }

  const title = getActionTitle(mode);

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/80 p-4 backdrop-blur">
      <div className="w-full max-w-2xl rounded-2xl border border-white/10 bg-[#080B10] p-5 shadow-2xl">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-black tracking-[0.28em] text-emerald-300">MACHINE ACTION</p>
            <h3 className="mt-2 text-2xl font-black">{title}</h3>
            <p className="mt-1 text-sm text-zinc-400">{machine.code || "-"} - {machine.name || "-"}</p>
          </div>
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white">Kapat</button>
        </div>

        {error && <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-4 text-sm font-bold text-red-100">{error}</div>}

        <div className="mt-5 space-y-4">
          {mode === "start" && (
            <div className="grid gap-4 sm:grid-cols-2">
              <TextInput label="Mevcut İş Emri" value={workOrderId} readonly={false} onChange={setWorkOrderId} />
              <TextInput label="Mevcut Operatör" value={operatorName} readonly={false} onChange={setOperatorName} />
            </div>
          )}
          {mode === "stop" && <TextAreaInput label="Durdurma Notu" value={note} readonly={false} onChange={setNote} />}
          {(mode === "maintenance" || mode === "cleaning" || mode === "calibration") && (
            <>
              <div className="grid gap-4 sm:grid-cols-2">
                <TextInput label={getActionDateLabel(mode)} value={eventDate} readonly={false} type="date" onChange={setEventDate} />
                <TextInput label={getActionNextDateLabel(mode)} value={nextDate} readonly={false} type="date" onChange={setNextDate} />
              </div>
              <TextAreaInput label="Not" value={note} readonly={false} onChange={setNote} />
            </>
          )}
          {mode === "production" && (
            <>
              <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
                <TextInput label="Üretilen Çift" value={producedPairs} readonly={false} type="number" onChange={setProducedPairs} />
                <TextInput label="Çalışma Saati" value={runningHours} readonly={false} type="number" onChange={setRunningHours} />
                <TextInput label="Availability %" value={availabilityPercent} readonly={false} type="number" onChange={setAvailabilityPercent} />
                <TextInput label="Performance %" value={performancePercent} readonly={false} type="number" onChange={setPerformancePercent} />
                <TextInput label="Quality %" value={qualityPercent} readonly={false} type="number" onChange={setQualityPercent} />
              </div>
              <TextAreaInput label="Not" value={note} readonly={false} onChange={setNote} />
            </>
          )}
        </div>

        <div className="mt-6 flex justify-end gap-3">
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-5 py-3 text-sm font-black text-white">Vazgeç</button>
          <button onClick={submit} disabled={saving} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black disabled:opacity-60">
            {saving ? "İşleniyor..." : "Kaydet"}
          </button>
        </div>
      </div>
    </div>
  );
}

function MachineSummary({ form, computedOee }: { form: MachineFormState; computedOee: number | null }) {
  const oee = form.oee ? safeParsedNumber(form.oee) : computedOee;
  const items = [
    ["Kod", form.code || "-"],
    ["Makine", form.name || "-"],
    ["Tip", form.machineType || "-"],
    ["Durum", form.currentStatus || "-"],
    ["Operatör", form.currentOperatorName || "-"],
    ["Kapasite", form.maximumDailyCapacity || "-"],
    ["Çalışma Saati", form.totalRunningHours || "0"],
    ["Üretilen Çift", form.totalProducedPairs || "0"],
    ["OEE", oee === null ? "-" : `%${formatNumber(oee)}`],
    ["Aktiflik", form.isActive ? "Aktif" : "Pasif"],
  ];

  return (
    <div className="mt-5 grid gap-2 sm:grid-cols-2 lg:grid-cols-5">
      {items.map(([label, value]) => (
        <div key={label} className={`rounded-xl border px-3 py-2 ${label === "OEE" ? getOeeBoxClass(oee || 0) : "border-white/10 bg-black/30"}`}>
          <p className="text-[10px] font-black uppercase tracking-[0.18em] text-zinc-500">{label}</p>
          <p className="mt-1 truncate text-sm font-black text-white" title={value}>{value}</p>
        </div>
      ))}
    </div>
  );
}

function GeneralTab({ form, readonly, updateField }: MachineTabProps) {
  return (
    <TabPanel title="Genel Bilgiler" note="Makine ana kartı. Diğer modüller bu kaydı seçer.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Makine Kodu" value={form.code} readonly={readonly} onChange={(value) => updateField("code", value)} />
        <TextInput label="Makine Adı" value={form.name} readonly={readonly} onChange={(value) => updateField("name", value)} />
        <SelectInput label="Makine Tipi" value={form.machineType} readonly={readonly} options={MACHINE_TYPES} onChange={(value) => updateField("machineType", value)} />
        <TextInput label="Model" value={form.model} readonly={readonly} onChange={(value) => updateField("model", value)} />
        <TextInput label="Üretici" value={form.manufacturer} readonly={readonly} onChange={(value) => updateField("manufacturer", value)} />
        <TextInput label="Seri Numarası" value={form.serialNumber} readonly={readonly} onChange={(value) => updateField("serialNumber", value)} />
        <TextInput label="Üretim Yılı" value={form.year} readonly={readonly} type="number" onChange={(value) => updateField("year", value)} />
        <TextInput label="Lokasyon" value={form.location} readonly={readonly} onChange={(value) => updateField("location", value)} />
        <Toggle label="Aktif/Pasif" checked={form.isActive} readonly={readonly} onChange={(value) => updateField("isActive", value)} />
      </div>
      <TextAreaInput label="Açıklama" value={form.description} readonly={readonly} onChange={(value) => updateField("description", value)} />
    </TabPanel>
  );
}

function CapacityTab({ form, readonly, updateField }: MachineTabProps) {
  const isInjection = form.machineType === "Injection";
  return (
    <TabPanel title="Teknik Kapasite" note="Kapasite planlama ve iş emri varsayılanları için teknik değerler.">
      {isInjection && (
        <InfoBox>
          FIXAR enjeksiyon hattı 24 istasyonlu çalışabilir. 10100 ve 10900 ürünlerinde çevrim/pişme süreleri farklıdır; kullanıcı tarafından tanımlanır.
        </InfoBox>
      )}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <TextInput label="İstasyon Sayısı" value={form.stationCount} readonly={readonly} type="number" onChange={(value) => updateField("stationCount", value)} />
        <TextInput label="Varsayılan Çevrim Süresi" value={form.defaultCycleTimeSeconds} readonly={readonly} type="number" onChange={(value) => updateField("defaultCycleTimeSeconds", value)} />
        <TextInput label="Maksimum Günlük Kapasite" value={form.maximumDailyCapacity} readonly={readonly} type="number" onChange={(value) => updateField("maximumDailyCapacity", value)} />
        <TextInput label="Günlük Çalışma Saati" value={form.workingHoursPerDay} readonly={readonly} type="number" onChange={(value) => updateField("workingHoursPerDay", value)} />
        <TextInput label="Enerji Tüketimi" value={form.energyConsumption} readonly={readonly} type="number" onChange={(value) => updateField("energyConsumption", value)} />
      </div>
    </TabPanel>
  );
}

function StatusTab({ form, readonly, updateField }: MachineTabProps) {
  return (
    <TabPanel title="Durum ve Atamalar" note="Canlı üretim bağlandığında iş emri ve operatör bilgisi buradan beslenecek.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <SelectInput label="Mevcut Durum" value={form.currentStatus} readonly={readonly} options={MACHINE_STATUSES} onChange={(value) => updateField("currentStatus", value)} />
        <TextInput label="Mevcut İş Emri" value={form.currentWorkOrderId} readonly={readonly} onChange={(value) => updateField("currentWorkOrderId", value)} />
        <TextInput label="Mevcut Operatör" value={form.currentOperatorName} readonly={readonly} onChange={(value) => updateField("currentOperatorName", value)} />
        <ReadOnlyInfo label="Başlatma Durumu" value={form.currentStatus === "Running" ? "Makine çalışıyor" : "Başlatılabilir"} />
        <ReadOnlyInfo label="Durdurma Durumu" value={form.currentStatus === "Idle" ? "Makine boşta" : "Durdurulabilir"} />
      </div>
      <InfoBox tone="cyan">Başlat ve durdur işlemleri liste üzerindeki ayrı aksiyonlarla backend’e gönderilir.</InfoBox>
    </TabPanel>
  );
}

function MaintenanceTab({ form }: { form: MachineFormState }) {
  return (
    <TabPanel title="Bakım / Temizlik / Kalibrasyon" note="Son kayıtlar Machine Master üzerinde görünür; geçmiş tabloları ileride bağlanabilir.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
        <ReadOnlyInfo label="Son Bakım" value={formatDate(form.lastMaintenanceDate)} />
        <ReadOnlyInfo label="Sonraki Bakım" value={formatDate(form.nextMaintenanceDate)} />
        <ReadOnlyInfo label="Son Temizlik" value={formatDate(form.lastCleaningDate)} />
        <ReadOnlyInfo label="Sonraki Temizlik" value={formatDate(form.nextCleaningDate)} />
        <ReadOnlyInfo label="Son Kalibrasyon" value={formatDate(form.lastCalibrationDate)} />
        <ReadOnlyInfo label="Sonraki Kalibrasyon" value={formatDate(form.nextCalibrationDate)} />
      </div>
      <InfoBox>Bakım, temizlik ve kalibrasyon kayıtları liste üzerindeki küçük aksiyon modallarıyla işlenir.</InfoBox>
    </TabPanel>
  );
}

function PerformanceTab({ form, computedOee }: { form: MachineFormState; computedOee: number | null }) {
  const oee = form.oee ? safeParsedNumber(form.oee) : computedOee;
  return (
    <TabPanel title="Performans ve OEE" note="İleride canlı üretimden otomatik beslenecek OEE yapısı.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Availability %" value={percentValue(form.availabilityPercent)} tone={getOeeTone(safeParsedNumber(form.availabilityPercent))} />
        <MetricCard label="Performance %" value={percentValue(form.performancePercent)} tone={getOeeTone(safeParsedNumber(form.performancePercent))} />
        <MetricCard label="Quality %" value={percentValue(form.qualityPercent)} tone={getOeeTone(safeParsedNumber(form.qualityPercent))} />
        <MetricCard label="OEE %" value={oee === null ? "-" : `%${formatNumber(oee)}`} tone={getOeeTone(oee || 0)} />
      </div>
    </TabPanel>
  );
}

function CountersTab({ form }: { form: MachineFormState }) {
  return (
    <TabPanel title="Sayaçlar" note="Üretim kaydı ile toplam çalışma saati ve toplam üretilen çift güncellenir.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Toplam Çalışma Saati" value={formatNumber(safeParsedNumber(form.totalRunningHours))} />
        <MetricCard label="Toplam Üretilen Çift" value={formatNumber(safeParsedNumber(form.totalProducedPairs))} />
      </div>
      <InfoBox>Üretim kaydı liste üzerindeki “Üretim Kaydı” aksiyonu ile backend’e gönderilir.</InfoBox>
    </TabPanel>
  );
}

function NotesTab({ form, readonly, updateField }: MachineTabProps) {
  return (
    <TabPanel title="Notlar" note="Machine Master genel notları.">
      <TextAreaInput label="Genel Notlar" value={form.notes} readonly={readonly} onChange={(value) => updateField("notes", value)} />
    </TabPanel>
  );
}

type MachineTabProps = {
  form: MachineFormState;
  readonly: boolean;
  updateField: <K extends keyof MachineFormState>(key: K, value: MachineFormState[K]) => void;
};

function TabPanel({ title, note, children }: { title: string; note: string; children: ReactNode }) {
  return (
    <section className="space-y-5">
      <div>
        <h3 className="text-xl font-black text-white">{title}</h3>
        <p className="mt-1 text-sm text-zinc-400">{note}</p>
      </div>
      {children}
    </section>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="block">
      <span className="mb-2 block text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{label}</span>
      {children}
    </label>
  );
}

function TextInput({ label, value, readonly, onChange, type = "text" }: { label: string; value: string; readonly: boolean; onChange: (value: string) => void; type?: string }) {
  return (
    <Field label={label}>
      <input value={value} type={type} step={type === "number" ? "0.01" : undefined} disabled={readonly} readOnly={readonly} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS} />
    </Field>
  );
}

function TextAreaInput({ label, value, readonly, onChange }: { label: string; value: string; readonly: boolean; onChange: (value: string) => void }) {
  return (
    <Field label={label}>
      <textarea value={value} disabled={readonly} readOnly={readonly} rows={4} onChange={(event) => onChange(event.target.value)} className={`${CONTROL_CLASS} min-h-28 resize-y`} />
    </Field>
  );
}

function SelectInput({ label, value, readonly, options, onChange }: { label: string; value: string; readonly: boolean; options: string[]; onChange: (value: string) => void }) {
  return (
    <Field label={label}>
      <select value={value} disabled={readonly} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS}>
        {options.map((option) => <option key={option}>{option}</option>)}
      </select>
    </Field>
  );
}

function FilterSelect({ label, value, options, onChange }: { label: string; value: string; options: string[]; onChange: (value: string) => void }) {
  return <SelectInput label={label} value={value} readonly={false} options={options} onChange={onChange} />;
}

function Toggle({ label, checked, readonly, onChange }: { label: string; checked: boolean; readonly: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="rounded-xl border border-white/10 bg-black/20 p-4">
      <span className="mb-3 block text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{label}</span>
      <button
        type="button"
        disabled={readonly}
        onClick={() => onChange(!checked)}
        className={`rounded-full px-4 py-2 text-sm font-black transition ${checked ? "bg-emerald-500 text-black" : "bg-zinc-700 text-zinc-200"} disabled:cursor-not-allowed disabled:opacity-70`}
      >
        {checked ? "Aktif" : "Pasif"}
      </button>
    </label>
  );
}

function ReadOnlyInfo({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs font-black uppercase tracking-[0.16em] text-zinc-500">{label}</p>
      <p className="mt-2 break-words text-sm font-black text-white">{value}</p>
    </div>
  );
}

function InfoBox({ children, tone = "amber" }: { children: ReactNode; tone?: "amber" | "red" | "cyan" }) {
  const className = tone === "red" ? "border-red-400/30 bg-red-500/10 text-red-100" : tone === "cyan" ? "border-cyan-400/30 bg-cyan-500/10 text-cyan-100" : "border-amber-400/30 bg-amber-500/10 text-amber-100";
  return <div className={`rounded-xl border p-4 text-sm font-bold ${className}`}>{children}</div>;
}

function DashboardCard({ title, value, note, tone }: { title: string; value: string; note: string; tone: DashboardTone }) {
  const toneClass = {
    emerald: "border-emerald-400/25 bg-emerald-500/10 text-emerald-200",
    cyan: "border-cyan-400/25 bg-cyan-500/10 text-cyan-200",
    amber: "border-amber-400/25 bg-amber-500/10 text-amber-200",
    red: "border-red-400/25 bg-red-500/10 text-red-200",
    blue: "border-blue-400/25 bg-blue-500/10 text-blue-200",
    violet: "border-violet-400/25 bg-violet-500/10 text-violet-200",
    zinc: "border-zinc-400/25 bg-zinc-500/10 text-zinc-200",
  }[tone];
  return (
    <article className={`rounded-2xl border p-5 shadow-xl ${toneClass}`}>
      <p className="text-xs font-black uppercase tracking-[0.18em] opacity-80">{title}</p>
      <p className="mt-3 text-2xl font-black text-white">{value}</p>
      <p className="mt-2 text-sm opacity-80">{note}</p>
    </article>
  );
}

function MetricCard({ label, value, tone = "zinc" }: { label: string; value: string; tone?: DashboardTone }) {
  const toneClass = {
    emerald: "border-emerald-400/30 bg-emerald-500/10 text-emerald-200",
    cyan: "border-cyan-400/30 bg-cyan-500/10 text-cyan-200",
    amber: "border-amber-400/30 bg-amber-500/10 text-amber-200",
    red: "border-red-400/30 bg-red-500/10 text-red-200",
    blue: "border-blue-400/30 bg-blue-500/10 text-blue-200",
    violet: "border-violet-400/30 bg-violet-500/10 text-violet-200",
    zinc: "border-white/10 bg-black/25 text-zinc-300",
  }[tone];
  return (
    <div className={`rounded-xl border p-4 ${toneClass}`}>
      <p className="text-xs font-black uppercase tracking-[0.18em] opacity-80">{label}</p>
      <p className="mt-2 text-2xl font-black text-white">{value}</p>
    </div>
  );
}

function MachineStatusBadge({ status }: { status: string }) {
  const className =
    status === "Running" ? "bg-emerald-500/15 text-emerald-200" :
    status === "Maintenance" ? "bg-amber-500/15 text-amber-200" :
    status === "Stopped" ? "bg-red-500/15 text-red-200" :
    "bg-zinc-500/15 text-zinc-200";
  return <span className={`rounded-full px-3 py-1 text-xs font-black ${className}`}>{status}</span>;
}

function ActiveBadge({ active }: { active: boolean }) {
  return <span className={`rounded-full px-3 py-1 text-xs font-black ${active ? "bg-emerald-500/15 text-emerald-200" : "bg-red-500/15 text-red-200"}`}>{active ? "Aktif" : "Pasif"}</span>;
}

function OeeBadge({ value }: { value: number }) {
  return <span className={`rounded-full px-3 py-1 text-xs font-black ${getOeeBadgeClass(value)}`}>{value > 0 ? `%${formatNumber(value)}` : "-"}</span>;
}

function ActionButton({ label, tone, onClick }: { label: string; tone: "cyan" | "emerald" | "amber" | "blue" | "zinc" | "red" | "violet"; onClick: () => void }) {
  const className = {
    cyan: "border-cyan-400/30 bg-cyan-400/10 text-cyan-100 hover:bg-cyan-400/20",
    emerald: "border-emerald-400/30 bg-emerald-400/10 text-emerald-100 hover:bg-emerald-400/20",
    amber: "border-amber-400/30 bg-amber-400/10 text-amber-100 hover:bg-amber-400/20",
    blue: "border-blue-400/30 bg-blue-400/10 text-blue-100 hover:bg-blue-400/20",
    zinc: "border-zinc-400/30 bg-zinc-400/10 text-zinc-100 hover:bg-zinc-400/20",
    red: "border-red-400/30 bg-red-400/10 text-red-100 hover:bg-red-400/20",
    violet: "border-violet-400/30 bg-violet-400/10 text-violet-100 hover:bg-violet-400/20",
  }[tone];
  return <button onClick={onClick} className={`rounded-lg border px-3 py-2 text-xs font-black transition ${className}`}>{label}</button>;
}

function LoadingState() {
  return <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-sm font-bold text-zinc-400">Yükleniyor...</div>;
}

function machineToForm(machine: Machine | null): MachineFormState {
  if (!machine) return { ...emptyForm };
  return {
    code: machine.code || "",
    name: machine.name || "",
    description: machine.description || "",
    machineType: machine.machineType || "Injection",
    model: machine.model || "",
    manufacturer: machine.manufacturer || "",
    serialNumber: machine.serialNumber || "",
    year: numberToString(machine.year),
    stationCount: numberToString(machine.stationCount),
    defaultCycleTimeSeconds: numberToString(machine.defaultCycleTimeSeconds),
    maximumDailyCapacity: numberToString(machine.maximumDailyCapacity),
    workingHoursPerDay: numberToString(machine.workingHoursPerDay),
    energyConsumption: numberToString(machine.energyConsumption),
    location: machine.location || "",
    currentStatus: machine.currentStatus || "Idle",
    currentWorkOrderId: machine.currentWorkOrderId || "",
    currentOperatorName: machine.currentOperatorName || "",
    lastMaintenanceDate: dateToInput(machine.lastMaintenanceDate),
    nextMaintenanceDate: dateToInput(machine.nextMaintenanceDate),
    lastCleaningDate: dateToInput(machine.lastCleaningDate),
    nextCleaningDate: dateToInput(machine.nextCleaningDate),
    lastCalibrationDate: dateToInput(machine.lastCalibrationDate),
    nextCalibrationDate: dateToInput(machine.nextCalibrationDate),
    totalRunningHours: numberToString(machine.totalRunningHours) || "0",
    totalProducedPairs: numberToString(machine.totalProducedPairs) || "0",
    availabilityPercent: numberToString(machine.availabilityPercent),
    performancePercent: numberToString(machine.performancePercent),
    qualityPercent: numberToString(machine.qualityPercent),
    oee: numberToString(machine.oee),
    notes: machine.notes || "",
    isActive: machine.isActive !== false,
  };
}

function validateForm(form: MachineFormState) {
  if (!form.code.trim()) return "Makine kodu zorunludur.";
  if (!form.name.trim()) return "Makine adı zorunludur.";
  if (!MACHINE_TYPES.includes(form.machineType)) return "Makine tipi geçersiz.";
  if (form.machineType === "Injection") {
    if (safeParsedNumber(form.stationCount) <= 0) return "Injection makinesi için istasyon sayısı 0'dan büyük olmalıdır.";
    if (safeParsedNumber(form.defaultCycleTimeSeconds) <= 0) return "Injection makinesi için varsayılan çevrim süresi 0'dan büyük olmalıdır.";
    if (safeParsedNumber(form.maximumDailyCapacity) <= 0) return "Injection makinesi için maksimum günlük kapasite 0'dan büyük olmalıdır.";
  }
  if (hasNegative(form.stationCount)) return "İstasyon sayısı negatif olamaz.";
  if (hasNegative(form.defaultCycleTimeSeconds)) return "Varsayılan çevrim süresi negatif olamaz.";
  if (hasNegative(form.maximumDailyCapacity)) return "Maksimum günlük kapasite negatif olamaz.";
  if (hasNegative(form.workingHoursPerDay)) return "Günlük çalışma saati negatif olamaz.";
  if (hasNegative(form.energyConsumption)) return "Enerji tüketimi negatif olamaz.";
  return null;
}

function toMachineRequest(form: MachineFormState) {
  return {
    code: form.code.trim(),
    name: form.name.trim(),
    description: form.description || null,
    machineType: form.machineType,
    model: form.model || null,
    manufacturer: form.manufacturer || null,
    serialNumber: form.serialNumber || null,
    year: nullableNumber(form.year),
    stationCount: nullableNumber(form.stationCount),
    defaultCycleTimeSeconds: nullableNumber(form.defaultCycleTimeSeconds),
    maximumDailyCapacity: nullableNumber(form.maximumDailyCapacity),
    workingHoursPerDay: nullableNumber(form.workingHoursPerDay),
    energyConsumption: nullableNumber(form.energyConsumption),
    location: form.location || null,
    currentStatus: form.currentStatus || "Idle",
    currentWorkOrderId: form.currentWorkOrderId || null,
    currentOperatorName: form.currentOperatorName || null,
    lastMaintenanceDate: toIsoOrNull(form.lastMaintenanceDate),
    nextMaintenanceDate: toIsoOrNull(form.nextMaintenanceDate),
    lastCleaningDate: toIsoOrNull(form.lastCleaningDate),
    nextCleaningDate: toIsoOrNull(form.nextCleaningDate),
    lastCalibrationDate: toIsoOrNull(form.lastCalibrationDate),
    nextCalibrationDate: toIsoOrNull(form.nextCalibrationDate),
    totalRunningHours: nullableNumber(form.totalRunningHours),
    totalProducedPairs: nullableNumber(form.totalProducedPairs),
    availabilityPercent: nullableNumber(form.availabilityPercent),
    performancePercent: nullableNumber(form.performancePercent),
    qualityPercent: nullableNumber(form.qualityPercent),
    oee: nullableNumber(form.oee) ?? calculateOeeFromForm(form),
    notes: form.notes || null,
    isActive: form.isActive,
  };
}

function getMachineType(machine: Machine) {
  return machine.machineType || "-";
}

function getMachineStatus(machine: Machine) {
  return machine.currentStatus || "Idle";
}

function getActionTitle(mode: ActionMode) {
  if (mode === "start") return "Makine Başlat";
  if (mode === "stop") return "Makine Durdur";
  if (mode === "maintenance") return "Bakım Kaydı";
  if (mode === "cleaning") return "Temizlik Kaydı";
  if (mode === "calibration") return "Kalibrasyon Kaydı";
  return "Üretim Kaydı";
}

function getActionDateLabel(mode: ActionMode) {
  if (mode === "maintenance") return "Bakım Tarihi";
  if (mode === "cleaning") return "Temizlik Tarihi";
  return "Kalibrasyon Tarihi";
}

function getActionNextDateLabel(mode: ActionMode) {
  if (mode === "maintenance") return "Sonraki Bakım Tarihi";
  if (mode === "cleaning") return "Sonraki Temizlik Tarihi";
  return "Sonraki Kalibrasyon Tarihi";
}

function calculateOeeFromForm(form: MachineFormState) {
  return calculateOee(nullableNumber(form.availabilityPercent), nullableNumber(form.performancePercent), nullableNumber(form.qualityPercent));
}

function calculateOee(availability: number | null, performance: number | null, quality: number | null) {
  if (availability === null || performance === null || quality === null) return null;
  return Math.round((availability * performance * quality) / 100) / 100;
}

function calculateAverage(values: number[]) {
  if (values.length === 0) return 0;
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function extractArray<T>(result: unknown): T[] {
  if (Array.isArray(result)) return result as T[];
  if (isRecord(result) && Array.isArray((result as ApiResponse<T[]>).data)) return (result as ApiResponse<T[]>).data || [];
  return [];
}

function extractOne<T>(result: unknown): T | null {
  if (isRecord(result) && isRecord((result as ApiResponse<T>).data)) return (result as ApiResponse<T>).data || null;
  return isRecord(result) ? result as T : null;
}

async function readError(response: Response, fallback: string) {
  try {
    const result = await safeResponseJson(response) as ApiResponse<unknown>;
    return result.message || result.errorCode || result.errors?.join(" ") || fallback;
  } catch {
    return fallback;
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function normalizeText(value: string | number | null | undefined) {
  return String(value || "").trim().toLocaleLowerCase("tr-TR");
}

function safeNumber(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function safeParsedNumber(value: string) {
  if (!value.trim()) return 0;
  const parsed = Number(value.replace(",", "."));
  return Number.isFinite(parsed) ? parsed : 0;
}

function nullableNumber(value: string) {
  if (!value.trim()) return null;
  const parsed = Number(value.replace(",", "."));
  return Number.isFinite(parsed) ? parsed : null;
}

function hasNegative(value: string) {
  return value.trim() ? safeParsedNumber(value) < 0 : false;
}

function numberToString(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? String(value) : "";
}

function formatNumber(value: number | null | undefined) {
  if (typeof value !== "number" || !Number.isFinite(value)) return "-";
  return value.toLocaleString("tr-TR", { maximumFractionDigits: 2 });
}

function formatDashboardValue(value: number, isPercent: boolean) {
  return isPercent ? `%${formatNumber(value)}` : value.toLocaleString("tr-TR", { maximumFractionDigits: 2 });
}

function percentValue(value: string) {
  return value.trim() ? `%${formatNumber(safeParsedNumber(value))}` : "-";
}

function dateToInput(value: string | null | undefined) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return formatDateInput(date);
}

function formatDateInput(value: Date) {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, "0");
  const day = String(value.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatDate(value: string | null | undefined) {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleDateString("tr-TR");
}

function toIsoOrNull(value: string) {
  if (!value) return null;
  const date = new Date(`${value}T00:00:00`);
  if (Number.isNaN(date.getTime())) return null;
  return date.toISOString();
}

function isDateWithinDays(value: string | null | undefined, days: number) {
  if (!value) return false;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return false;
  const now = new Date();
  const limit = new Date();
  limit.setDate(now.getDate() + days);
  return date >= now && date <= limit;
}

function getOeeTone(value: number): DashboardTone {
  if (value >= 85) return "emerald";
  if (value >= 60) return "amber";
  if (value > 0) return "red";
  return "zinc";
}

function getOeeBadgeClass(value: number) {
  if (value >= 85) return "bg-emerald-500/15 text-emerald-200";
  if (value >= 60) return "bg-amber-500/15 text-amber-200";
  if (value > 0) return "bg-red-500/15 text-red-200";
  return "bg-zinc-500/15 text-zinc-200";
}

function getOeeBoxClass(value: number) {
  if (value >= 85) return "border-emerald-400/30 bg-emerald-500/10";
  if (value >= 60) return "border-amber-400/30 bg-amber-500/10";
  if (value > 0) return "border-red-400/30 bg-red-500/10";
  return "border-white/10 bg-black/30";
}
