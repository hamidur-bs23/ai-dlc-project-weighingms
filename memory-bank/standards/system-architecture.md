# System Architecture

## Overview
High-level architecture for WeighingMS. It is a **Clean modular monolith** (WeighingMS.Api), a **separate simulator process** that stands in for OrgaTEX and the station devices, an **Angular SPA**, and **PostgreSQL**. Business rules are isolated from integrations so that the dummy systems can be replaced by real OrgaTEX, scales, scanner and printer through configuration and adapters only. Detailed context, inbound/outbound data and failure boundaries are elaborated in Inception (System Context, STEP 9). This standard fixes the architectural *patterns*.

## Architecture Style
**Clean architecture modular monolith + out-of-process simulator host.**

```text
┌──────────────── Station browser ────────────────┐
│ Angular SPA (signals, SignalStore, Transloco)   │
└───────────────┬─────────────────────────────────┘
                │ same origin /api, cookie auth + XSRF, traceparent
┌───────────────▼──────────────── WeighingMS.Api (:5080) ─────────────────────┐
│ Api            controllers · ProblemDetails · auth policies · health · OTel │
│ Application    use cases (commands/queries) · ports · validators · mapping  │
│ Domain         Batch · Recipe · Component · ToleranceBand · ScaleSelection  │
│                WeighingRecord · Override · domain errors (pure, no I/O)     │
│ Infrastructure EF Core/Postgres · outbox dispatcher · typed HttpClient       │
│                adapters (OrgaTEX, Scale, Printer, Scanner) + resilience     │
└───────┬───────────────────────────────────────────┬─────────────────────────┘
        │ EF Core / Npgsql                          │ HTTP (OTLP-traced)
┌───────▼────────┐                 ┌────────────────▼─── WeighingMS.Simulators.Host (:5090) ─┐
│ PostgreSQL 17  │                 │ Dummy OrgaTEX · Dummy Scale(s) · Dummy Printer ·         │
│ WeighingMS data│                 │ Dummy Scanner · JSON seed (B-24187…) · /_sim/* faults    │
└────────────────┘                 └──────────────────────────────────────────────────────────┘
                 all processes ──OTLP──▶ OTel Collector ─▶ Prometheus · Tempo · Loki ─▶ Grafana
```

**Dependency rule**: `Api → Application → Domain`; `Infrastructure → Application/Domain`. Domain depends on nothing; Application defines **ports** (`IOrgatexGateway`, `IScaleDevice`, `IPrinter`, `IScanner`, repositories, `TimeProvider`). The rule is enforced by project references and architecture tests.

**Modules (capabilities) inside the monolith** are organised as folders per capability in each layer (Batches, Weighing, Scales, Tickets). The final Unit decomposition comes in Inception (STEP 10) and may refine them.

**Simulator host** is a separate deployable. It owns the training data and contracts (`/api/batches…`, `/api/scales…`, `/api/tickets`, scanner TBD) and fault injection. It is **product code** and is tested like product code.

## API Design
- **REST over HTTP/JSON with ASP.NET Core Controllers**, documented with OpenAPI (`/openapi/v1.json`, Scalar UI). The full rules are in `api-conventions.md`
- The UI calls **only** WeighingMS.Api. Api endpoints preserve the workbook `/api/*` paths and are served through the adapters
- Live weight is **polled** (`GET /api/scales/{scaleId}/weight`, ~250 ms, configurable). Each reading returned to the UI includes the server-side tolerance classification for the active component
- Commands that change state (confirm, tare, complete, print, override) are explicit POST endpoints; the server re-validates every rule

