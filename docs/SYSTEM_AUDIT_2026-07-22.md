# FIXAR Digital Factory sistem denetimi — başlangıç envanteri

Tarih: 22 Temmuz 2026  
Branch: `fix/live-production-integration`

## Envanter

- Backend: 48 controller dosyası; ASP.NET Core, EF Core ve PostgreSQL.
- Frontend: 53 `page.tsx`, production build çıktısında 57 route.
- Domain: 53 C# kaynak dosyası.
- Migration: 34 ana migration ve güncel model snapshot.
- Otomatik test: tek test projesinde 24 test.
- Ana üretim zinciri: `Customer → Quote → Order → OrderItem → WorkOrder → StationAssignment → InjectionStation → add-turn → ProductionBox`.
- Stok zinciri: `Material → StockItem → MaterialLot → MaterialContainer → StockReservation → MaterialConsumption`.

## Başlangıç doğrulaması

| Kontrol | Sonuç |
|---|---|
| Backend solution build | Başarılı, 0 hata / 0 uyarı |
| Backend testleri | 24/24 başarılı |
| TypeScript | Başarılı |
| Frontend production build | Başarılı, 57/57 route üretildi |
| Full frontend lint | Başarısız: başlangıçta 54 hata / 23 uyarı |
| Yanlış `/api/backend/v1/...` yolu | Bulunmadı |
| Native response `.json()` | Güvenli parser'a taşındı |
| Boş public API env fallback | Düzeltildi |

## Önem derecesi

### Critical — bu aşamada düzeltildi

1. Boş tanımlanmış `NEXT_PUBLIC_API_BASE_URL` / `NEXT_PUBLIC_API_URL` değerleri proxy fallback'ini devre dışı bırakarak tarayıcı isteklerini köksüz veya hatalı endpointlere gönderebiliyordu.
2. Çok sayıda ekranda native `response.json()` boş 200, 204 veya JSON olmayan cevapta render akışını kırabiliyordu.
3. Login, session ve refresh akışları JSON olmayan backend cevabını güvenli biçimde ele almıyordu.
4. Bakım ve Sistem Kontrol istemcileri ortak güvenli response katmanını kullanmıyordu.

### High — sonraki aşamalarda ele alınacak

1. Full lint 54 hata ve 23 uyarıyla başarısız; yoğunluk bakım bileşenlerindeki `any`, effect içinde senkron state güncelleme ve hook dependency sorunlarında.
2. Test projesi solution içindeki normal `dotnet test` çalıştırmasında keşfedilmiyor; açık proje yolu ile çalıştırılması gerekiyor.
3. Otomatik test kapsamı 53 frontend sayfası ve 48 controller için yetersiz.
4. Auth, rol matrisi ve tüm modül CRUD/durum akışları gerçek PostgreSQL ve tarayıcı oturumuyla henüz tam uçtan uca doğrulanmadı.

## Veri güvenliği

- Kullanıcı verisi değiştirilmedi veya silinmedi.
- Veritabanı migration'ı oluşturulmadı.
- Gerçek secret rapora veya kaynak koda yazılmadı.
- Bu rapordaki “başarılı” ifadeleri yalnız çalıştırılmış otomatik doğrulamalar için kullanılır.
