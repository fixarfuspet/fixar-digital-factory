# Finance, Cash and Ledger Architecture Audit — 2026-07-22

## Mevcut yapı

FIXAR OS tek bir finans omurgasına sahiptir ve bu korunacaktır:

- `FinancialAccount` kasa/banka/POS kartını; bakiye, `OpeningBalance + posted FinancialTransaction` üzerinden hesaplanır.
- `FinancialTransaction` kasa hareketinin kaynak gerçeğidir. Müşteri tahsilatı, tedarikçi ödemesi, çek tahsilatı, transfer ve manuel hareketler bu tabloyu kullanır.
- `CustomerReceivable -> CollectionAllocation -> CustomerCollection` müşteri açık bakiyesini; `CustomerLedgerEntry` ekstresini oluşturur.
- `SupplierPayable -> SupplierPaymentAllocation -> SupplierPayment` tedarikçi açık borcunu; `SupplierLedgerEntry` ekstresini oluşturur.
- `CustomerCheque`, `ChequeEvent` ve `ChequeEndorsement` fiziksel çek yaşam döngüsünü aynı ödeme zincirine bağlar.
- Purchase completion, tekil `PurchaseOrderId` ile SupplierPayable; satış siparişi, tekil `OrderId` ile CustomerReceivable üretir. Backfill yalnız açık kullanıcı seçimiyle çalışır.
- Cash-flow ve liquidity raporları `FinancialTransaction` üzerinden üretilir; profitability ayrı gerçek maliyet snapshotlarını kullanır.
- Kritik controller işlemleri EF transaction, idempotency filter, advisory number lock ve audit kullanır.

## Korunacak yapılar

Mevcut entity, endpoint, durum ve reversal zincirleri kaldırılmayacak; paralel muhasebe defteri oluşturulmayacaktır. Cari bakiye Customer/Supplier ana kartına yazılan değişken bir kolon değil, receivable/payable ve allocation toplamından türetilmeye devam edecektir. Kasa bakiyesi yalnız FinancialTransaction hareketlerinden türetilecektir.

## Kök eksikler ve riskler

1. Tahsilat/ödeme oluşturma, post ve allocation ayrı HTTP/transaction adımlarıdır. Ara adımda posted fakat dağıtılmamış kayıt kalabilir. Tek çağrılı atomik akış eksiktir.
2. Manual expense/income hareketinde kategori, supplier, ödeme yöntemi, belge ve kur snapshotı yoktur.
3. FinancialTransaction yalnız Customer bağlantısı taşır; Supplier/Purchase/Order/Shipment bağlantıları raporda izlenemez.
4. Kategori master ve alt kategori/gider merkezi/maliyete dahil/sabit-değişken bilgisi yoktur.
5. Dövizli hareketler account currency tutarıyla sınırlıdır; raporlama para birimi, snapshot kur ve reporting amount saklanmaz.
6. Transfer ve manuel çıkışta kullanılabilir bakiye/yetersiz kasa politikası uygulanmaz.
7. Açılış bakiyesi kart alanıdır; ayrı auditli finans hareketi değildir.
8. CSV export vardır; gerçek XLSX/PDF, snapshot mutabakat kaydı ve değişmez mutabakat belgesi yoktur.
9. FinancialTransaction kaynak+referans tekilliği DB constraint ile korunmuyor. Idempotency HTTP seviyesinde olsa da farklı anahtarla aynı ticari referans tekrar edebilir.
10. Supplier payment controller audit kapsamı müşteri akışından zayıftır; reversal zaman/kullanıcı/financial transaction durumu eksik kalabilir.
11. Kasa detayında running balance, bugünkü/aylık giriş-çıkış ve karşı taraf bilgisi tek response içinde yoktur.
12. Müşteri/tedarikçi detay ekranları finans, sipariş/satın alma, ürün/lot/sevkiyat bağlantılarını tek scope edilmiş endpointte birleştirmiyor.

## Çözüm yönü

- FinancialTransaction genişletilecek: supplier, category, purchase/order/shipment, ödeme yöntemi, belge, karşı taraf, kur snapshotı, reporting amount, idempotent business reference ve `AffectsBalance`.
- Yönetilebilir, pasifleştirilebilir hiyerarşik `FinanceCategory` eklenecek; fiziksel delete endpointi olmayacak.
- Değişmez müşteri/tedarikçi `AccountReconciliation` snapshotı eklenecek.
- Tahsilat ve ödeme için create+post+allocation aynı serializable transaction içinde çalışan atomik endpointler eklenecek; eski endpointler uyumluluk için kalacak.
- Kasa çıkışı önce aynı para birimindeki kullanılabilir bakiyeyi kontrol edecek. Negatif istisna yalnız CEO policy ve zorunlu gerekçeyle ayrı explicit alan üzerinden kabul edilecek.
- Purchase/Order kök tekillikleri korunacak; shipment seviyesinde receivable üretimi ancak mevcut shipment sözleşmesinde fiyat/fatura olayı kesinleşirse açılacak. Şimdilik Order receivable bağlantısı bozulmayacak ve shipment bilgisi raporda OrderItems üzerinden gösterilecek.
- XLSX ve PDF exportlar sunucu tarafında yetkili, tarih aralıklı ve cari kimliğine scope edilmiş üretilecek.

## Migration ihtiyacı

Yeni migration gereklidir: `FinanceCategories`, `AccountReconciliations`, geniş FinancialTransactions alanları ve performans/tekillik indexleri. Eski migrationlar değiştirilmeyecek. Yeni nullable bağlantılar mevcut veriyi bozmaz. Seed edilen kategori kodları migration içinde deterministik olacaktır.

## Veri bütünlüğü kapıları

- Finans ve cari kayıtlarında physical delete yok; iptal/reversal zorunlu.
- Para birimleri snapshot kur olmadan birleştirilmeyecek.
- Aynı source type/source id/business reference için aktif finans hareketi tekil olacak.
- Allocation customer/supplier ve currency sınırını aşamayacak.
- Reversal ikinci kez çalışmayacak; original ve reverse birbirine bağlanacak.
- Bütün kritik değişiklikler tek transaction, audit ve idempotency altında olacaktır.
- Mevcut veriler otomatik backfill edilmeyecek; preview ve açık seçim gerekecektir.
