# Migration and Rollback Test

## Dağıtım kapısı

1. Maintenance mode açılır ve yeni yazma işlemleri durdurulur.
2. `backup-postgres.sh` çalıştırılır; manifest ve checksum doğrulanır.
3. Backup ayrı `fixar_restore_test` veritabanına restore edilip doğrulanır.
4. Uygulama sürümü ve migration SQL hash'i kaydedilir.
5. Idempotent migration SQL önce kopyada, ardından hedefte tek transaction sınırlarıyla uygulanır.
6. `/health/ready`, login ve kritik smoke geçmeden maintenance mode kapatılmaz.

## Karar ağacı

- Migration uygulanmadıysa: yeni uygulama sürümünü devreye alma; eski sürümü koru.
- Migration başarılı, uygulama health başarısızsa: eski uygulama sürümünü dene. Yeni şema geriye uyumluysa DB korunur.
- Veri doğrulaması başarısızsa veya migration kısmen uygulanmışsa: yazmaları kapalı tut, hedef DB'yi ayır, doğrulanmış backup'ı yeni veritabanına restore et ve bağlantıyı atomik olarak geri çevir.
- EF `Down` yalnız veri kaybı oluşturmadığı ayrıca kanıtlanmış migrationlarda kullanılır. Drop/column daraltma içeren rollback production üzerinde körlemesine çalıştırılmaz.

## 2026-07-22 kontrollü senaryo

Development veritabanının `pg_dump` kopyası `fixar_restore_test` olarak geri yüklendi. Kopyada migration sayısı 35'ten 37'ye çıkarıldı; Customers 3, Products 4, Orders 4 ve StockItems 22 olarak korundu. Aynı backup'tan yeniden restore, veritabanı geri dönüş mekanizmasının kontrollü provasıdır. Restore sonrası live ve ready health 200/Healthy döndü.
