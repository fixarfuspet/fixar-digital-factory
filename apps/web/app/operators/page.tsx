"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";

type DashboardTone = "emerald" | "cyan" | "amber" | "red" | "blue" | "violet" | "zinc";
type DialogMode = "create" | "edit" | "detail" | null;
type OperatorTab = "general" | "department" | "skills" | "assignments" | "status" | "performance" | "notes";
type ActionMode = "machine" | "workOrder" | "station" | "startWork" | "stopWork" | "startBreak" | "endBreak" | "production" | "performance" | null;

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
  machineType?: string | null;
  isActive?: boolean | null;
};

type Operator = {
  id: string;
  code?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  fullName?: string | null;
  nationalId?: string | null;
  employeeNumber?: string | null;
  department?: string | null;
  role?: string | null;
  phone?: string | null;
  email?: string | null;
  hireDate?: string | null;
  terminationDate?: string | null;
  shift?: number | null;
  defaultMachineId?: string | null;
  defaultMachineCode?: string | null;
  defaultMachineName?: string | null;
  canUseInjectionMachine?: boolean | null;
  canUseGezerKafa?: boolean | null;
  canUseDonerKafa?: boolean | null;
  canUseDtfMachine?: boolean | null;
  canPerformQualityControl?: boolean | null;
  canPerformMaintenance?: boolean | null;
  canApproveWorkOrder?: boolean | null;
  currentMachineId?: string | null;
  currentMachineCode?: string | null;
  currentMachineName?: string | null;
  currentWorkOrderId?: string | null;
  currentWorkOrderNumber?: string | null;
  currentStationNumber?: number | null;
  currentStatus?: string | null;
  totalProducedPairs?: number | null;
  totalWorkingHours?: number | null;
  totalFirePairs?: number | null;
  averageFirePercent?: number | null;
  performancePercent?: number | null;
  qualityScore?: number | null;
  lastPerformanceUpdate?: string | null;
  photoPath?: string | null;
  qrCode?: string | null;
  barcode?: string | null;
  notes?: string | null;
  isActive?: boolean | null;
  createdAt?: string | null;
  updatedAt?: string | null;
};

type OperatorFormState = {
  code: string;
  firstName: string;
  lastName: string;
  nationalId: string;
  employeeNumber: string;
  department: string;
  role: string;
  phone: string;
  email: string;
  hireDate: string;
  terminationDate: string;
  shift: string;
  defaultMachineId: string;
  canUseInjectionMachine: boolean;
  canUseGezerKafa: boolean;
  canUseDonerKafa: boolean;
  canUseDtfMachine: boolean;
  canPerformQualityControl: boolean;
  canPerformMaintenance: boolean;
  canApproveWorkOrder: boolean;
  currentMachineId: string;
  currentWorkOrderId: string;
  currentWorkOrderNumber: string;
  currentStationNumber: string;
  currentStatus: string;
  totalProducedPairs: string;
  totalWorkingHours: string;
  totalFirePairs: string;
  averageFirePercent: string;
  performancePercent: string;
  qualityScore: string;
  lastPerformanceUpdate: string;
  photoPath: string;
  qrCode: string;
  barcode: string;
  notes: string;
  isActive: boolean;
};

const API = "http://localhost:5000/api/v1";
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60 disabled:cursor-not-allowed disabled:opacity-70";
const DEPARTMENTS = ["Injection", "Cutting", "Packaging", "Warehouse", "Quality", "Maintenance", "ProductionManagement"];
const ROLES = ["InjectionOperator", "CuttingOperator", "PackagingOperator", "WarehouseOperator", "QualityOperator", "MaintenanceOperator", "ProductionManager"];
const SHIFTS = ["1", "2", "3"];
const STATUSES = ["Available", "Working", "Break", "Leave", "Absent", "Inactive"];
const ACTIVE_FILTERS = ["Tümü", "Aktif", "Pasif"];
const TABS: Array<{ id: OperatorTab; label: string }> = [
  { id: "general", label: "1 Genel Bilgiler" },
  { id: "department", label: "2 Departman ve Rol" },
  { id: "skills", label: "3 Yetkinlikler" },
  { id: "assignments", label: "4 Makine / İş Emri / İstasyon" },
  { id: "status", label: "5 Çalışma Durumu" },
  { id: "performance", label: "6 Performans" },
  { id: "notes", label: "7 Notlar ve Kimlik" },
];
const DEPARTMENT_LABELS: Record<string, string> = {
  Injection: "Enjeksiyon",
  Cutting: "Kesim",
  Packaging: "Paketleme",
  Warehouse: "Depo",
  Quality: "Kalite",
  Maintenance: "Bakım",
  ProductionManagement: "Üretim Yönetimi",
};
const ROLE_LABELS: Record<string, string> = {
  InjectionOperator: "Poliüretan Enjeksiyon Operatörü",
  CuttingOperator: "Kesim Operatörü",
  PackagingOperator: "Paketleme Operatörü",
  WarehouseOperator: "Depocu",
  QualityOperator: "Kalite Kontrol",
  MaintenanceOperator: "Bakım Operatörü",
  ProductionManager: "Üretim Müdürü",
};

const emptyForm: OperatorFormState = {
  code: "",
  firstName: "",
  lastName: "",
  nationalId: "",
  employeeNumber: "",
  department: "Injection",
  role: "InjectionOperator",
  phone: "",
  email: "",
  hireDate: "",
  terminationDate: "",
  shift: "1",
  defaultMachineId: "",
  canUseInjectionMachine: false,
  canUseGezerKafa: false,
  canUseDonerKafa: false,
  canUseDtfMachine: false,
  canPerformQualityControl: false,
  canPerformMaintenance: false,
  canApproveWorkOrder: false,
  currentMachineId: "",
  currentWorkOrderId: "",
  currentWorkOrderNumber: "",
  currentStationNumber: "",
  currentStatus: "Available",
  totalProducedPairs: "0",
  totalWorkingHours: "0",
  totalFirePairs: "0",
  averageFirePercent: "",
  performancePercent: "",
  qualityScore: "",
  lastPerformanceUpdate: "",
  photoPath: "",
  qrCode: "",
  barcode: "",
  notes: "",
  isActive: true,
};

