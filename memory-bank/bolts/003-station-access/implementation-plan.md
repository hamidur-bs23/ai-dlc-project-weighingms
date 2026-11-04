---
stage: plan
bolt: 003-station-access
created: 2026-11-04T20:10:00Z
---

## Implementation Plan: station-access (bolt 003)

### Objective

Give the station an identity and an audit trail:
- operators sign in with a typed badge number and get a cookie session;
- a supervisor authorises one privileged action with badge + PIN (a single-use **step-up grant**);
- an append-only **audit trail** records sensitive events;
- every time is stored in UTC and shown in the **plant time zone** (`Asia/Dhaka` by default).

This is the first bolt that builds WeighingMS.Api with PostgreSQL and EF Core, so it also lays the persistence and error-handling foundations every later bolt reuses. These are the standard error responses, the mapping from ErrorOr results to ProblemDetails, and migrations.

Decisions: ADR-009 (badge sign-in + supervisor step-up), PD-06 (time zone). Requirements: FR-22, FR-19 (operator on every record), FR-15 (supervisor authorisation), NFR Security.

### Stories in scope

1. `001-operator-badge-sign-in` (must)
2. `002-supervisor-pin-step-up` (must)
3. `003-immutable-audit-trail` (should)
4. `004-plant-time-zone` (must)

### What will change

1. **Domain** (`WeighingMS.Domain/Access`, `/Audit`), pure with no I/O:
   - `StationUser`: badge ID, name, role (`Operator` / `Supervisor`), PIN hash (supervisors only), failed PIN attempts, lock-out time;
   - `StepUpGrant`: supervisor, action, issued, expires, used. It is single-use;
   - `AuditEntry`: type, actor, subject, reason, UTC time. There is no way to change one.
   - The domain rules:
     - a supervisor is locked out after N wrong PINs, for M minutes;
     - a grant can be used once, for its own action, before it expires.
2. **Application** (`WeighingMS.Application/Access`, `/Audit`, `/Abstractions`), with use cases returning ErrorOr:
   - `SignIn`, `SignOut`, `GetCurrentSession`;
   - `RequestStepUp` (verify badge + PIN → grant), and `ConsumeStepUpGrant` for later bolts (override in bolt 009);
   - the ports `IStationUserRepository`, `IStepUpGrantStore`, `IAuditTrail`, `IPinHasher`, `IPlantClock`;
   - FluentValidation validators;
   - an error catalogue: `Auth.SignInRequired`, `Auth.BadgeNotRecognised`, `Auth.SupervisorRequired`, `Auth.StepUpFailed`, `Auth.LockedOut`, `Auth.GrantInvalid`.
3. **Infrastructure** (`WeighingMS.Infrastructure/Persistence`, `/Access`):
   - `WeighingDbContext` using EF Core 10 with Npgsql, snake_case names and UUIDv7 keys;
   - the tables `station_users`, `step_up_grants`, `audit_entries`;
   - the first **migration**, `InitialStationAccess`;
   - repositories;
   - a PIN hasher built on `PasswordHasher<T>`;
   - `PlantClock`: `TimeProvider` plus the configured IANA zone, validated on start;
   - an **idempotent development seeder** that creates the lab users from `appsettings.Development.json` (dev seed data only).
4. **Api** (`WeighingMS.Api`):
   - cookie authentication: `HttpOnly`, `Secure`, `SameSite=Strict`, sliding expiry. 401 and 403 come back as ProblemDetails, never as a redirect;
   - authorization policies `RequireOperator` and `RequireSupervisor`;
   - **antiforgery**: an `XSRF-TOKEN` cookie, checked through the `X-XSRF-TOKEN` header on every state-changing request;
   - **rate limiting** on sign-in and step-up;
   - an `ApiController` base class that maps ErrorOr results to ProblemDetails, following the api-conventions table (Validation 422, Unauthorized 401, Forbidden 403, NotFound 404, Conflict 409, TooManyRequests 429);
   - every ProblemDetails carries `traceId`; a global exception handler covers unexpected errors;
   - `/health/ready` checks PostgreSQL; `Cache-Control: no-store` on `/api/*`.
5. **Tooling**:
   - the `dotnet-ef` local tool;
   - new pinned packages (below);
   - npm script `db:update`, which applies migrations to the development database;
   - updates to `.env.example` and `CLAUDE.md`.

### Files to be created / modified

