# FIXAR Digital Factory sistem denetimi — başlangıç envanteri

Tarih: 22 Temmuz 2026  
Branch: `fix/live-production-integration`

## Envanter

- Backend: 48 controller dosyasında 52 controller sınıfı; ASP.NET Core, EF Core ve PostgreSQL.
- Frontend: 53 `page.tsx`, production build çıktısında 57 route.
- Domain: 53 C# kaynak dosyası.
- Migration: başlangıçta 35 ana migration; denetimde eklenen güvenli FK migrationıyla 36 ve güncel model snapshot.
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

## Aşama 5 — Backend controller ve API denetimi

Durum: Kısmen tamamlandı; controller güvenlik/route sözleşmesi otomatikleştirildi, tüm endpointlerin gerçek PostgreSQL başarılı-hatalı-yetkisiz matrisi tamamlanmadı.

### Otomatik sözleşme denetimi

- Gerçek envanter 48 dosya içinde 52 controller sınıfı olarak düzeltildi.
- Her controller sınıfında `[ApiController]` ve route tanımı bulunması zorunlu testle korunuyor.
- Her HTTP action için class veya action seviyesinde açık `[Authorize]` / `[AllowAnonymous]` sözleşmesi zorunlu.
- HTTP verb + controller route + action route birleşimlerinin benzersizliği kontrol ediliyor; 150'den fazla endpoint keşfedilmezse test hata veriyor.
- Anonim endpointler yalnız Auth ve development seed controller alanıyla sınırlandırılıyor.

Sonuç: Toplam backend testi 48/48 başarılı. Açık authorization action veya çakışan HTTP route bulunmadı.

### Açık kapsam

- Her endpoint için gerçek veritabanıyla ayrı success, validation, not-found, conflict, transaction rollback ve IDOR testi henüz yoktur.
- Bu nedenle controller katmanı için “canlıya hazır/tamamlandı” sonucu verilmedi.

## Aşama 6 — Veritabanı ve migration

Durum: Kısmen tamamlandı; migration zinciri ve yeni migration SQL'i doğrulandı, izole PostgreSQL apply/rollback testi ortam yokluğu nedeniyle yapılamadı.

### Düzeltme

`ProtectHistoricalRecordsFromCascadeDelete` migrationı aşağıdaki sekiz tarihsel ilişkiyi Cascade yerine Restrict yaptı:

- Customer → Order
- Product → Order
- CuttingMachine → CuttingRecord
- Order → CuttingRecord
- InjectionStation → ProductionRecord
- Mold → ProductionRecord
- Order → ProductionRecord
- StockItem → PurchaseOrderLine

Bu değişiklik kayıt veya kolon silmez; yalnız master kaydın yanlışlıkla fiziksel silinmesi halinde geçmiş sipariş, üretim, kesim ve satın alma kayıtlarının zincirleme silinmesini engeller. `Down` bölümü önceki davranışı açıkça geri yükler.

### Doğrulama

- 35 mevcut migration sıralı listelendi; yeni migration ile toplam 36.
- Tam idempotent migration SQL'i üretildi: 5.878 satır.
- Yeni migration için 38 satırlık hedef SQL üretildi; `DROP TABLE`, `DROP COLUMN`, `DELETE` veya `TRUNCATE` içermiyor.
- Sekiz ilişkinin EF modelinde `DeleteBehavior.Restrict` olduğunu doğrulayan regression testi eklendi.
- Backend testleri 49/49 başarılı.

### Ortam engeli

- `127.0.0.1:5432` kapalı, Docker ve yerel PostgreSQL istemcisi mevcut değil. Bu nedenle temiz izole PostgreSQL veritabanına apply ve rollback testi çalıştırılamadı.
- Migration gerçek kullanıcı veritabanına uygulanmadı.

## Aşama 7 — Sipariş ve iş emri

Durum: Kısmen tamamlandı; kritik miktar, tekrar gönderim ve yetki açıkları düzeltildi. Gerçek PostgreSQL uçtan uca sipariş üretim/sevkiyat senaryosu test edilemedi.

### Düzeltilenler

- Sipariş oluşturma ve çoğaltma idempotency korumasına alındı.
- Sipariş toplamlarını yeniden hesaplayan yazma endpointi satış siparişi yönetim policy'sine ve idempotency korumasına alındı.
- Sipariş kalemi miktarı artık yalnız üretilen miktarın değil; üretilen, kesilen veya sevk edilen değerlerin en büyüğünün altına indirilemiyor.
- İş emri create, plan, ready, pause, resume, complete, cancel ve duplicate endpointleri yönetim policy'sine ve idempotency korumasına alındı.
- İş emri çoğaltma akışı ortak kalan-miktar doğrulamasını atlıyordu. Çoğaltma öncesinde sipariş kaleminin kalan miktarı ve diğer açık iş emirlerinin planlanan toplamı kontrol ediliyor.