export default function OperatorsPage() {
  const [operators, setOperators] = useState<Operator[]>([]);
  const [machines, setMachines] = useState<Machine[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [departmentFilter, setDepartmentFilter] = useState("Tümü");
  const [roleFilter, setRoleFilter] = useState("Tümü");
  const [shiftFilter, setShiftFilter] = useState("Tümü");
  const [statusFilter, setStatusFilter] = useState("Tümü");
  const [activeFilter, setActiveFilter] = useState("Tümü");
  const [dialogMode, setDialogMode] = useState<DialogMode>(null);
  const [selectedOperator, setSelectedOperator] = useState<Operator | null>(null);
  const [actionMode, setActionMode] = useState<ActionMode>(null);
  const [actionOperator, setActionOperator] = useState<Operator | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  async function loadData() {
    setLoading(true);
    setError(null);
    try {
      const [operatorsResponse, machinesResponse] = await Promise.all([
        fetch(API + "/operators"),
        fetch(API + "/machines"),
      ]);
      if (!operatorsResponse.ok) throw new Error(await readError(operatorsResponse, "Operatör listesi alınamadı."));
      if (!machinesResponse.ok) throw new Error(await readError(machinesResponse, "Makine listesi alınamadı."));
      setOperators(extractArray<Operator>(await operatorsResponse.json()));
      setMachines(extractArray<Machine>(await machinesResponse.json()).filter((machine) => machine.isActive !== false));
    } catch (err) {
      setOperators([]);
      setMachines([]);
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  function openDialog(mode: DialogMode, operatorEntity: Operator | null = null) {
    setSuccessMessage(null);
    setSelectedOperator(operatorEntity);
    setDialogMode(mode);
  }

  function closeDialog() {
    setDialogMode(null);
    setSelectedOperator(null);
  }

  function openAction(mode: ActionMode, operatorEntity: Operator) {
    setSuccessMessage(null);
    setActionOperator(operatorEntity);
    setActionMode(mode);
  }

  function closeAction() {
    setActionMode(null);
    setActionOperator(null);
  }

  async function handleActionSuccess(message: string, operatorId?: string) {
    await loadData();
    if (operatorId && selectedOperator?.id === operatorId) {
      const response = await fetch(`${API}/operators/${operatorId}`);
      if (response.ok) setSelectedOperator(extractOne<Operator>(await response.json()));
    }
    closeAction();
    setSuccessMessage(message);
  }

  const filteredOperators = useMemo(() => {
    const term = normalizeText(search);
    return operators.filter((operatorEntity) => {
      const haystack = [
        operatorEntity.code,
        operatorEntity.firstName,
        operatorEntity.lastName,
        operatorEntity.fullName,
        operatorEntity.employeeNumber,
        operatorEntity.department,
        operatorEntity.role,
        operatorEntity.defaultMachineName,
        operatorEntity.currentMachineName,
        operatorEntity.currentWorkOrderNumber,
      ].join(" ");
      return (
        (!term || normalizeText(haystack).includes(term)) &&
        (departmentFilter === "Tümü" || operatorEntity.department === departmentFilter) &&
        (roleFilter === "Tümü" || operatorEntity.role === roleFilter) &&
        (shiftFilter === "Tümü" || String(operatorEntity.shift || "") === shiftFilter) &&
        (statusFilter === "Tümü" || getOperatorStatus(operatorEntity) === statusFilter) &&
        (activeFilter === "Tümü" || (activeFilter === "Aktif" ? operatorEntity.isActive !== false : operatorEntity.isActive === false))
      );
    });
  }, [activeFilter, departmentFilter, operators, roleFilter, search, shiftFilter, statusFilter]);

  const averageFire = calculateAverage(operators.map((item) => safeNumber(item.averageFirePercent)).filter((value) => value > 0));
  const dashboardCards = [
    { title: "Toplam Operatör", value: operators.length, note: "Operator Master", tone: "emerald" as DashboardTone },
    { title: "Aktif Operatör", value: operators.filter((item) => item.isActive !== false).length, note: "Çalışabilir kayıt", tone: "cyan" as DashboardTone },
    { title: "Çalışıyor", value: operators.filter((item) => getOperatorStatus(item) === "Working").length, note: "Aktif üretim", tone: "emerald" as DashboardTone },
    { title: "Müsait", value: operators.filter((item) => getOperatorStatus(item) === "Available").length, note: "Atanabilir", tone: "zinc" as DashboardTone },
    { title: "Molada", value: operators.filter((item) => getOperatorStatus(item) === "Break").length, note: "Mola", tone: "amber" as DashboardTone },
    { title: "İzinli", value: operators.filter((item) => getOperatorStatus(item) === "Leave").length, note: "Planlı izin", tone: "blue" as DashboardTone },
    { title: "Toplam Üretilen Çift", value: operators.reduce((sum, item) => sum + safeNumber(item.totalProducedPairs), 0), note: "Tüm operatörler", tone: "violet" as DashboardTone },
    { title: "Ortalama Fire %", value: averageFire, note: "Fire analizi", tone: averageFire > 8 ? "red" as DashboardTone : averageFire > 4 ? "amber" as DashboardTone : "emerald" as DashboardTone },
  ];

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(16,185,129,0.16),transparent_34%),radial-gradient(circle_at_bottom_right,rgba(14,165,233,0.12),transparent_32%)] px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-xs font-black tracking-[0.38em] text-emerald-300">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">Operator Master</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                İş emri, Machine Master, canlı üretim, kalite, bakım ve dashboard için tek operatör ana veri kaynağı.
              </p>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <button onClick={loadData} disabled={loading} className="rounded-xl border border-white/10 bg-white/[0.08] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.14] disabled:opacity-50">
                {loading ? "Yenileniyor..." : "Listeyi Yenile"}
              </button>
              <button onClick={() => openDialog("create")} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400">
                + Yeni Operatör
              </button>
            </div>
          </header>

          {successMessage && <div className="rounded-xl border border-emerald-400/30 bg-emerald-500/10 p-4 text-sm font-bold text-emerald-100">{successMessage}</div>}

          <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {dashboardCards.map((card) => (
              <DashboardCard key={card.title} title={card.title} value={formatDashboardValue(card.value, card.title.includes("%"))} note={card.note} tone={card.tone} />
            ))}
          </section>

          <section className="rounded-2xl border border-white/10 bg-white/[0.06] p-5 shadow-2xl backdrop-blur">
            <div className="flex flex-col gap-4 border-b border-white/10 pb-5">
              <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
                <div>
                  <h2 className="text-2xl font-black">Operatör Listesi</h2>
                  <p className="mt-1 text-sm text-zinc-400">{filteredOperators.length.toLocaleString("tr-TR")} operatör listeleniyor.</p>
                </div>
                <div className="w-full xl:max-w-md">
                  <Field label="Arama">
                    <input value={search} onChange={(event) => setSearch(event.target.value)} className={CONTROL_CLASS} placeholder="Kod, ad, sicil, rol, makine, iş emri" />
                  </Field>
                </div>
              </div>

              <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-5">
                <FilterSelect label="Departman" value={departmentFilter} options={["Tümü", ...DEPARTMENTS]} onChange={setDepartmentFilter} labels={DEPARTMENT_LABELS} />
                <FilterSelect label="Rol" value={roleFilter} options={["Tümü", ...ROLES]} onChange={setRoleFilter} labels={ROLE_LABELS} />
                <FilterSelect label="Vardiya" value={shiftFilter} options={["Tümü", ...SHIFTS]} onChange={setShiftFilter} />
                <FilterSelect label="Durum" value={statusFilter} options={["Tümü", ...STATUSES]} onChange={setStatusFilter} />
                <FilterSelect label="Aktif/Pasif" value={activeFilter} options={ACTIVE_FILTERS} onChange={setActiveFilter} />
              </div>
            </div>

            {loading && <LoadingState />}

            {!loading && error && (
              <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-5 text-sm text-red-100">
                <p className="font-black">Operatör verileri yüklenemedi.</p>
                <p className="mt-1 text-red-200">{error}</p>
              </div>
            )}

            {!loading && !error && filteredOperators.length === 0 && (
              <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-8 text-center text-zinc-300">Operatör kaydı bulunamadı.</div>
            )}

            {!loading && !error && filteredOperators.length > 0 && (
              <div className="mt-5 overflow-x-auto">
                <table className="min-w-[1680px] w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-white/10 text-xs uppercase tracking-[0.16em] text-zinc-500">
                      <th className="py-3 pr-4">Kod</th>
                      <th className="py-3 pr-4">Operatör</th>
                      <th className="py-3 pr-4">Sicil No</th>
                      <th className="py-3 pr-4">Departman</th>
                      <th className="py-3 pr-4">Rol</th>
                      <th className="py-3 pr-4">Vardiya</th>
                      <th className="py-3 pr-4">Durum</th>
                      <th className="py-3 pr-4">Varsayılan Makine</th>
                      <th className="py-3 pr-4">Mevcut Makine</th>
                      <th className="py-3 pr-4">İstasyon</th>
                      <th className="py-3 pr-4">İş Emri</th>
                      <th className="py-3 pr-4">Toplam Üretim</th>
                      <th className="py-3 pr-4">Toplam Fire</th>
                      <th className="py-3 pr-4">Fire %</th>
                      <th className="py-3 pr-4">Performans</th>
                      <th className="py-3 pr-4">Kalite Skoru</th>
                      <th className="py-3 pr-4">Aktiflik</th>
                      <th className="py-3 text-right">İşlemler</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/10">
                    {filteredOperators.map((operatorEntity) => (
                      <tr key={operatorEntity.id} className="align-middle text-zinc-200 transition hover:bg-white/[0.04]">
                        <td className="py-4 pr-4 font-mono text-xs text-emerald-200">{operatorEntity.code || "-"}</td>
                        <td className="py-4 pr-4 font-black text-white">{getFullName(operatorEntity)}</td>
                        <td className="py-4 pr-4">{operatorEntity.employeeNumber || "-"}</td>
                        <td className="py-4 pr-4">{labelOf(operatorEntity.department, DEPARTMENT_LABELS)}</td>
                        <td className="py-4 pr-4">{labelOf(operatorEntity.role, ROLE_LABELS)}</td>
                        <td className="py-4 pr-4">{operatorEntity.shift || "-"}</td>
                        <td className="py-4 pr-4"><OperatorStatusBadge status={getOperatorStatus(operatorEntity)} /></td>
                        <td className="py-4 pr-4">{operatorEntity.defaultMachineName || operatorEntity.defaultMachineCode || "Atanmamış"}</td>
                        <td className="py-4 pr-4">{operatorEntity.currentMachineName || operatorEntity.currentMachineCode || "Atanmamış"}</td>
                        <td className="py-4 pr-4">{operatorEntity.currentStationNumber || "-"}</td>
                        <td className="py-4 pr-4">{operatorEntity.currentWorkOrderNumber || "Atanmamış"}</td>
                        <td className="py-4 pr-4">{formatNumber(operatorEntity.totalProducedPairs)}</td>
                        <td className="py-4 pr-4">{formatNumber(operatorEntity.totalFirePairs)}</td>
                        <td className="py-4 pr-4">{percentText(operatorEntity.averageFirePercent)}</td>
                        <td className="py-4 pr-4">{percentText(operatorEntity.performancePercent)}</td>
                        <td className="py-4 pr-4">{percentText(operatorEntity.qualityScore)}</td>
                        <td className="py-4 pr-4"><ActiveBadge active={operatorEntity.isActive !== false} /></td>
                        <td className="py-4">
                          <div className="flex min-w-[620px] flex-wrap justify-end gap-2">
                            <ActionButton label="Detay" tone="cyan" onClick={() => openDialog("detail", operatorEntity)} />
                            <ActionButton label="Düzenle" tone="emerald" onClick={() => openDialog("edit", operatorEntity)} />
                            <ActionButton label="Makine Ata" tone="blue" onClick={() => openAction("machine", operatorEntity)} />
                            <ActionButton label="İş Emri Ata" tone="violet" onClick={() => openAction("workOrder", operatorEntity)} />
                            <ActionButton label="İstasyon Ata" tone="amber" onClick={() => openAction("station", operatorEntity)} />
                            <ActionButton label="Çalışmayı Başlat" tone="emerald" onClick={() => openAction("startWork", operatorEntity)} />
                            <ActionButton label="Çalışmayı Durdur" tone="zinc" onClick={() => openAction("stopWork", operatorEntity)} />
                            <ActionButton label="Mola Başlat" tone="amber" onClick={() => openAction("startBreak", operatorEntity)} />
                            <ActionButton label="Molayı Bitir" tone="cyan" onClick={() => openAction("endBreak", operatorEntity)} />
                            <ActionButton label="Üretim Kaydı" tone="blue" onClick={() => openAction("production", operatorEntity)} />
                            <ActionButton label="Performans Kaydı" tone="red" onClick={() => openAction("performance", operatorEntity)} />
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
        <OperatorModal
          mode={dialogMode}
          operatorEntity={selectedOperator}
          machines={machines}
          onClose={closeDialog}
          onSaved={async (message) => {
            await loadData();
            closeDialog();
            setSuccessMessage(message);
          }}
        />
      )}

      {actionMode && actionOperator && (
        <OperatorActionModal
          mode={actionMode}
          operatorEntity={actionOperator}
          machines={machines}
          onClose={closeAction}
          onSuccess={(message) => handleActionSuccess(message, actionOperator.id)}
        />
      )}
    </main>
  );
}

function OperatorModal({ mode, operatorEntity, machines, onClose, onSaved }: { mode: DialogMode; operatorEntity: Operator | null; machines: Machine[]; onClose: () => void; onSaved: (message: string) => Promise<void> }) {
  const [activeTab, setActiveTab] = useState<OperatorTab>("general");
  const [form, setForm] = useState<OperatorFormState>(() => operatorToForm(operatorEntity));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const readonly = mode === "detail";

  function updateField<K extends keyof OperatorFormState>(key: K, value: OperatorFormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function saveOperator() {
    setError(null);
    const validation = validateForm(form);
    if (validation) {
      setError(validation);
      setActiveTab("general");
      return;
    }
    setSaving(true);
    try {
      const response = await fetch(mode === "edit" && operatorEntity ? `${API}/operators/${operatorEntity.id}` : `${API}/operators`, {
        method: mode === "edit" ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(toOperatorRequest(form)),
      });
      if (!response.ok) throw new Error(await readError(response, "Operatör kaydedilemedi."));
      await onSaved(mode === "edit" ? "Operatör güncellendi." : "Operatör oluşturuldu.");
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
              <p className="text-xs font-black tracking-[0.34em] text-emerald-300">OPERATOR MASTER</p>
              <h2 className="mt-2 text-2xl font-black text-white">{readonly ? "Operatör Detayı" : mode === "edit" ? "Operatör Düzenle" : "Yeni Operatör"}</h2>
              <p className="mt-1 text-sm text-zinc-400">Operatör bilgisi tek kartta tutulur; üretim ve iş emri ekranları buradan seçim yapar.</p>
            </div>
            <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white transition hover:bg-white/[0.12]">Kapat</button>
          </div>
          <OperatorSummary form={form} machines={machines} />
        </div>

        <div className="border-b border-white/10 px-5 pt-4">
          <div className="flex gap-2 overflow-x-auto pb-4">
            {TABS.map((tab) => (
              <button key={tab.id} onClick={() => setActiveTab(tab.id)} className={`whitespace-nowrap rounded-xl px-4 py-2 text-sm font-black transition ${activeTab === tab.id ? "bg-emerald-500 text-black" : "border border-white/10 bg-black/30 text-zinc-300 hover:bg-white/[0.08]"}`}>
                {tab.label}
              </button>
            ))}
          </div>
        </div>

        <div className="overflow-y-auto p-5">
          {error && <div className="mb-5 rounded-xl border border-red-400/30 bg-red-500/10 p-4 text-sm font-bold text-red-100">{error}</div>}
          {activeTab === "general" && <GeneralTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "department" && <DepartmentTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "skills" && <SkillsTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "assignments" && <AssignmentsTab form={form} machines={machines} readonly={readonly} updateField={updateField} />}
          {activeTab === "status" && <StatusTab form={form} readonly={readonly} updateField={updateField} />}
          {activeTab === "performance" && <PerformanceTab form={form} />}
          {activeTab === "notes" && <NotesTab form={form} readonly={readonly} updateField={updateField} />}
        </div>

        <div className="flex flex-col gap-3 border-t border-white/10 bg-black/30 p-5 sm:flex-row sm:justify-end">
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-5 py-3 text-sm font-black text-white transition hover:bg-white/[0.12]">{readonly ? "Kapat" : "Vazgeç"}</button>
          {!readonly && <button onClick={saveOperator} disabled={saving} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400 disabled:opacity-60">{saving ? "Kaydediliyor..." : "Kaydet"}</button>}
        </div>
      </div>
    </div>
  );
}

function OperatorActionModal({ mode, operatorEntity, machines, onClose, onSuccess }: { mode: ActionMode; operatorEntity: Operator; machines: Machine[]; onClose: () => void; onSuccess: (message: string) => void }) {
  const [machineId, setMachineId] = useState(operatorEntity.currentMachineId || operatorEntity.defaultMachineId || "");
  const [assignmentType, setAssignmentType] = useState("Current");
  const [workOrderId, setWorkOrderId] = useState(operatorEntity.currentWorkOrderId || "");
  const [workOrderNumber, setWorkOrderNumber] = useState(operatorEntity.currentWorkOrderNumber || "");
  const [stationNumber, setStationNumber] = useState(operatorEntity.currentStationNumber ? String(operatorEntity.currentStationNumber) : "");
  const [producedPairs, setProducedPairs] = useState("");
  const [workingHours, setWorkingHours] = useState("");
  const [firePairs, setFirePairs] = useState("");
  const [performancePercent, setPerformancePercent] = useState(numberToString(operatorEntity.performancePercent));
  const [qualityScore, setQualityScore] = useState(numberToString(operatorEntity.qualityScore));
  const [updateDate, setUpdateDate] = useState(formatDateInput(new Date()));
  const [note, setNote] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function submit() {
    setError(null);
    setSaving(true);
    try {
      let url = "";
      let body: Record<string, unknown> | undefined;
      let message = "";
      if (mode === "machine") {
        if (!machineId) throw new Error("Makine seçmelisiniz.");
        url = `${API}/operators/${operatorEntity.id}/assign-machine`;
        body = { machineId, assignmentType };
        message = "Makine ataması güncellendi.";
      } else if (mode === "workOrder") {
        url = `${API}/operators/${operatorEntity.id}/assign-work-order`;
        body = { workOrderId: workOrderId || null, workOrderNumber: workOrderNumber || null };
        message = "İş emri ataması güncellendi.";
      } else if (mode === "station") {
        const station = safeParsedNumber(stationNumber);
        if (station < 1 || station > 24) throw new Error("İstasyon 1 ile 24 arasında olmalıdır.");
        url = `${API}/operators/${operatorEntity.id}/assign-station`;
        body = { stationNumber: station };
        message = "İstasyon ataması güncellendi.";
      } else if (mode === "production") {
        const produced = safeParsedNumber(producedPairs);
        const hours = safeParsedNumber(workingHours);
        const fire = safeParsedNumber(firePairs);
        if (produced < 0 || hours < 0 || fire < 0) throw new Error("Üretim, çalışma saati ve fire negatif olamaz.");
        url = `${API}/operators/${operatorEntity.id}/record-production`;
        body = { producedPairs: produced, workingHours: hours, firePairs: fire };
        message = "Üretim kaydı işlendi.";
      } else if (mode === "performance") {
        const performance = safeParsedNumber(performancePercent);
        const quality = safeParsedNumber(qualityScore);
        if (!isPercent(performance) || !isPercent(quality)) throw new Error("Performans ve kalite skoru 0-100 arasında olmalıdır.");
        url = `${API}/operators/${operatorEntity.id}/record-performance`;
        body = { performancePercent: performance, qualityScore: quality, updateDate: toIsoOrNull(updateDate), note };
        message = "Performans kaydı işlendi.";
      } else {
        url = `${API}/operators/${operatorEntity.id}/${actionPath(mode)}`;
        body = undefined;
        message = getActionTitle(mode) + " tamamlandı.";
      }

      const response = await fetch(url, {
        method: "POST",
        headers: body ? { "Content-Type": "application/json" } : undefined,
        body: body ? JSON.stringify(body) : undefined,
      });
      if (!response.ok) throw new Error(await readError(response, "İşlem tamamlanamadı."));
      onSuccess(message);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/80 p-4 backdrop-blur">
      <div className="w-full max-w-2xl rounded-2xl border border-white/10 bg-[#080B10] p-5 shadow-2xl">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-black tracking-[0.28em] text-emerald-300">OPERATOR ACTION</p>
            <h3 className="mt-2 text-2xl font-black">{getActionTitle(mode)}</h3>
            <p className="mt-1 text-sm text-zinc-400">{operatorEntity.code || "-"} - {getFullName(operatorEntity)}</p>
          </div>
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm font-black text-white">Kapat</button>
        </div>
        {error && <div className="mt-5 rounded-xl border border-red-400/30 bg-red-500/10 p-4 text-sm font-bold text-red-100">{error}</div>}
        <div className="mt-5 space-y-4">
          {mode === "machine" && (
            <div className="grid gap-4 sm:grid-cols-2">
              <MachineSelect label="Makine" value={machineId} readonly={false} machines={machines} onChange={setMachineId} />
              <SelectInput label="Atama Tipi" value={assignmentType} readonly={false} options={["Default", "Current"]} onChange={setAssignmentType} />
            </div>
          )}
          {mode === "workOrder" && (
            <div className="grid gap-4 sm:grid-cols-2">
              <TextInput label="Work Order Id" value={workOrderId} readonly={false} onChange={setWorkOrderId} />
              <TextInput label="Work Order Number" value={workOrderNumber} readonly={false} onChange={setWorkOrderNumber} />
            </div>
          )}
          {mode === "station" && <TextInput label="İstasyon No" value={stationNumber} readonly={false} type="number" onChange={setStationNumber} />}
          {mode === "production" && (
            <div className="grid gap-4 sm:grid-cols-3">
              <TextInput label="Üretilen Çift" value={producedPairs} readonly={false} type="number" onChange={setProducedPairs} />
              <TextInput label="Çalışma Saati" value={workingHours} readonly={false} type="number" onChange={setWorkingHours} />
              <TextInput label="Fire Çifti" value={firePairs} readonly={false} type="number" onChange={setFirePairs} />
            </div>
          )}
          {mode === "performance" && (
            <>
              <div className="grid gap-4 sm:grid-cols-3">
                <TextInput label="Performans %" value={performancePercent} readonly={false} type="number" onChange={setPerformancePercent} />
                <TextInput label="Kalite Skoru" value={qualityScore} readonly={false} type="number" onChange={setQualityScore} />
                <TextInput label="Güncelleme Tarihi" value={updateDate} readonly={false} type="date" onChange={setUpdateDate} />
              </div>
              <TextAreaInput label="Not" value={note} readonly={false} onChange={setNote} />
            </>
          )}
          {["startWork", "stopWork", "startBreak", "endBreak"].includes(String(mode)) && <InfoBox>{getActionTitle(mode)} işlemi operatör durumunu günceller.</InfoBox>}
        </div>
        <div className="mt-6 flex justify-end gap-3">
          <button onClick={onClose} className="rounded-xl border border-white/10 bg-white/[0.06] px-5 py-3 text-sm font-black text-white">Vazgeç</button>
          <button onClick={submit} disabled={saving} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black disabled:opacity-60">{saving ? "İşleniyor..." : "Kaydet"}</button>
        </div>
      </div>
    </div>
  );
}

function OperatorSummary({ form, machines }: { form: OperatorFormState; machines: Machine[] }) {
  const defaultMachine = machines.find((machine) => machine.id === form.defaultMachineId);
  const currentMachine = machines.find((machine) => machine.id === form.currentMachineId);
  const items = [
    ["Kod", form.code || "-"],
    ["Operatör", `${form.firstName} ${form.lastName}`.trim() || "-"],
    ["Departman", labelOf(form.department, DEPARTMENT_LABELS)],
    ["Rol", labelOf(form.role, ROLE_LABELS)],
    ["Vardiya", form.shift || "-"],
    ["Durum", form.currentStatus || "Available"],
    ["Varsayılan Makine", defaultMachine?.name || "Atanmamış"],
    ["Mevcut Makine", currentMachine?.name || "Atanmamış"],
    ["İş Emri", form.currentWorkOrderNumber || "Atanmamış"],
    ["Fire", form.averageFirePercent ? `%${form.averageFirePercent}` : "-"],
  ];
  return (
    <div className="mt-5 grid gap-2 sm:grid-cols-2 lg:grid-cols-5">
      {items.map(([label, value]) => (
        <div key={label} className="rounded-xl border border-white/10 bg-black/30 px-3 py-2">
          <p className="text-[10px] font-black uppercase tracking-[0.18em] text-zinc-500">{label}</p>
          <p className="mt-1 truncate text-sm font-black text-white" title={value}>{value}</p>
        </div>
      ))}
    </div>
  );
}

function GeneralTab({ form, readonly, updateField }: OperatorTabProps) {
  const fullName = `${form.firstName} ${form.lastName}`.trim() || "-";
  return (
    <TabPanel title="Genel Bilgiler" note="Operatör kimlik ve iletişim ana verisi.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <TextInput label="Operatör Kodu" value={form.code} readonly={readonly} onChange={(value) => updateField("code", value)} />
        <TextInput label="Ad" value={form.firstName} readonly={readonly} onChange={(value) => updateField("firstName", value)} />
        <TextInput label="Soyad" value={form.lastName} readonly={readonly} onChange={(value) => updateField("lastName", value)} />
        <ReadOnlyInfo label="Tam Ad" value={fullName} />
        <TextInput label="T.C. Kimlik No" value={form.nationalId} readonly={readonly} onChange={(value) => updateField("nationalId", value)} />
        <TextInput label="Sicil Numarası" value={form.employeeNumber} readonly={readonly} onChange={(value) => updateField("employeeNumber", value)} />
        <TextInput label="Telefon" value={form.phone} readonly={readonly} onChange={(value) => updateField("phone", value)} />
        <TextInput label="E-posta" value={form.email} readonly={readonly} onChange={(value) => updateField("email", value)} />
        <TextInput label="İşe Giriş Tarihi" value={form.hireDate} readonly={readonly} type="date" onChange={(value) => updateField("hireDate", value)} />
        <TextInput label="İşten Ayrılma Tarihi" value={form.terminationDate} readonly={readonly} type="date" onChange={(value) => updateField("terminationDate", value)} />
        <SelectInput label="Vardiya" value={form.shift} readonly={readonly} options={["", ...SHIFTS]} onChange={(value) => updateField("shift", value)} />
        <Toggle label="Aktif/Pasif" checked={form.isActive} readonly={readonly} onChange={(value) => updateField("isActive", value)} />
      </div>
    </TabPanel>
  );
}

function DepartmentTab({ form, readonly, updateField }: OperatorTabProps) {
  return (
    <TabPanel title="Departman ve Rol" note="Türkçe etiket gösterilir, backend’e enum değerleri gönderilir.">
      <div className="grid gap-4 md:grid-cols-2">
        <SelectInput label="Departman" value={form.department} readonly={readonly} options={DEPARTMENTS} labels={DEPARTMENT_LABELS} onChange={(value) => updateField("department", value)} />
        <SelectInput label="Rol" value={form.role} readonly={readonly} options={ROLES} labels={ROLE_LABELS} onChange={(value) => updateField("role", value)} />
      </div>
    </TabPanel>
  );
}

function SkillsTab({ form, readonly, updateField }: OperatorTabProps) {
  return (
    <TabPanel title="Yetkinlikler" note="Üretim Müdürü ve operatör yetki alanları ileride gerçek yetkilendirmeye bağlanabilir.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <Toggle label="Enjeksiyon Makinesi Kullanabilir" checked={form.canUseInjectionMachine} readonly={readonly} onChange={(value) => updateField("canUseInjectionMachine", value)} />
        <Toggle label="Gezer Kafa Kullanabilir" checked={form.canUseGezerKafa} readonly={readonly} onChange={(value) => updateField("canUseGezerKafa", value)} />
        <Toggle label="Döner Kafa Kullanabilir" checked={form.canUseDonerKafa} readonly={readonly} onChange={(value) => updateField("canUseDonerKafa", value)} />
        <Toggle label="DTF Makinesi Kullanabilir" checked={form.canUseDtfMachine} readonly={readonly} onChange={(value) => updateField("canUseDtfMachine", value)} />
        <Toggle label="Kalite Kontrol Yapabilir" checked={form.canPerformQualityControl} readonly={readonly} onChange={(value) => updateField("canPerformQualityControl", value)} />
        <Toggle label="Bakım Yapabilir" checked={form.canPerformMaintenance} readonly={readonly} onChange={(value) => updateField("canPerformMaintenance", value)} />
        <Toggle label="İş Emri Onaylayabilir" checked={form.canApproveWorkOrder} readonly={readonly} onChange={(value) => updateField("canApproveWorkOrder", value)} />
      </div>
    </TabPanel>
  );
}

function AssignmentsTab({ form, machines, readonly, updateField }: OperatorTabProps & { machines: Machine[] }) {
  return (
    <TabPanel title="Makine / İş Emri / İstasyon" note="Makine seçimleri Machine Master kayıtlarından gelir.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MachineSelect label="Varsayılan Makine" value={form.defaultMachineId} readonly={readonly} machines={machines} onChange={(value) => updateField("defaultMachineId", value)} />
        <MachineSelect label="Mevcut Makine" value={form.currentMachineId} readonly={readonly} machines={machines} onChange={(value) => updateField("currentMachineId", value)} />
        <TextInput label="Mevcut İş Emri No" value={form.currentWorkOrderNumber} readonly={readonly} onChange={(value) => updateField("currentWorkOrderNumber", value)} />
        <TextInput label="Mevcut İstasyon" value={form.currentStationNumber} readonly={readonly} type="number" onChange={(value) => updateField("currentStationNumber", value)} />
      </div>
      <InfoBox>Makine, iş emri ve istasyon atamaları liste üzerindeki küçük aksiyon modallarıyla backend’e ayrı endpointlerden gönderilir.</InfoBox>
    </TabPanel>
  );
}

function StatusTab({ form, readonly, updateField }: OperatorTabProps) {
  return (
    <TabPanel title="Çalışma Durumu" note="Canlı üretim ekranlarında kullanılacak operatör durumu.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <SelectInput label="CurrentStatus" value={form.currentStatus} readonly={readonly} options={STATUSES} onChange={(value) => updateField("currentStatus", value)} />
        <ReadOnlyInfo label="Çalışmayı Başlat" value="Working" />
        <ReadOnlyInfo label="Çalışmayı Durdur" value="Available + iş emri/istasyon temizlenir" />
        <ReadOnlyInfo label="Mola" value="Break / Working" />
      </div>
    </TabPanel>
  );
}

function PerformanceTab({ form }: { form: OperatorFormState }) {
  return (
    <TabPanel title="Performans" note="Üretim ve kalite göstergeleri readonly izlenir.">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Toplam Üretilen Çift" value={form.totalProducedPairs || "0"} />
        <MetricCard label="Toplam Çalışma Saati" value={form.totalWorkingHours || "0"} />
        <MetricCard label="Toplam Fire Çifti" value={form.totalFirePairs || "0"} />
        <MetricCard label="Ortalama Fire %" value={form.averageFirePercent ? `%${form.averageFirePercent}` : "-"} tone={safeParsedNumber(form.averageFirePercent) > 8 ? "red" : "amber"} />
        <MetricCard label="Performans %" value={form.performancePercent ? `%${form.performancePercent}` : "-"} tone={safeParsedNumber(form.performancePercent) >= 85 ? "emerald" : "cyan"} />
        <MetricCard label="Kalite Skoru" value={form.qualityScore ? `%${form.qualityScore}` : "-"} tone={safeParsedNumber(form.qualityScore) >= 85 ? "emerald" : "cyan"} />
        <MetricCard label="Son Performans Güncellemesi" value={formatDate(form.lastPerformanceUpdate)} />
      </div>
    </TabPanel>
  );
}

function NotesTab({ form, readonly, updateField }: OperatorTabProps) {
  return (
    <TabPanel title="Notlar ve Kimlik" note="Dosya yükleme endpoint’i hazır olana kadar path/text olarak tutulur.">
      <div className="grid gap-4 lg:grid-cols-[1fr_1.4fr]">
        <div className="overflow-hidden rounded-xl border border-white/10 bg-black/30">
          <div className="flex aspect-[4/3] items-center justify-center bg-white/[0.04]">
            {form.photoPath ? <p className="px-4 text-center text-sm font-black text-emerald-200">{form.photoPath}</p> : <p className="text-xs font-black uppercase tracking-[0.18em] text-zinc-500">Fotoğraf Önizleme</p>}
          </div>
        </div>
        <div className="grid gap-4 md:grid-cols-2">
          <TextInput label="Fotoğraf Path" value={form.photoPath} readonly={readonly} onChange={(value) => updateField("photoPath", value)} />
          <TextInput label="QR Kod" value={form.qrCode} readonly={readonly} onChange={(value) => updateField("qrCode", value)} />
          <TextInput label="Barkod" value={form.barcode} readonly={readonly} onChange={(value) => updateField("barcode", value)} />
          <div className="md:col-span-2"><TextAreaInput label="Notlar" value={form.notes} readonly={readonly} onChange={(value) => updateField("notes", value)} /></div>
        </div>
      </div>
    </TabPanel>
  );
}

type OperatorTabProps = {
  form: OperatorFormState;
  readonly: boolean;
  updateField: <K extends keyof OperatorFormState>(key: K, value: OperatorFormState[K]) => void;
};

function TabPanel({ title, note, children }: { title: string; note: string; children: ReactNode }) {
  return <section className="space-y-5"><div><h3 className="text-xl font-black text-white">{title}</h3><p className="mt-1 text-sm text-zinc-400">{note}</p></div>{children}</section>;
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{label}</span>{children}</label>;
}

function TextInput({ label, value, readonly, onChange, type = "text" }: { label: string; value: string; readonly: boolean; onChange: (value: string) => void; type?: string }) {
  return <Field label={label}><input value={value} type={type} step={type === "number" ? "0.01" : undefined} disabled={readonly} readOnly={readonly} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS} /></Field>;
}

function TextAreaInput({ label, value, readonly, onChange }: { label: string; value: string; readonly: boolean; onChange: (value: string) => void }) {
  return <Field label={label}><textarea value={value} disabled={readonly} readOnly={readonly} rows={4} onChange={(event) => onChange(event.target.value)} className={`${CONTROL_CLASS} min-h-28 resize-y`} /></Field>;
}

function SelectInput({ label, value, readonly, options, onChange, labels }: { label: string; value: string; readonly: boolean; options: string[]; onChange: (value: string) => void; labels?: Record<string, string> }) {
  return (
    <Field label={label}>
      <select value={value} disabled={readonly} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS}>
        {options.map((option) => <option key={option} value={option}>{labels?.[option] || option || "Seçilmemiş"}</option>)}
      </select>
    </Field>
  );
}

function FilterSelect({ label, value, options, onChange, labels }: { label: string; value: string; options: string[]; onChange: (value: string) => void; labels?: Record<string, string> }) {
  return <SelectInput label={label} value={value} readonly={false} options={options} labels={labels} onChange={onChange} />;
}

function MachineSelect({ label, value, readonly, machines, onChange }: { label: string; value: string; readonly: boolean; machines: Machine[]; onChange: (value: string) => void }) {
  return (
    <Field label={label}>
      <select value={value} disabled={readonly} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS}>
        <option value="">Atanmamış</option>
        {machines.map((machine) => <option key={machine.id} value={machine.id}>{[machine.code, machine.name, machine.machineType].filter(Boolean).join(" - ")}</option>)}
      </select>
    </Field>
  );
}

function Toggle({ label, checked, readonly, onChange }: { label: string; checked: boolean; readonly: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="rounded-xl border border-white/10 bg-black/20 p-4">
      <span className="mb-3 block text-xs font-black uppercase tracking-[0.18em] text-zinc-500">{label}</span>
      <button type="button" disabled={readonly} onClick={() => onChange(!checked)} className={`rounded-full px-4 py-2 text-sm font-black transition ${checked ? "bg-emerald-500 text-black" : "bg-zinc-700 text-zinc-200"} disabled:cursor-not-allowed disabled:opacity-70`}>{checked ? "Evet" : "Hayır"}</button>
    </label>
  );
}

function ReadOnlyInfo({ label, value }: { label: string; value: string }) {
  return <div className="rounded-xl border border-white/10 bg-black/20 p-4"><p className="text-xs font-black uppercase tracking-[0.16em] text-zinc-500">{label}</p><p className="mt-2 break-words text-sm font-black text-white">{value}</p></div>;
}

function InfoBox({ children }: { children: ReactNode }) {
  return <div className="rounded-xl border border-cyan-400/30 bg-cyan-500/10 p-4 text-sm font-bold text-cyan-100">{children}</div>;
}

function DashboardCard({ title, value, note, tone }: { title: string; value: string; note: string; tone: DashboardTone }) {
  const toneClass = toneClassFor(tone);
  return <article className={`rounded-2xl border p-5 shadow-xl ${toneClass}`}><p className="text-xs font-black uppercase tracking-[0.18em] opacity-80">{title}</p><p className="mt-3 text-2xl font-black text-white">{value}</p><p className="mt-2 text-sm opacity-80">{note}</p></article>;
}

function MetricCard({ label, value, tone = "zinc" }: { label: string; value: string; tone?: DashboardTone }) {
  return <div className={`rounded-xl border p-4 ${toneClassFor(tone)}`}><p className="text-xs font-black uppercase tracking-[0.18em] opacity-80">{label}</p><p className="mt-2 text-2xl font-black text-white">{value}</p></div>;
}

function OperatorStatusBadge({ status }: { status: string }) {
  const className = status === "Working" ? "bg-emerald-500/15 text-emerald-200" : status === "Break" ? "bg-amber-500/15 text-amber-200" : status === "Leave" ? "bg-blue-500/15 text-blue-200" : status === "Absent" || status === "Inactive" ? "bg-red-500/15 text-red-200" : "bg-zinc-500/15 text-zinc-200";
  return <span className={`rounded-full px-3 py-1 text-xs font-black ${className}`}>{status}</span>;
}

function ActiveBadge({ active }: { active: boolean }) {
  return <span className={`rounded-full px-3 py-1 text-xs font-black ${active ? "bg-emerald-500/15 text-emerald-200" : "bg-red-500/15 text-red-200"}`}>{active ? "Aktif" : "Pasif"}</span>;
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

function operatorToForm(operatorEntity: Operator | null): OperatorFormState {
  if (!operatorEntity) return { ...emptyForm };
  return {
    code: operatorEntity.code || "",
    firstName: operatorEntity.firstName || "",
    lastName: operatorEntity.lastName || "",
    nationalId: operatorEntity.nationalId || "",
    employeeNumber: operatorEntity.employeeNumber || "",
    department: operatorEntity.department || "Injection",
    role: operatorEntity.role || "InjectionOperator",
    phone: operatorEntity.phone || "",
    email: operatorEntity.email || "",
    hireDate: dateToInput(operatorEntity.hireDate),
    terminationDate: dateToInput(operatorEntity.terminationDate),
    shift: operatorEntity.shift && operatorEntity.shift > 0 ? String(operatorEntity.shift) : "",
    defaultMachineId: operatorEntity.defaultMachineId || "",
    canUseInjectionMachine: Boolean(operatorEntity.canUseInjectionMachine),
    canUseGezerKafa: Boolean(operatorEntity.canUseGezerKafa),
    canUseDonerKafa: Boolean(operatorEntity.canUseDonerKafa),
    canUseDtfMachine: Boolean(operatorEntity.canUseDtfMachine),
    canPerformQualityControl: Boolean(operatorEntity.canPerformQualityControl),
    canPerformMaintenance: Boolean(operatorEntity.canPerformMaintenance),
    canApproveWorkOrder: Boolean(operatorEntity.canApproveWorkOrder),
    currentMachineId: operatorEntity.currentMachineId || "",
    currentWorkOrderId: operatorEntity.currentWorkOrderId || "",
    currentWorkOrderNumber: operatorEntity.currentWorkOrderNumber || "",
    currentStationNumber: numberToString(operatorEntity.currentStationNumber),
    currentStatus: operatorEntity.currentStatus || "Available",
    totalProducedPairs: numberToString(operatorEntity.totalProducedPairs) || "0",
    totalWorkingHours: numberToString(operatorEntity.totalWorkingHours) || "0",
    totalFirePairs: numberToString(operatorEntity.totalFirePairs) || "0",
    averageFirePercent: numberToString(operatorEntity.averageFirePercent),
    performancePercent: numberToString(operatorEntity.performancePercent),
    qualityScore: numberToString(operatorEntity.qualityScore),
    lastPerformanceUpdate: dateToInput(operatorEntity.lastPerformanceUpdate),
    photoPath: operatorEntity.photoPath || "",
    qrCode: operatorEntity.qrCode || "",
    barcode: operatorEntity.barcode || "",
    notes: operatorEntity.notes || "",
    isActive: operatorEntity.isActive !== false,
  };
}

function validateForm(form: OperatorFormState) {
  if (!form.code.trim()) return "Operatör kodu zorunludur.";
  if (!form.firstName.trim()) return "Ad zorunludur.";
  if (!form.lastName.trim()) return "Soyad zorunludur.";
  if (!DEPARTMENTS.includes(form.department)) return "Departman geçersiz.";
  if (!ROLES.includes(form.role)) return "Rol geçersiz.";
  if (!SHIFTS.includes(form.shift)) return "Vardiya 1, 2 veya 3 olmalıdır.";
  if (!STATUSES.includes(form.currentStatus)) return "Durum geçersiz.";
  const station = safeParsedNumber(form.currentStationNumber);
  if (form.currentStationNumber && (station < 1 || station > 24)) return "İstasyon 1 ile 24 arasında olmalıdır.";
  if (hasNegative(form.totalProducedPairs) || hasNegative(form.totalWorkingHours) || hasNegative(form.totalFirePairs)) return "Üretim, çalışma saati ve fire negatif olamaz.";
  if (form.performancePercent && !isPercent(safeParsedNumber(form.performancePercent))) return "Performans 0-100 arasında olmalıdır.";
  if (form.qualityScore && !isPercent(safeParsedNumber(form.qualityScore))) return "Kalite skoru 0-100 arasında olmalıdır.";
  return null;
}

function toOperatorRequest(form: OperatorFormState) {
  return {
    code: form.code.trim(),
    firstName: form.firstName.trim(),
    lastName: form.lastName.trim(),
    nationalId: form.nationalId || null,
    employeeNumber: form.employeeNumber || null,
    department: form.department,
    role: form.role,
    phone: form.phone || null,
    email: form.email || null,
    hireDate: toIsoOrNull(form.hireDate),
    terminationDate: toIsoOrNull(form.terminationDate),
    shift: safeParsedNumber(form.shift),
    defaultMachineId: form.defaultMachineId || null,
    canUseInjectionMachine: form.canUseInjectionMachine,
    canUseGezerKafa: form.canUseGezerKafa,
    canUseDonerKafa: form.canUseDonerKafa,
    canUseDtfMachine: form.canUseDtfMachine,
    canPerformQualityControl: form.canPerformQualityControl,
    canPerformMaintenance: form.canPerformMaintenance,
    canApproveWorkOrder: form.canApproveWorkOrder,
    currentMachineId: form.currentMachineId || null,
    currentWorkOrderId: form.currentWorkOrderId || null,
    currentWorkOrderNumber: form.currentWorkOrderNumber || null,
    currentStationNumber: nullableNumber(form.currentStationNumber),
    currentStatus: form.currentStatus,
    totalProducedPairs: nullableNumber(form.totalProducedPairs),
    totalWorkingHours: nullableNumber(form.totalWorkingHours),
    totalFirePairs: nullableNumber(form.totalFirePairs),
    performancePercent: nullableNumber(form.performancePercent),
    qualityScore: nullableNumber(form.qualityScore),
    lastPerformanceUpdate: toIsoOrNull(form.lastPerformanceUpdate),
    photoPath: form.photoPath || null,
    qrCode: form.qrCode || null,
    barcode: form.barcode || null,
    notes: form.notes || null,
    isActive: form.isActive,
  };
}

function actionPath(mode: ActionMode) {
  if (mode === "startWork") return "start-work";
  if (mode === "stopWork") return "stop-work";
  if (mode === "startBreak") return "start-break";
  if (mode === "endBreak") return "end-break";
  return "";
}

function getActionTitle(mode: ActionMode) {
  if (mode === "machine") return "Makine Ata";
  if (mode === "workOrder") return "İş Emri Ata";
  if (mode === "station") return "İstasyon Ata";
  if (mode === "startWork") return "Çalışmayı Başlat";
  if (mode === "stopWork") return "Çalışmayı Durdur";
  if (mode === "startBreak") return "Mola Başlat";
  if (mode === "endBreak") return "Molayı Bitir";
  if (mode === "production") return "Üretim Kaydı";
  return "Performans Kaydı";
}

function getOperatorStatus(operatorEntity: Operator) {
  return operatorEntity.currentStatus || "Available";
}

function getFullName(operatorEntity: Operator) {
  return operatorEntity.fullName || `${operatorEntity.firstName || ""} ${operatorEntity.lastName || ""}`.trim() || "-";
}

function labelOf(value: string | null | undefined, labels: Record<string, string>) {
  return labels[value || ""] || value || "-";
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
    const result = await response.json() as ApiResponse<unknown>;
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

function isPercent(value: number) {
  return value >= 0 && value <= 100;
}

function numberToString(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? String(value) : "";
}

function formatNumber(value: number | null | undefined) {
  if (typeof value !== "number" || !Number.isFinite(value)) return "-";
  return value.toLocaleString("tr-TR", { maximumFractionDigits: 2 });
}

function percentText(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? `%${formatNumber(value)}` : "-";
}

function formatDashboardValue(value: number, isPercent: boolean) {
  return isPercent ? `%${formatNumber(value)}` : value.toLocaleString("tr-TR", { maximumFractionDigits: 2 });
}

function calculateAverage(values: number[]) {
  if (values.length === 0) return 0;
  return values.reduce((sum, value) => sum + value, 0) / values.length;
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

function toneClassFor(tone: DashboardTone) {
  return {
    emerald: "border-emerald-400/25 bg-emerald-500/10 text-emerald-200",
    cyan: "border-cyan-400/25 bg-cyan-500/10 text-cyan-200",
    amber: "border-amber-400/25 bg-amber-500/10 text-amber-200",
    red: "border-red-400/25 bg-red-500/10 text-red-200",
    blue: "border-blue-400/25 bg-blue-500/10 text-blue-200",
    violet: "border-violet-400/25 bg-violet-500/10 text-violet-200",
    zinc: "border-zinc-400/25 bg-zinc-500/10 text-zinc-200",
  }[tone];
}