- **Created**:
  - `src/WeighingMS.Domain/Access/*`, `src/WeighingMS.Domain/Audit/*`
  - `src/WeighingMS.Application/Access/*`, `Audit/*`, `Abstractions/*`, `Common/Errors/*`
  - `src/WeighingMS.Infrastructure/Persistence/*` (DbContext, configurations, `Migrations/`), `Access/*` (repositories, hasher, clock, seeder), `DependencyInjection.cs`
  - `src/WeighingMS.Api/Controllers/SessionController.cs`, `StationController.cs`, `Common/ApiControllerBase.cs`, `Common/ProblemDetailsMapping.cs`
  - `.config/dotnet-tools.json`
  - Tests (Stage 3): `tests/WeighingMS.Domain.Tests`, `tests/WeighingMS.Application.Tests`, `tests/WeighingMS.Api.IntegrationTests`
- **Modified**:
  - `src/WeighingMS.Api/Program.cs`, `appsettings*.json`
  - `Directory.Packages.props`, `WeighingMS.sln`, `package.json`, `.env.example`, `CLAUDE.md`

### APIs involved (WeighingMS.Api :5080; the route names are new WeighingMS resources)

| Method | Path | Auth | Purpose | Story |
|---|---|---|---|---|
| GET | `/api/antiforgery` | none | Issues the `XSRF-TOKEN` cookie the SPA sends back as `X-XSRF-TOKEN` | 001 |
| POST | `/api/session` | none + XSRF | Sign in with `{ badgeId, shift? }`. Returns 200 with the session (operator name, role, station, shift, plant time zone). An unknown badge returns 401 `Auth.BadgeNotRecognised`. A new sign-in replaces any current session and is audited | 001 |
| GET | `/api/session` | operator | Current session, or 401 `Auth.SignInRequired` | 001 |
| DELETE | `/api/session` | operator + XSRF | Sign out → 204, audited | 001 |
| POST | `/api/session/step-up` | operator + XSRF | Body `{ badgeId, pin, action }`. Returns 200 with `{ grantId, supervisorName, action, expiresAt }`. A wrong PIN returns 403 `Auth.StepUpFailed`; an operator badge returns 403 `Auth.SupervisorRequired`; a locked supervisor returns 429 `Auth.LockedOut`. Every attempt is audited | 002 |
| GET | `/api/station` | operator | `{ stationId, timeZone, utcNow, plantNow }`, for the status-strip clock and later display | 004 |

There is no audit-browsing endpoint; the story keeps that out of V1. The audit trail is a port that later bolts call: quit, override, re-tare, reprint, sync failure.

### Dependencies

