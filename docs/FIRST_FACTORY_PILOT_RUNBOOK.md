# First Factory Pilot Runbook

## Kapsam ve durdurma yetkisi

Tek onaylı müşteri, tek ürün ailesi, en fazla iki numara, tek aktif reçete, en fazla iki kalıp/istasyon, bir vardiya, kısmi sevkiyat ve kısmi tahsilat. Pilot kodu `PILOT-YYYYMMDD-01` tüm referans/not alanlarında kullanılır. Üretim Müdürü, Kalite veya Finans herhangi bir tutarsızlıkta pilotu durdurabilir.

Başlamadan önce backup/restore kanıtı, health, 24 istasyon envanteri, pilot lotları, reçete versiyonu, kullanıcı rolleri ve geri dönüş sorumluları onaylanır.

| # | Kullanıcı / ekran | Girilecek veri | Beklenen sonuç / DB etkisi | Endpoint kontrolü | Başarısızlık ve geri alma |
|---:|---|---|---|---|---|
| 1 | Satış / Customers | Tek pilot müşteri | Customer aktif/tekil | `GET /api/v1/customers` | Mükerrer/vergi uyumsuzsa dur; kullanılmamış kartı pasifleştir |
| 2 | Satış / Quotes | Ürün, miktar, fiyat, vade | Draft Quote + items/totals | `/api/v1/quotes` | Toplam/kur yanlışsa draft düzelt |
| 3 | Satış / Quote detail | Onay/dönüştür | Tek Order oluşur | `/quotes/{id}/convert-to-order` | Çift dönüşümde dur; idempotency/audit incele |
| 4 | Satış / Orders | Numara/renk/termin | OrderItem toplamı siparişle eşit | `/api/v1/orders/{id}` | Miktar uyumsuzsa planlamaya geçme |
| 5 | Planlama / Work Orders | Reçete, planlanan çift, tarih | WorkOrder draft/planned | `/api/v1/work-orders` | Yanlış reçetede iptal/pasifleştir |
| 6 | Üretim / Reservations | İş emri için reserve | FIFO lot/container tahsisi; eksi stok yok | `/api/v1/stock-reservations` | Eksik stokta üretimi başlatma; rezervasyonu bırak |
| 7 | Üretim / Station assignment | En fazla 2 istasyon/kalıp/operator | Active assignment tekil | `/api/v1/station-assignments` | Çift aktif atamada dur; yanlış atamayı bitir/iptal et |
| 8 | Enjeksiyon / Live production | Turn ve iyi/fire çift | Sayaçlar atomik artar | `/station-assignments/{id}/turns` | 409/quantity farkında tekrar gönderme; idempotency kontrol |
| 9 | Depo/üretim / Consumption | Gerçek tüketim | Lot/container/StockItem aynı azalır | `/api/v1/material-consumptions` | Negatif/çift harekette dur; yetkili reversal |
| 10 | Operatör / Production Box | Kutu QR, çift miktarı | Tekil traceability code | `/api/v1/production-boxes` | QR/miktar yoksa kutuyu fiziksel ayır |
| 11 | Kesim / Cutting | Kutu, iyi/fire miktarı | CutQuantity kontrollü artar | `/api/v1/cutting-records` | Kutu toplamı aşılırsa kayıt yapma/reversal prosedürü |
| 12 | Operatör / DTF | Gerekiyorsa etiket onayı | Ürün gereksinimi kaydedilir | Traceability detail | Yanlış etiketi karantinaya al |
| 13 | Depo / Packaging | Kutu/paket adedi | Paketlenmiş miktar izlenebilir | Production box detail | Eksik QR'da depoya kabul etme |
| 14 | Depo / Warehouse | Lokasyon ve kabul | Hazır stok/lokasyon görünür | `/api/v1/stocks` | Fiziksel-sistem farkında hareketi durdur |
| 15 | Depo / Shipment | Toplamdan az sevk | Partial shipment; ShippedQuantity artar | shipment endpoint/order detail | Fazla sevk reddedilmeli; belgeyi iptal/reverse et |
| 16 | Finans / Receivables | Sevk/sipariş alacağı | Müşteri alacağı ve vade oluşur | `/api/v1/customer-receivables` | Tutar/kur farkında tahsilat girme |
| 17 | Finans / Collections | Alacaktan düşük tahsilat | Kısmi allocation; bakiye kalır | `/api/v1/customer-collections` | Yanlış hesapta yetkili reversal |
| 18 | CEO/Finans / Profitability | Pilot order filtre | Gelir, gerçek maliyet, marj | profitability endpoint | Eksik maliyet satırında sonucu onaylama |
| 19 | Kalite/CEO / Traceability | QR/order/lot arama | Customer→lot→box→shipment zinciri | `/api/v1/traceability` | Kopuk zincirde pilot NO-GO; kayıtları değiştirmeden incele |

## Kabul kriterleri

- Ana miktar eşitlikleri ve stok/lot/container toplamları tutarlı; negatif yok.
- Aynı komut tekrarı çift finans/stok/üretim hareketi oluşturmuyor.
- Kısmi sevkiyat ve tahsilat sonrası kalan miktar/bakiye doğru.
- Her kritik işlem audit/correlation ile izlenebilir.
- Traceability zinciri uçtan uca tamam; restore edilebilir pilot-sonu backup alınmış.

Tek bir veri bütünlüğü, yetkisiz erişim, negatif stok, çift kayıt veya izlenebilirlik kopukluğu pilotu durdurur. Hatalı kayıt doğrudan SQL ile düzeltilmez; mevcut reversal/cancel akışları kullanılır.
