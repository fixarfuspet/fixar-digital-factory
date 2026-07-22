# Production Monitoring

`monitor-fixar-os.sh` harici servis gerektirmeyen ilk seviye izleme kapısıdır. Frontend, API live/ready/database, disk, son backup/checksum, son 15 dakikadaki 5xx, başarısız login ve kritik job hata işaretlerini kontrol eder; tek satır JSON ve non-zero alarm kodu üretir.

```bash
scripts/monitor-fixar-os.sh --dry-run
FIXAR_HEALTH_BASE_URL=https://fixar.example.com \
FIXAR_LOG_DIR=/var/log/fixar-os \
FIXAR_BACKUP_DIR=/var/backups/fixar-os \
FIXAR_PG_DATABASE=fixar_os \
scripts/monitor-fixar-os.sh >> /var/log/fixar-os/monitor.jsonl
```

Beş dakikada bir systemd timer/cron ile çalıştırılır. Non-zero sonuç nöbetçiye iletilir. Eşikler: disk %85, backup 26 saat, 5xx/15 dk en fazla 5, kritik job 0. Login başarısızlıkları raporlanır; hız artışı güvenlik olayıdır.

Migration durumu deploy sırasında idempotent SQL ve migration count ile, günlük olarak CEO `/system-control` ekranından izlenir. Bekleyen migration production sırasında otomatik uygulanmaz. Monitoring secret, token veya response body loglamaz.

Olay kaydı: zaman, release, kullanıcı rolü, ekran/endpoint, HTTP status, correlation ID, etkilenen order/work order ve geri alma eylemini içerir. Veri bütünlüğü, auth ihlali, negatif stok veya restore başarısızlığı P0; pilot derhal durdurulur.
