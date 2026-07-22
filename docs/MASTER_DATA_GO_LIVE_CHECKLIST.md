# Master Data Go-Live Checklist

Her satır iki kişi kuralıyla yürütülür: **Giren** kaydı oluşturur, **Onaylayan** kaynak belge ve sistem ekranını karşılaştırır. Bir aşama onaylanmadan sonraki bağımlı aşamaya geçilmez.

| Sıra | Ana veri | Zorunlu alan / örnek | Giren | Onaylayan | Son kontrol |
|---:|---|---|---|---|---|
| 1 | Kullanıcılar | kurumsal e-posta, ad, aktiflik | Sistem yöneticisi | CEO | Ortak hesap yok; MFA/şifre kanalı ayrı |
| 2 | Roller/yetkiler | tek minimum görev rolü | Sistem yöneticisi | CEO | Yetki matrisi ve test login |
| 3 | Makineler | kod `ENJ-MAK-01`, ad, kapasite, aktif | Üretim müdürü | Fabrika müdürü | Fiziksel etiket eşleşir |
| 4 | 24 enjeksiyon istasyonu | 1–24, makine, durum | Üretim müdürü | Fabrika müdürü | Eksik/çift numara yok |
| 5 | Operatörler | sicil, ad, yetkinlik, vardiya | İK/üretim | Üretim müdürü | Kullanıcı eşleşmesi ve aktiflik |
| 6 | Tedarikçiler | kod `TED-0001`, unvan, vergi, vade, para birimi | Satın alma | Finans | Mükerrer vergi no yok |
| 7 | Hammaddeler | kod `MAT-0001`, birim, lot takibi, yoğunluk | Satın alma | Kalite/üretim | Teknik föy ve birim |
| 8 | Malzeme fiyatları | tarih, tedarikçi, para birimi, birim fiyat | Satın alma | Finans | Kur ve geçerlilik tarihi |
| 9 | Açılış stokları | malzeme, depo, miktar, birim | Depo | Finans | Sayım tutanağı toplamı |
| 10 | Lot/varil | lot no, giriş/son kullanma, miktar, container | Depo | Kalite | Lot toplamı ana stokla eşit |
| 11 | Müşteriler | `MUS-0001`, unvan, vergi, vade, para birimi | Satış | Finans | Cari mükerrer değil |
| 12 | Ürünler | `URN-0001`, model, tür, ağırlık, kutu adedi | Ürün/üretim | Kalite | Müşteri/model eşleşir |
| 13 | Varyant/numara | ürün, numara aralığı, renk | Satış | Üretim | Pilot kapsamıyla sınırlı |
| 14 | Kalıplar | `KLP-0001`, göz, numara, ürün, çevrim | Kalıphane | Üretim müdürü | QR/fiziksel kalıp eşleşir |
| 15 | Reçeteler | `RCP-0001`, ürün, versiyon, kalem/birim/fire | Proses | Kalite/üretim | Toplam ve efektif tarih |
| 16 | Maliyet ayarları | işçilik, enerji, genel gider, tarih | Finans | CEO | Onaylı tarife belgesi |
| 17 | Döviz kurları | tarih, kaynak, alış/satış | Finans | CEO | Kaynak ve tekillik |
| 18 | Finans hesapları | kasa/banka kodu, para birimi | Finans | CEO | IBAN/kasa ve açılış mutabakatı |
| 19 | Müşteri açılış bakiyesi | müşteri, belge, tarih, vade, bakiye | Finans | CEO | Cari yaşlandırma toplamı |
| 20 | Tedarikçi açılış bakiyesi | tedarikçi, belge, tarih, vade, bakiye | Finans | CEO | Borç yaşlandırma toplamı |

## İçe aktarma kuralları

- CSV UTF-8, başlıklar template ile aynı, ondalık ayırıcı nokta ve tarih `YYYY-MM-DD`.
- Boş kod, negatif miktar/fiyat, bilinmeyen foreign key ve mükerrer kod reddedilir.
- Önce staging/dry-run; hata raporu sıfır olmadan gerçek import yapılmaz.
- Her dosya SHA-256, hazırlayan, onaylayan ve zaman damgasıyla arşivlenir.
- Açılış stok/cari girişi öncesi backup, sonrası ana toplam mutabakatı zorunludur.

## Pilot kapısı

24 istasyonun tamamı tanımlı ve tekil; pilot müşteri/ürün/kalıp/reçete/lot zinciri aktif; finans hesapları ve kurlar onaylı; kritik stok negatif değilse ana veri kapısı geçer.
