---
intent: 002-production-readiness
phase: inception
status: draft
created: '2026-11-12T20:15:00Z'
updated: '2026-11-12T20:25:00Z'
---

# Requirements: Production Readiness (WeighingMS 1.x)

> **Stage: Requirements. Awaiting CHECKPOINT 2 (`APPROVE REQUIREMENTS`).**
> CHECKPOINT 1 (clarifying questions) was answered on 2026-11-13: env bootstrap, own-PIN step-up, an optional observability profile everywhere, GitHub Actions.

## Intent Overview

Release 1.0.0 (intent 001) completes the guided weighing workflow, but Operations found it cannot run in production (`memory-bank/operations/deployment/`). This intent makes WeighingMS deployable and operable at an FCI station:

1. **User provisioning (blocker, OPS-PROD-USERS):** outside Development no station users exist, so nobody can sign in.
2. **Hosting hardening:** OPS-FORWARDED-HEADERS, OPS-READY-SIMULATORS, OPS-LOG-JSON.
3. **Observability stack:** OPS-MONITORING-DEFERRED.
4. **CI pipeline:** E2E-CI-DEFERRED.

**Source basis (ADR-015).** Neither the PDF nor `workbook.md` covers production operation or user management. `brief.md` §9 makes WeighingMS (or an FCI directory) the **operator master**, with fields `badge_id`, `name`, `role (OPERATOR | SUPERVISOR)` and `active` (**A-09, unconfirmed**), and NFR "privileged actions require authentication; credentials never shared between roles". Everything else comes from the Operations findings and the decisions below. **These are project decisions, not FCI requirements.**

## Scope

- **In:** the four areas above, and release 1.1.0 through Operations with staging in the `Production` environment
- **Out:** sign-in against an FCI directory (AD/LDAP, A-09); real OrgaTEX and device integrations; central or multi-station deployment; a container registry (a CI artifact is enough until FCI names one); operator PINs (operators keep badge-only sign-in, as in intent 001)

## Decisions taken at intent creation and CHECKPOINT 1 (2026-11-13)

| ID | Decision |
|---|---|
| USR-ADMIN-SCREEN | Supervisors manage station users from a **Users** screen in the operator terminal. Not chosen: a config roster, an admin CLI, an FCI directory |
| USR-BOOTSTRAP-ENV | The first supervisor comes from a one-time environment bootstrap |
| USR-STEP-UP-OWN-PIN | Each user-management change needs the acting supervisor to re-enter their own PIN (step-up action `users.manage`) |
| OBS-OPTIONAL-PROFILE | The observability stack is a Compose profile for dev and staging, and an optional add-on in the production package. The apps can also export to an FCI-provided OTLP endpoint instead |
| CI-GITHUB-ACTIONS | CI targets GitHub Actions. Until a remote exists it is dry-run locally with `act` |

## Business Goals

| Goal | Success metric | Priority |
|---|---|---|
| BG-1 A station can be put into service without developer help | From the runbook alone, a fresh install reaches "first supervisor signed in, first operator added, B-24187 weighed end to end" in the `Production` environment | Must |
| BG-2 Access to the station stays under supervisor control and traceable | Every user change is made by a supervisor after PIN re-entry and appears in the audit log; deactivated users cannot sign in | Must |
| BG-3 Problems at a station are visible before operators report them | Dashboards and alerts cover readiness, devices, OrgaTEX sync and print backlog; each alert fires in a fault-injection test | Should |
| BG-4 Every change is verified the same way before release | CI runs the build, format, lint, unit, integration and E2E checks on every push, and builds release images for amd64 and arm64 on a version tag | Should |

---

## Functional Requirements

### A · User provisioning (unit candidates: station access, terminal UI)

#### FR-1: Bootstrap the first supervisor
- **Description**: At Api startup in any environment, if **no active supervisor exists** and `Bootstrap:Supervisor:BadgeId`, `:DisplayName` and `:Pin` are all set (for example `BOOTSTRAP_SUPERVISOR_*` in the env file), create that supervisor with the PIN stored hashed. Once any active supervisor exists, the values are ignored.
- **Acceptance Criteria**: (1) Fresh database plus values → one supervisor created, an audit entry `UserBootstrapped`, and a warning log saying to remove the values. The PIN never appears in logs. (2) Supervisor exists → nothing created; an info log if values are still set. (3) Values partly set → startup fails with a clear message. (4) No values and no supervisor → the Api starts and logs a warning that nobody can manage users.
- **Priority**: Must
- **Source**: OPS-PROD-USERS; USR-BOOTSTRAP-ENV

