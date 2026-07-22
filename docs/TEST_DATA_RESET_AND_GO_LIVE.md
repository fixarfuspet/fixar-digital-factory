# Test Data Reset and Go-Live

`scripts/test-data-reset.sh` yalnız `TEST-` önekiyle açıkça işaretlenmiş iş kayıtlarını sayar ve varsayılan olarak dry-run'dır. Kullanıcı, rol, izin, istasyon, makine ve `__EFMigrationsHistory` hedeflenmez.

```bash
FIXAR_PG_USER=fixar_operator \
FIXAR_PG_DATABASE=fixar_stage \
scripts/test-data-reset.sh
```

Apply modu fail-closed'dur. Açık TEST işaretli müşteri, malzeme ve reçeteler yalnız tarihsel order/quote, lot/consumption veya work-order bağımlılığı yoksa transaction içinde temizlenebilir. Finans, stok hareketi, üretim ve audit geçmişi otomatik silinmez. Bağımlılık bulunursa tüm transaction hata verip rollback olur.

```bash
FIXAR_PG_USER=fixar_operator FIXAR_PG_DATABASE=fixar_stage \
FIXAR_BACKUP_FILE=/secure/verified.dump \
scripts/test-data-reset.sh --confirm-test-data-reset
```

Production'da ayrıca `--confirm-production-test-data-reset` ve `FIXAR_PRODUCTION_RESET_APPROVAL=DELETE-CONFIRMED-TEST-DATA` zorunludur.

Canlı başlangıç temizliği için süreç şudur:

1. Dry-run çıktısını iş birimi sahibi onaylar.
2. Doğrulanmış backup alınır ve restore testi geçer.
3. Maintenance mode açılır.
4. Onaylı kayıt kimlikleri transaction içinde temizlenir; sayı dry-run ile eşleşmezse rollback yapılır.
5. Bütünlük kontrolleri, health, login ve route smoke çalışır.
6. Kanıt saklandıktan sonra maintenance mode kapatılır.
