# Test Data Reset Preview — 2026-07-22

Bu rapor yalnız `fixar_restore_test` kopyasında salt okunur sorgularla üretildi. `fixar_os_dev` ve production değiştirilmedi. Gerçek silme yapılmadı.

Kural: Kod/numara, ad, operatör veya açıklama alanında açık `TEST` işareti bulunmayan hiçbir kayıt aday değildir. Bağımlı kayıtlar otomatik silinmez; kök kayıtla birlikte ilişki sırası ayrıca onaylanmalıdır.

| Tablo | Aday | Örnek | Bağımlı kayıt | Karar |
|---|---:|---|---|---|
| Customers | 1 | `CUS-000003` (adı TEST) | Orders 2, Quotes 0, Receivables 0 | İncele; müşteri ve siparişler birlikte onaylanmadan silme |
| Products | 0 | — | — | Koru |
| Materials | 4 | `KP-10900` (adı TEST) | RecipeItems 3, Lots 6, Consumptions 1 | İncele; stok/tüketim mutabakatı olmadan silme |
| Recipes | 6 | `TEST-RCP-79522033` | RecipeItems 6, WorkOrders 4 | İncele; iş emri geçmişi nedeniyle varsayılan koru |
| Molds | 0 | — | — | Koru |
| Quotes | 0 | — | — | Koru |
| Orders | 0 doğrudan işaretli | — | TEST müşteriye bağlı 2 | Bağımlı aday; iş sahibi onayı gerekir |
| WorkOrders | 0 doğrudan işaretli | — | TEST reçeteye bağlı 4 | Geçmiş bütünlüğü için koru |
| StationAssignments | 1 | `TEST OPERATOR` | Events 2, Fires 0, Downtimes 0 | İncele; üretim geçmişiyle birlikte değerlendirilir |
| ProductionRecords | 0 | — | — | Koru |
| StockMovements | 4 | `TEST-001` | StockItem/lot etkisi olabilir | Mutabakat olmadan silme |
| FinancialTransactions | 0 | — | — | Koru |
| Customer collections/receivables | 0 açık TEST | — | — | Koru |
| Supplier payables/payments | 0 açık TEST | — | — | Koru |

## Kesin korunacak sistem kayıtları

- Users, Roles, UserRoles ve izin politikaları
- InjectionStations, ProductionStations, Machines ve CuttingMachines
- `__EFMigrationsHistory`
- AuditLogs
- Açık TEST işareti olmayan tüm gerçek müşteri, ürün, malzeme, stok ve finans kayıtları

## Sonuç

Dry-run güvenilir şekilde aday üretiyor fakat bağımlı üretim, lot, tüketim ve stok hareketleri bulunduğu için bu görevde fiziksel temizlik güvenli değildir. Temizlik komutu fail-closed kalacak; açık kullanıcı onayı, doğrulanmış backup ve ikinci production onayı olmadan apply yapılmayacaktır.
