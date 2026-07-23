# FIXAR OS Web

Next.js 16 / React 19 istemcisidir. Kimlik doğrulama httpOnly cookie ve server-side BFF proxy üzerinden yürür; token localStorage'a yazılmaz.

```bash
npm ci
API_BASE_URL=http://127.0.0.1:5000 npm run dev -- --hostname 0.0.0.0 --port 3000
```

Doğrulamalar:

```bash
npm exec tsc -- --noEmit
npm run lint
npm run build
npm run test:smoke
```

Tarayıcıdaki tüm backend çağrıları ortak API istemcisi üzerinden aynı origin'deki
`/api/backend/api/v1/...` BFF yoluna gider. Tarayıcıya backend adresi açan
`NEXT_PUBLIC_API_*` değişkenleri kullanılmaz.