- **Bolt 001** (complete): the solution, the build settings and Central Package Management.
- **A running Docker daemon is required for the Test stage.** The integration tests use Testcontainers PostgreSQL 17, and Docker (OrbStack) is **not running** right now. Without it, Stage 3 can only run the unit tests.
- **New NuGet packages**, all pinned:
  - `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `Microsoft.EntityFrameworkCore.Design`, `dotnet-ef` (local tool)
  - `ErrorOr`, `FluentValidation.DependencyInjectionExtensions`
  - `AspNetCore.HealthChecks.NpgSql`
  - for the tests: `Testcontainers.PostgreSql`, `Respawn`

### Technical approach

- **Clean layering**:
  - Domain holds the rules (lockout, grant validity);
  - Application orchestrates through ports;
  - Infrastructure implements the ports with EF Core;
  - the Api only translates HTTP to use cases.

  The architecture tests from bolt 001 keep this honest.
- **Operator sign-in is badge-only.** There is no PIN for operators (ADR-009, A-09), and badge scanning is out of V1 (PD-03). Supervisors need a PIN only for a step-up.
- **The step-up grant is a database row**, single-use, with a configurable time-to-live (default 2 minutes). Bolt 009 consumes it when it records an override, in the same transaction.
- **Audit writes join the action's transaction** (same DbContext and SaveChanges), so an audited action and its audit entry commit or fail together.
- **Lockout** is per supervisor: 5 wrong PINs lock the supervisor for 5 minutes, configurable. A per-IP rate limiter sits in front of the sign-in and step-up endpoints.
- **Time**:
  - everything is produced by `TimeProvider` and stored as `timestamptz` UTC;
  - `IPlantClock` converts to the configured IANA zone, so the server's own time zone has no effect;
  - an invalid zone stops start-up.
- **Seed users** live in `appsettings.Development.json`:
  - two operators, one of them "R. Karim", the operator shown in the mockup;
  - one supervisor, whose dev PIN is hashed when seeded.

  These are dev seed data only, allowed by the "no credentials" NFR. Production gets no seeded users.
- **Migrations are not applied at Api start-up** (data-stack). They are applied by `npm run db:update` in development and by the test fixture. The `migrator` service in Compose arrives with the Api container (bolt 006).

### Decisions at this checkpoint: resolved (APPROVE PLAN, 2026-11-05)

- **D1: option 1 chosen.** The simulator contracts stay as they are; the exception is recorded as SIM-CONTRACTS in `decision-index.md`.
- **D2: option 1 chosen.** OpenTelemetry moves to bolt 004.

#### Options as presented

**D1. The simulator contracts don't follow `api-conventions.md`.** Bolts 001 and 002 return:
- quantities as a plain number with a separate unit field (`targetQuantity` + `unit`, `net` + `unit`), where the convention wants `{ value, unit }`;
- collections as plain arrays, where the convention wants `{ items: [] }`;
- 400 for validation errors, where the convention wants 422.

The standard says it covers both WeighingMS.Api and the simulator.
1 - **(Recommended)** Keep the simulator contracts as they are, and record an exception: the simulator imitates *foreign* systems (OrgaTEX and devices), whose shapes we don't control. The adapters (bolts 004 and 005) translate them into WeighingMS conventions, which is what a real integration needs anyway. WeighingMS.Api follows `api-conventions.md` strictly.
2 - Align the simulator now: change both bolts' contracts and 113 tests to `{ value, unit }`, `{ items }` and 422.

**D2. OpenTelemetry moves to bolt 004, not this bolt** (bolt 001 deferred it "to bolt 003"). The reason for the deferral was "the first bolt with a real Api → simulator call", and that is bolt 004. This bolt makes no simulator call.
1 - **(Recommended)** Add OpenTelemetry in bolt 004.
2 - Add it in this bolt anyway.

### Acceptance criteria (from the stories)

- [ ] A seeded badge signs in; the session shows the name and shift. An unknown badge returns 401 `Auth.BadgeNotRecognised` and starts no session.
- [ ] Without a session, `GET /api/session` and `/api/station` return 401. Sign-out returns 204 and ends the session. A new sign-in replaces the current one and is audited.
- [ ] A correct supervisor badge + PIN gives a single-use grant for that action. A wrong PIN or an operator badge is refused, changes nothing, and is audited.
- [ ] 5 wrong PINs lock the supervisor (429 `Auth.LockedOut`). A grant can't be used twice, used for another action, or used after expiry.
- [ ] Audit entries are written with type, actor, subject, reason and UTC time. No code path updates or deletes one.
- [ ] With zone `Asia/Dhaka`, 2026-11-03T21:48:22Z shows as 2026-11-04 03:48, whatever the server's time zone. Every stored timestamp is UTC.
- [ ] State-changing requests without a valid XSRF token are rejected (400).
- [ ] PINs are stored only as hashes and never appear in logs or ProblemDetails.
- [ ] Errors are ProblemDetails with an `Area.Reason` type and a `traceId`.
- [ ] Build: 0 warnings, format check clean, architecture tests still pass.

### How this will be tested (Stage 3)

1. **Domain tests**: the lockout threshold and expiry, the grant rules (single use, action match, expiry), and that an audit entry can't be changed.
2. **Application tests**, with fakes: sign-in, unknown badge, step-up paths, lockout, an audit entry for each event, plant-time conversion using a `FakeTimeProvider` and the 21:48:22Z → 03:48 example.
3. **Api integration tests**: `WebApplicationFactory` plus **Testcontainers PostgreSQL 17** and Respawn.
   - the full HTTP flow with the cookie and XSRF token;
   - 401, 403 and 429 as ProblemDetails;
   - the migration applies cleanly;
   - restarting the Api keeps the audit entries;
   - the server time zone is set to UTC and to another zone.
4. **Architecture tests**: the existing layer rules, plus one new rule: no EF Core in Application.

All of these **need Docker running**.

### Out of scope

- Override recording (bolt 009); reprint auditing (bolt 012); sync-failure auditing (bolt 011).
- The sign-in screen (bolt 006).
- Badge scanning (PD-03); a real identity provider.
- An audit browsing UI.
