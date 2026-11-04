---
last_updated: 2026-11-03T21:13:25Z
total_decisions: 15
---

# Decision Index

This index tracks all Architecture Decision Records (ADRs). ADR-001..015 were decided during **project-init** (Tech Stack Standard, 2026-11-03) and point to the standard that holds the full rationale; later ADRs are created during Construction bolts under `bolts/{bolt-id}/`.

## How to Use

**For Agents**: Scan the "Read when" fields below to identify decisions relevant to your current task. Before implementing new features, check if existing ADRs constrain or guide your approach. Load the full ADR (or referenced standard) for matching entries.

**For Humans**: Browse decisions chronologically or search for keywords. Each entry links to the full rationale, alternatives considered, and consequences.

---

## Decisions

<!-- Entries are appended below in reverse chronological order (newest first) -->

### ADR-015: Source precedence for requirements
- **Status**: accepted
- **Date**: 2026-11-03
- **Bolt**: project-init (standards)
- **Path**: `../../docs/project-requirement.md` (Reference notes §C)
- **Summary**: The PDF (project requirement) is silent on write-back, printer/ticket, scanner, outage and time zone, which workbook.md mandates and brief.md details on unconfirmed assumptions. The PDF and workbook.md define mandatory scope; brief.md fills gaps only where both are silent, citing its IDs and assumption IDs (A-xx), marked unconfirmed; the HTML mockup defines visual and interaction patterns.
- **Read when**: deriving requirements, resolving conflicts between sources, citing brief.md, scope questions

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
- **Summary**: WeighingMS needs transactional, reviewable weighing records. Postgres 17 with EF Core code-first, numeric quantities and UTC timestamptz are chosen; dummy OrgaTEX data stays in the simulator. (The outbox for write-back is pending PD-01.)
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

---

## Pending Decisions (resolve in Inception/Construction)

Behaviours that are **not decided**. Standards that mention them say *"pending PD-0x"*; any text there is a proposed default only. Each is resolved at the step shown, then converted into requirements (and an ADR where architectural) and marked `resolved` here.

| ID | Topic | Decide at | PDF | workbook.md | brief.md (unconfirmed) | Mockup | Options discussed | Referenced in |
|---|---|---|---|---|---|---|---|---|
| PD-01 | OrgaTEX unavailable: consumption write-back, sync, locally held batch/recipe data | STEP 7 | silent | STEP 26 POST consumption; STEP 30 "defined behaviour" | FR-26, FR-30–33, BR-16/17 (Must), BR-18 (Should), A-03; l.1211 alternative | status strip | store-and-forward outbox · stop the station · local save + manual retry | system-architecture, data-stack, api-conventions |
| PD-02 | Printer unavailable at completion | STEP 7 | silent | STEP 27 dummy printer, print/preview; STEP 30 | FR-27, FR-28, IR-16, A-04 | ticket view (Print F1) | print pending + manual print · print pending + auto-print · block completion | system-architecture, api-conventions |
| PD-03 | Scanner mode and scan scope | STEP 7 | silent | §0 Dummy Scanner required; contract undefined | FR-02, FR-09, FR-10, IR-15 (wedge or serial), A-09, A-10 | — | wedge + device API · device API only · wedge only; scope candidates: badge, batch, dyestuff/lot, container | tech-stack, api-conventions, ux-guide, system-architecture |
| PD-04 | Ticket: paper size, layout, configurability, barcode content, override marking | STEP 7 | silent (completed-job summary, §6 Step 9) | STEP 27 fields: batch, order, machine, shade, date, operator, components, target, actual, barcode | FR-27, FR-28, IR-16 ("configurable without code change"), A-04 | ticket mock; content marked "invented" | fixed V1 layout · configurable template; 80 mm / A6 / A4 | ux-guide |
| PD-05 | Overshoot / surplus handling | **STEP 24** (workbook mandate) | warn (Step 6); no success without explicit authorised exception (Step 7) | STEP 24: present 2–3 options, human decides; do not copy prototype calculation | FR-17–24, A-08 | overshoot + override behaviour | (presented at STEP 24) | ux-guide, system-architecture, coding-standards |
| PD-06 | Time zone for displayed and printed timestamps | STEP 7 | silent ("confirmation timestamp") | ticket "date" | silent | ticket shows local time | configured IANA zone · station OS time · UTC | data-stack, ux-guide, coding-standards |
| PD-07 | Chemical weighing order | STEP 7 | operator selects a chemical (Steps 4, 8) | STEP 23 fixed progression | FR-06 "present in recipe sequence" | any non-done chemical selectable | free selection (sequence shown) · strict sequence · sequence + supervisor skip | system-architecture, ux-guide, coding-standards |

