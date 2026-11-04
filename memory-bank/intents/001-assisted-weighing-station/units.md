---
intent: 001-assisted-weighing-station
phase: inception
status: units-decomposed
updated: 2026-11-04T15:00:00Z
---

# Assisted Weighing Station: Unit Decomposition

> Inputs: the approved Intent, `requirements.md` (FR-1…FR-28, CHECKPOINT 2), `system-context.md` (ports, boundaries B0–B3) and `ui-analysis.md`.
> Rule followed: **units are capabilities, not screens**. The three mockup screens are all served by one UI unit. The business logic behind them lives in the backend units.
> The project type is `full-stack-web`. The backend is decomposed domain-driven, with one frontend unit (`{intent}-ui`).

## 1. Evaluating the candidate capabilities

| Candidate (workbook) | Verdict | Reason |
|---|---|---|
| Batch Management | **Merged** with Recipe → `003-batch-recipe` | Both are read from OrgaTEX in one load. A recipe has no life without its batch, and WeighingMS never edits either (OrgaTEX owns them: system-context Ownership). One snapshot gives one consistent read model. |
| Recipe & Material Management | **Merged** (above), plus the material config | "Material" in V1 means only the configured tolerance per chemical + scale (the TOL fallback). There is no material master or stock (out of scope). |
| Weighing Station | **Its own unit, the core** → `005-weighing-station` | This is the domain heart: tolerance band, 4 classes, scale selection, tare and stability guards, confirm, override, quit, progression, completion and records. It is **more than a screen**: every rule is enforced on the server (FR-9 server-side class), so the UI only renders it. |
| Scale Integration | **Own unit, broadened to the station devices** → `004-device-integration` (scale + scanner) | Both are polled device adapters behind ports (`IScaleDevice`, `IScanner`), with the same health and status-strip concerns and the same failure handling (FR-23, FR-24). They're too small to justify one unit each. |
| OrgaTEX Integration | **Split by direction** | **Inbound** (queue + recipe) is only the source of the batch data, so it goes inside `003-batch-recipe`. **Outbound** write-back has its own reliability capability (outbox, idempotency, bounded retry, sync failed), so it gets its own unit, `006-consumption-writeback`. Keeping both directions in one unit would create a dependency cycle (batch load → weighing → completion → write-back). |
| Ticket & Printing | **Own unit** → `007-ticket-printing` | Ticket composition (fixed 80 mm layout, Code 128, OVR, plant time zone), the print queue, manual print and REPRINT form a separate lifecycle that runs after completion, with its own failure mode (PD-02). |
| *(not listed)* Simulators | **Own unit** → `001-station-simulators` | A separate deployable (ADR-003), first-class product code (CLAUDE.md), and everything else is tested against it. |
| *(not listed)* Station access | **Own unit** → `002-station-access` | Operator sign-in, the supervisor PIN step-up (ADR-009), the audit trail and the plant time zone are cross-cutting. FR-15, FR-19, FR-21 and FR-22 all depend on them. |
| *(required by project type)* UI | **One frontend unit** → `008-assisted-weighing-station-ui` | All three mockup screens, the status strip and the dialogs. There's deliberately **one** UI unit, not one per screen. |

## 2. Units overview

| # | Unit | Responsibility (one line) | Assigned FRs | Bolt type | Complexity |
|---|---|---|---|---|---|
| 001 | `station-simulators` | Training dummies for OrgaTEX, scales, scanner and printer, plus `/_sim/*` control | FR-25, FR-26, FR-27, FR-28 | Simple | M |
| 002 | `station-access` | Who is at the station and what they may do: sign-in, step-up, audit, plant time | FR-22 | Simple | S |
| 003 | `batch-recipe` | Batch queue, selection, recipe snapshot from OrgaTEX, tolerance config | FR-1, FR-2, FR-3, FR-24(a) | Simple | M |
| 004 | `device-integration` | Scale and scanner adapters: reading, tare, status, scan delivery, device health | FR-23, FR-24(b) | Simple | M |
| 005 | `weighing-station` | Guided-weighing domain: classification, guards, confirm, override, progression, completion, records | FR-4 … FR-19 | **DDD** | L |
| 006 | `consumption-writeback` | Store-and-forward posting of the actual consumption to OrgaTEX | FR-20, FR-24(c) | Simple | M |
| 007 | `ticket-printing` | Ticket composition, print queue, manual print, reprint | FR-21, FR-24(d) | Simple | S |
| 008 | `assisted-weighing-station-ui` | Angular operator terminal: 3 screens, status strip, dialogs, keys, translations | — (renders all user-facing FRs; owns the Usability NFRs) | Simple | L |

