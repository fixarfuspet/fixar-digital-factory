"use client";

import { useEffect, useState, type ReactNode } from "react";

type Props = {
  open: boolean;
  station: number | null;
  onClose: () => void;
};

type LookupItem = { id: string; name: string };

type OrderItem = {
  id: string;
  moldId: string | null;
  productName: string;
  moldName: string;
  quantityPairs: number;
  producedPairs: number;
  remainingPairs: number;
  productionType: string;
  fabricColor: string;
};

type Order = {
  id: string;
  customerName: string;
  productName: string;
  quantity?: number;
  remainingQuantity?: number;
  items: OrderItem[];
};

type ActiveAssignment = {
  stationNumberSnapshot?: number;
  moldName?: string;
  status?: string;
};

type AvailableWorkOrder = {
  id: string;
  workOrderNumber: string;
  customerName?: string | null;
  productId: string;
  productCode?: string | null;
  productName?: string | null;
  orderItemId: string;
  plannedPairs: number;
  assignedPairs: number;
  producedPairs: number;
  remainingToAssignPairs: number;
  priority: string;
  status: string;
  recipeCode?: string | null;
  recipeName?: string | null;
};

type ProductionRecipe = {
  code: string;
  name: string;
  revision: string;
  materialType: string;
  polyol: string;
  iso: string;
  additive: string;
  polyolSetting: string;
  isoSetting: string;
};

const API = "http://localhost:5000/api/v1";
const PAIRS_PER_MOLD_HOUR = 9;

export default function AssignmentModal({ open, station, onClose }: Props) {
  const [customers, setCustomers] = useState<LookupItem[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [operators, setOperators] = useState<LookupItem[]>([]);
  const [activeAssignments, setActiveAssignments] = useState<ActiveAssignment[]>([]);
  const [workOrders, setWorkOrders] = useState<AvailableWorkOrder[]>([]);

  const [workOrderId, setWorkOrderId] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [orderItemId, setOrderItemId] = useState("");
  const [plannedPairs, setPlannedPairs] = useState("");
  const [operatorName, setOperatorName] = useState("");
  const [dailyWorkHours, setDailyWorkHours] = useState(18);
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return;

    fetch(API + "/production-planning/lookups/customers")
      .then((r) => r.json())
      .then((r) => setCustomers(r.data ?? []));

    fetch(API + "/production-planning/lookups/operators")
      .then((r) => r.json())
      .then((r) => setOperators(r.data ?? []));

    fetch(API + "/station-assignments/active")
      .then((r) => r.json())
      .then((r) => setActiveAssignments(r.data ?? []));

    fetch(API + "/work-orders/available-for-planning")
      .then((r) => r.json())
      .then((r) => setWorkOrders(r.data ?? []));
  }, [open]);

  useEffect(() => {
    if (!customerId) {
      setOrders([]);
      setOrderItemId("");
      return;
    }

    fetch(API + "/production-planning/lookups/orders?customerId=" + customerId)
      .then((r) => r.json())
      .then((r) => setOrders(r.data ?? []));
  }, [customerId]);

  if (!open) return null;

  const orderItems = orders.flatMap((order) =>
    order.items.map((item) => ({
      ...item,
      orderId: order.id,
      customerName: order.customerName,
      orderTotalQuantity: order.quantity ?? item.quantityPairs,
      orderRemainingQuantity: order.remainingQuantity ?? item.remainingPairs,
      orderCode: "ORD-" + order.id.substring(0, 8).toUpperCase(),
      label:
        order.customerName +
        " / " +
        item.productName +
        " / " +
        item.remainingPairs.toLocaleString("tr-TR") +
        " çift kaldı",
    }))
  );

  const selectedItem = orderItems.find((x) => x.id === orderItemId);
  const selectedWorkOrder = workOrders.find((x) => x.id === workOrderId);
  const season = getCurrentSeason();
  const recipe: ProductionRecipe = selectedWorkOrder?.recipeCode
    ? {
        code: selectedWorkOrder.recipeCode,
        name: selectedWorkOrder.recipeName ?? "WorkOrder Reçetesi",
        revision: "-",
        materialType: "Recipe/BOM",
        polyol: "-",
        iso: "-",
        additive: "-",
        polyolSetting: "-",
        isoSetting: "-",
      }
    : getRecipe(selectedItem?.productName ?? selectedWorkOrder?.productName ?? "", season);
  const moldInfo = getMoldInfo(selectedItem?.moldName ?? "");

  const activeSameMoldCount = getActiveSameMoldCount(
    selectedItem?.moldName ?? "",
    activeAssignments
  );

  const estimate = getEstimate({
    remainingPairs: selectedItem?.remainingPairs ?? 0,
    activeMoldCount: activeSameMoldCount,
    dailyWorkHours,
  });

  const progress =
    selectedItem && selectedItem.quantityPairs > 0
      ? Math.round((selectedItem.producedPairs / selectedItem.quantityPairs) * 100)
      : 0;

  async function startJob() {
    const assignmentOrderItemId = selectedWorkOrder?.orderItemId ?? orderItemId;
    const assignmentPlannedPairs = selectedWorkOrder ? Number(plannedPairs) : 0;

    if (!station || !assignmentOrderItemId || !operatorName) {
      alert("İş emri veya sipariş kalemi ve operatör seçmelisin.");
      return;
    }

    if (selectedWorkOrder && (!Number.isFinite(assignmentPlannedPairs) || assignmentPlannedPairs <= 0)) {
      alert("İş emrine atanacak çift miktarı 0'dan büyük olmalıdır.");
      return;
    }

    if (selectedWorkOrder && assignmentPlannedPairs > selectedWorkOrder.remainingToAssignPairs) {
      alert("Atanacak çift miktarı iş emrinin kalan atanabilir miktarını aşamaz.");
      return;
    }

    setSaving(true);

    const response = await fetch(API + "/station-assignments/assign", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        stationNumber: station,
        orderItemId: assignmentOrderItemId,
        moldId: selectedItem?.moldId ?? null,
        workOrderId: selectedWorkOrder?.id ?? null,
        plannedPairs: assignmentPlannedPairs,
        operatorName: operatorName,
        note:
          "Reçete: " +
          recipe.code +
          " / Reçete Adı: " +
          recipe.name +
          " / Günlük Çalışma: " +
          dailyWorkHours +
          " saat" +
          " / Aktif Aynı Kalıp: " +
          activeSameMoldCount +
          " / Saatlik Kapasite: " +
          estimate.hourlyCapacity +
          " çift" +
          (note ? " / Not: " + note : ""),
      }),
    });

    setSaving(false);

    const text = await response.text();