## State Management
| Layer | Approach |
|---|---|
| Domain | Aggregates enforce invariants: a component cannot be confirmed outside tolerance (unless a supervisor override is granted); components progress in recipe order; a batch completes only when every component is confirmed |
| Persistence | Postgres is the source of truth for weighing sessions and records; optimistic concurrency (`xmin`) |
| Frontend: weighing session | **NgRx SignalStore** (`WeighingSessionStore`): explicit status union (`idle → taring → weighing → in-tolerance / under / over → confirmed`, `error`), current component, live reading, tare, lot/container, confirmations |
| Frontend: elsewhere | Plain signal services (`BatchQueueService`, `TicketService`, `ScannerService`, auth/session) |
| Reload / recovery | No browser persistence; on reload the SPA rebuilds state from the API (an in-progress batch resumes) |

## Caching Strategy
**No caching of business data in V1.** Batch queue, recipe and live weight are always fetched fresh; correctness beats latency for weighing.
- Configuration (threshold, scale assignments, tolerances) is read through `IOptionsMonitor<T>` (in-memory, reloadable)
- Static SPA assets use hashed filenames + long-lived cache headers; `index.html` is `no-cache`
- HTTP caching headers on `/api/*`: `Cache-Control: no-store`

## Security Patterns
- **Authentication**: badge sign-in → cookie session (`HttpOnly`, `Secure`, `SameSite=Strict`); roles `Operator`, `Supervisor`
- **Authorization**: `[Authorize]` by default; policy-based (`RequireOperator`, `RequireSupervisorStepUp`)
- **Step-up authentication** for privileged actions (override, manual scale reselection, calibration bypass): supervisor badge + PIN per action plus a mandatory reason, written to the audit trail (FR-35, NFR-12)
- **CSRF**: ASP.NET Core antiforgery with Angular XSRF support
- **Abuse protection**: rate limiting + lockout on sign-in and PIN verification
- **Transport**: HTTPS/HSTS; the simulator host is reachable only from the Api network in Compose (not published in `full` profile except for evidence/demo)
- **Headers**: CSP (`default-src 'self'`), `X-Content-Type-Options`, `Referrer-Policy`, `frame-ancestors 'none'`
- **Data protection**: PINs hashed; no secrets in the repo; PII kept out of telemetry
- **Auditability**: append-only audit records for confirmations, overrides, write-backs and prints

## Resilience & Failure Handling
| Failure | Architectural response |
|---|---|
| Scale unavailable / timeout | Adapter returns `IntegrationErrors.ScaleUnavailable` (503); UI shows a safe "scale offline" state; confirm disabled; no record written |
| OrgaTEX unavailable at batch completion | Completion and consumption are committed locally; the **outbox** retries delivery with backoff; UI shows "write-back pending" until confirmed; no re-keying, no duplicates (idempotency key) |
| OrgaTEX unavailable at batch load | Batch queue shows an unavailable state with retry; no stale invented data |
| Printer unavailable | Ticket is stored and marked "print pending"; reprint possible; batch completion is not rolled back |
| Invalid / out-of-range reading | Rejected by domain rules; never recorded as successful without an authorised override |
| Unexpected exception | Global `IExceptionHandler` → 500 ProblemDetails with traceId; logged and traced |

All adapters use `Microsoft.Extensions.Http.Resilience` (timeout, retry with jitter for idempotent calls, circuit breaker). The final behaviours are confirmed in Requirements (STEP 7), and the failure tests are in STEP 30.

## Health & Observability
- `/health/live` (process up) and `/health/ready` (Postgres + simulator reachable); the simulator exposes the same pair; Compose `--wait` depends on them
- OpenTelemetry in every process (see tech-stack → Observability); traces flow SPA → Api → Simulators → Postgres; the Collector applies filtering and PII scrubbing

## Decision Relationships
- Ports in Application + adapters in Infrastructure + an out-of-process simulator make "replace mocks with real systems" a configuration and adapter change, as the PDF requires
- The pure Domain + ErrorOr makes the tolerance and completion invariants testable in isolation (DDD bolts); UI and integration work fits Simple bolts
- The outbox + idempotency keys implement the brief's outage tolerance without distributed transactions
- No business-data caching + server-side re-validation guarantee a failed weighing is never recorded as successful
