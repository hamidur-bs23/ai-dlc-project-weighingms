# Tech Stack

## Overview
WeighingMS is a full-stack web application: an **Angular** operator-terminal SPA served by an **ASP.NET Core (.NET 10 LTS) Web API**, which is a Clean modular monolith over **PostgreSQL 17**. The external systems (OrgaTEX, scales, scanner, printer) are replaced by a **separate dummy host process** that the API calls over HTTP. The domain rules (tolerance, scale selection, batch completion) stay isolated from the integrations, and the mocks can later be replaced by real systems by changing configuration only.

> Decided 2026-11-03 (UTC) during project-init. Exact package versions are pinned when Construction scaffolds the code (Bolt 1). This file records the choices and the reasons for them.

## Languages
- **C# 14 / .NET 10 (LTS)** for the backend and the dummy host. SDK pinned in `global.json` (10.0.1xx).
- **TypeScript (strict)** for the frontend and the E2E tests.
- **SCSS** for styling.

Rationale: C# gives a strongly typed domain model with nullable reference types for the weighing rules. .NET 10 is the current LTS release, supported until Nov 2028. TypeScript strict mode and `strictTemplates` catch UI contract errors at compile time.

## Framework

### Backend: ASP.NET Core Web API (Controllers)
| Concern | Choice |
|---|---|
| API style | **Controllers** with attribute routing. One controller group per integration boundary |
| Architecture | **Clean modular monolith**: `WeighingMS.Domain` → `WeighingMS.Application` → `WeighingMS.Infrastructure` → `WeighingMS.Api` |
| Dummy integrations | **Separate process** `WeighingMS.Simulators.Host` (Dummy OrgaTEX, Scale, Printer, Scanner, with fault injection) |
| API docs | Built-in `Microsoft.AspNetCore.OpenApi` + **Scalar** UI (`/scalar`) |
| Validation | **FluentValidation** |
| Mapping | **Mapster** + **Mapster.Tool** (mappers generated at build time) |
| Result pattern | **ErrorOr**: expected failures are returned as `ErrorOr<T>` (see coding-standards → Error Handling) |
| Errors on the wire | **ProblemDetails (RFC 9457)**: base-controller mapping plus a global `IExceptionHandler` |
| Resilience | **Microsoft.Extensions.Http.Resilience** (Polly): timeouts, retry, circuit breaker on adapters |
| Time | **`TimeProvider`**, injected so timestamps can be tested |
| Logging | **Serilog** (structured, with a correlation ID per batch) |