#### FR-2: Active flag and sign-in
- **Description**: Station users gain an `active` flag (brief.md §9 OPERATOR.active, A-09). Inactive users cannot sign in or step up. Deactivation ends the user's session at their next request.
- **Acceptance Criteria**: Sign-in with an inactive badge returns the same `401 Auth.BadgeNotRecognised` as an unknown badge, so it doesn't reveal that the account exists, and is audited. A session belonging to a user deactivated mid-session gets 401 on its next Api call. Existing users migrate as active.
- **Priority**: Must

#### FR-3: Users screen
- **Description**: A **Users** screen, reachable from the batch queue by supervisors only, lists every station user: badge, display name, role, status (active, inactive, PIN locked until …). Operators don't see the entry point, and the Api refuses them with 403.
- **Acceptance Criteria**: The list comes from the Api, with no hard-coded users. It sorts by active first, then name, and has a filter for inactive users. It follows `ux-guide.md` (touch targets, F-keys, AA contrast) and Transloco.
- **Priority**: Must

#### FR-4: Add a user
- **Description**: A supervisor adds an operator (badge + name) or a supervisor (badge + name + PIN entered twice).
- **Acceptance Criteria**: Badges are normalised the same way as at sign-in and must be unique among all users, active or not (409 `User.BadgeInUse`). Names must not be empty (FluentValidation, ProblemDetails). The supervisor PIN follows the existing rule (4–8 digits, Q-1). The new user can sign in immediately.
- **Priority**: Must

#### FR-5: Edit a user
- **Description**: A supervisor changes a user's display name or role. Operator → supervisor requires setting a PIN; supervisor → operator removes the PIN. The badge can't be changed; a new badge means a new user.
- **Acceptance Criteria**: Weighing records, tickets and audit entries keep the name **as recorded at the time** (no retroactive rename). Demoting the last active supervisor is refused (409 `User.LastSupervisor`).
- **Priority**: Must

#### FR-6: Deactivate and reactivate
- **Description**: A supervisor deactivates or reactivates a user. Users are **never deleted**, because records reference them (DR-01/DR-02, A-14).
- **Acceptance Criteria**: Deactivating yourself is refused (409 `User.Self`), and so is deactivating the last active supervisor (409 `User.LastSupervisor`). A reactivated user can sign in again with the same badge (and PIN).
- **Priority**: Must

#### FR-7: Reset a supervisor PIN and unlock
- **Description**: A supervisor sets a new PIN for another supervisor, which also clears a PIN lockout. A supervisor changes their own PIN by entering the current PIN.
- **Acceptance Criteria**: Lockout counters reset. Existing step-up grants for that user are revoked. Neither the old nor the new PIN is ever returned, logged or audited.
- **Priority**: Must

#### FR-8: Step-up for every user change
- **Description**: Every change in FR-4 to FR-7 requires the acting supervisor's own PIN, through the existing step-up flow with a new action `users.manage` (`Station:StepUpActions`). The existing lockout policy applies.
- **Acceptance Criteria**: Without a valid, unexpired grant for `users.manage` held by the acting supervisor → 403 `StepUp.Required`. A grant is single-use per change. A wrong PIN counts toward lockout. The UI asks for the PIN in a dialog and never caches it.
- **Priority**: Must
- **Source**: USR-STEP-UP-OWN-PIN; brief.md NFR "privileged actions require authentication"

#### FR-9: Audit of user changes
- **Description**: Each change is audited with the actor, the target badge, the change (fields changed with old and new values, PINs excluded) and the time. A supervisor can see a user's change history on the Users screen.
- **Acceptance Criteria**: New audit types: `UserBootstrapped`, `UserCreated`, `UserUpdated`, `UserDeactivated`, `UserReactivated`, `UserPinReset`, `UserPinChanged`. History is newest first and read-only.
- **Priority**: Must (audit) / Should (history view)

#### FR-10: Development seeding unchanged
- **Description**: The lab users are still seeded only in Development and Test, now with `active = true`. E2E and the evidence scripts keep working.
- **Acceptance Criteria**: The existing E2E suite passes without changes to its sign-in steps.
- **Priority**: Must

### B · Hosting hardening

#### FR-11: Forwarded headers in code
- **Description**: The Api handles `X-Forwarded-Proto` and `X-Forwarded-For` itself, trusting only proxies or networks listed in configuration (`Hosting:ForwardedHeaders:KnownProxies` / `KnownNetworks`; default loopback). This replaces the `ASPNETCORE_FORWARDEDHEADERS_ENABLED` mitigation.
- **Acceptance Criteria**: Behind a listed proxy: antiforgery 204 and sign-in works. The same headers from a non-listed address are ignored (antiforgery 500 / not HTTPS). The production package drops the environment-variable mitigation.
- **Priority**: Must
- **Source**: OPS-FORWARDED-HEADERS

