# PostgreSQL Backup and Recovery

Bu prosedür varsayılan olarak hiçbir veritabanına bağlanmaz. Kullanıcı, veritabanı ve hedef dosya açık ortam değişkenleriyle verilmelidir. Production benzeri adlar ayrıca `FIXAR_ALLOW_PRODUCTION_DATABASE=true` olmadan reddedilir.

## Yedek

```bash
FIXAR_PG_USER=fixar_backup \
FIXAR_PG_DATABASE=fixar_stage \
FIXAR_BACKUP_DIR=/secure/fixar-backups \
scripts/backup-postgres.sh
```

Parola gerekiyorsa interaktif prompt veya işletim sisteminin korumalı `.pgpass` mekanizması kullanılmalıdır. Parola komut satırına ve repoya yazılmaz. Script custom-format `pg_dump`, SHA-256 ve JSON manifest üretir.

## Ayrı veritabanına restore

```bash
FIXAR_PG_USER=fixar_restore \
FIXAR_PG_DATABASE=fixar_stage \
FIXAR_RESTORE_DATABASE=fixar_restore_test \
FIXAR_BACKUP_FILE=/secure/fixar-backups/example.dump \
FIXAR_RECREATE_RESTORE_DATABASE=true \
scripts/restore-postgres.sh
```

Kaynak üzerine restore reddedilir. Restore sonrasında kritik tabloları karşılaştırın:

```bash
FIXAR_PG_USER=fixar_restore \
FIXAR_PG_DATABASE=fixar_stage \
FIXAR_RESTORE_DATABASE=fixar_restore_test \
scripts/verify-postgres-backup.sh
```

Son olarak restore veritabanıyla API başlatılır; `/health/live`, `/health/ready`, login ve yetkili route smoke çalıştırılır. Yedek ancak bu adımlar geçerse kullanılabilir kabul edilir.

## Saklama ve güvenlik

- Günlük yedekler en az 35 gün; aylık yedekler 12 ay saklanır.
- En az bir kopya uygulama sunucusundan ayrı, şifreli depoda tutulur.
- Restore testi en az ayda bir yapılır ve JSON kanıtı saklanır.
- Backup rolü salt okunur ve minimum yetkili olmalıdır.