### Regression testleri

- Sevk edilmiş miktarın altına sipariş kalemi düşürme engeli.
- Sipariş kalanını aşan mükerrer iş emri engeli.

Sonuç: Backend testleri 51/51 başarılı.

## Aşama 8 — Canlı üretim ve istasyon hareketleri

Durum: Kısmen tamamlandı; kritik hedef aşımı ve eşzamanlı yazım riski giderildi. Gerçek PostgreSQL ile paralel istek yük testi ortam eksikliği nedeniyle çalıştırılamadı.

### Kök neden ve düzeltme

- Manuel üretim ve toplu tur ekleme akışları yalnız pozitif miktarı kontrol ediyor, istasyon planı veya sipariş kaleminin kalan miktarını aşmayı engellemiyordu.
- Farklı idempotency anahtarlarıyla aynı anda gelen geçerli iki istek aynı eski miktarı okuyabildiği için kayıp güncelleme veya fazla üretim riski vardı.
- Manuel üretim ve tur ekleme işlemleri PostgreSQL transaction-scoped advisory lock altında seri hâle getirildi. Kalan miktar kontrolü kilit alındıktan sonra yapılıyor.
- Manuel üretim, hem `StationAssignment.PlannedPairs` hem `OrderItem.QuantityPairs` kalanını aşarsa kontrollü `409 PRODUCTION_EXCEEDS_REMAINING` döndürüyor.
- Toplu tur işlemi hedefi dolu istasyonları atlıyor; hiçbir istasyonda yeterli kalan yoksa `409 PRODUCTION_TARGET_REACHED` döndürüyor.
- Yanıt toplamları yalnız gerçekten işlenen istasyonlardan hesaplanıyor. Başarılı turda istasyon ve sipariş kalemi miktarı aynı transaction içinde artıyor ve `Tur Eklendi` denetim olayı oluşuyor.
- Mevcut idempotency filtresi ve istemci `RequestId` tekrar kontrolü korunuyor.

### Regression testleri

- Manuel üretimin kalan miktarı aşması engelleniyor ve hiçbir miktar/olay değişmiyor.
- Toplu turun kalan miktarı aşması engelleniyor ve hiçbir miktar/olay değişmiyor.
- Tam kalan miktardaki tur istasyon ile sipariş kalemini birlikte hedefe getiriyor ve denetim olayı yazıyor.

Sonuç: Backend testleri 54/54 başarılı.

## Aşama 9 — Kesim modülü

Durum: Kısmen tamamlandı; kesim miktarı bütünlüğü, düzeltme yetkisi ve eşzamanlı yazım güvenliği sağlandı. Makine/bıçak eşleşmesi ve gerçek iki makine paralel E2E senaryosu mevcut veri modeli ve test ortamı sınırları nedeniyle doğrulanamadı.

### Kök neden ve düzeltmeler

- Kesim kaydı `Completed` durumuna geçerken `OrderItem.CutPairs` hiç güncellenmiyordu. Sipariş ilerlemesi bu nedenle gerçek kesimi göstermiyor ve sonraki iş akışları yanlış kalan miktar hesaplayabiliyordu.
- Tamamlama artık sağlam çift miktarını sipariş kaleminin kesilen miktarına aynı transaction içinde ekliyor; üretilen kalan miktarın aşılması `409 CUTTING_EXCEEDS_ORDER_REMAINING` ile engelleniyor.
- Tamamlanmış bir kesim, bağlı aktif koli yoksa yetkili düzeltmeyle iptal edildiğinde aynı miktar sipariş kaleminden güvenli biçimde geri alınıyor ve değer negatife düşmüyor.
- Create doğrulaması transaction ve PostgreSQL advisory lock içine taşındı. Create, update, complete ve cancel işlemleri aynı seri kesim yazma kilidini kullanıyor; böylece iki makinenin eşzamanlı olarak aynı üretilmiş bakiyeyi tüketmesi engelleniyor.
- Update ve cancel yalnız CEO/Üretim Müdürü override policy'sine, idempotency korumasıyla bağlandı. Complete kesim operatörü policy'sini ve idempotency korumasını kullanmaya devam ediyor.
- Oluşturma, güncelleme, tamamlama ve iptal denetim kayıtları korunuyor.

### Regression testleri

- Kesim tamamlamanın sipariş kalemi kesilen miktarını tek kez artırması.
- Üretilmiş kalan miktarı aşan kesim tamamlamanın hiçbir değeri değiştirmeden engellenmesi.
- Tamamlanmış kesim iptalinin sipariş kalemi miktarını geri alması ve audit oluşturması.