#### FR-12: Health that covers dependencies
- **Description**: The simulator host gets `/health/live` and `/health/ready`, as in `system-architecture.md`. The Api's `/health/ready` adds a check that the integrations host is reachable, reported as **Degraded**, not Unhealthy. The station keeps working with devices or OrgaTEX down (safe-failure behaviour from intent 001), so a dependency outage must not take the Api out of service.
- **Acceptance Criteria**: Simulators down → Api `/health/ready` returns 200 with status `Degraded` and the failing check named. Postgres down → 503. Compose healthchecks use the simulator's `/health/ready`.
- **Priority**: Should
- **Source**: OPS-READY-SIMULATORS

#### FR-13: Structured logs
- **Description**: Outside Development, the Api and the simulators log compact JSON to the console (Serilog), carrying `trace_id`/`span_id` and the station ID. Development keeps the readable console format.
- **Acceptance Criteria**: Every log line in `Production` parses as JSON. No PIN, PIN hash, cookie or antiforgery token appears in any log (asserted by a test).
- **Priority**: Should
- **Source**: OPS-LOG-JSON

### C · Observability (tech-stack.md → Observability; ADR-014)

#### FR-14: Observability profile
- **Description**: Compose profile `observability` with the OTel Collector (contrib), Prometheus, Tempo, Loki and Grafana (provisioned datasources), configured under `observability/`. With the profile on, the Api and the simulators export OTLP to the Collector. The Collector filters healthy scale-poll spans and scrubs PII (`wms.pin`, badge secrets), as `tech-stack.md` specifies.
- **Acceptance Criteria**: `docker compose --profile full --profile observability up --wait` brings everything up healthy. One weighing produces a trace that spans SPA → Api → simulators → Postgres in Tempo, logs in Loki linked by trace ID, and metrics in Prometheus.
- **Priority**: Should

#### FR-15: Frontend telemetry
- **Description**: OTel Web SDK (stage 2 of ADR-014): `WebTracerProvider` + `StackContextManager` (zoneless), manual spans for business actions (select batch, confirm, override, complete, print, user change), Fetch + DocumentLoad instrumentation with scale polling excluded, web-vitals. Export goes to the same-origin `/otlp`, which the Api proxies to the Collector. With no Collector configured, this is a no-op.
- **Acceptance Criteria**: A confirm in the UI and the matching Api span share one trace. Telemetry failures never affect the UI.
- **Priority**: Could

#### FR-16: Station metrics
- **Description**: New meters in `WmsTelemetry` for the operational state: unsynced write-backs by status, tickets `printPending`, device readiness (per device), weighings confirmed, overrides, and sign-in failures and lockouts.
- **Acceptance Criteria**: Each metric is visible in Prometheus, and its value matches `/api/devices/health` and the database in an integration test.
- **Priority**: Should

#### FR-17: Dashboards and alerts
- **Description**: Provisioned Grafana dashboard "Station overview" (readiness, request rate, errors, latency, scale read duration, devices, OrgaTEX sync, print backlog, sign-ins) and alert rules from `operations/deployment/monitoring.md`: Api not ready, OrgaTEX sync failed, tickets piling up, device offline, scale read latency, disk.
- **Acceptance Criteria**: Each alert fires in a test that injects the matching fault through `/_sim/faults` (or stops a container) and clears afterwards. Thresholds are configurable.
- **Priority**: Should

#### FR-18: Observability in the production package
- **Description**: An optional `deploy/docker-compose.observability.yml` add-on with retention configurable (default 7 days), plus runbook sections for "run the stack on the station" and "send OTLP to FCI's collector instead".
- **Acceptance Criteria**: The production rehearsal with the add-on is healthy, and Grafana is reachable through the TLS proxy behind its own sign-in.
- **Priority**: Could

### D · CI (GitHub Actions)

#### FR-19: Verify on every push and pull request
- **Description**: The workflow `.github/workflows/ci.yml` runs on push and PR: `dotnet build` (warnings are errors), `dotnet format --verify-no-changes`, `npm run lint`, web unit tests, backend tests (Testcontainers), `specsmd:validate` and `specsmd:integrity`. Node comes from `.nvmrc`, .NET from `global.json`.
- **Acceptance Criteria**: Green on the current main. A deliberate format error, test failure or lint error each fails the run.
- **Priority**: Should

