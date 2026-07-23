# FIXAR OS — Digital Factory

FIXAR OS; sipariş, teklif, iş emri, canlı üretim, kalite, kesim, koli/sevkiyat, hammadde lotu, stok rezervasyonu/tüketimi, bakım ve finans süreçlerini bir araya getiren fabrika yönetim sistemidir.

## Gereksinimler

- .NET SDK 9
- Node.js 22 ve npm
- PostgreSQL 16

## Yerel çalıştırma

Backend için gerçek değerleri ortam değişkeninden verin; secret dosyaya yazmayın:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=fixar_os_dev;Username=postgres;Password=...'
export Jwt__Secret='en-az-32-byte-guclu-bir-development-secret'
dotnet run --project apps/api/src/Fixar.API/Fixar.API.csproj --no-launch-profile --urls http://0.0.0.0:5000
```

Development test kullanıcıları yalnız `FIXAR_DEV_TEST_PASSWORD` açıkça tanımlandığında hazırlanır. Bu değişken production ortamında kullanılmamalıdır.

Frontend:

```bash
cd apps/web
npm ci
npm run dev -- --hostname 0.0.0.0 --port 3000
```

Tarayıcı istekleri Next.js BFF üzerinden `/api/backend/api/v1/...` yoluyla API'ye gider. Server-side API adresi gerekiyorsa `API_BASE_URL=http://127.0.0.1:5000` kullanın.

## Migration

```bash
dotnet ef database update \
  --project apps/api/src/Fixar.Infrastructure/Fixar.Infrastructure.csproj \
  --startup-project apps/api/src/Fixar.API/Fixar.API.csproj
```

Production dahil tüm ortamlarda bekleyen migrationlar Kestrel başlamadan önce otomatik uygulanır.
Migration veya başlangıç seed'i başarısız olursa API non-zero kodla kapanır. Production'da
`BootstrapAdmin__Email` ve `BootstrapAdmin__Password` zorunludur; Docker Compose bunları
`.env.production` içindeki `FIXAR_ADMIN_EMAIL` ve `FIXAR_ADMIN_PASSWORD` değerlerinden geçirir.

## Doğrulama

```bash
dotnet build apps/api/src/Fixar.API/Fixar.API.csproj --no-restore -m:1 -v:minimal
dotnet test apps/api/tests/Fixar.Quotation.Tests/Fixar.Quotation.Tests.csproj --no-restore -m:1 -v:minimal
cd apps/web
npm exec tsc -- --noEmit
npm run lint
npm run build
npm run test:smoke
```

`/health/live` süreç canlılığını, `/health/ready` PostgreSQL erişimini denetler. Swagger yalnız Development ortamında açıktır.

## Production notları

- `ConnectionStrings__DefaultConnection`, `Jwt__Secret`, bootstrap admin parolası ve CORS originleri secret/config yönetiminden gelmelidir.
- Reverse proxy HTTPS sonlandırması, trusted proxy listesi ve backup/restore politikası deployment ortamında açıkça tanımlanmalıdır.
- PostgreSQL yedek/restore runbook'u henüz depoda bulunmuyor; doğrulanmadan canlıya çıkılmamalıdır.
- Ayrıntılı mevcut durum ve kalan riskler: `docs/SYSTEM_AUDIT_2026-07-22.md`.
