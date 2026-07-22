# Automated Backup Operations

Günlük iş `run-postgres-backup.sh` üzerinden çalışır. Secret içeren `/etc/fixar-os/backup.env` repoya girmez ve `0600` olmalıdır.

```dotenv
FIXAR_PG_HOST=127.0.0.1
FIXAR_PG_PORT=5432
FIXAR_PG_USER=fixar_backup
FIXAR_PG_DATABASE=fixar_os
FIXAR_ALLOW_PRODUCTION_DATABASE=true
FIXAR_BACKUP_DIR=/var/backups/fixar-os
FIXAR_BACKUP_RETENTION_DAYS=35
FIXAR_BACKUP_MIN_FREE_MB=5120
```

Linux: service/timer dosyaları `/etc/systemd/system` altına kopyalanır, sonra `systemctl enable --now fixar-postgres-backup.timer`. macOS: plist içindeki yollar uyarlanıp LaunchAgents/LaunchDaemons altında yüklenir.

Her yedek custom pg_dump, SHA-256 manifest ve tarihli log üretir. Disk eşiği aşılırsa, pg_dump/manifest başarısızsa veya secret eksikse non-zero çıkar. Retention varsayılan 35 gündür; harici disk/object-storage mount'u `FIXAR_BACKUP_DIR` olarak verilebilir.

```bash
FIXAR_BACKUP_DIR=/var/backups/fixar-os FIXAR_PG_DATABASE=fixar_os scripts/check-last-backup.sh
FIXAR_PG_USER=fixar_restore FIXAR_PG_DATABASE=fixar_os FIXAR_RESTORE_DATABASE=fixar_restore_test scripts/verify-postgres-backup.sh
```

Son başarılı backup kontrolü 26 saatten eski, eksik manifestli veya checksum hatalı yedeği reddeder. Aylık restore provası ve health/login smoke zorunludur.