All PD items registered 2026-11-03.

### Status

Decisions marked *resolved* below were taken by the product owner at STEP 7 on 2026-11-03T22:30:00Z, and approved at CHECKPOINT 2 (`APPROVE REQUIREMENTS`) on 2026-11-04T07:30:00Z. Full evidence for each is in `intents/001-assisted-weighing-station/decisions-brief.md`, and the requirements that apply them are in that intent's `requirements.md`. These are project decisions, not FCI confirmations.

| ID | Status | Decision |
|---|---|---|
| PD-01 | resolved (STEP 7, approved CP2) | Store-and-forward outbox. Completion and the outbox entry are written in one transaction. Idempotent POST, automatic bounded retry, then a visible "sync failed" state. The unsynced count is shown |
| PD-02 | resolved (STEP 7, approved CP2) | Completion is not blocked. The ticket is held as print pending and printed manually (F1); prints after the first are marked REPRINT. Accepted risk: BR-15 |
| PD-03 | resolved (STEP 7, approved CP2) | Device-style HTTP scanner dummy (`POST /_sim/scanner/scan`). It scans batch and lot, with touch or typed fallback. No wedge, badge scan, container scan or identity verification. Deviates from IR-15 and FR-09 |
| PD-04 | resolved (STEP 7, approved CP2) | Fixed V1 layout, 80 mm, with STEP 27 fields. Code 128 barcode = batch ID; overridden lines marked "OVR"; on-screen preview. Deviates from IR-16 and NFR-14 |
| PD-05 | **pending** | Overshoot / surplus handling, decided at STEP 24 |
| PD-06 | resolved (STEP 7, approved CP2) | Stored as UTC; shown and printed in a configured IANA zone (default `Asia/Dhaka`) |
| PD-07 | resolved (STEP 7, approved CP2) | Free selection of any pending chemical; sequence shown and the next one suggested. Completion needs all N chemicals |
| TOL | resolved (STEP 7, approved CP2) | Recipe tolerance if provided, else configured tolerance per chemical + scale, else weighing is blocked. No universal default. Inclusive bounds, snapshotted on the record |
| OVR | resolved (STEP 7, approved CP2) | Any out-of-tolerance weighing needs a supervisor badge + PIN step-up (ADR-009) and a mandatory reason. The record keeps its true class and is flagged as overridden |
| SEED-01 | resolved (CP5, bolt 001) | Dummy OrgaTEX serves training recipes for B-24188, B-24190 and B-24191. They are invented for the lab (no source), flagged `trainingOnly` in the seed, and are not claims about FCI data |
| SIM-CONTRACTS | resolved (bolt 003 plan, 2026-11-05) | The simulator contracts imitate foreign systems (OrgaTEX, devices). They keep plain numbers with a separate unit, plain arrays and 400 validation errors. They are exempt from the `{value, unit}`, `{items}` and 422 rules of `api-conventions.md`, and the WeighingMS adapters translate them. WeighingMS.Api follows `api-conventions.md` strictly |
| OTEL-TIMING | resolved (bolt 003 plan, 2026-11-05) | OpenTelemetry instrumentation is added in bolt 004, the first Api → simulator call; tech-stack had planned it for "Bolt 1" |