### Frontend: Angular (latest stable, pinned at scaffold)
| Concern | Choice |
|---|---|
| Component model | **Standalone** components, `OnPush`, signals, `@if/@for` control flow, `input()/output()` |
| Change detection | **Zoneless**, if it is the default in the pinned version |
| Build | `@angular/build` (esbuild + Vite dev server). The dev proxy sends `/api` to WeighingMS.Api |
| Routing | Angular Router with 3 routes (`/batches`, `/batches/:id/weigh`, `/batches/:id/ticket`) + guards |
| State | **NgRx SignalStore** (`@ngrx/signals`) for the weighing session; **plain signal services** elsewhere |
| HTTP | `HttpClient` (`withFetch`, functional interceptors); RxJS `timer`+`switchMap` for scale polling |
| API types | **openapi-typescript**, generated from `/openapi/v1.json`, with thin hand-written API services |
| Forms | Typed Reactive Forms |
| Styling | **SCSS + CSS custom properties** (the mockup's 22 tokens) + **Angular CDK** (Dialog, A11y, Overlay) + our own components. **No Tailwind** |
| Printing | Print-preview route + print CSS + **JsBarcode** (Code128) |
| Scanner | `ScannerService` abstraction + Dummy Scanner. Mode (keyboard-wedge / device API / both) and scan scope *pending PD-03* |

### Runtime topology
```text
Angular SPA ──/api──▶ WeighingMS.Api (:5080) ──HTTP──▶ WeighingMS.Simulators.Host (:5090)
 (:4200 dev)           domain, use cases, records        Dummy OrgaTEX / Scale / Printer / Scanner
                       │                                  workbook contract: /api/batches, /api/scales,
                       ▼                                  /api/tickets, scanner (TBD), fault injection
                  PostgreSQL 17
```
- The UI calls **only** WeighingMS.Api. The Api exposes the workbook `/api/*` paths to the UI and serves them through typed HttpClient adapters (`IOrgatexGateway`, `IScaleDevice`, `IPrinter`, `IScanner`).
- The live weight is read by **polling** (`GET /api/scales/{id}/weight`, about every 250 ms).
- The final boundaries are recorded in System Context (STEP 9).

## Authentication
**Badge sign-in + supervisor PIN**, using ASP.NET Core cookie authentication with role-based authorization policies.
- The operator signs in with a badge (`badge_id`, assumption **A-09**), typed or scanned. Badge scanning is part of scan scope *pending PD-03*.
- Roles: `OPERATOR` and `SUPERVISOR`.
- Privileged actions require supervisor re-authentication (badge + PIN, stored hashed) for each action, with a recorded reason. These actions are supervisor override, manual scale reselection and calibration bypass (**FR-35, NFR-12**, brief l.1395).
- Rationale: suits a shared station terminal used by gloved operators, and can be replaced by AD/SSO later. Training-grade: the PINs and badges are seed data.

## Infrastructure & Deployment
**Docker Compose with profiles (hybrid)**, on **Docker Desktop**, which is already installed.
| Mode | Command | Runs |
|---|---|---|
| Development | `docker compose up -d postgres` | Postgres in a container; Api, Simulators.Host (`dotnet watch`) and Angular (`ng serve`) run natively for hot reload and F5 debugging (Rider / VS Code) |
| Demo / E2E / CI | `docker compose --profile full up --wait` | `postgres`, `migrator` (EF migrations bundle, runs once), `simulators`, `api` (multi-stage image serving API + Angular `dist`) |

- Configuration comes from environment variables (`ConnectionStrings__Db`, `Integrations__Simulators__BaseUrl`, …). Secrets go in a git-ignored `.env`; `.env.example` is committed.
- Outage scenarios (STEP-30): `docker compose stop simulators` / `pause` really makes the dependencies unavailable.
- **CI**: GitHub Actions. Build, lint, unit and integration tests (Testcontainers), then the Compose `full` profile and Playwright.
- **Target production (assumption, A-11):** one station or site server on-premise per site, with local Postgres so weighing continues during OrgaTEX or network outages. Details are deferred to the Operations phase.
- **Known constraints:** Docker Desktop on Windows/macOS cannot pass through serial/USB scales, so a real scale adapter may need to run natively. Docker Desktop's commercial licensing applies to larger organisations.

## Observability
**OpenTelemetry (vendor-neutral, OTLP)** for traces, metrics and logs. It complements the **Postgres audit trail**, which remains the system of record for business traceability; it never replaces it.

| Part | Choice | When |
|---|---|---|
| .NET instrumentation | `OpenTelemetry.Extensions.Hosting` + AspNetCore, HttpClient, Npgsql (`AddNpgsql`) and Runtime instrumentation; custom `ActivitySource("WeighingMS")` + `Meter("WeighingMS")`; `UseOtlpExporter()` (endpoint from `OTEL_EXPORTER_OTLP_ENDPOINT`) | **Bolt 1**, in Api and Simulators.Host |
| Logs | Serilog → Console/File **and** OTLP (`Serilog.Sinks.OpenTelemetry`), carrying TraceId/SpanId | Bolt 1 |
| W3C trace context | Propagated automatically Api → Simulators.Host (one trace across processes) | Bolt 1 |
| Frontend, stage 1 | `traceparent` HTTP interceptor; trace id shown as "Ref" in error banners | First frontend bolt |
| Backend stack | Compose profile `observability`: **OTel Collector (contrib)** → **Prometheus** (metrics), **Tempo** (traces), **Loki** (logs), **Grafana** (provisioned datasources + dashboards). Config in `observability/` | **Dedicated observability bolt**, after the core workflow |
| Frontend, stage 2 | **OTel Web SDK**: `WebTracerProvider` + `StackContextManager` (zoneless app, so business actions get manual spans), Fetch + DocumentLoad instrumentation (scale polling excluded), `web-vitals`, export to same-origin `/otlp` proxied to the Collector | Same observability bolt |

The Collector owns the cross-cutting telemetry policy: it filters healthy scale-poll spans, scrubs PII (`wms.pin`, badge secrets), batches, and routes to the backends. Production backend and retention are decided in Operations; because the apps only emit OTLP, swapping the backend is a configuration change.

## Platform Concerns (cross-cutting)
| Concern | Choice |
|---|---|
| Health | `Microsoft.Extensions.Diagnostics.HealthChecks` + `AspNetCore.HealthChecks.NpgSql`; `/health/live`, `/health/ready` (DB + simulator reachability); used by Compose `healthcheck` / `--wait` |
| Configuration | Options pattern with `ValidateDataAnnotations()` / FluentValidation + `ValidateOnStart()` for scale threshold, scale assignments, tolerances, integration URLs |
| i18n | **Transloco** (`@jsverse/transloco`): runtime JSON translations, English only in V1 (brief A-10) |
| Security | Cookie auth + role policies; **Antiforgery** (`IAntiforgery`, XSRF-TOKEN cookie → `X-XSRF-TOKEN` header via Angular `withXsrfConfiguration`); built-in **rate limiting** (`Microsoft.AspNetCore.RateLimiting`) for sign-in and PIN attempts + lockout; PIN hashing with ASP.NET Core `PasswordHasher<T>`; HTTPS/HSTS on the station network; security headers (CSP, X-Content-Type-Options, frame-ancestors) |
| Supply chain | **Dependabot** (NuGet, npm, Docker, GitHub Actions); CI runs `dotnet list package --vulnerable --include-transitive` and `npm audit --audit-level=high` |

## Package Manager
- **npm** (v12.2.1) with **npm workspaces** (`web/`, `playwright/`). Node **v26.13.0 via nvm only**, pinned in `.nvmrc`, `engines` and `.npmrc` (`engine-strict=true`); `~/.zshenv` loads nvm so non-interactive shells (IDE tasks, git hooks, Claude Code) use it too. Homebrew's node (24.4.1, below Angular CLI 22's `^24.15.0` minimum) is **not** used for the project. Angular CLI 22.2.5 supports `>=26.0.0` (verified 2026-11-04).
- **NuGet** with **Central Package Management** (`Directory.Packages.props`).
- **dotnet local tools** (`.config/dotnet-tools.json`): `dotnet-ef`, `Mapster.Tool`.

