---
last_updated: 2026-11-03T20:03:53Z
total_decisions: 14
---

# Decision Index

This index tracks all Architecture Decision Records (ADRs). ADR-001..014 were decided during **project-init** (Tech Stack Standard, 2026-11-03) and point to the standard that holds the full rationale; later ADRs are created during Construction bolts under `bolts/{bolt-id}/`.

## How to Use

**For Agents**: Scan the "Read when" fields below to identify decisions relevant to your current task. Before implementing new features, check if existing ADRs constrain or guide your approach. Load the full ADR (or referenced standard) for matching entries.

**For Humans**: Browse decisions chronologically or search for keywords. Each entry links to the full rationale, alternatives considered, and consequences.

---

## Decisions

<!-- Entries are appended below in reverse chronological order (newest first) -->

### ADR-014: Separate frontend telemetry stages: traceparent first, OTel Web SDK later
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/tech-stack.md#observability`
- **Summary**: Full browser tracing is wanted but costs time, and zoneless Angular complicates context propagation. A traceparent interceptor + 'Ref' id ships early; the OTel Web SDK (StackContextManager, manual business spans, polling excluded) comes in the observability bolt.
- **Read when**: frontend telemetry, error banner Ref, OTel web, zoneless context

### ADR-013: OpenTelemetry everywhere; separate Collector + Prometheus + Tempo + Loki + Grafana
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/tech-stack.md#observability`
- **Summary**: Operational traceability across 3 processes is needed alongside the Postgres audit trail. All apps emit OTLP from Bolt 1; the 5-component stack is built in a dedicated observability bolt behind the Compose 'observability' profile.
- **Read when**: tracing, metrics, logs, Grafana, Collector config, metric labels, polling spans

### ADR-012: Transloco for externalised operator text
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/coding-standards.md#internationalisation-i18n`
- **Summary**: Brief A-10 requires all operator-facing text externalised. Transloco is chosen for runtime JSON translations (English only in V1).
- **Read when**: UI text, labels, translations, error message mapping

### ADR-011: SCSS + prototype CSS tokens + Angular CDK; no Tailwind
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/ux-guide.md`
- **Summary**: The prototype is token-based hand-written CSS. SCSS with the same token names and own CDK-based components is chosen; Tailwind and component libraries are rejected.
- **Read when**: styling, components, colours, status semantics, accessibility

### ADR-010: NgRx SignalStore for the weighing session; signal services elsewhere
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/system-architecture.md#state-management`
- **Summary**: The weighing screen is a state machine with derived rules. SignalStore models it with an explicit status union; other features use plain signal services.
- **Read when**: frontend state, weigh screen, confirm enabled, progression

### ADR-009: Badge sign-in + supervisor step-up PIN (cookie auth)
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/system-architecture.md#security-patterns`
- **Summary**: Operators are identified by badge (A-09) and privileged actions need supervisor authentication (FR-35, NFR-12). Cookie auth with role policies, per-action supervisor badge+PIN step-up, antiforgery and rate limiting are chosen.
- **Read when**: authentication, override, supervisor, CSRF, lockout

### ADR-008: Docker Compose with profiles (dev / full / observability) on Docker Desktop
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/tech-stack.md#infrastructure--deployment`
- **Summary**: A fast native dev loop and a one-command production-like stack are both needed. Compose profiles are chosen: postgres-only for dev, full (migrator, simulators, api) for demo/E2E/CI, observability for the telemetry stack.
- **Read when**: running the app, CI, E2E, outage tests, migrations

### ADR-007: PostgreSQL 17 + EF Core (Npgsql), simulator owns OrgaTEX data
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/data-stack.md`
- **Summary**: WeighingMS needs transactional, reviewable weighing records. Postgres 17 with EF Core code-first, numeric quantities, UTC timestamptz and a transactional outbox are chosen; dummy OrgaTEX data stays in the simulator.
- **Read when**: persistence, migrations, quantities, outbox, write-back, seed data

### ADR-006: Mapster with Mapster.Tool code generation
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/tech-stack.md#framework`
- **Summary**: DTO/domain mapping is needed without licensing risk. Mapster with build-time generated mappers is chosen.
- **Read when**: mapping, DTOs

### ADR-005: Result pattern with ErrorOr + ProblemDetails
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/coding-standards.md#error-handling`
- **Summary**: Expected business/device failures must be explicit and never recorded as success. Domain/Application return ErrorOr<T>; controllers map error types to RFC 9457 ProblemDetails; exceptions only for unexpected faults.
- **Read when**: errors, tolerance failures, HTTP status codes, controllers

### ADR-004: Polling for live weight
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/api-conventions.md#api-style`
- **Summary**: The required contract is GET /api/scales/:scaleId/weight. The UI polls (~250 ms, configurable) through WeighingMS.Api; SignalR is a possible later upgrade.
- **Read when**: live weight, scale API, polling interval

### ADR-003: Separate simulator host process for dummy integrations
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/system-architecture.md#architecture-style`
- **Summary**: Dummy OrgaTEX/Scale/Printer/Scanner are product code and must be replaceable. They run in WeighingMS.Simulators.Host, called over HTTP by typed adapters; the UI calls only WeighingMS.Api; fault injection via /_sim/*.
- **Read when**: integration boundaries, dummy APIs, fault injection, replacing mocks

### ADR-002: Controllers + Clean modular monolith on .NET 10
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/system-architecture.md#architecture-style`
- **Summary**: Domain rules (tolerance, scale selection, completion) must be isolated from integrations. ASP.NET Core Controllers over Domain/Application/Infrastructure/Api projects are chosen.
- **Read when**: project structure, layering, new endpoints, DDD bolts

### ADR-001: Angular + ASP.NET Core Web API + PostgreSQL stack
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `standards/tech-stack.md`
- **Summary**: A full-stack web app is needed for the operator terminal. Angular (standalone, signals, zoneless) + ASP.NET Core .NET 10 LTS + PostgreSQL 17, npm/Node 26 and Playwright E2E are chosen.
- **Read when**: any technology choice, adding dependencies
