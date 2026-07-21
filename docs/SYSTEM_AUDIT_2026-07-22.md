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

## Aşama 2 — Authentication ve session

Durum: Tamamlandı (kod tabanında ve otomatik güvenlik testleriyle).

### Düzeltilenler

- Login, refresh ve logout endpointlerine IP bazlı sabit pencere rate-limit eklendi: dakikada en fazla 10 istek, kuyruk yok, aşımda HTTP 429.
- Yeni refresh tokenlar veritabanında ham değer yerine `SHA-256` hash ile saklanıyor. Geçiş sırasında mevcut düz metin tokenlar bir kez daha kullanılabildiği için aktif oturumlar bozulmuyor; döndürülen yeni token hashleniyor.
- Logout, süresi dolmuş access token nedeniyle refresh tokenı iptal edememe sorununu gidermek için refresh tokenı kendi yetkilendirme kanıtı olarak kabul ediyor. Token bilinmeden başka oturum iptal edilemiyor.
- BFF login, logout ve tüm veri değiştiren proxy isteklerine same-origin kontrolü eklendi. Cookie tabanlı oturumda CSRF için `SameSite=Lax` korumasına ek savunma sağlandı.
- Next.js session kapısı yalnız `/home` ve `/dashboard` yerine bütün uygulama sayfalarını kapsayacak şekilde genişletildi; API ve statik asset yolları hariç tutuldu.
- Access ve refresh cookie'lerinin `HttpOnly`, production ortamında `Secure`, `SameSite=Lax` ve kök path ayarları doğrulandı.
- JWT issuer, audience, imza, ömür ve 1 dakikalık clock-skew doğrulaması; 15 dakikalık access ve 7 günlük refresh ömrü doğrulandı.
- Identity parola politikası: en az 12 karakter, büyük/küçük harf, rakam ve özel karakter. Beş başarısız denemede 15 dakika kilit.
- Development test kullanıcıları yalnız `Development` ortamında ve `FIXAR_DEV_TEST_PASSWORD` açıkça verilirse hazırlanıyor; parola loglanmıyor.

### Otomatik test matrisi

- Auth endpoint rate-limit attribute kontrolü.
- Süresi dolmuş access token durumunda logout/revoke sözleşmesi.
- Refresh token hashinin deterministik ve tek yönlü olması.
- Parola, unique email ve lockout ayarları.
- Enjeksiyon, kesim ve depo operatörlerinin maliyet, kârlılık ve yönetici dashboard policy'lerinden dışlanması.

Sonuç: Backend testleri 28/28 başarılı; backend build, TypeScript, ilgili frontend lint ve 57 route production build başarılı.

### Sınırlar

- Gerçek tarayıcıda çoklu sekme ve token süresi ilerletme testi henüz E2E altyapısı kurulmadığı için Aşama 4/29 kapsamına bırakıldı.
- Refresh token hash geçişi şema değiştirmedi; migration oluşturulmadı.

## Aşama 3 — Rol ve yetki denetimi

Durum: Tamamlandı (tanımlı rol/policy matrisi ve kritik mutation endpointleri).

### Düzeltilen yetki açıkları

- Station assignment üzerindeki üretim ekleme, durdurma, devam ettirme, bitirme, fire, duruş ve release işlemleri yalnız genel oturum kontrolünden çıkarılarak ilgili üretim/planlama policy'lerine bağlandı.
- Fire iptali yalnız CEO ve Üretim Müdürü'nün bulunduğu `CanOverrideProductionRules` policy'sine alındı.
- ProductionBox güncelleme, iptal ve boşaltma işlemleri yönetici override policy'sine alındı.
- Eski ProductionBox create/fill/cutting/warehouse/shipment endpointleri de policy ve idempotency korumasına alındı; eski endpointler paralel sistem olarak genişletilmedi.
- Üretim Müdürü frontend'de müşteri ve sipariş modüllerini görebiliyor; backend müşteri/sipariş yönetimiyle eşleştirildi.
- Finans rollerinin sipariş değiştirme yetkisi kaldırıldı. Backend'in izin verdiği yönetici dashboard'u frontend'de Finans rollerine görünür hâle getirildi.

### Doğrulanan rol sınırları

| Rol | Üretim kaydı | Kesim | Depo | Sipariş/müşteri düzeltme | Maliyet/kârlılık |
|---|---:|---:|---:|---:|---:|
| CEO | Evet | Evet | Evet | Evet | Evet |
| Üretim Müdürü | Evet | Evet | Evet | Evet | Evet |
| Enjeksiyon Operatörü | Evet | Hayır | Hayır | Hayır | Hayır |
| Kesim Operatörü | Hayır | Evet | Hayır | Hayır | Hayır |
| Depo Operatörü | Hayır | Hayır | Evet | Hayır | Hayır |

Kod modelinde Gezer Kafa ve Döner Kafa operatörleri ayrı roller değil, ortak `CuttingOperator` rolüdür. Makine bazlı ayrım rol katmanında iddia edilmedi; operasyon ataması düzeyinde sonraki kesim denetiminde ele alınacaktır.

### Test sonucu

- 13 pozitif/negatif rol-policy senaryosu ve 4 kritik controller action policy testi eklendi.
- Backend toplamı 45/45 test başarılı.
- Backend build, TypeScript ve ilgili frontend lint başarılı.

## Aşama 4 — Frontend route ve sayfa denetimi

Durum: Kısmen tamamlandı; production route/session smoke tamam, authenticated browser CRUD ve responsive görsel denetim test edilemedi.

### Eklenen güvenlik ağı

- `npm run test:smoke` production Next.js sunucusunu izole portta başlatır, `app/**/page.tsx` dosyalarından route'ları otomatik keşfeder ve bütün 53 kullanıcı sayfasını kontrol eder.
- Login sayfasının 200 açılması ve korunan 52 sayfanın oturum yokken 307/308 ile login'e yönlenmesi doğrulanır.
- Dinamik `/traceability/[code]` route'u kontrollü `TEST-SMOKE` koduyla kapsanır.
- Global Türkçe runtime hata ekranı eklendi; teknik ayrıntı yalnız `console.error` içine yazılır.
- Türkçe 404 ekranı ve ana sayfaya dönüş bağlantısı eklendi.

### Sonuç

- Production build: 57 Next.js route başarılı.
- Route smoke: 53/53 kullanıcı route'u başarılı.
- TypeScript ve eklenen dosyaların lint kontrolü başarılı.

### Test edilemeyenler

- Depoda Playwright/Cypress bulunmuyor.
- `FIXAR_DEV_TEST_PASSWORD` mevcut süreç ortamında tanımlı değil. Kullanıcı verisine veya mevcut hesap parolalarına müdahale etmeden authenticated browser CRUD, console, mobil/tablet/TV ve form state senaryoları çalıştırılamadı.
- Bu maddeler tamamlanmadan Aşama 4 için “Tamamlandı” sonucu verilmedi.
