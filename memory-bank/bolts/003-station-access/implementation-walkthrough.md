---
stage: implement
bolt: 003-station-access
created: 2026-11-04T20:55:00Z
---

## Implementation Walkthrough: station-access (bolt 003)

### Summary

WeighingMS.Api now has station access:
- **badge sign-in** with a cookie session;
- **supervisor badge + PIN step-up**, which issues a single-use grant and locks the supervisor out after 5 wrong PINs;
- an **append-only audit trail**, saved in the same transaction as the action it records;
- the **plant clock** (UTC, shown in Asia/Dhaka).

It runs on the first EF Core and PostgreSQL schema, `InitialStationAccess`. The Api pipeline is in place for every later bolt: errors as ProblemDetails, CSRF protection, rate limiting, no-store caching and health checks.

### Structure Overview

- **Domain** holds the rules, with no I/O:
  - a station user with lockout;
  - a step-up grant that can be used once, for its own action, before it expires;
  - an immutable audit entry.
- **Application** runs the use cases through ports (users, grants, audit trail, PIN hasher, plant clock, unit of work) and returns ErrorOr results. FluentValidation checks the input.
- **Infrastructure** implements the ports:
  - EF Core over Npgsql, with snake_case names, UUIDv7 keys and `xmin` concurrency;
  - a PBKDF2 PIN hasher;
  - a time-zone-aware clock that fails at start-up on an unknown zone;
  - an idempotent development user seeder.
- **The Api** maps HTTP to the use cases:
  - one base controller maps ErrorOr errors to ProblemDetails, following the api-conventions table;
  - authentication, antiforgery and rate-limit failures also come back as ProblemDetails.

### Completed Work

- [x] `src/WeighingMS.Domain/Access/StationUser.cs`, `StationRole.cs`, `LockoutPolicy.cs` - users, roles, normalised badges, the wrong-PIN lockout rule
- [x] `src/WeighingMS.Domain/Access/StepUpGrant.cs`, `AccessErrors.cs` - single-use grant (used once, own action, before expiry) and the `Auth.*` error catalogue
- [x] `src/WeighingMS.Domain/Audit/AuditEntry.cs`, `AuditEventType.cs` - immutable audit entries; event types for this bolt and for later bolts
- [x] `src/WeighingMS.Application/Access/StationAccessService.cs` - sign in (audits a replaced session), sign out, step-up (unknown badge, non-supervisor, locked out, wrong PIN, granted, all audited), consume a grant (for bolt 009)
- [x] `src/WeighingMS.Application/Access/*Command*.cs`, `*Validator.cs`, `StationAccessOptions.cs`, `SessionInfo.cs`, `StepUpGrantResult.cs` - inputs, validation (badge format, 4–8 digit PIN, known action), configuration
- [x] `src/WeighingMS.Application/Abstractions/*`, `Access/I*.cs`, `DependencyInjection.cs` - ports and registration
- [x] `src/WeighingMS.Infrastructure/Persistence/WeighingDbContext.cs` - DbContext and unit of work; refuses to modify or delete audit entries
- [x] `src/WeighingMS.Infrastructure/Persistence/Configurations/*` - the `station_users`, `step_up_grants` and `audit_entries` mappings
- [x] `src/WeighingMS.Infrastructure/Persistence/Migrations/*_InitialStationAccess.cs` - the first migration (generated; analyzers disabled for this folder)
- [x] `src/WeighingMS.Infrastructure/Persistence/DesignTimeDbContextFactory.cs` - used by `dotnet ef`; reads `ConnectionStrings__Db` or the local development default
- [x] `src/WeighingMS.Infrastructure/Access/*` - repositories, audit trail, PIN hasher, plant clock, development user seeder
- [x] `src/WeighingMS.Api/Program.cs` - composition:
  - cookie authentication that returns ProblemDetails instead of redirecting;
  - policies, antiforgery and the rate limiter;
  - the no-store header on `/api/*`;
  - `/health/live` and `/health/ready` (PostgreSQL);
  - development seeding.
