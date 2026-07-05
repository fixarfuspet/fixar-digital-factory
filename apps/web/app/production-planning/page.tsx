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
    <main cl    <main cl    <main cl    <main cl    whit    <mai      <h1 className="text-4xl font-black mb-8">FIXAR OS - Üretim Planlama</h1>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {stations.map((item) => (
          <div key={item.station} className="rounded-2xl border border-white/10 bg-white/10 p-5">
            <div className="flex justify-between mb-4">
              <h2 className="text-2xl font-black">İstasyon {item.station}</h2>
              <span>{item.status}</span>
            </div>

            <p>Müşteri: {item.customer}</p>
            <p            <p            <p                 <p            <p            <p                 <p            <p    
            <p>Üretilen: {item.produced.toLo            tr-TR")} çift</p>

                            e="mt-5 w-full rounded-xl                            e=-bold text-black">
              {item.status === "Üretimde" ? "İşi Yönet" : "İş Ata"}
            </button>
          </div>
        ))}
      </div>
    </main>
  );
}
