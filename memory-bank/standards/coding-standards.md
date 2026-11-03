# Coding Standards

## Overview
Automated, strict-by-default standards for a C# (.NET 10) backend and an Angular/TypeScript frontend. Formatters and linters run on save, before each commit (husky + lint-staged) and in CI, and warnings fail the build. These rules keep business rules testable, keep integrations replaceable, and make failures explicit.

## Code Formatting

**Tools**: `dotnet format` (driven by `.editorconfig`) for C#; **Prettier** for TS/HTML/SCSS/JSON/MD.

**Key Settings**:
- C#: 4-space indent, file-scoped namespaces, `var` when the type is obvious, braces always, `using` directives outside the namespace and sorted (`System` first)
- TS/HTML/SCSS: 2-space indent, single quotes, semicolons, trailing commas `all`, print width 100
- Line endings LF, UTF-8, final newline

**Enforcement**: format on save (Rider / VS Code); `lint-staged` runs `prettier --write` and `dotnet format --include` on staged files; CI runs `dotnet format --verify-no-changes` and `prettier --check`.

## Linting

**Tools**: .NET analyzers + Roslynator + SonarAnalyzer.CSharp (C#); **angular-eslint** + typescript-eslint (TS/templates); **stylelint** with `stylelint-config-standard-scss` (SCSS).
**Base Config**: `AnalysisLevel=latest-recommended`; `angular-eslint` recommended + template accessibility; `typescript-eslint` strict-type-checked.
**Strictness**: **strict**. `TreatWarningsAsErrors=true` (C#); ESLint errors fail the build.

**Key Rules**:
- `<Nullable>enable</Nullable>` everywhere: null-safety for domain values
- No `float`/`double` for quantities, only `decimal` (code review + analyzer suppression is forbidden): avoids errors at tolerance boundaries
- `@typescript-eslint/no-explicit-any`: error, so API types stay honest
- `@angular-eslint/prefer-on-push-component-change-detection`: error, for a zoneless, signal-driven UI
- `@angular-eslint/template/*` accessibility rules (`label-has-associated-control`, `interactive-supports-focus`): error, for operator terminal usability
- **No hard-coded scenario data in the UI**: no batch, recipe or scale literals in `web/src/app/**`. Data comes from the API (workbook Rule 5). Checked in review and by a lint rule (`no-restricted-syntax` on `B-\d{5}` literals outside tests)
- Domain projects must not reference Infrastructure, ASP.NET or EF Core. Enforced by project references, plus an architecture test (`NetArchTest`-style check in the test suite)

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| C# types, methods, properties | PascalCase | `ToleranceBand`, `ConfirmComponent` |
| C# interfaces | `I` + PascalCase | `IScaleDevice`, `IOrgatexGateway` |
| C# private fields | `_camelCase` | `_clock` |
| C# locals/parameters | camelCase | `netWeight` |
| Async methods | `Async` suffix | `ReadWeightAsync` |
| Use cases | Verb + noun command/query + `Handler` | `ConfirmComponentCommand`, `ConfirmComponentHandler` |
| Error codes | `Area.Reason` | `Weighing.OutOfTolerance`, `Scale.Unavailable` |
| DB tables/columns | snake_case (by convention) | `weighing_records.actual_quantity` |
| HTTP routes | lower-case, plural nouns, kebab-case | `/api/batches/{batchId}/consumption` |
| JSON properties | camelCase | `actualWeight` |
| TS classes/types/interfaces | PascalCase, no `I` prefix | `WeighingSessionStore`, `ScaleReading` |
| TS variables/functions/signals | camelCase; signals named as nouns | `liveReading`, `canConfirm` |
| Angular selectors | `wms-` prefix, kebab-case | `wms-weigh-panel` |
| SCSS custom properties | keep the mockup token names | `--hmi-panel`, `--ok`, `--over` |
| SCSS classes | BEM-lite (`block__elem--mod`) inside component scope | `status-pill--over` |

**File Naming**:
- C#: one public type per file, file name = type name (`ToleranceBand.cs`)
- Angular: CLI style, kebab-case + type suffix (`weigh-panel.component.ts`, `weighing-session.store.ts`, `scale-api.service.ts`)
- Tests: C# `<Subject>Tests.cs` in a mirrored test project; TS `*.spec.ts` next to the source; Playwright `*.e2e.ts` under `playwright/tests/`

## File Organization

**Pattern**: **Clean architecture (backend, modules by capability)** + **feature-based (frontend)**.

**Structure**:
```text
WeighingMS.sln
global.json  Directory.Build.props  Directory.Packages.props  .editorconfig
docker-compose.yml  .env.example
observability/                  # otel-collector.yaml, prometheus.yml, tempo.yaml, loki.yaml, grafana/provisioning/
.github/                        # workflows/, dependabot.yml, pull_request_template.md
src/
  WeighingMS.Domain/            # entities, value objects, domain errors (no dependencies)
    Batches/  Weighing/  Scales/  Tickets/  Common/
  WeighingMS.Application/       # use cases (commands/queries), ports (interfaces), validators, Mapster configs
    Batches/  Weighing/  Scales/  Tickets/  Abstractions/
  WeighingMS.Infrastructure/    # EF Core, adapters to simulators (typed HttpClients + resilience), Serilog enrichers
    Persistence/  Integrations/OrgaTex|Scales|Printer|Scanner/
  WeighingMS.Api/               # controllers, ProblemDetails mapping, auth, OpenAPI, serves Angular dist
  WeighingMS.Simulators.Host/   # dummy OrgaTEX/Scale/Printer/Scanner + seed + fault injection
tests/
  WeighingMS.Domain.Tests/  WeighingMS.Application.Tests/
  WeighingMS.Api.IntegrationTests/  WeighingMS.Simulators.Tests/  WeighingMS.Architecture.Tests/
web/                            # Angular workspace (npm workspace)
  src/app/core/                 # api services, interceptors, error model, auth, pipes
  src/app/features/batch-queue/  weighing/  ticket/
  src/app/shared/ui/            # own CDK-based components
  src/styles/                   # _tokens.scss _mixins.scss _base.scss
  public/i18n/en.json           # Transloco translations
playwright/                     # npm workspace: config, fixtures, tests/*.e2e.ts
```

**Conventions**:
- Tests: backend in mirrored `tests/*` projects; frontend `*.spec.ts` co-located; E2E in `playwright/`
- Types: C# DTOs/contracts in `Application/<Feature>/Contracts`; TS API types generated into `web/src/app/core/api/generated/` (not edited by hand)
- Index files: no barrel `index.ts` files except the `core/api` public surface; import paths via the `@core/*`, `@features/*`, `@shared/*` aliases
- Configuration (scale threshold, scale assignments, tolerances, simulator URLs) comes from `appsettings*.json`/environment variables, **never constants in code**

## Testing Strategy

**Frameworks**: xUnit v3 + Shouldly (C#); Vitest + Testing Library (Angular); Playwright (E2E).
**Coverage Target**: **≥ 90 % line coverage on `WeighingMS.Domain` and `WeighingMS.Application`**; ≥ 70 % overall; every business rule in the requirements is covered by at least one test (traceability beats percentage).

**Test Types**:

| Type | Tool | When to Use |
|------|------|-------------|
| Domain unit | xUnit v3 + Shouldly | Every rule: tolerance classification (incl. boundaries 93.4/93.5/94.0/94.5/94.6), scale selection (≤ 3 kg vs > 3 kg incl. exactly 3 kg), progression, completion, overshoot rule |
| Application unit | xUnit + hand-written fakes of ports | Use-case orchestration and error paths (device unavailable, already confirmed) |
| API integration | WebApplicationFactory + Testcontainers Postgres + Respawn; simulator replaced by an in-process fake or a `Simulators.Host` test server | HTTP contract, ProblemDetails mapping, persistence, auth policies |
| Simulator | xUnit + WebApplicationFactory | Dummy contracts, scenarios (rising, below range, in range, overshoot, scale precision), fault injection |
| Architecture | NetArchTest-style rules | Layer dependencies (Domain has no dependency on Infrastructure/ASP.NET) |
| Frontend unit/component | Vitest + @testing-library/angular + `HttpTestingController` | Stores, guards, pipes, components by role/label |
| E2E | Playwright (+ axe) against the Compose `full` profile | Full B-24187 workflow, scale switch, tolerance UI, failure scenarios (STEP-29/30) |

**Conventions**:
- Test naming: C# `Method_State_ExpectedResult` (e.g. `Confirm_WeightAboveUpperTolerance_ReturnsOutOfTolerance`); TS/Playwright `describe('<unit>') / it('should …')`
- Test structure: Arrange–Act–Assert, one behaviour per test; `[Theory]` tables for boundaries
- Mock strategy: **fakes over mocking libraries** at port boundaries (`FakeScaleDevice`, `FakeOrgatexGateway`); `FakeTimeProvider` for time; no mocking of the Domain; E2E failure scenarios use simulator fault injection, not UI stubs
- TDD for Domain/Application in DDD bolts (test first for each business rule)

## Error Handling

**Pattern**: **Result pattern with ErrorOr** for expected failures; exceptions only for unexpected faults.

**Custom Errors**: yes. Static error catalogues per area (`WeighingErrors`, `BatchErrors`, `ScaleErrors`, `IntegrationErrors`), with codes in the `Area.Reason` form and a custom `Unavailable` error type for device/integration outages:
```csharp
public ErrorOr<WeighingRecord> Confirm(Reading r, OperatorId op, TimeProvider clock)
{
    if (Status == ComponentStatus.Confirmed) return WeighingErrors.AlreadyConfirmed;
    if (!Tolerance.Contains(r.Net)) return WeighingErrors.OutOfTolerance(Code, r.Net, Tolerance);
    ...
}
```
- Domain/Application return `ErrorOr<T>`; adapters translate HTTP or timeout failures into `IntegrationErrors.*Unavailable` (never leaking `HttpRequestException` upward)
- FluentValidation failures → `Error.Validation` entries
- No empty `catch`; no catching `Exception` except in the global handler and at adapter boundaries (where it is translated + logged)

**API Errors**: **ProblemDetails (RFC 9457)**. Mapping: Validation → 422 (`ValidationProblemDetails`), Conflict → 409, NotFound → 404, Unauthorized → 401, Forbidden → 403, Unavailable → 503, unexpected → 500 (via `IExceptionHandler`). `type` = error code, plus `traceId`, `batchId` and rule data (e.g. `min`, `max`, `net`). The Angular error interceptor maps ProblemDetails to a typed `AppError` and the UI shows defined, operator-readable states.

## Logging

**Tool**: Serilog (`Serilog.AspNetCore`; Console + rolling File sinks; Seq optional).
**Format**: structured JSON in containers/CI, readable console output in development.

**Levels**:

| Level | Usage |
|-------|-------|
| Error | Unexpected exceptions, write-back permanently failed, data integrity violations |
| Warning | Integration unavailable / retries exhausted, out-of-tolerance confirm attempt, supervisor override granted |
| Information | Business events: batch started, component confirmed, batch completed, consumption posted, ticket printed, operator sign-in/out |
| Debug | Adapter request/response summaries, state transitions (dev only) |
| Verbose | Per-poll scale readings (off by default) |

**Rules**:
- Always log: `CorrelationId` (per request) and **`BatchId`** + `OperatorId` (enriched via `LogContext` for the whole weighing session); event name; error code for failures; supervisor id + reason for overrides
- Never log: PINs, PIN hashes, cookies/auth tokens, connection strings, full request bodies with credentials
- Use message templates (`Log.Information("Component {ComponentCode} confirmed at {Net} {Unit}", …)`), never string interpolation
- Audit trail (who/what/when for confirmations and overrides) is **persisted in Postgres**, not just logged

## Observability

**Tool**: OpenTelemetry (.NET SDK; Angular: `traceparent` interceptor → OTel Web SDK in the observability bolt). See tech-stack → Observability.

**Naming**:

| Element | Convention | Example |
|---|---|---|
| Service name | `weighingms-<process>` | `weighingms-api`, `weighingms-simulators`, `weighingms-web` |
| Custom spans | `wms.<area>.<action>` | `wms.weighing.confirm`, `wms.batch.complete`, `wms.orgatex.post-consumption` |
| Span attributes | `wms.<noun>` (OTel semantic conventions for HTTP/DB) | `wms.batch.id`, `wms.component.code`, `wms.scale.id`, `wms.result` |
| Metrics | `wms.<area>.<measure>` + unit | `wms.weighings.confirmed` (count), `wms.scale.read.duration` (s), `wms.outbox.pending` |

**Rules**:
- **Low-cardinality metric labels only**: `scale_type`, `result`, `system`, `error_code`. **Never** use `batchId`, `operatorId`, `lot` or `container` as metric labels; they belong on spans and logs
- Healthy scale-poll requests (`GET /api/scales/*/weight`) are **not traced** (filtered in instrumentation and in the Collector) and are measured with `wms.scale.read.duration` instead. Failed polls are traced
- Every ProblemDetails includes `traceId`; the UI shows its first 8 characters as "Ref" in error banners
- Never put PINs, badge secrets, cookies or connection strings in spans, logs or metrics (also scrubbed in the Collector)
- Business actions are wrapped in a span in the Application use case, so a trace reads like the workflow
- Business audit (who/what/when) is written to Postgres; telemetry is operational and short-retention

## Configuration
- All tunable business values come from configuration, **never constants**: precision/heavy-duty scale threshold (default 3 kg, inclusive), scale assignments, tolerance overrides, polling interval, integration base URLs, timeouts
- Bound with the options pattern (`IOptions<T>`), validated with `ValidateOnStart()`; the app refuses to start on invalid configuration
- Environment-specific values via `appsettings.{Environment}.json` and environment variables; secrets only via environment or `.env` (git-ignored); `.env.example` is committed

## Internationalisation (i18n)
- **No literal operator-facing text in templates or TS.** Every string goes through Transloco (`{{ 'weigh.actions.confirm' | transloco }}` / `*transloco="let t"`)
- Key format: `<feature>.<element>.<action|label>` (lower camel segments), e.g. `batchQueue.table.shade`, `weigh.status.overTolerance`
- English (`en.json`) is the only V1 language; missing keys fail tests in CI (`missingHandler` throws in test mode)
- Numbers, units and dates are formatted with locale-aware pipes (`WeightPipe`, Angular `DatePipe` with the plant time zone), never string concatenation
- Backend error `title`/`detail` are developer-facing; the UI maps ProblemDetails `type` (error code) to a translated operator message

## Security Rules
- Cookie authentication (`HttpOnly`, `Secure`, `SameSite=Strict`) with role policies `Operator` and `Supervisor`; controllers are `[Authorize]` by default; anonymous endpoints are explicit (`/health/*`, sign-in)
- **Antiforgery** on all state-changing requests (XSRF cookie → `X-XSRF-TOKEN` header)
- Supervisor actions (override, manual scale reselection, calibration bypass) require **step-up re-authentication** (badge + PIN) per action plus a mandatory reason; PINs hashed with `PasswordHasher<T>`
- Rate limiting + lockout on sign-in and PIN attempts (e.g. 5 failures → 5-minute lockout, values configurable)
- Validate all input at the API boundary (FluentValidation); never trust UI-side tolerance checks: the server re-evaluates every confirmation
- No secrets in git; Dependabot enabled; CI fails on high/critical vulnerabilities (`npm audit`, `dotnet list package --vulnerable`)
- Simulator `/_sim/*` fault-injection endpoints are enabled only in Development/Test environments

## Development Workflow
- **Branching**: `main` is protected; one branch per bolt, `bolt/<bolt-id>-<unit-name>` (e.g. `bolt/001-dummy-integrations`); documentation-only work on `docs/<topic>`
- **Commits**: Conventional Commits: `feat(weighing): …`, `fix(api): …`, `test(domain): …`, `docs(memory-bank): …`, `chore: …`; reference story/requirement IDs in the body (`Refs: S-003, FR-12`)
- **Pull requests**: the template requires bolt id, stories, requirement IDs, test evidence (commands + results), screenshots for UI changes, and a checklist (no hard-coded scenario data, i18n keys added, tests added)
- **Pre-commit** (husky + lint-staged): Prettier, ESLint, stylelint, `dotnet format` on staged files
- **CI gates** (GitHub Actions): build → lint/format check → unit tests → integration tests (Testcontainers) → coverage threshold → vulnerability scan → Compose `full` + Playwright E2E (report and traces uploaded as artifacts)
- **Definition of done for a bolt**: all stages complete, tests green locally and in CI, the bolt's evidence files exist under `evidence/`, memory-bank statuses updated, human approval recorded