- [x] `src/WeighingMS.Api/Common/*` - error-to-ProblemDetails base controller, antiforgery filter and cookies, claim and policy names, ProblemDetails writer, trace id
- [x] `src/WeighingMS.Api/Controllers/AntiforgeryController.cs`, `SessionController.cs`, `StationController.cs` - the endpoints from the plan
- [x] `src/WeighingMS.Api/Contracts/*` - request and response records
- [x] `src/WeighingMS.Api/appsettings*.json` - station, lockout and rate-limit settings; development connection string and lab users
- [x] `.config/dotnet-tools.json` - `dotnet-ef` 10.0.14
- [x] `docker-compose.yml`, `.env.example` - host port 5433 for Postgres (`POSTGRES_PORT`)
- [x] `.editorconfig` - migrations treated as generated code
- [x] `package.json` - `db:update`, `db:migrations:add`
- [x] `CLAUDE.md` - commands, lab users, session endpoints, port 5433

### Key Decisions

- **Cookie security depends on the environment.** Test and Production use `Secure` cookies with the `__Host-` prefix. Local Development runs over plain HTTP (`http://localhost:5080`), so it uses ordinary cookies. ASP.NET's antiforgery refuses Secure cookies on a non-HTTPS request, which the smoke test showed.
- **The antiforgery token is re-issued on sign-in and sign-out.** ASP.NET binds tokens to the identity, so the token from before sign-in is invalid afterwards. The smoke test found this, and the fix is in place.
- **The development Postgres moved to host port 5433.** A native PostgreSQL (Homebrew/EDB) was already listening on 5432 on this machine, so `localhost:5432` reached the wrong server. The port is configurable through `POSTGRES_PORT`.
- **Grants and lockout are stored in the database**, not in memory, so they survive an Api restart. A second consumption of the same grant at the same time is blocked by `xmin` concurrency.
- **Rate limit: 10 sign-in and step-up attempts per minute per IP** (configurable), in front of the per-supervisor lockout.
- **The audit trail can't be modified.** The DbContext throws if an audit entry is ever modified or deleted, in addition to there being no endpoint to do so.
- **Every ProblemDetails `traceId` is the W3C trace id when there is one**, falling back to the ASP.NET request id. Responses no longer mix the two formats.

### Deviations from Plan

- **OpenTelemetry**: none in this bolt, per decision D2 (moved to bolt 004).
- **Lab users**: only R. Karim comes from the mockup. `OP-1002` (S. Akter) and `SV-2001` (M. Rahman, dev PIN 2468) are lab seed data.
- **Extra plumbing for local development**: cookie security by environment, the antiforgery token refresh, port 5433, and the design-time connection from the environment. The live smoke test made all four necessary.

### Dependencies Added

- [x] `ErrorOr` 2.1.1 (Domain) - result pattern
- [x] `FluentValidation.DependencyInjectionExtensions` 12.2.0 (Application) - validation
- [x] `Microsoft.EntityFrameworkCore` 10.0.14, `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, `EFCore.NamingConventions` 10.0.1, `Microsoft.EntityFrameworkCore.Design` 10.0.14 (Infrastructure, and design-time in the Api) - persistence and migrations
- [x] `Microsoft.Extensions.Identity.Core` 10.0.14 - `PasswordHasher` for PINs
- [x] `AspNetCore.HealthChecks.NpgSql` 9.0.0 - readiness check
- [x] Pinned for Stage 3: `Testcontainers.PostgreSql` 4.16.0, `Respawn` 7.0.0

### Developer Notes

- **Local run**:
  1. `POSTGRES_PASSWORD=change-me docker compose up -d postgres`, or create a `.env` from `.env.example`;
  2. `npm run db:update`;
  3. `npm run api`.
  
  Sign in as `OP-1001`; the supervisor is `SV-2001` with PIN `2468` (dev seed only).
- **Verification so far**:
  - `dotnet build`: 0 warnings, 0 errors; format check clean; all 122 existing tests pass.
  - The migration SQL was checked: the `xmin` system column is not created.
  - The migration applies to Postgres 17.
  - A live smoke test of every endpoint passed. It covered XSRF, an unknown badge, sign-in, the session, the station clock, a grant, a non-supervisor badge, lockout on the 5th wrong PIN, 422 validation, replacing a session, sign-out and the rate limiter. It produced 13 correct audit rows, and no PIN appeared in the logs.
- **Workbook relevance**: this provides the "operator" field recorded on confirmations (STEP 25).
- The development database still holds the smoke-test rows, and `SV-2001` is locked until the lockout expires (5 minutes). The integration tests use their own throwaway database.