console.log("STATUS:", response.status);
console.log("BODY:", text);

if (!response.ok) {
    alert(
        "HTTP: " +
        response.status +
        "\n\n" +
        (text || "Boş response")
    );
    return;
}

    onClose();
    window.location.reload();
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4 backdrop-blur-sm">
      <div className="max-h-[92vh] w-full max-w-5xl overflow-y-auto rounded-3xl border border-white/10 bg-[#0F1115] p-8 shadow-2xl">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <p className="text-sm font-bold text-emerald-400">FIXAR OS</p>
            <h2 className="mt-1 text-3xl font-black text-white">
              İstasyon {station} İş Atama
            </h2>
          </div>

          <button onClick={onClose} className="rounded-xl bg-zinc-800 px-4 py-2 text-white">
            Kapat
          </button>
        </div>

        <div className="mb-6 rounded-3xl border border-emerald-400/30 bg-emerald-400/10 p-5">
          <p className="text-xs font-bold text-emerald-300">AKTİF ATAMA ÖZETİ</p>
          <h3 className="mt-2 text-2xl font-black text-white">
            İSTASYON {station} · {selectedItem?.moldName ?? "KALIP"} ·{" "}
            {selectedItem?.productName ?? "ÜRÜN"}
          </h3>
          <p className="mt-2 text-sm font-bold text-emerald-300">
            {recipe.code} · {recipe.name}
          </p>
        </div>

        <div className="grid gap-5">
          <div className="rounded-2xl border border-emerald-400/20 bg-emerald-500/10 p-5">
            <Field label="WorkOrder ile Ata">
              <select
                value={workOrderId}
                onChange={(e) => {
                  const value = e.target.value;
                  const selected = workOrders.find((x) => x.id === value);
                  setWorkOrderId(value);
                  setOrderItemId(selected?.orderItemId ?? "");
                  setPlannedPairs(selected ? String(selected.remainingToAssignPairs) : "");
                }}
                className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
              >
                <option value="">Opsiyonel: iş emri seç</option>
                {workOrders.map((x) => (
                  <option key={x.id} value={x.id}>
                    {x.workOrderNumber} / {x.customerName ?? "-"} / {x.productName ?? "-"} / {x.remainingToAssignPairs.toLocaleString("tr-TR")} çift atanabilir
                  </option>
                ))}
              </select>
            </Field>
            {selectedWorkOrder && (
              <div className="mt-4 grid gap-3 text-sm md:grid-cols-4">
                <Info label="Müşteri" value={selectedWorkOrder.customerName ?? "-"} />
                <Info label="Ürün" value={selectedWorkOrder.productName ?? "-"} />
                <Info label="Reçete" value={[selectedWorkOrder.recipeCode, selectedWorkOrder.recipeName].filter(Boolean).join(" - ") || "-"} />
                <Field label="Atanacak Çift">
                  <input
                    type="number"
                    min={1}
                    max={selectedWorkOrder.remainingToAssignPairs}
                    value={plannedPairs}
                    onChange={(event) => setPlannedPairs(event.target.value)}
                    className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
                  />
                </Field>
              </div>
            )}
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <Field label="Müşteri">
              <select
                value={customerId}
                onChange={(e) => setCustomerId(e.target.value)}
                disabled={!!selectedWorkOrder}
                className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
              >
                <option value="">Seçiniz...</option>
                {customers.map((x) => (
                  <option key={x.id} value={x.id}>
                    {x.name}
                  </option>
                ))}
              </select>
            </Field>

            <Field label="Sipariş Kalemi">
              <select
                value={orderItemId}
                onChange={(e) => setOrderItemId(e.target.value)}
                disabled={!!selectedWorkOrder}
                className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
              >
                <option value="">Seçiniz...</option>
                {orderItems.map((x) => (
                  <option key={x.id} value={x.id}>
                    {x.label}
                  </option>
                ))}
              </select>
            </Field>

            <Field label="Günlük Çalışma">
              <select
                value={dailyWorkHours}
                onChange={(e) => setDailyWorkHours(Number(e.target.value))}
                className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
              >
                <option value={9}>Tek Vardiya - 9 saat</option>
                <option value={18}>Çift Vardiya - 18 saat</option>
              </select>
            </Field>
          </div>

          <Panel title="📦 Sipariş Bilgisi">
            <Info label="Sipariş No" value={selectedItem?.orderCode ?? "-"} />
            <Info label="Müşteri" value={selectedItem?.customerName ?? "-"} />
            <Info label="Ürün" value={selectedItem?.productName ?? "-"} />
            <Info label="Üretim Tipi" value={selectedItem?.productionType ?? "-"} />
            <Info label="Kumaş Rengi" value={selectedItem?.fabricColor ?? "-"} />
            <Info
              label="Sipariş Toplamı"
              value={
                selectedItem
                  ? selectedItem.orderTotalQuantity.toLocaleString("tr-TR") + " çift"
                  : "-"
              }
            />
            <Info
              label="Bu Kalıp Toplamı"
              value={
                selectedItem
                  ? selectedItem.quantityPairs.toLocaleString("tr-TR") + " çift"
                  : "-"
              }
            />
          </Panel>

          <Panel title="📊 Üretim Durumu">
            <Info
              label="Toplam"
              value={
                selectedItem
                  ? selectedItem.quantityPairs.toLocaleString("tr-TR") + " çift"
                  : "-"
              }
            />
            <Info
              label="Üretilen"
              value={
                selectedItem
                  ? selectedItem.producedPairs.toLocaleString("tr-TR") + " çift"
                  : "-"
              }
            />
            <Info
              label="Kalan"
              value={
                selectedItem
                  ? selectedItem.remainingPairs.toLocaleString("tr-TR") + " çift"
                  : "-"
              }
            />
            <Info label="İlerleme" value={selectedItem ? "%" + progress : "-"} />
            <Info label="Aktif Aynı Kalıp" value={selectedItem ? activeSameMoldCount + " adet" : "-"} />
            <Info label="Kalıp Başı Hedef" value={PAIRS_PER_MOLD_HOUR + " çift/saat"} />
            <Info label="Toplam Saatlik Kapasite" value={selectedItem ? estimate.hourlyCapacity + " çift/saat" : "-"} />
            <Info label="Tahmini Süre" value={estimate.duration} />
            <Info label="Tahmini Bitiş" value={estimate.finish} />

            <div className="col-span-2 md:col-span-3">
              <div className="mt-2 h-4 overflow-hidden rounded-full bg-zinc-800">
                <div
                  className="h-full rounded-full bg-emerald-500"
                  style={{ width: selectedItem ? progress + "%" : "0%" }}
                />
              </div>
            </div>
          </Panel>

          <Panel title="🧪 Üretim Reçetesi">
            <Info label="Reçete Adı" value={recipe.name} />
            <Info label="Reçete Kodu" value={recipe.code} />
            <Info label="Revizyon" value={recipe.revision} />
            <Info label="Hammadde Tipi" value={recipe.materialType} />
            <Info label="Poliol" value={recipe.polyol} />
            <Info label="İzosiyanat" value={recipe.iso} />
            <Info label="Katkı" value={recipe.additive} />
            <Info label="Poliol Ayarı" value={recipe.polyolSetting} />
            <Info label="İzo Ayarı" value={recipe.isoSetting} />
            <Info label="Kalıp Sıcaklığı" value="55 °C" />
            <Info label="Kalıp Açılma" value="360 sn" />
          </Panel>

          <Panel title="⚙️ Kalıp Bilgisi">
            <Info label="Kalıp" value={selectedItem?.moldName ?? "-"} />
            <Info label="X Koordinatı" value={moldInfo.x} />
            <Info label="Y Koordinatı" value={moldInfo.y} />
            <Info label="Döküm Gramajı" value={moldInfo.gram} />
            <Info label="Aktif Kalıp" value={selectedItem ? activeSameMoldCount + " adet" : "-"} />
            <Info
              label="Toplam Kapasite"
              value={selectedItem ? estimate.hourlyCapacity + " çift/saat" : "-"}
            />
          </Panel>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <Field label="Operatör">
              <select
                value={operatorName}
                onChange={(e) => setOperatorName(e.target.value)}
                className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
              >
                <option value="">Seçiniz...</option>
                {operators.map((x) => (
                  <option key={x.id} value={x.name}>
                    {x.name}
                  </option>
                ))}
              </select>
            </Field>

            <Field label="Not">
              <input
                value={note}
                onChange={(e) => setNote(e.target.value)}
                className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white"
                placeholder="İsteğe bağlı not"
              />
            </Field>
          </div>

          {operatorName && (
            <div className="flex items-center gap-4 rounded-2xl border border-white/10 bg-black/20 p-4">
              <div className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-500 text-2xl font-black text-black">
                {operatorName.substring(0, 1)}
              </div>
              <div>
                <p className="text-xs text-zinc-500">Seçili Operatör</p>
                <p className="text-xl font-black text-white">{operatorName}</p>
                <p className="text-sm text-emerald-300">1. Vardiya</p>
                <p className="text-xs text-zinc-400">PU Enjeksiyon Operatörü</p>
                <p className="text-xs text-zinc-400">Bugünkü Üretim: 1.240 çift</p>
                <p className="text-xs text-zinc-400">Verimlilik: %98</p>
              </div>
            </div>
          )}

          <div className="rounded-3xl border border-emerald-400/30 bg-emerald-400/10 p-5">
            <p className="text-xs font-bold text-emerald-300">BAŞLATMA ONAYI</p>
            <h3 className="mt-2 text-xl font-black text-white">
              Bu iş başlatıldığında İstasyon {station} üzerinde üretim açılacak.
            </h3>
            <div className="mt-4 grid grid-cols-2 gap-4 md:grid-cols-4">
              <Info label="Müşteri" value={selectedItem?.customerName ?? "-"} />
              <Info label="Kalıp" value={selectedItem?.moldName ?? "-"} />
              <Info label="Operatör" value={operatorName || "-"} />
              <Info label="Reçete" value={recipe.code} />
              <Info
                label="Kalan"
                value={
                  selectedItem
                    ? selectedItem.remainingPairs.toLocaleString("tr-TR") + " çift"
                    : "-"
                }
              />
              <Info label="Aktif Kalıp" value={selectedItem ? activeSameMoldCount + " adet" : "-"} />
              <Info label="Tahmini Süre" value={estimate.duration} />
              <Info label="Tahmini Bitiş" value={estimate.finish} />
            </div>
          </div>
        </div>

        <div className="sticky bottom-0 mt-8 flex justify-end gap-4 border-t border-white/10 bg-[#0F1115] pt-5">
          <button onClick={onClose} className="rounded-xl bg-zinc-700 px-6 py-3 font-bold text-white">
            İptal
          </button>

          <button onClick={startJob} disabled={saving} className="rounded-xl bg-emerald-500 px-6 py-3 font-bold text-black hover:bg-emerald-400 disabled:opacity-50">
            {saving ? "Başlatılıyor..." : "İşi Başlat"}
          </button>
        </div>
      </div>
    </div>
  );
}

