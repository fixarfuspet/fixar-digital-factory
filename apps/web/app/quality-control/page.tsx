"use client";

import { Suspense, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";

type ApiResponse<T> = {
  data?: T;
  message?: string;
  errorMessage?: string;
  success?: boolean;
};

type QualityInspection = {
  id: string;
  inspectionNumber: string;
  inspectionType: string;
  inspectionDate: string;
  stationNumber: number;
  stationAssignmentId?: string;
  workOrderNumber?: string | null;
  customerName?: string | null;
  productCode?: string | null;
  productName?: string | null;
  moldCode?: string | null;
  operatorName?: string | null;
  checkedPairs: number;
  rejectedPairs: number;
  result: string;
  status: string;
  holdProduction: boolean;
  linkedFirePairs: number;
};

type QualitySummary = {
  totalInspections: number;
  passedCount: number;
  conditionalCount: number;
  failedCount: number;
  pendingCount: number;
  totalCheckedPairs: number;
  totalRejectedPairs: number;
  defectRate: number;
  totalFirePairsLinked: number;
  holdCount: number;
};

type StationAssignment = {
  id: string;
  stationNumberSnapshot: number;
  status: string;
  operatorName?: string | null;
  producedPairs: number;
  firePairs: number;
  goodPairs: number;
  customerName?: string | null;
  productName?: string | null;
  productCode?: string | null;
  workOrderId?: string | null;
  workOrderNumber?: string | null;
  moldName?: string | null;
  moldCode?: string | null;
};

type QualityDefectForm = {
  defectType: string;
  defectCode: string;
  description: string;
  defectPairs: string;
  severity: string;
  isFireRelated: boolean;
  correctiveAction: string;
};

type QualityForm = {
  stationAssignmentId: string;
  inspectionType: string;
  sampleSizePairs: string;
  checkedPairs: string;
  acceptedPairs: string;
  rejectedPairs: string;
  conditionalAcceptedPairs: string;
  measuredWeightGrams: string;
  targetWeightGrams: string;
  weightToleranceMinus: string;
  weightTolerancePlus: string;
  measuredDensity: string;
  densityMinimum: string;
  densityMaximum: string;
  measuredX: string;
  measuredY: string;
  targetX: string;
  targetY: string;
  dimensionTolerance: string;
  visualResult: string;
  colorResult: string;
  surfaceResult: string;
  fabricBondingResult: string;
  correctiveAction: string;
  generalNotes: string;
  holdProduction: boolean;
  createFireRecord: boolean;
  fireReason: string;
  firePairs: string;
  defects: QualityDefectForm[];
};

const API = (process.env.NEXT_PUBLIC_API_BASE_URL?.trim() || "/api/backend/api/v1").replace(/\/$/, "");
const CONTROL_CLASS =
  "w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white outline-none transition placeholder:text-zinc-600 focus:border-emerald-400/60 disabled:cursor-not-allowed disabled:opacity-70";
const INSPECTION_TYPES = [
  { value: "StartUp", label: "Başlangıç Kontrolü" },
  { value: "InProcess", label: "Ara Kontrol" },
  { value: "Final", label: "Final Kontrol" },
  { value: "Random", label: "Rastgele Kontrol" },
  { value: "ComplaintReview", label: "Şikayet İncelemesi" },
];
const CHECK_RESULTS = ["NotChecked", "Passed", "Warning", "Failed"];
const FIRE_REASONS = ["Gramaj Hatası", "Yoğunluk Hatası", "Yüzey Bozukluğu", "Renk Hatası", "Kalıp Kaynaklı", "Operatör Kaynaklı", "Diğer"];

const emptyForm: QualityForm = {
  stationAssignmentId: "",
  inspectionType: "InProcess",
  sampleSizePairs: "10",
  checkedPairs: "10",
  acceptedPairs: "10",
  rejectedPairs: "0",
  conditionalAcceptedPairs: "0",
  measuredWeightGrams: "",
  targetWeightGrams: "",
  weightToleranceMinus: "",
  weightTolerancePlus: "",
  measuredDensity: "",
  densityMinimum: "",
  densityMaximum: "",
  measuredX: "",
  measuredY: "",
  targetX: "",
  targetY: "",
  dimensionTolerance: "",
  visualResult: "NotChecked",
  colorResult: "NotChecked",
  surfaceResult: "NotChecked",
  fabricBondingResult: "NotChecked",
  correctiveAction: "",
  generalNotes: "",
  holdProduction: false,
  createFireRecord: false,
  fireReason: "Gramaj Hatası",
  firePairs: "0",
  defects: [],
};

export default function QualityControlPage() {
  return (
    <Suspense fallback={<main className="min-h-screen bg-[#05070A] p-10 text-white">Kalite Kontrol yükleniyor...</main>}>
      <QualityControlContent />
    </Suspense>
  );
}

function QualityControlContent() {
  const searchParams = useSearchParams();
  const [inspections, setInspections] = useState<QualityInspection[]>([]);
  const [summary, setSummary] = useState<QualitySummary | null>(null);
  const [assignments, setAssignments] = useState<StationAssignment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [resultFilter, setResultFilter] = useState("Tümü");
  const [typeFilter, setTypeFilter] = useState("Tümü");
  const [modalOpen, setModalOpen] = useState(false);
  const [detail, setDetail] = useState<QualityInspection | null>(null);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadData(), 0);
    return () => window.clearTimeout(timer);
  }, []);

  useEffect(() => {
    const stationAssignmentId = searchParams.get("stationAssignmentId");
    const timer = stationAssignmentId && assignments.length > 0
      ? window.setTimeout(() => { setDetail(null); setModalOpen(true); }, 0)
      : undefined;
    return () => { if (timer !== undefined) window.clearTimeout(timer); };
  }, [assignments, searchParams]);

  async function loadData() {
    setLoading(true);
    setError(null);
    try {
      const [inspectionData, summaryData, assignmentData] = await Promise.all([
        apiGet<unknown>("/quality-inspections"),
        apiGet<QualitySummary>("/quality-inspections/summary"),
        apiGet<unknown>("/station-assignments/active"),
      ]);
      setInspections(extractArray(inspectionData).map(mapInspection).filter(Boolean) as QualityInspection[]);
      setSummary(summaryData);
      setAssignments(extractArray(assignmentData).map(mapAssignment).filter(Boolean) as StationAssignment[]);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Kalite verileri alınamadı.");
    } finally {
      setLoading(false);
    }
  }

  async function openDetail(inspection: QualityInspection) {
    setError(null);
    try {
      const data = await apiGet<unknown>("/quality-inspections/" + inspection.id);
      setDetail(mapInspection(data) ?? inspection);
      setModalOpen(true);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Kalite detayı alınamadı.");
    }
  }

  const filtered = useMemo(() => {
    const normalized = normalize(query);
    return inspections.filter((item) => {
      const matchesText =
        !normalized ||
        [item.inspectionNumber, item.customerName, item.productCode, item.productName, item.workOrderNumber, item.moldCode, item.operatorName]
          .filter(Boolean)
          .some((value) => normalize(String(value)).includes(normalized));
      const matchesResult = resultFilter === "Tümü" || item.result === resultFilter;
      const matchesType = typeFilter === "Tümü" || item.inspectionType === typeFilter;
      return matchesText && matchesResult && matchesType;
    });
  }, [inspections, query, resultFilter, typeFilter]);

  return (
    <main className="min-h-screen bg-[#05070A] text-white">
      <div className="min-h-screen px-4 py-6 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl space-y-6">
          <header className="flex flex-col gap-5 border-b border-white/10 pb-6 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-sm font-bold tracking-[0.36em] text-emerald-400">FIXAR OS</p>
              <h1 className="mt-2 text-3xl font-black sm:text-4xl">Kalite Kontrol</h1>
              <p className="mt-2 max-w-3xl text-sm text-zinc-400">
                StationAssignment merkezli üretim kalite kayıtları, fire bağlantısı ve üretim bekletme yönetimi.
              </p>
            </div>
            <button
              onClick={() => {
                setDetail(null);
                setModalOpen(true);
              }}
              className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black transition hover:bg-emerald-400"
            >
              + Yeni Kontrol
            </button>
          </header>

          {(error || success) && (
            <div className={"rounded-2xl border px-5 py-4 text-sm font-bold " + (error ? "border-red-400/30 bg-red-500/10 text-red-200" : "border-emerald-400/30 bg-emerald-500/10 text-emerald-200")}>
              {error ?? success}
            </div>
          )}

          <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <MetricCard label="Toplam Kontrol" value={summary?.totalInspections ?? 0} />
            <MetricCard label="Uygun" value={summary?.passedCount ?? 0} tone="emerald" />
            <MetricCard label="Şartlı Uygun" value={summary?.conditionalCount ?? 0} tone="amber" />
            <MetricCard label="Uygunsuz" value={summary?.failedCount ?? 0} tone="red" />
            <MetricCard label="Kontrol Edilen Çift" value={summary?.totalCheckedPairs ?? 0} />
            <MetricCard label="Uygunsuz Çift" value={summary?.totalRejectedPairs ?? 0} tone="red" />
            <MetricCard label="Hata Oranı" value={`%${formatNumber(summary?.defectRate ?? 0)}`} tone="amber" />
            <MetricCard label="Fire Bağlantılı Çift" value={summary?.totalFirePairsLinked ?? 0} tone="red" />
          </section>

          <section className="rounded-3xl border border-white/10 bg-white/[0.055] p-4 shadow-2xl sm:p-6">
            <div className="mb-5 flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
              <div>
                <h2 className="text-2xl font-black">Kalite Kontrol Kayıtları</h2>
                <p className="mt-1 text-sm text-zinc-400">{filtered.length} kayıt gösteriliyor</p>
              </div>
              <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
                <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Kontrol no, müşteri, ürün, kalıp..." className={CONTROL_CLASS} />
                <select value={typeFilter} onChange={(event) => setTypeFilter(event.target.value)} className={CONTROL_CLASS}>
                  <option>Tümü</option>
                  {INSPECTION_TYPES.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
                </select>
                <select value={resultFilter} onChange={(event) => setResultFilter(event.target.value)} className={CONTROL_CLASS}>
                  {["Tümü", "Pending", "Passed", "Conditional", "Failed", "Cancelled"].map((item) => <option key={item}>{item}</option>)}
                </select>
              </div>
            </div>

            {loading ? (
              <EmptyState text="Kalite kayıtları yükleniyor..." />
            ) : filtered.length === 0 ? (
              <EmptyState text="Henüz kalite kontrol kaydı bulunmuyor." />
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[1180px] text-left text-sm">
                  <thead className="text-xs uppercase tracking-[0.16em] text-zinc-500">
                    <tr>
                      {["Kontrol No", "Tarih", "Tür", "İstasyon", "İş Emri", "Müşteri", "Ürün", "Kalıp", "Operatör", "Kontrol Edilen", "Uygunsuz", "Sonuç", "Bekletme", "İşlemler"].map((head) => (
                        <th key={head} className="pb-3 pr-4">{head}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/10">
                    {filtered.map((item) => (
                      <tr key={item.id} className="text-zinc-200">
                        <td className="py-4 pr-4 font-black text-white">{item.inspectionNumber}</td>
                        <td className="py-4 pr-4">{formatDate(item.inspectionDate)}</td>
                        <td className="py-4 pr-4">{translateInspectionType(item.inspectionType)}</td>
                        <td className="py-4 pr-4">{item.stationNumber}</td>
                        <td className="py-4 pr-4">{item.workOrderNumber ?? "-"}</td>
                        <td className="py-4 pr-4">{item.customerName ?? "-"}</td>
                        <td className="py-4 pr-4">{[item.productCode, item.productName].filter(Boolean).join(" - ") || "-"}</td>
                        <td className="py-4 pr-4">{item.moldCode ?? "-"}</td>
                        <td className="py-4 pr-4">{item.operatorName ?? "-"}</td>
                        <td className="py-4 pr-4">{item.checkedPairs}</td>
                        <td className="py-4 pr-4">{item.rejectedPairs}</td>
                        <td className="py-4 pr-4"><ResultBadge result={item.result} /></td>
                        <td className="py-4 pr-4">{item.holdProduction ? "Var" : "Yok"}</td>
                        <td className="py-4 pr-4">
                          <button onClick={() => openDetail(item)} className="rounded-lg border border-white/10 px-3 py-2 text-xs font-bold text-white hover:border-emerald-400/50">
                            Detay
                          </button>
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

      {modalOpen && (
        <QualityModal
          detail={detail}
          assignments={assignments}
          initialAssignmentId={searchParams.get("stationAssignmentId") ?? ""}
          onClose={() => setModalOpen(false)}
          onSaved={async (message) => {
            setSuccess(message);
            setModalOpen(false);
            await loadData();
          }}
        />
      )}
    </main>
  );
}

function QualityModal({
  detail,
  assignments,
  initialAssignmentId,
  onClose,
  onSaved,
}: {
  detail: QualityInspection | null;
  assignments: StationAssignment[];
  initialAssignmentId: string;
  onClose: () => void;
  onSaved: (message: string) => Promise<void>;
}) {
  const readonly = Boolean(detail);
  const [form, setForm] = useState<QualityForm>(() => ({ ...emptyForm, stationAssignmentId: initialAssignmentId || "" }));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedAssignment = assignments.find((item) => item.id === form.stationAssignmentId);

  async function save() {
    setSaving(true);
    setError(null);
    try {
      const created = await apiPost<{ id: string }>("/quality-inspections", toRequest(form));
      const id = String((created as { id?: string }).id ?? "");
      if (id) {
        await apiPost<unknown>("/quality-inspections/" + id + "/complete", {});
      }
      await onSaved("Kalite kontrolü oluşturuldu ve tamamlandı.");
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Kalite kontrolü kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  function update<K extends keyof QualityForm>(key: K, value: QualityForm[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function addDefect() {
    setForm((current) => ({
      ...current,
      defects: [
        ...current.defects,
        { defectType: "Surface", defectCode: "", description: "", defectPairs: "1", severity: "Major", isFireRelated: current.createFireRecord, correctiveAction: "" },
      ],
    }));
  }

  return (
    <div className="fixed inset-0 z-[90] overflow-y-auto bg-black/75 p-4 backdrop-blur-sm">
      <div className="mx-auto my-6 w-full max-w-6xl rounded-3xl border border-white/10 bg-[#0F1115] shadow-2xl">
        <div className="flex flex-col gap-4 border-b border-white/10 p-6 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <p className="text-sm font-black tracking-[0.22em] text-emerald-300">KALİTE KONTROL</p>
            <h2 className="mt-2 text-3xl font-black">{detail?.inspectionNumber ?? "Yeni Kalite Kontrolü"}</h2>
            <p className="mt-2 text-sm text-zinc-400">
              {selectedAssignment ? [selectedAssignment.customerName, selectedAssignment.productName, `İstasyon ${selectedAssignment.stationNumberSnapshot}`].filter(Boolean).join(" · ") : "Aktif üretim ataması seçin."}
            </p>
          </div>
          <button onClick={onClose} className="rounded-xl bg-zinc-800 px-4 py-2 text-sm font-bold text-white">Kapat</button>
        </div>

        <div className="space-y-6 p-6">
          {error && <div className="rounded-2xl border border-red-400/30 bg-red-500/10 p-4 text-sm font-bold text-red-200">{error}</div>}

          {readonly ? (
            <DetailView detail={detail} />
          ) : (
            <>
              <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                <Field label="Aktif / geçmiş StationAssignment">
                  <select value={form.stationAssignmentId} onChange={(event) => update("stationAssignmentId", event.target.value)} className={CONTROL_CLASS}>
                    <option value="">İstasyon ataması seçin</option>
                    {assignments.map((assignment) => (
                      <option key={assignment.id} value={assignment.id}>
                        {assignment.stationNumberSnapshot} - {assignment.customerName} - {assignment.productCode} {assignment.productName}
                      </option>
                    ))}
                  </select>
                </Field>
                <Field label="Kontrol türü">
                  <select value={form.inspectionType} onChange={(event) => update("inspectionType", event.target.value)} className={CONTROL_CLASS}>
                    {INSPECTION_TYPES.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
                  </select>
                </Field>
                <ReadOnlyBox label="Mevcut üretim" value={selectedAssignment ? `${selectedAssignment.producedPairs} çift / Fire ${selectedAssignment.firePairs}` : "-"} />
              </div>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-5">
                <TextField label="Numune" value={form.sampleSizePairs} onChange={(value) => update("sampleSizePairs", value)} />
                <TextField label="Kontrol Edilen" value={form.checkedPairs} onChange={(value) => update("checkedPairs", value)} />
                <TextField label="Uygun" value={form.acceptedPairs} onChange={(value) => update("acceptedPairs", value)} />
                <TextField label="Şartlı Uygun" value={form.conditionalAcceptedPairs} onChange={(value) => update("conditionalAcceptedPairs", value)} />
                <TextField label="Uygunsuz" value={form.rejectedPairs} onChange={(value) => update("rejectedPairs", value)} />
              </div>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                <TextField label="Gramaj" value={form.measuredWeightGrams} onChange={(value) => update("measuredWeightGrams", value)} />
                <TextField label="Hedef Gramaj" value={form.targetWeightGrams} onChange={(value) => update("targetWeightGrams", value)} />
                <TextField label="Gramaj Toleransı +/-" value={form.weightTolerancePlus} onChange={(value) => { update("weightTolerancePlus", value); update("weightToleranceMinus", value); }} />
                <TextField label="Poliol Yoğunluk" value={form.measuredDensity} onChange={(value) => update("measuredDensity", value)} />
                <TextField label="Minimum Yoğunluk" value={form.densityMinimum} onChange={(value) => update("densityMinimum", value)} />
                <TextField label="Maksimum Yoğunluk" value={form.densityMaximum} onChange={(value) => update("densityMaximum", value)} />
                <TextField label="X Ölçüsü" value={form.measuredX} onChange={(value) => update("measuredX", value)} />
                <TextField label="Y Ölçüsü" value={form.measuredY} onChange={(value) => update("measuredY", value)} />
                <TextField label="XY Toleransı" value={form.dimensionTolerance} onChange={(value) => update("dimensionTolerance", value)} />
              </div>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
                <SelectField label="Görsel" value={form.visualResult} onChange={(value) => update("visualResult", value)} />
                <SelectField label="Renk" value={form.colorResult} onChange={(value) => update("colorResult", value)} />
                <SelectField label="Yüzey" value={form.surfaceResult} onChange={(value) => update("surfaceResult", value)} />
                <SelectField label="Kumaş Yapışması" value={form.fabricBondingResult} onChange={(value) => update("fabricBondingResult", value)} />
              </div>

              <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
                <div className="mb-3 flex items-center justify-between">
                  <h3 className="font-black">Kusurlar</h3>
                  <button onClick={addDefect} className="rounded-lg bg-white/10 px-3 py-2 text-xs font-bold">+ Kusur Ekle</button>
                </div>
                <div className="space-y-3">
                  {form.defects.length === 0 && <p className="text-sm text-zinc-500">Kusur kaydı yok.</p>}
                  {form.defects.map((defect, index) => (
                    <div key={index} className="grid grid-cols-1 gap-3 rounded-xl border border-white/10 bg-white/[0.04] p-3 md:grid-cols-6">
                      <input value={defect.defectType} onChange={(event) => updateDefect(setForm, index, "defectType", event.target.value)} className={CONTROL_CLASS} placeholder="Tip" />
                      <input value={defect.defectCode} onChange={(event) => updateDefect(setForm, index, "defectCode", event.target.value)} className={CONTROL_CLASS} placeholder="Kod" />
                      <input value={defect.description} onChange={(event) => updateDefect(setForm, index, "description", event.target.value)} className={CONTROL_CLASS} placeholder="Açıklama" />
                      <input value={defect.defectPairs} onChange={(event) => updateDefect(setForm, index, "defectPairs", event.target.value)} className={CONTROL_CLASS} type="number" min="0" />
                      <select value={defect.severity} onChange={(event) => updateDefect(setForm, index, "severity", event.target.value)} className={CONTROL_CLASS}>
                        {["Minor", "Major", "Critical"].map((item) => <option key={item}>{item}</option>)}
                      </select>
                      <label className="flex items-center gap-2 text-sm text-zinc-300">
                        <input type="checkbox" checked={defect.isFireRelated} onChange={(event) => updateDefect(setForm, index, "isFireRelated", event.target.checked)} />
                        Fire bağlantılı
                      </label>
                    </div>
                  ))}
                </div>
              </div>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                <label className="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/[0.04] p-4 text-sm font-bold">
                  <input type="checkbox" checked={form.holdProduction} onChange={(event) => update("holdProduction", event.target.checked)} />
                  Üretimi beklet
                </label>
                <label className="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/[0.04] p-4 text-sm font-bold">
                  <input type="checkbox" checked={form.createFireRecord} onChange={(event) => update("createFireRecord", event.target.checked)} />
                  Fire kaydı oluştur
                </label>
                {form.createFireRecord && (
                  <>
                    <Field label="Fire nedeni">
                      <select value={form.fireReason} onChange={(event) => update("fireReason", event.target.value)} className={CONTROL_CLASS}>
                        {FIRE_REASONS.map((item) => <option key={item}>{item}</option>)}
                      </select>
                    </Field>
                    <TextField label="Fire adedi" value={form.firePairs} onChange={(value) => update("firePairs", value)} />
                  </>
                )}
                <Field label="Düzeltici aksiyon">
                  <textarea value={form.correctiveAction} onChange={(event) => update("correctiveAction", event.target.value)} className={CONTROL_CLASS} />
                </Field>
                <Field label="Not">
                  <textarea value={form.generalNotes} onChange={(event) => update("generalNotes", event.target.value)} className={CONTROL_CLASS} />
                </Field>
              </div>

              <div className="flex justify-end gap-3 border-t border-white/10 pt-5">
                <button onClick={onClose} className="rounded-xl bg-zinc-800 px-5 py-3 text-sm font-bold text-white">Vazgeç</button>
                <button onClick={save} disabled={saving} className="rounded-xl bg-emerald-500 px-5 py-3 text-sm font-black text-black disabled:opacity-60">
                  Kaydet ve Tamamla
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

function DetailView({ detail }: { detail: QualityInspection | null }) {
  if (!detail) return null;
  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
      <ReadOnlyBox label="Sonuç" value={detail.result} />
      <ReadOnlyBox label="İstasyon" value={String(detail.stationNumber)} />
      <ReadOnlyBox label="Kontrol Edilen" value={String(detail.checkedPairs)} />
      <ReadOnlyBox label="Uygunsuz" value={String(detail.rejectedPairs)} />
      <ReadOnlyBox label="Müşteri" value={detail.customerName ?? "-"} />
      <ReadOnlyBox label="Ürün" value={[detail.productCode, detail.productName].filter(Boolean).join(" - ") || "-"} />
      <ReadOnlyBox label="Kalıp" value={detail.moldCode ?? "-"} />
      <ReadOnlyBox label="Operatör" value={detail.operatorName ?? "-"} />
      <ReadOnlyBox label="Fire Bağlantılı" value={String(detail.linkedFirePairs ?? 0)} />
    </div>
  );
}

function MetricCard({ label, value, tone = "cyan" }: { label: string; value: number | string; tone?: "cyan" | "emerald" | "amber" | "red" }) {
  const toneClass = tone === "emerald" ? "text-emerald-300" : tone === "amber" ? "text-amber-300" : tone === "red" ? "text-red-300" : "text-cyan-300";
  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.055] p-5">
      <p className="text-xs font-bold uppercase tracking-[0.18em] text-zinc-500">{label}</p>
      <p className={"mt-3 text-3xl font-black " + toneClass}>{value}</p>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="mb-2 block text-xs font-bold uppercase tracking-[0.16em] text-zinc-500">{label}</span>
      {children}
    </label>
  );
}

function TextField({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return (
    <Field label={label}>
      <input value={value} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS} type="number" />
    </Field>
  );
}

function SelectField({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return (
    <Field label={label}>
      <select value={value} onChange={(event) => onChange(event.target.value)} className={CONTROL_CLASS}>
        {CHECK_RESULTS.map((item) => <option key={item}>{item}</option>)}
      </select>
    </Field>
  );
}

function ReadOnlyBox({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs font-bold uppercase tracking-[0.16em] text-zinc-500">{label}</p>
      <p className="mt-2 font-black text-white">{value}</p>
    </div>
  );
}

function EmptyState({ text }: { text: string }) {
  return <div className="rounded-2xl border border-white/10 bg-black/20 p-10 text-center text-zinc-400">{text}</div>;
}

function ResultBadge({ result }: { result: string }) {
  const color = result === "Passed" ? "bg-emerald-400/10 text-emerald-200" : result === "Conditional" ? "bg-amber-400/10 text-amber-200" : result === "Failed" ? "bg-red-400/10 text-red-200" : "bg-zinc-400/10 text-zinc-200";
  return <span className={"rounded-full px-3 py-1 text-xs font-black " + color}>{result}</span>;
}

function updateDefect<K extends keyof QualityDefectForm>(
  setForm: React.Dispatch<React.SetStateAction<QualityForm>>,
  index: number,
  key: K,
  value: QualityDefectForm[K]
) {
  setForm((current) => ({
    ...current,
    defects: current.defects.map((item, itemIndex) => (itemIndex === index ? { ...item, [key]: value } : item)),
  }));
}

function toRequest(form: QualityForm) {
  return {
    inspectionType: form.inspectionType,
    stationAssignmentId: form.stationAssignmentId,
    sampleSizePairs: toNumber(form.sampleSizePairs),
    checkedPairs: toNumber(form.checkedPairs),
    acceptedPairs: toNumber(form.acceptedPairs),
    rejectedPairs: toNumber(form.rejectedPairs),
    conditionalAcceptedPairs: toNumber(form.conditionalAcceptedPairs),
    measuredWeightGrams: optionalNumber(form.measuredWeightGrams),
    targetWeightGrams: optionalNumber(form.targetWeightGrams),
    weightToleranceMinus: optionalNumber(form.weightToleranceMinus),
    weightTolerancePlus: optionalNumber(form.weightTolerancePlus),
    measuredDensity: optionalNumber(form.measuredDensity),
    densityMinimum: optionalNumber(form.densityMinimum),
    densityMaximum: optionalNumber(form.densityMaximum),
    measuredX: optionalNumber(form.measuredX),
    measuredY: optionalNumber(form.measuredY),
    targetX: optionalNumber(form.targetX),
    targetY: optionalNumber(form.targetY),
    dimensionTolerance: optionalNumber(form.dimensionTolerance),
    visualResult: form.visualResult,
    colorResult: form.colorResult,
    surfaceResult: form.surfaceResult,
    fabricBondingResult: form.fabricBondingResult,
    correctiveAction: form.correctiveAction || null,
    generalNotes: form.generalNotes || null,
    holdProduction: form.holdProduction,
    createFireRecord: form.createFireRecord,
    fireReason: form.createFireRecord ? form.fireReason : null,
    firePairs: form.createFireRecord ? toNumber(form.firePairs) : 0,
    defects: form.defects.map((defect) => ({
      defectType: defect.defectType,
      defectCode: defect.defectCode || null,
      description: defect.description || null,
      defectPairs: toNumber(defect.defectPairs),
      severity: defect.severity,
      isFireRelated: defect.isFireRelated,
      correctiveAction: defect.correctiveAction || null,
    })),
  };
}

async function apiGet<T>(path: string): Promise<T> {
  return apiRequest<T>(path, { method: "GET" });
}

async function apiPost<T>(path: string, body: unknown): Promise<T> {
  return apiRequest<T>(path, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
}

async function apiRequest<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(API + path, init);
  const text = await response.text();
  const payload = text ? JSON.parse(text) as ApiResponse<T> | T : undefined;
  if (!response.ok) {
    const apiPayload = isRecord(payload) ? payload as ApiResponse<T> : undefined;
    throw new Error(apiPayload?.message || apiPayload?.errorMessage || "İstek başarısız oldu.");
  }
  if (isRecord(payload) && "data" in payload) return (payload as ApiResponse<T>).data as T;
  return payload as T;
}

function extractArray(value: unknown): unknown[] {
  if (Array.isArray(value)) return value;
  if (isRecord(value) && Array.isArray(value.data)) return value.data;
  return [];
}

function mapInspection(value: unknown): QualityInspection | null {
  if (!isRecord(value)) return null;
  return {
    id: String(value.id ?? ""),
    inspectionNumber: String(value.inspectionNumber ?? ""),
    inspectionType: String(value.inspectionType ?? ""),
    inspectionDate: String(value.inspectionDate ?? ""),
    stationNumber: toNumber(value.stationNumber),
    stationAssignmentId: typeof value.stationAssignmentId === "string" ? value.stationAssignmentId : undefined,
    workOrderNumber: readString(value.workOrderNumber ?? value.workOrderNumberSnapshot),
    customerName: readString(value.customerName ?? value.customerNameSnapshot),
    productCode: readString(value.productCode ?? value.productCodeSnapshot),
    productName: readString(value.productName ?? value.productNameSnapshot),
    moldCode: readString(value.moldCode ?? value.moldCodeSnapshot),
    operatorName: readString(value.operatorName ?? value.operatorNameSnapshot),
    checkedPairs: toNumber(value.checkedPairs),
    rejectedPairs: toNumber(value.rejectedPairs),
    result: String(value.result ?? "Pending"),
    status: String(value.status ?? "Draft"),
    holdProduction: Boolean(value.holdProduction),
    linkedFirePairs: toNumber(value.linkedFirePairs),
  };
}

function mapAssignment(value: unknown): StationAssignment | null {
  if (!isRecord(value)) return null;
  return {
    id: String(value.id ?? ""),
    stationNumberSnapshot: toNumber(value.stationNumberSnapshot),
    status: String(value.status ?? ""),
    operatorName: readString(value.operatorName),
    producedPairs: toNumber(value.producedPairs),
    firePairs: toNumber(value.firePairs),
    goodPairs: toNumber(value.goodPairs),
    customerName: readString(value.customerName),
    productName: readString(value.productName),
    productCode: readString(value.productCode),
    workOrderId: readString(value.workOrderId),
    workOrderNumber: readString(value.workOrderNumber),
    moldName: readString(value.moldName),
    moldCode: readString(value.moldCode),
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function readString(value: unknown): string | null {
  return typeof value === "string" && value.trim() ? value : null;
}

function toNumber(value: unknown): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function optionalNumber(value: string): number | null {
  if (!value.trim()) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function normalize(value: string) {
  return value.toLocaleLowerCase("tr-TR").trim();
}

function formatDate(value?: string | null) {
  if (!value) return "-";
  return new Intl.DateTimeFormat("tr-TR", { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
}

function formatNumber(value: number) {
  return new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 2 }).format(value);
}

function translateInspectionType(value: string) {
  return INSPECTION_TYPES.find((item) => item.value === value)?.label ?? value;
}