## 3. Requirement-to-Unit mapping (each FR assigned once)

| FR | Requirement | Unit |
|---|---|---|
| FR-1 | Batch queue | 003-batch-recipe |
| FR-2 | Batch selection | 003-batch-recipe |
| FR-3 | Recipe loading | 003-batch-recipe |
| FR-4 | Component identification and selection | 005-weighing-station |
| FR-5 | Dyestuff lot | 005-weighing-station |
| FR-6 | Container | 005-weighing-station |
| FR-7 | Tare and re-tare | 005-weighing-station |
| FR-8 | Scale selection (≤ 3 kg threshold) | 005-weighing-station |
| FR-9 | Live weight (server-side class) | 005-weighing-station |
| FR-10 | Target quantity | 005-weighing-station |
| FR-11 | Remaining quantity | 005-weighing-station |
| FR-12 | Tolerance (4 classes, inclusive, TOL source) | 005-weighing-station |
| FR-13 | Confirmation | 005-weighing-station |
| FR-14 | Quit and incomplete batch | 005-weighing-station |
| FR-15 | Supervisor override | 005-weighing-station |
| FR-16 | Overshoot (warn / block; PD-05 pending) | 005-weighing-station |
| FR-17 | Component progression | 005-weighing-station |
| FR-18 | Batch completion and summary | 005-weighing-station |
| FR-19 | Weighing record and traceability | 005-weighing-station |
| FR-20 | OrgaTEX write-back (outbox) | 006-consumption-writeback |
| FR-21 | Ticket generation | 007-ticket-printing |
| FR-22 | Time zone | 002-station-access |
| FR-23 | Scale unavailable or invalid weight | 004-device-integration |
| FR-24 | OrgaTEX / scanner / printer unavailable | **Split by criterion** (the only cross-cutting FR): (a) OrgaTEX on load → 003 · (b) scanner → 004 · (c) OrgaTEX at completion → 006 · (d) printer → 007 |
| FR-25 | OrgaTEX dummy | 001-station-simulators |
| FR-26 | Scale dummy | 001-station-simulators |
| FR-27 | Scanner dummy | 001-station-simulators |
| FR-28 | Printer dummy | 001-station-simulators |

**NFR ownership:**
- Security (sign-in, step-up, audit) → 002.
- Usability (F-keys, text labels, Transloco, touch targets) → 008.
- Performance (≤ 250 ms reading, < 1 s confirm) → 004 + 005.
- Reliability: durability and resume → 005; outage tolerance → 006.
- Configurability:
  - threshold → 005;
  - tolerance config → 003;
  - time zone → 002;
  - poll interval → 004;
  - retry policy → 006.
- Testability: every unit; the end-to-end Playwright test → 008.

## 4. Unit Dependency Graph (unit map)

```text
                       ┌──────────────────────────┐
                       │ 001 station-simulators   │  (separate process :5090; contracts B2)
                       └───┬──────────┬───────┬───┘
            dummy contract │          │       │ dummy contract
     ┌─────────────────────┘          │       └──────────────────────────┐
     ▼                                ▼                                  ▼
┌──────────────────┐         ┌──────────────────────┐   ┌────────────────────────────────────┐
│ 003 batch-recipe │         │ 004 device-integration│   │ 006 consumption-writeback          │
│ (OrgaTEX inbound)│         │ (scale + scanner)    │   │ 007 ticket-printing   (both use the │
└────────┬─────────┘         └──────────┬───────────┘   │  OrgaTEX / printer dummies)         │
         │ recipe snapshot,             │ IScaleDevice  └───────────────▲────────────────────┘
         │ tolerance config             │ readings, tare                │ BatchCompleted
         ▼                              ▼                               │ (domain event)
       ┌──────────────────────────────────────────────────────────────┴─┐
       │ 005 weighing-station   (DDD core: classify · guard · confirm · │
       │                         override · progress · complete · record)│
       └───────────────▲────────────────────────────────────────────────┘
                       │ operator identity, supervisor step-up, audit, plant time
              ┌────────┴─────────┐
              │ 002 station-access│ ◄── also used by 007 (time zone, reprint auth)
              └──────────────────┘

 008 assisted-weighing-station-ui ──HTTP /api only (B0)──► the Api endpoints of 002, 003, 004, 005, 006, 007
```