Sonuç: Backend testleri 57/57 başarılı.

## Aşama 10 — Stok ve hammadde

Durum: Kısmen tamamlandı; lot/container/rezervasyon/tüketim zinciri ile ana stok kaynağını bozabilen doğrudan değişiklikler kapatıldı. Gerçek PostgreSQL FIFO ve eşzamanlı tüketim E2E testi ortam eksikliği nedeniyle çalıştırılamadı.

### Doğrulanan mevcut zincir

- Lot oluşturma ana stok miktarını artırıyor; lot güncelleme eski/yeni mevcut miktar farkını ana karta yansıtıyor ve kart değişiminde eski karttan düşüp yeni karta ekliyor.
- Container lot içi fiziksel dağılım olarak tutuluyor; oluşturma, açma veya kapatma ana stok toplamını değiştirmiyor.
- Rezervasyon lot ve container rezerve miktarını transaction içinde artırıyor; serbest bırakma/iptal kullanılmayan miktarı geri açıyor.
- Tüketim stok, lot ve container miktarını birlikte azaltıyor; rezervasyon tüketimini ilerletiyor ve stok hareketi/audit oluşturuyor.
- Reversal aynı fiziksel miktarları geri ekliyor, rezervasyonu uygun durumdaysa yeniden açıyor ve ikinci geri almayı engelliyor.
- Birim eşleşmesi, negatif stok, lot/container uygunluğu, kalite/son kullanma ve rezervasyon bakiyesi kontrolleri mevcut.

### Bu aşamada düzeltilenler

- Hammaddeye bağlı `StockItem` miktarı genel stok hareketi veya kart güncellemesiyle değiştirilebiliyordu. Bu, lot toplamı ile ana stok arasında sessiz fark oluşturuyordu. Bağlı kartların miktarı artık yalnız lot/tüketim/reversal iş akışından değiştirilebilir; doğrudan giriş `409 LOT_CONTROLLED_STOCK` ile engellenir.
- Genel stok kartlarında negatif başlangıç veya güncelleme miktarı engellendi.
- Genel stok miktarı kart düzenlemesinden değiştirildiğinde açıklama zorunlu ve fark kadar `Sayım Girişi` / `Sayım Çıkışı` hareketi oluşturuluyor.
- Manuel hareket ve kart güncellemesi ilgili policy, idempotency, serializable transaction ve PostgreSQL advisory lock ile korunuyor.

### Regression testleri

- Hammaddeye bağlı karta doğrudan hareketin miktarı ve hareket tablosunu değiştirmeden engellenmesi.
- Hammaddeye bağlı kart miktarının doğrudan düzenlenememesi.
- Genel stok sayım farkında gerekçe zorunluluğu ve fark kadar stok hareketi oluşturulması.

Sonuç: Backend testleri 60/60 başarılı.

## Aşama 11 — İzlenebilirlik ve QR

Durum: Kısmen tamamlandı; geçmiş koli QR erişimini engelleyen şema boşluğu giderildi ve izlenebilirlik kodu benzersizlik/zorunluluk sözleşmesi test edildi. Gerçek etiket yazıcısı, QR tekrar baskı cihazı ve müşteri dış erişim senaryosu ortamda bulunmadığından test edilemedi.

### Doğrulanan zincir

- Koli kaydı kesim, istasyon ataması, iş emri, sipariş kalemi, sipariş, müşteri ve ürün kimliklerini/snapshot verilerini taşıyor.
- Detay endpointi sipariş, reçete, üretim/istasyon, kalıp, operatör, kesim, kalite, depo ve sevkiyat bilgilerini tek cevapta birleştiriyor.
- Timeline sipariş, iş emri, istasyon üretim/fire/duruş olayları, hammadde lot tüketimi ve reversal, kesim, kalite, koli, depo ve sevkiyat olaylarını geriye dönük sunuyor.
- `BoxNumber` ve `TraceabilityCode` benzersiz indekslerle korunuyor; yeni koli kodu ve izlenebilirlik kodu transaction içinde üretiliyor.

### Kök neden ve düzeltme

- Önceki koli akışı migrationı eski kayıtların `BoxNumber` ve `Barcode` alanlarını dolduruyor, ancak `TraceabilityCode` üretmiyordu. Alan nullable kaldığı için eski koliler listelenebilse de QR, detay, timeline ve label endpointleriyle adreslenemiyordu.
- `RequireProductionBoxTraceabilityCode` migrationı yalnız boş/null eski kodları koli kimliğinden deterministik 32 karakterli kodla dolduruyor ve ardından kolonu `NOT NULL` yapıyor. Mevcut benzersiz indeks korunuyor; kayıt silme veya yeniden numaralandırma yok.
- Domain modeli alanı zorunlu hâle getirildi. İzlenebilirlik controllerı açık `CanViewTraceability` policy'sine bağlandı.
- Üretilen hedef migration SQL'i backfill → `SET NOT NULL` sırasını doğruladı ve veri silen komut içermiyor.