#### FR-20: E2E in CI
- **Description**: A job runs `npm run e2e` against the Compose `full` stack and uploads the Playwright report as an artifact.
- **Acceptance Criteria**: 15 or more specs pass in CI. The report is downloadable.
- **Priority**: Should

#### FR-21: Release on a version tag
- **Description**: On tag `v*`, build `weighingms/{api,migrator,simulators}` for linux/amd64 and linux/arm64 and upload the image tarballs, `SHA256SUMS` and the `deploy/` files as a release artifact (the same layout as the 1.0.0 handoff package).
- **Acceptance Criteria**: A tag produces an artifact whose checksums verify and whose amd64 images boot with the runbook.
- **Priority**: Should

#### FR-22: Run CI before a remote exists
- **Description**: Until the GitHub remote exists, the workflow can be run locally with `act` (documented), and every CI stage is also an npm script, so nothing is CI-only.
- **Acceptance Criteria**: `act push` (or the npm scripts) runs FR-19's jobs green locally.
- **Priority**: Should

### E · Release

#### FR-23: Release 1.1.0 through Operations
- **Description**: 1.1.0 goes through Build → Dev → Staging → Production handoff again, with **staging in the `Production` environment** (bootstrap supervisor, behind a TLS proxy). This retires OPS-STAGING-DEV and OPS-PROD-USERS.
- **Acceptance Criteria**: E2E passes against the 1.1.0 images. BG-1's walkthrough passes in the staging `Production` environment.
- **Priority**: Must

---

## Non-Functional Requirements

### Security
| Requirement | Target |
|---|---|
| PINs | Hashed with the existing `IPinHasher`; never returned, logged, audited or put in telemetry (asserted by tests and by the Collector scrub) |
| Authorisation | User management is supervisor-only on the server (403 otherwise), not just hidden in the UI |
| Bootstrap secret | Read only at startup and never persisted except as a hash; the runbook says to remove it after first use |
| Enumeration | An inactive or unknown badge gives the same response at sign-in and at step-up |
| Forwarded headers | Trusted only from configured proxies or networks |

### Reliability and operability
| Requirement | Target |
|---|---|
| Dependency outage | Simulators or OrgaTEX down never makes the Api unready (Degraded only); weighing safe-failure is unchanged |
| Observability failure | Collector or backends down never affects the station (export is fire-and-forget) |
| Observability footprint | The profile fits a station server: < 1.5 GB RAM total in default settings; retention 7 days by default |

### Performance
| Requirement | Target |
|---|---|
| Users screen | List and change round trip < 500 ms p95 on the station server for up to 500 users |
| Telemetry overhead | Api p95 latency rises < 10 % with export on |
| CI duration | Push pipeline without E2E < 15 min; with E2E < 30 min |

### Accessibility and UX
| Requirement | Target |
|---|---|
| Users screen | `ux-guide.md`: AA contrast, touch targets, F-keys, Transloco; axe checks in E2E as for the existing screens |

### Compatibility
| Requirement | Target |
|---|---|
| Existing data | Migration marks existing users active; no data loss; 1.0.0 → 1.1.0 upgrade follows the runbook (backup → load → up) |
| Tests | The existing backend (717), web (205) and E2E (15) suites pass unchanged, except for intentional updates |

---

## Assumptions and open questions

| ID | Item | Status |
|---|---|---|
| A-09 | WeighingMS is the operator master (not an FCI directory) | Unconfirmed (brief.md); built as a WeighingMS master |
| A-11 | One station or site server per site, local Postgres | Unconfirmed |
| A-14 | Records retained ≥ 5 years, hence no user deletion | Unconfirmed |
| Q-1 | PIN rules: reuse the existing rule, 4–8 digits (`StepUpCommandValidator`), hashed with ASP.NET Core `PasswordHasher` (`PinHasher`). No reuse or history rules | Default; confirm at CHECKPOINT 2 |
| Q-2 | Who may manage supervisors: any active supervisor (no "admin" tier) | Default; confirm at CHECKPOINT 2 |
| Q-3 | GitHub repository name and visibility | Needed before FR-19 runs remotely; the user creates the remote |

## Traceability to Operations findings

| Finding | Requirements |
|---|---|
| OPS-PROD-USERS | FR-1 – FR-10, FR-23 |
| OPS-FORWARDED-HEADERS | FR-11 |
| OPS-READY-SIMULATORS | FR-12 |
| OPS-LOG-JSON | FR-13 |
| OPS-MONITORING-DEFERRED | FR-14 – FR-18 |
| E2E-CI-DEFERRED | FR-19 – FR-22 |
| OPS-STAGING-DEV | FR-23 |