| Unit | Depends on | Depended by | Kind of dependency |
|---|---|---|---|
| 001 station-simulators | — | 003, 004, 006, 007 | HTTP training contract |
| 002 station-access | — | 005, 007, 008 | Identity / step-up / audit / clock services |
| 003 batch-recipe | 001 | 005, 008 | Recipe snapshot + tolerance config (read model) |
| 004 device-integration | 001 | 005, 008 | `IScaleDevice`, `IScanner` ports |
| 005 weighing-station | 002, 003, 004 | 006, 007, 008 | `BatchCompleted` event + records (read) |
| 006 consumption-writeback | 001, 005 | 008 | Outbox status (read) |
| 007 ticket-printing | 001, 002, 005 | 008 | Ticket / print status (read) |
| 008 …-ui | 002–007 (HTTP) | — | Api only |

There are no cycles. Completion is decoupled by the **`BatchCompleted`** domain event, which goes through the same transaction as the outbox entry (FR-20). Weighing never calls write-back or printing directly.

> **Refinement to `system-architecture.md` (proposed; pending your approval at CP3):** the single port `IOrgatexGateway` is split into **`IOrgatexBatchReader`** (003) and **`IOrgatexConsumptionWriter`** (006), following interface segregation. Both are still backed by one typed OrgaTEX HttpClient, and there is no change to the boundary (B1).

## 5. Execution order

1. **001 station-simulators** and **002 station-access**. These are the foundation and can be built in parallel. The first bolt also scaffolds the solution (Api / Domain / Application / Infrastructure / Simulators.Host / web), per CLAUDE.md.
2. **003 batch-recipe** and **004 device-integration**, in parallel.
3. **005 weighing-station**: the DDD core.
4. **006 consumption-writeback** and **007 ticket-printing**, in parallel.
5. **008 assisted-weighing-station-ui**: built progressively. Each UI bolt follows its backend bolt (queue → weigh → complete/print), so the end-to-end flow works early (top acceptance criterion). The final bolt is the Playwright end-to-end test (STEP 29).

The exact bolts are planned at STEP 12.

## 6. Units in detail

### Unit 001: station-simulators
- **Responsibility**: Provide realistic, controllable **training substitutes** for OrgaTEX, the two scales, the scanner and the printer, in a separate process (`WeighingMS.Simulators.Host`, :5090).
- **Included capabilities**:
  - Dummy OrgaTEX API: `GET /api/batches`, `/:id`, `/:id/recipe`, `POST /:id/consumption`; seed B-24187 and others.
  - Dummy Scale API: 2 scales; status, weight, tare; scenarios rising / belowRange / enterRange / overshoot / stable; resolution per scale.
  - Dummy Scanner: `POST /_sim/scanner/scan`, status.
  - Dummy Printer: `POST /api/tickets`.
  - `/_sim/reset`, `/_sim/faults` and the inspection endpoints (`/_sim/orgatex/consumption`, `/_sim/printer/tickets`).
- **Dependencies**: none.
- **Why separate**: a separate deployable (ADR-003) whose contracts are training-only (B2). Every integration unit is tested against it. It changes for different reasons (scenarios, faults) than the product logic does.
- **Bolt type**: **Simple**: integration/utility work with no domain rules; the behaviour is defined by FR-25 to FR-28.

### Unit 002: station-access
- **Responsibility**: Identify who is at the station and authorise privileged actions. Keep the immutable audit trail. Provide the plant clock and time zone.
- **Included capabilities**:
  - Badge sign-in (typed) → cookie session, roles Operator and Supervisor.
  - Supervisor badge + PIN step-up for each action (rate-limited, PIN hashed).
  - Audit records (overrides, quits, re-tares, reprints, sync failures).
  - `TimeProvider` with UTC storage and display in the configured IANA zone (default Asia/Dhaka, FR-22).
  - Lab seed users.
- **Dependencies**: none.
- **Why separate**: cross-cutting security policy (ADR-009) that 005, 007 and 008 use. Keeping it apart stops auth concerns leaking into the weighing domain, and it can be swapped for a real identity source later.
- **Bolt type**: **Simple**: uses the standard ASP.NET Core auth and policy building blocks, with few domain rules.

