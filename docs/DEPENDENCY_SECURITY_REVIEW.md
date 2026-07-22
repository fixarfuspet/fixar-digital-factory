# Frontend Dependency Security Review — 2026-07-22

## Başlangıç

`npm audit` 3 bulgu verdi: 2 high, 1 moderate.

| Paket | Zincir | Advisory | Etkilenen sürüm | Etki |
|---|---|---|---|---|
| `sharp` | `next@16.2.9 -> sharp@0.34.5` | GHSA-f88m-g3jw-g9cj; CVE-2026-33327, CVE-2026-33328, CVE-2026-35590, CVE-2026-35591 | `<0.35.0` | Sunucu tarafı güvenilmeyen görsel işlenirse libvips bellek/işleme açıkları tetiklenebilir. FIXAR doğrudan image optimizer yükleme akışı sunmasa da production Next runtime içinde bulunur. |
| `postcss` | `next@16.2.9 -> postcss@8.4.31` | GHSA-qx2v-qp2m-jg93, CWE-79 | `<8.5.10` | Güvenilmeyen CSS `</style>` içeriği stringify edilirse XSS riski. Uygulama kullanıcı CSS'i işlemese de build zincirinde açık sürüm tutulmamalıdır. |
| `next` | Yukarıdaki transit paketleri içerir | Transit özet | `9.3.4-canary.0 - 16.3.0-preview.7` | npm yalnız `next@9.3.3` downgrade önerdi; bu React 19/Next 16 uygulamasında ağır breaking change oluşturur. |

## Uygulanan çözüm

`package.json` overrides ile PostCSS `>=8.5.10`, Sharp `>=0.35.0` zorlandı. Etkin sürümler PostCSS 8.5.x ve Sharp 0.35.x'tir. Next/React sürümü değiştirilmedi. Lockfile normal `npm install` ile güncellendi; `npm audit` sonucu 0 bulgudur.

Sharp 0.35 semver-major olduğundan risk, tam TypeScript/lint/build/route smoke kapısıyla yönetilir. Next’in image optimizer ve kullanılan uygulama rotaları production build/smoke içinde doğrulanır. Körlemesine `npm audit fix --force` kullanılmadı.

## Sürekli kontrol

- CI her lockfile değişikliğinde `npm ci`, `npm audit`, TypeScript, lint, production build ve route smoke çalıştırmalıdır.
- Next resmi olarak düzeltilmiş transit sürümleri paketlediğinde override kaldırılıp aynı kapılar tekrar çalıştırılmalıdır.
- Kullanıcı kontrollü görsel veya CSS işleme eklenirse ayrıca kötü amaçlı dosya testleri eklenmelidir.
