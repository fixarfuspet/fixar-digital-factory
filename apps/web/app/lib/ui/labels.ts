const UI_LABELS: Record<string, string> = {
  Draft: "Taslak", Confirmed: "Onaylandı", InProduction: "Üretimde",
  Sent: "Gönderildi", Converted: "Siparişe Dönüştürüldü",
  PartiallyCompleted: "Kısmen Tamamlandı", Completed: "Tamamlandı", Cancelled: "İptal Edildi",
  Active: "Aktif", Inactive: "Pasif", Available: "Kullanılabilir",
  PartiallyUsed: "Kısmen Kullanıldı", FullyUsed: "Tükendi", Depleted: "Tükendi",
  Blocked: "Blokeli", Expired: "Süresi Doldu", Pending: "Bekliyor",
  Approved: "Onaylandı", Conditional: "Koşullu", Rejected: "Reddedildi",
  Sealed: "Kapalı", Open: "Açık", Empty: "Boş", Damaged: "Hasarlı",
  Drum: "Varil", IBC: "IBC Tank", Can: "Teneke", Bag: "Çuval", Box: "Kutu",
  Roll: "Rulo", Other: "Diğer", Production: "Üretim", Setup: "Kurulum",
  Sample: "Numune", Waste: "Fire / Atık", Correction: "Düzeltme",
  Inflow: "Giriş", Outflow: "Çıkış", Cash: "Nakit",
  BankTransfer: "Banka Havalesi", CreditCard: "Kredi Kartı", Cheque: "Çek",
  CustomerPayment: "Müşteri Tahsilatı", SupplierPayment: "Tedarikçi Ödemesi",
  Manual: "Manuel", Receipt: "Tahsilat", Payment: "Ödeme",
  Low: "Düşük", Medium: "Orta", High: "Yüksek", Critical: "Kritik",
  New: "Yeni", Assigned: "Atandı", OnHold: "Beklemede", Closed: "Kapalı",
};

export function uiLabel(value: string | null | undefined): string {
  if (!value) return "-";
  return UI_LABELS[value] ?? value.replace(/([a-z0-9])([A-Z])/g, "$1 $2");
}
