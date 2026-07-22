# Controlled Go-Live Readiness — 2026-07-22

## 1. Genel sonuç

Kod ve operasyon paketi kontrollü ilk pilot için hazırlandı. Production veritabanına bağlanılmadı, gerçek test verisi silinmedi ve push yapılmadı. Teknik P0 yoktur; gerçek production sunucusunda TLS, secret, compose ve zamanlayıcı kurulumu henüz yapılmadığından **pilot provası GO, gerçek fabrikadaki ilk canlı pilot CONDITIONAL GO** kararı verilmiştir.

## 2. Production environment

API/web production env örnekleri, web image, API+web+PostgreSQL+Nginx compose, TLS reverse proxy, internal database network, restart policy, log rotation, start/stop/restart/deploy/rollback/health scriptleri hazırdır. Deploy varsayılan dry-run; doğrulanmış backup ve incelenmiş migration SQL olmadan execute olmaz. Docker bu workstation'da bulunmadığı için gerçek `docker compose config`/container start çalıştırılmadı; YAML parser doğrulaması geçti.

## 3. Backup otomasyonu

Günlük custom pg_dump, SHA-256 manifest, disk eşiği, 35 günlük retention, tarihli log, non-zero failure, harici mount ve son başarılı backup kontrolü hazırdır. macOS launchd ve Linux systemd timer örnekleri eklendi. Mevcut 454.248 byte test backup'ı checksum/yaş kontrolünden geçti.

## 4. Test verisi dry-run

Yalnız `fixar_restore_test` üzerinde: Customers 1, Materials 4, Recipes 6, StationAssignments 1, StockMovements 4; Products/Molds/Quotes/direct Orders/direct WorkOrders/FinancialTransactions 0. Dry-run hiçbir değişiklik yapmadı.

## 5. Korunacak kayıtlar

Users, Roles, izinler, 24 InjectionStations (1–24 tekil), Machines, ProductionStations, migration geçmişi, AuditLogs ve açık TEST işareti olmayan tüm kayıtlar korunur. Tarihsel iş emri, lot, consumption, finance, production ve stock movement kayıtları otomatik silinmez.

## 6. Temizlenecek test kayıtları

Yalnız açık TEST işaretli ve tarihsel bağımlılığı olmayan müşteri/malzeme/reçete master kayıtları adaydır. Mevcut adaylarda 2 müşteri siparişi, 3 material recipe item, 6 lot, 1 consumption, 4 recipe work order ve station events bulunduğundan gerçek temizlik bu görevde uygulanmadı.

## 7. Ana veri girişi

20 aşamalı çift-onay checklist ve customer/product/material/mold/supplier/opening-stock CSV şablonları hazırdır. UTF-8, ISO tarih, nokta ondalık, staging dry-run, SHA-256 ve açılış mutabakatı kuralları tanımlandı.

## 8. Pilot sipariş

Customer→Quote→Order→WorkOrder→Reservation→Station→Turn→Consumption→Box→Cutting→DTF→Packaging→Warehouse→Partial Shipment→Receivable→Partial Collection→Profitability→Traceability akışı, rol/ekran/veri/DB etkisi/endpoint/fail/rollback kriterleriyle hazırdır. Sonuç ve miktar mutabakat formu eklendi.

## 9. Kullanıcı kılavuzları

CEO, Üretim Müdürü, Enjeksiyon, Kesim, Depo, Finans ve Bakım için giriş, ekran, günlük işlem, hata, düzeltme, yasak işlem ve çıkış kılavuzları hazırdır.

## 10. Günlük kontrol listeleri

Sabah health/backup/disk/login/24 istasyon/iş emri/stok/bakım/finans; akşam üretim/fire/kesim/paket/sevkiyat/stok/cari/hata/bakım/backup mutabakat listeleri hazırdır.

## 11. Monitoring

Script tabanlı frontend, API live/ready/database, disk, backup yaşı/checksum, 5xx, login failure ve kritik job kontrolü JSON üretir. Migration otomatik uygulanmaz; deploy kapısı ve `/system-control` ile izlenir. Monitoring dry-run geçti.

## 12. Test sonuçları

- Backend test: 72/72
- Backend build: başarılı
- TypeScript: başarılı
- Full lint: 0 hata / 0 uyarı
- Frontend production build: başarılı
- Route smoke: 53/53
- npm audit: 0 bulgu
- Compose YAML: parser doğrulaması başarılı; Docker executable yok
- Shell scriptler: `bash -n` başarılı; shellcheck executable yok
- launchd plist: başarılı
- Deploy/rollback/health/monitor: dry-run başarılı
- Test reset: restore DB dry-run başarılı, `NO_CHANGES`
- `git diff --check`: başarılı

## 13. Açık P0

Yok.

## 14. Açık P1

- Hedef production hostunda Docker compose config/build/up ve TLS sertifika testi çalıştırılmalı.
- Gerçek secretlar secret manager/0600 env ile sağlanmalı.
- Backup timer kurulup ilk otomatik çalışma ve harici hedef kanıtlanmalı.
- TEST bağımlılıklarının hangisinin gerçekten silineceği iş sahiplerince imzalanmalı.
- İlk pilot kullanıcı eğitimi ve fiziksel ana veri sayımı tamamlanmalı.

## 15. İlk pilot kararı

**CONDITIONAL GO.** Pilot provası yapılabilir. Gerçek üretim pilotu; P1'deki host/TLS/secret/backup timer, ana veri ve kullanıcı imzaları tamamlanıp açılış checklist'i geçince GO olur.

## 16. Gerçek temizleme komutu

Bu görevde çalıştırılmadı:

```bash
FIXAR_PG_USER=fixar_operator FIXAR_PG_DATABASE=fixar_os \
FIXAR_ALLOW_PRODUCTION_DATABASE=true FIXAR_BACKUP_FILE=/secure/verified.dump \
FIXAR_ENVIRONMENT=Production FIXAR_PRODUCTION_RESET_APPROVAL=DELETE-CONFIRMED-TEST-DATA \
scripts/test-data-reset.sh --confirm-test-data-reset --confirm-production-test-data-reset
```

Bağımlı tarihsel kayıt bulunduğunda transaction rollback olur.

## 17. Production deploy komutu

```bash
FIXAR_ENV_FILE=$PWD/.env.production FIXAR_VERIFIED_BACKUP=/secure/verified.dump \
FIXAR_MIGRATION_SQL=/secure/reviewed-migrations.sql scripts/deploy-production.sh --execute
```

## 18. Rollback komutu

```bash
FIXAR_ENV_FILE=$PWD/.env.production FIXAR_CONFIRM_ROLLBACK=ROLLBACK \
FIXAR_PREVIOUS_RELEASE=<git-sha> scripts/rollback-production.sh --execute
```

DB veri doğrulaması bozuksa uygulama rollback'i yerine maintenance altında doğrulanmış backup yeni DB'ye restore edilip bağlantı atomik çevrilir.

## 19. Commit listesi

- `0461205` Prepare test data reset preview
- `c050187` Prepare production runtime and deployment controls
- `0e070fe` Automate production backup monitoring
- `e6bd31d` Add master data go-live checklist and templates
- `7a7ae41` Prepare first factory pilot runbook
- `63e3e03` Add role based operating guides
- `22fbcaa` Add daily factory operating checklists
- `30e13c4` Add production health monitoring
- `22c15c0` Finalize safe test data reset controls
- `34d3742` Add production health check dry run
- Bu rapor: takip commit'i

## 20. Push

Push yapılmadı.
