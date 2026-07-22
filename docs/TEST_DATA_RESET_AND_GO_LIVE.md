# Test Data Reset and Go-Live

`scripts/test-data-reset.sh` yalnız `TEST-` önekiyle açıkça işaretlenmiş iş kayıtlarını sayar ve varsayılan olarak dry-run'dır. Kullanıcı, rol, izin, istasyon, makine ve `__EFMigrationsHistory` hedeflenmez.

```bash
FIXAR_PG_USER=fixar_operator \
FIXAR_PG_DATABASE=fixar_stage \
scripts/test-data-reset.sh
```

Apply modu mevcut aşamada güvenlik gereği kapalıdır. İlişkisel silme sırası gerçek veri kopyasında ayrıca onaylanmadan otomatik silme yapılmaz. Bu nedenle canlı başlangıç temizliği için süreç şudur:

1. Dry-run çıktısını iş birimi sahibi onaylar.
2. Doğrulanmış backup alınır ve restore testi geçer.
3. Maintenance mode açılır.
4. Onaylı kayıt kimlikleri transaction içinde temizlenir; sayı dry-run ile eşleşmezse rollback yapılır.
5. Bütünlük kontrolleri, health, login ve route smoke çalışır.
6. Kanıt saklandıktan sonra maintenance mode kapatılır.
