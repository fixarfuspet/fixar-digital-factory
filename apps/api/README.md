# FIXAR OS API

Backend foundation for FIXAR OS, built with ASP.NET Core 9 and Clean Architecture.
This solution intentionally contains **no business modules** (Production, Inventory,
Quality, etc.) — only the cross-cutting infrastructure those modules will be built on.

## Solution layout

```
apps/api/
├── FixarOS.sln
├── src/
│   ├── Fixar.Domain          # Entities, enums, no external dependencies
│   ├── Fixar.Application     # Interfaces, DTOs, exceptions (depends on Domain only)
│   ├── Fixar.Infrastructure  # EF Core, ASP.NET Identity, JWT, repositories, audit interceptor
│   └── Fixar.API             # Program.cs, controllers, middleware, Swagger, versioning
├── Dockerfile
├── docker-compose.yml
└── .env.example
```

Dependency direction: `API -> Infrastructure -> Application -> Domain`. Domain has zero
NuGet dependencies; Application depends only on Domain; business modules will plug into
Application (use cases) and Infrastructure (persistence), never the other way around.

## What's included

| Requirement | Where |
|---|---|
| Clean Architecture | 4 projects, one-way dependency flow (see above) |
| EF Core + PostgreSQL | `Fixar.Infrastructure/Persistence/ApplicationDbContext.cs`, `Npgsql.EntityFrameworkCore.PostgreSQL` |
| JWT Authentication | `Fixar.Infrastructure/Identity/AuthService.cs`, JWT bearer setup in `Fixar.Infrastructure/DependencyInjection.cs` |
| ASP.NET Identity | `ApplicationUser`, `ApplicationRole` (`Fixar.Infrastructure/Identity`), 16 RBAC roles seeded from `docs/06_USER_ROLES.md` |
| Serilog | Console/File/Seq sinks, configured per environment in `appsettings.*.json` |
| Swagger/OpenAPI | `Fixar.API/Extensions/SwaggerServiceExtensions.cs`, JWT bearer scheme, enabled in Development |
| Health Checks | `/health/live`, `/health/ready` (PostgreSQL), `/health` — `Fixar.API/Extensions/HealthCheckEndpointExtensions.cs` |
| Docker | Multi-stage `Dockerfile`, `docker-compose.yml` (api + postgres + seq) |
| Repository Pattern | `IRepository<T>` / `IUnitOfWork` (Application) + `Repository<T>` / `UnitOfWork` (Infrastructure) |
| Dependency Injection | `AddApplication()`, `AddInfrastructure()`, `AddApiServices()` extension methods per layer |
| Global Exception Middleware | `Fixar.API/Middleware/GlobalExceptionHandler.cs` (`IExceptionHandler`), maps to the `{success, data, message}` envelope |
| Audit Logging | `AuditableEntitySaveChangesInterceptor` — auto-stamps `Created`/`LastModified` and writes `AuditLog` rows for every insert/update/delete |
| Dev/Prod configuration | `appsettings.Development.json`, `appsettings.Production.json` |

## Authentication

`AuthController` (`/api/v1/auth`) exposes `register`, `login`, `refresh-token`, `logout`
(revokes the refresh token) and `me`. Access tokens are short-lived JWTs (15 min default);
refresh tokens are opaque, single-use, and rotated on every refresh (`RefreshToken` table).

New users are placed in the `Guest` role by default — assigning real roles (CEO, Factory
Manager, etc.) is left to the future user-management module.

## Running locally (without Docker)

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) and a local
PostgreSQL instance.

```bash
cd apps/api

# 1. Point the app at your database (dev secret already set in appsettings.Development.json,
#    but override it for anything beyond a laptop):
dotnet user-secrets init --project src/Fixar.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=fixar_os_dev;Username=postgres;Password=postgres" --project src/Fixar.API

# 2. Run (pending migrations are applied automatically before the API starts):
dotnet run --project src/Fixar.API
```

Swagger UI: `http://localhost:5000/swagger`. Health checks: `/health/live`, `/health/ready`.

The `InitialCreate` migration (`Fixar.Infrastructure/Persistence/Migrations`) is committed and
has been validated end to end in this environment: applied against a real PostgreSQL 16
instance, then exercised through `register` → `login` → `me` → `refresh-token`, confirming
JWT issuance, the audit-log interceptor (rows appear in `AuditLogs` automatically), and the
standard response envelope all work together. Regenerate it (`dotnet ef migrations add
<Name>`) whenever the model changes.

## Running with Docker Compose

```bash
cd apps/api
cp .env.example .env   # fill in POSTGRES_PASSWORD and JWT_SECRET
docker compose up --build
```

This starts PostgreSQL, [Seq](https://datalust.co/seq) (structured log viewer, UI at
`http://localhost:8081`), and the API (`http://localhost:8080`). The API container runs
`ASPNETCORE_ENVIRONMENT=Production` by default. On every startup, the API waits for
PostgreSQL, applies all pending migrations, seeds the fixed RBAC roles, and only then begins
accepting requests. A migration or seed failure stops startup with a non-zero exit code so an
outdated schema is never served. `FIXAR_ADMIN_EMAIL` and `FIXAR_ADMIN_PASSWORD` are required
by Compose; the configured account is synchronized as an active `CEO` bootstrap admin.

## Configuration

All settings can be overridden via environment variables using the standard ASP.NET Core
double-underscore convention, e.g. `ConnectionStrings__DefaultConnection`, `Jwt__Secret`,
`Cors__AllowedOrigins__0`, `BootstrapAdmin__Email`, and `BootstrapAdmin__Password`. Never commit real secrets — `appsettings.Development.json`
contains a placeholder JWT secret for local use only, and `appsettings.json` /
`appsettings.Production.json` leave `Jwt:Secret` and the connection string empty on
purpose; the app fails fast at startup if `Jwt:Secret` is missing.

## Production notes

- **Migrations are applied automatically in every environment before request handling
  starts.** Database changes must remain backward compatible during rolling deployments.
  Review generated migrations before release and avoid starting multiple replicas
  simultaneously while a long-running migration is in progress.
- **Swagger is only enabled in Development.** Enable it for staging by adjusting the
  `IsDevelopment()` check in `Program.cs` if needed, ideally behind authentication.
- Rotate `Jwt:Secret` per environment and store it in a secret manager, not in
  `appsettings.*.json`.
- The RBAC role list seeded on startup (`ApplicationDbContextInitialiser`) comes from
  `docs/06_USER_ROLES.md`; update both together if roles change.

## Next steps (deliberately out of scope here)

- Business modules (Production, Inventory, Quality, Finance, etc.)
- MFA / SSO / passkey login (the docs call for these; only password + JWT is wired up)
- Automated test project