### Unit 003: batch-recipe
- **Responsibility**: Present the OrgaTEX batch queue, let the operator select a batch (touch or scan), and load an immutable **recipe snapshot**. Also own the configured tolerance per chemical + scale (the TOL fallback).
- **Included capabilities**:
  - The queue (FR-1).
  - Selection by touch or scanned batch ID (FR-2).
  - Recipe load and snapshot: ordered components, target, unit, recipe tolerance (FR-3).
  - The tolerance configuration read model.
  - The `IOrgatexBatchReader` adapter.
  - OrgaTEX-unavailable-on-load behaviour: error + Retry, no invented data (FR-24a).
- **Dependencies**: 001 (dummy OrgaTEX contract).
- **Why separate**: OrgaTEX owns this data, and WeighingMS only reads it. It's a read model with a different change cadence and failure mode from the weighing rules. It can be developed and tested against the dummy before any weighing exists.
- **Bolt type**: **Simple**: mostly adapter + read model; its invariants (snapshot immutability, sequence) are small.

### Unit 004: device-integration
- **Responsibility**: Turn the station devices into reliable ports: live scale readings (value, unit, stable flag), tare commands, scale and scanner status, and scan delivery to the Api.
- **Included capabilities**:
  - `IScaleDevice` adapter, polled at about 250 ms (ADR-004, configurable).
  - Invalid-reading detection: missing, non-numeric, below zero, above capacity, error flag (FR-23).
  - Scale-offline handling.
  - `IScanner` adapter and scan routing hand-off (FR-27 consumer side; the delivery detail is fixed in its bolt).
  - Scanner-offline behaviour (FR-24b).
  - Device health for the status strip.
  - Resilience policies (timeout, retry, circuit breaker).
- **Dependencies**: 001 (dummy scale and scanner contracts).
- **Why separate**: the hardware boundary (B1). The real scales and scanner have unknown protocols (brief Q15, Q16), so isolating them here means replacing them later is an adapter change only. The weighing domain stays free of I/O.
- **Bolt type**: **Simple**: integration work; its rules are transport and validity, not business rules.

### Unit 005: weighing-station
- **Responsibility**: The **guided-weighing domain**: everything that decides whether a weighing is valid and what is recorded.
- **Included capabilities**:
  - Component selection: free choice, sequence suggested (PD-07; FR-4, FR-17).
  - Lot and container capture (FR-5, FR-6).
  - Tare required before confirm, plus re-tare (FR-7).
  - Automatic scale selection with a configurable ≤ 3 kg inclusive threshold (FR-8).
  - Server-side classification of every reading into 4 classes with inclusive bounds and fixed-point decimals (FR-9 to FR-12).
  - Tolerance source: recipe, then config, otherwise blocked.
  - Confirm guard: in tolerance, stable and tared (FR-13).
  - Quit / incomplete batch (FR-14).
  - Supervisor override with PIN + reason, keeping the true class and an OVR flag (FR-15).
  - Overshoot warn/block, with the PD-05 policy pending (FR-16).
  - Completion when all N components are confirmed, plus the summary and review list (FR-18).
  - The weighing record and traceability (FR-19).
  - Resume after an interruption.
  - Raises `BatchCompleted`.
- **Dependencies**: 002 (operator identity, step-up, audit, clock), 003 (recipe snapshot, tolerance config), 004 (scale port).
- **Why separate**: this is the core domain with the richest invariants. It is the reason the product exists, and it's **more than a screen**: the UI only renders the classification and state this unit computes. It has to be modelled carefully (aggregates `WeighingSession`, `ComponentWeighing`, value objects `Quantity`, `ToleranceBand`, `ScaleSelection`, `WeighingRecord`, `Override`) and tested exhaustively (PDF §7 and STEP 22 boundary values).
- **Bolt type**: **DDD** (Model → Design → ADR → Implement → Test): domain-heavy, with aggregates, invariants and an open policy (PD-05) that will need an ADR at STEP 24.

### Unit 006: consumption-writeback
- **Responsibility**: Deliver the actual consumption of every completed batch to OrgaTEX **exactly once from the operator's point of view**, without ever blocking the station.
- **Included capabilities**:
  - An outbox entry written in the completion transaction (handles `BatchCompleted`).
  - A background dispatcher: `POST /api/batches/:id/consumption` with an `Idempotency-Key`, bounded automatic retry with back-off, then the "sync failed" state and manual retry (FR-20, PD-01).
  - Sync status and the unsynced count for the UI.
  - OrgaTEX-unavailable-at-completion behaviour (FR-24c).
  - The `IOrgatexConsumptionWriter` adapter.