### Regression testi

- `ProductionBox.TraceabilityCode` alanının EF modelinde zorunlu ve tekil indeksli olduğu doğrulanıyor.

Sonuç: Backend testleri 61/61 başarılı. Migration gerçek kullanıcı veritabanına uygulanmadı.

## Aşama 12 — Kalite ve fire

Durum: Kısmen tamamlandı; kalite örneklem/üretim miktarı ve bağlı fire düzeltme bütünlüğü güvenceye alındı. Fotoğraf/ek belge alanı mevcut modelde bulunmuyor; harici dosya depolama kararı verilmeden tahmini bir yükleme sistemi eklenmedi.

### Düzeltilenler

- Kontrol edilen çift sayısı numune büyüklüğünü aşabiliyordu. Artık `CHECKED_EXCEEDS_SAMPLE` ile engelleniyor.
- Kalite kontrolü istasyonda gerçekten üretilenden daha fazla çift için oluşturulabiliyor veya tamamlanabiliyordu. Create, update ve complete akışları istasyon üretim miktarıyla sınırlandı.
- Kalite tamamlaması ile manuel üretim/fire işlemleri aynı üretim advisory lock ve serializable transaction altında seri hâle getirildi; eşzamanlı fire toplamının üretimi aşması engellendi.
- Tamamlanmış kalite kontrolünün ürettiği aktif fire kaydı varken kontrol iptal edilebiliyor ve fiziksel fire toplamı geride kalabiliyordu. Artık bağlı aktif fire önce CEO/Üretim Müdürü fire iptal akışından gerekçeyle geri alınmadan kalite kontrolü iptal edilemiyor.
- Create, update, complete ve duplicate kalite yazmaları idempotency korumasına alındı. İptal yalnız override policy'sine bağlandı.
- Mevcut sağlam/şartlı/ret toplamı, kusur toplamı, fire≤ret, ölçüm hedefleri, kritik kusur sonucu, kalite hold/duruş ve audit kuralları korundu.

### Regression testleri

- Kontrol edilen miktarın numune miktarını aşmasının engellenmesi.
- Kontrol edilen miktarın istasyon üretimini aşmasının engellenmesi.
- Aktif bağlı fire bulunan tamamlanmış kontrolün sessizce iptal edilememesi.

Sonuç: Backend testleri 64/64 başarılı.

## Aşama 13 — Koli ve sevkiyat

Durum: Kısmen tamamlandı; modern kesim→koli→depo→sevkiyat akışında sipariş ilerlemesi ve eşzamanlı miktar bütünlüğü sağlandı. İrsaliye ayrı entity/controller olarak mevcut değil; mevcut `ShipmentReference` sözleşmesi değiştirilmedi.

### Kök neden ve düzeltmeler

- Koli `Shipped` durumuna geçiyor fakat bağlı `OrderItem.ShippedPairs` hiç güncellenmiyordu. Sipariş ekranı sevkiyatı ve kalan miktarı yanlış gösteriyordu.
- Tekli ve toplu sevkiyat artık koli çiftlerini sipariş kaleminin sevk edilen miktarına aynı transaction içinde ekliyor.
- Sevkiyat, `CutPairs - ShippedPairs` kalanını aşarsa `409 SHIPMENT_EXCEEDS_CUT_REMAINING` ile hiçbir koli veya sipariş miktarı değişmeden engelleniyor.
- Sipariş kalemi bağlantısı olmayan koli modern sevkiyat akışına alınmıyor; eksik izlenebilirlik zinciri `BOX_ORDER_ITEM_MISSING` ile görünür kılınıyor.
- Aynı kolinin toplu istek içinde iki kez seçilmesi kontrollü validation hatasıdır.
- Koli oluşturma ve tekli/toplu durum geçişleri serializable transaction ve ortak PostgreSQL advisory lock altında seri hâle getirildi. Aynı kesilmiş bakiye iki eşzamanlı sevkiyat tarafından tüketilemez.
- Koli update idempotency korumasına alındı; mevcut create/depo/sevkiyat/cancel policy ve audit olayları korundu.

### Regression testleri

- Tekli sevkiyatın sipariş kalemi sevk miktarını artırması ve audit oluşturması.
- Kesilmiş kalanı aşan sevkiyatın koli/sipariş durumunu değiştirmeden engellenmesi.
- Toplu sevkiyatta mükerrer koli seçiminin engellenmesi.

Sonuç: Backend testleri 67/67 başarılı.