## Testing & Quality Tooling
| Layer | Tools |
|---|---|
| Backend unit | xUnit v3 + Shouldly |
| Backend integration | `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory) + Testcontainers.PostgreSql + Respawn |
| Coverage | coverlet / Microsoft Testing Platform code coverage |
| C# analysis | .NET analyzers (`AnalysisLevel=latest-recommended`), Roslynator, SonarAnalyzer.CSharp, `.editorconfig` + `dotnet format` |
| Frontend unit / component | Vitest (Angular unit-test builder, jsdom) + @testing-library/angular + user-event |
| Frontend lint / format | angular-eslint (flat config) + Prettier + stylelint (standard-scss) |
| E2E | Playwright (TypeScript) + @axe-core/playwright |
| Dev ergonomics | concurrently (`npm run dev`), husky + lint-staged (pre-commit) |

## Editors & Local Tools
- **Rider** (backend) and **VS Code** with C# Dev Kit, Angular Language Service, ESLint, Prettier, Stylelint, Playwright Test, Docker.
- Docker Desktop 27.5, PostgreSQL client tools, an optional pgAdmin/DBeaver, and the `postgres-inspector` MCP, which will be re-registered against the Compose database (positional connection URL, local scope).

## Decision Relationships
- **Separate dummy host + typed HttpClient adapters + Polly resilience** together make the "OrgaTEX / scale / printer unavailable" behaviour real and testable, and make real systems a configuration change.
- **Clean modular monolith + ErrorOr** keep the tolerance and batch-completion rules pure and visible in method signatures. This supports the PDF's rule that a failed weighing must never be recorded as successful.
- **OpenAPI → openapi-typescript** ties the Angular models to the C# contracts, so they cannot drift.
- **Polling** matches the required `GET /weight` contract and is easy to intercept in Playwright. SignalR remains a possible later upgrade.
- **SCSS + mockup CSS tokens (no Tailwind)** keeps the prototype's visual language without copying its implementation.
- **Compose profiles** give a fast native development loop *and* a one-command, production-like stack for the demo, E2E and CI.
- **Testcontainers + Postgres 17** means the integration tests run against the same database engine as production.
- **OpenTelemetry + a separate simulator host** produces genuine distributed traces (UI → Api → Simulators → Postgres). Emitting OTLP from Bolt 1 while building the Grafana stack in its own bolt keeps observability from delaying the core workflow, the PDF's top priority.
- **Zoneless Angular** means the Web SDK cannot rely on `ZoneContextManager`, so business actions (confirm, complete, print) get explicit spans.
- **Cookie auth** requires **antiforgery**. The SPA and API share one origin (Api serves `dist`; dev proxy), so no CORS is needed.
- **Transloco + externalised text** satisfies brief A-10 without a build per locale, so a station can switch language per operator later.
- **`ValidateOnStart` configuration** puts the PDF's "configurable, not hard-coded" threshold and tolerance rules into practice: bad configuration stops startup instead of producing wrong weighings.