function getCurrentSeason() {
  const month = new Date().getMonth() + 1;
  if (month === 12 || month === 1 || month === 2 || month === 3) {
    return "KIŞ";
  }
  return "YAZ";
}

function getActiveSameMoldCount(moldName: string, assignments: ActiveAssignment[]) {
  if (!moldName) return 1;

  const activeCount = assignments.filter((x) => {
    const activeMoldName = x.moldName ?? "";
    const status = x.status ?? "";
    return activeMoldName === moldName && status === "Üretimde";
  }).length;

  return Math.max(1, activeCount + 1);
}

function getRecipe(productName: string, season: string) {
  const isVisco =
    productName.toLowerCase().includes("10900") ||
    productName.toLowerCase().includes("memory") ||
    productName.toLowerCase().includes("comfy");

  if (isVisco) {
    return {
      name: "10900 VISCO PU - " + season,
      code: season === "KIŞ" ? "RCP-10900-W-01" : "RCP-10900-S-01",
      revision: "R3",
      materialType: "10900 Visco PU",
      polyol: "Kimfoot AA 10900 MF Poliol",
      iso: "İzokim AA 46400 MF İzo",
      additive: "Crosskim AA 036 FS",
      polyolSetting: season === "KIŞ" ? "42.50" : "41.80",
      isoSetting: season === "KIŞ" ? "38.20" : "37.70",
    };
  }

  return {
    name: "10100 PU - " + season,
    code: season === "KIŞ" ? "RCP-10100-W-01" : "RCP-10100-S-01",
    revision: "R1",
    materialType: "10100 PU",
    polyol: "10100 Poliol",
    iso: "İzokim AA 46400 PU İzo",
    additive: "Crosskim AA 036 FS",
    polyolSetting: season === "KIŞ" ? "40.00" : "39.50",
    isoSetting: season === "KIŞ" ? "36.50" : "36.00",
  };
}