- **Dependencies**: 005 (`BatchCompleted` and the record data), 001 (dummy OrgaTEX consumption endpoint).
- **Why separate**: a distinct reliability capability (store-and-forward) with its own state machine, background process and failure mode. Separating it removes the inbound/outbound cycle with 003, and it can be tested by injecting OrgaTEX faults without touching the weighing logic.
- **Bolt type**: **Simple**: integration work. The outbox state machine (pending → sent / failed) is small and fully specified by FR-20; it doesn't need DDD stages.

### Unit 007: ticket-printing
- **Responsibility**: Produce the weighing ticket for a completed batch and get it printed, without blocking completion.
- **Included capabilities**:
  - Ticket composition with a fixed V1 layout (80 mm): STEP 27 fields, plant-zone date, OVR marks, and a Code 128 barcode of the batch ID (PD-04, FR-21).
  - On-screen preview data.
  - A print queue: "print pending" → printed.
  - Manual print (F1), and REPRINT marking for every print after the first (PD-02).
  - Printer-offline behaviour (FR-24d).
  - The `IPrinter` adapter.
- **Dependencies**: 005 (`BatchCompleted`, records), 002 (time zone, reprint audit), 001 (dummy printer).
- **Why separate**: an output channel with its own lifecycle after completion and its own device failure. The layout may become configurable later (it deviates from brief IR-16, which is noted), so isolating it now keeps that change local.
- **Bolt type**: **Simple**: formatting and integration, with no deep domain rules.

### Unit 008: assisted-weighing-station-ui
- **Responsibility**: The operator terminal. It reproduces the mockup's functional experience (Select batch, Weigh, Confirm & print) **using only the Api**, with no scenario data in components.
- **Included capabilities**:
  - Queue view and scan prompt.
  - Recipe rail; weigh panel (readout, bar with tolerance band and target line, status line, distinct Target-achieved display).
  - Lot / container entry; tare / confirm / quit keys (F1–F4 handlers).
  - Override dialog (badge + PIN + reason).
  - Completion summary and review list; ticket preview, print and reprint.
  - Status strip (scales, scanner, printer, OrgaTEX, unsynced count).
  - Sign-in screen; error and offline states.
  - Transloco (EN), and `WeighingSessionStore` (NgRx SignalStore).
  - The Playwright end-to-end test (STEP 29).
  - Closes the ui-analysis gaps.
- **Dependencies**: 002 to 007, over HTTP `/api` (B0).
- **Why separate**: the project type requires one frontend unit. It has a different stack (Angular), different tests (Vitest, Playwright) and a different change cadence. It's **one unit for all screens** rather than one per screen, because the screens share one session store and one status strip.
- **Bolt type**: **Simple** (Plan → Implement → Test): UI work; all rules come from the backend.

## 7. Independence check

| Check | 001 | 002 | 003 | 004 | 005 | 006 | 007 | 008 |
|---|---|---|---|---|---|---|---|---|
| Single responsibility | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Buildable / testable alone (against the dummies or fakes of its ports) | ✅ | ✅ | ✅ | ✅ | ✅ (fakes of 002 / 003 / 004 ports) | ✅ | ✅ | ✅ (Api mocked in unit tests) |
| Clear interface | HTTP contracts | auth / audit / clock services | read model + port | ports | commands + event | outbox status | ticket API | — |
| Failure doesn't cascade | ✅ | ✅ | load error only | device offline only | — | sync failed only | print pending only | — |
| Separately deployable | ✅ (own process) | in the Api | in the Api | in the Api | in the Api | in the Api (worker) | in the Api | ✅ (SPA) |

002 to 007 are **modules of the one Api process** (Clean modular monolith, system-architecture). They're independent to *develop and test*, but they're deployed together. That's intentional, not a defect.

## 8. CHECKPOINT 3 self-check (workbook)

- [x] **Does every requirement belong to a Unit?** FR-1 to FR-28 are mapped in §3. FR-24 is split by criterion, and the NFRs are assigned.
- [x] **Are Units capability-oriented?** Yes. There's one UI unit for all three screens, and the others are named after capabilities.
- [x] **Are dependencies understandable?** §4 gives the map and table. There are no cycles, and completion is decoupled by an event.
- [x] **Is the Weighing Station more than a screen?** Yes. It's a DDD domain unit (005), and the UI only renders its results.
- [x] **Are integrations separated appropriately?** Simulators (001), devices (004), OrgaTEX inbound (inside 003) and outbound (006), and the printer (007), each behind its own port.
