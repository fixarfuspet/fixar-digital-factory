# FIXAR OS Sistem Denetim Özeti

Tarih: 21 Temmuz 2026  
Branch: `fix/live-production-integration`

## Sistem haritası

- Frontend: Next.js App Router üzerinde 56 uygulama rotası (Sistem Kontrol Merkezi dahil).
- Backend: ASP.NET Core API, EF Core/PostgreSQL, JWT + refresh cookie, rol politikaları ve idempotency filtresi.
- Ana üretim kaynağı: `Customer → Order → OrderItem → WorkOrder → StationAssignment → InjectionStation`.
- Stok zinciri: `Material → StockItem → MaterialLot → MaterialContainer → StockReservation → MaterialConsumption`.
- Finans zinciri: müşteri alacağı/tahsilatı/cari/finans hesabı/çek ile tedarikçi borcu/ödemesi/cari/çek cirosu.
- Sonraki operasyonlar: üretim kolisi, kesim, mamul depo, sevkiyat ve izlenebilirlik.
- Bakım: varlık, talep, iş emri, periyodik plan, checklist ve yedek parça tüketimi.

## Doğrulanan güçlü yönler

- Kritik stok tüketimi, geri alma, bakım parçası post/geri alma ve finans post işlemlerinin önemli bölümü transaction ve idempotency korumasına sahip.
- Lot giriş/güncelleme ile ana stok senkronizasyonu mevcut.
- Container lot içi ambalaj dağılımı olarak tutuluyor; oluşturma toplam stoğu artırmıyor.
- Sipariş, iş emri, canlı üretim, kesim, koli, depo, sevkiyat, cari ve kârlılık için gerçek API/controller ve frontend ekranları bulunuyor.
- Operasyonel yetkiler yalnız menüde değil backend authorization policy katmanında da uygulanıyor.

## Kritik açıklar

1. Teklif domain modeli, API’si, migration’ı ve frontend ekranı bulunmuyor. Tekliften siparişe dönüşüm zinciri bu nedenle mevcut değil.
2. Ayrı bir DTF operasyon modeli/kuyruğu bulunmuyor; ürün üzerinde yalnız `HasDTFLabel` bilgisi var.
3. Otomatik test projesi bulunmuyor. Belirtilen 22 kritik senaryo otomasyonla güvence altında değil.
4. Üretim kasası kavramı mevcut `ProductionBox` modeliyle kısmen karşılanıyor; istenen “doluyor/kesime hazır/DTF” durum zinciri tam değil.
5. Bazı frontend sayfaları teknik enum değerlerini doğrudan gösteriyor ve ortak hata/yükleme bileşenlerini kullanmıyor.
6. Controller dosyalarının bir bölümü aşırı yoğun ve servis katmanına ayrılmamış; bu durum test edilebilirliği ve transaction incelemesini zorlaştırıyor.
7. Eski verilerde üretim-kesim-koli-finans ilişkilerinin eksik olma ihtimali var; sahte veriyle doldurulmamalı.

## Bu aşamada eklenen güvenlik ağı

CEO’ya özel, salt-okunur Sistem Kontrol Merkezi aşağıdaki kontrolleri raporlar:

- Bekleyen migration
- Negatif stok
- Ana stok / aktif lot farkı
- Lot / ambalaj farkı
- Stoğu aşan rezervasyon
- Tükenmiş aktif lot
- Açık ve sıfır miktarlı ambalaj
- Malzeme / stok bağlantı açıkları
- Birim uyumsuzluğu
- Reçetesiz, kalıpsız veya üretim ana verisi eksik ürün
- Atamasız iş emri ve hedef aşımı
- Koliye aktarılmamış kesim
- Sevk referansı olmayan koli
- Cari kaydı olmayan onaylı sipariş
- Finans hesabına işlenmemiş müşteri/tedarikçi ödemesi
- Gecikmiş bakım planı

Kontrol merkezi otomatik veri düzeltmez; böylece canlı veriye zarar vermeden üretim öncesi riskleri görünür kılar.