function getMoldInfo(moldName: string) {
  if (moldName.includes("ICE")) {
    return { x: "125", y: "88", gram: "72 g/çift" };
  }

  if (moldName.includes("CL")) {
    return { x: "118", y: "82", gram: "68 g/çift" };
  }

  return { x: "-", y: "-", gram: "-" };
}

function getEstimate({
  remainingPairs,
  activeMoldCount,
  dailyWorkHours,
}: {
  remainingPairs: number;
  activeMoldCount: number;
  dailyWorkHours: number;
}) {
  if (!remainingPairs || !activeMoldCount || !dailyWorkHours) {
    return { duration: "-", finish: "-", hourlyCapacity: "-" };
  }

  const hourlyCapacity = activeMoldCount * PAIRS_PER_MOLD_HOUR;
  const totalHours = remainingPairs / hourlyCapacity;
  const workDays = Math.floor(totalHours / dailyWorkHours);
  const remainingHours = Math.round(totalHours % dailyWorkHours);

  const finishDate = new Date();
  finishDate.setHours(finishDate.getHours() + workDays * 24 + remainingHours);

  return {
    duration: workDays + " iş günü " + remainingHours + " saat",
    finish: finishDate.toLocaleString("tr-TR"),
    hourlyCapacity: hourlyCapacity.toLocaleString("tr-TR"),
  };
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <label className="mb-2 block text-sm font-bold text-zinc-300">{label}</label>
      {children}
    </div>
  );
}

function Panel({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
      <h3 className="mb-4 text-lg font-black text-emerald-300">{title}</h3>
      <div className="grid grid-cols-2 gap-4 md:grid-cols-3">{children}</div>
    </div>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-zinc-500">{label}</p>
      <p className="text-sm font-bold text-white">{value}</p>
    </div>
  );
}
