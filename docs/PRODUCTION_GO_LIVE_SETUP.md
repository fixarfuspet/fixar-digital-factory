# Production Go-Live Setup

1. `.env.production` yalnız sunucuda, `0600` izinle oluşturulur; example dosyalardaki placeholder'lar güçlü secretlarla değiştirilir. `FIXAR_ADMIN_EMAIL` ve `FIXAR_ADMIN_PASSWORD` zorunludur; bootstrap admin her başlangıçta aktif, CEO rolünde ve bu parolayla giriş yapabilir halde senkronize edilir. `FIXAR_DEV_TEST_PASSWORD` production'a konmaz.
2. TLS `fullchain.pem` ve `privkey.pem` dosyaları `ops/certs` altında deployment secret mekanizmasıyla sağlanır; repoya eklenmez.
3. `docker compose --env-file .env.production -f docker-compose.production.yml config --quiet` çalıştırılır. PostgreSQL'in host portu yoktur; API ve web yalnız internal networktedir, dış erişim Nginx 80/443 üzerindendir.
4. Migration öncesi doğrulanmış backup zorunludur. `deploy-production.sh` varsayılan dry-run'dır; `--execute`, `FIXAR_VERIFIED_BACKUP` ve incelenmiş `FIXAR_MIGRATION_SQL` olmadan yazma yapmaz. API ayrıca startup sırasında aynı migration zincirini idempotent olarak doğrular ve bekleyen migrationları Kestrel başlamadan önce uygular.
5. Deploy sonrası frontend, live health, ready/database health kontrol edilir. Başarısızlıkta maintenance sürdürülür ve `rollback-production.sh` planı uygulanır.

```bash
FIXAR_ENV_FILE=$PWD/.env.production scripts/deploy-production.sh
FIXAR_ENV_FILE=$PWD/.env.production FIXAR_VERIFIED_BACKUP=/secure/backup.dump FIXAR_MIGRATION_SQL=/secure/migrations.sql scripts/deploy-production.sh --execute
```

Containerlar `restart: unless-stopped`; loglar 20 MB x 10 dosya döner. Günlük DB backup ayrı zamanlayıcıyla çalışır. Production firewall yalnız 80/443 ve yönetim ağı SSH'a izin vermelidir.
