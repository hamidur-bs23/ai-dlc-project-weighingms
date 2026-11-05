---
stage: implement
bolt: 007-weighing-station
created: 2026-11-05T17:55:00Z
---

## Implementation Walkthrough: weighing-station (bolt 007)

### Summary

WeighingMS now has the guided-weighing core:
- a **weighing session per batch** that is started or resumed and survives restarts;
- **free selection of a pending component**, with the first pending one suggested;
- **automatic scale selection** by the configured threshold (3 kg or less → Precision);
- the **tolerance source** (recipe → configured → blocked);
- **server-side classification** of the active component's live reading into Underweight, Within range, Target achieved or Overweight, with the remaining quantity.

Every rule is in the pure Domain project. The Api only maps results.

### Structure Overview

- **Domain** (`Domain/Weighing`):
  - the `WeighingSession` aggregate and its `ComponentWeighing` entity;
  - value objects `ScaleSelectionPolicy`, `ScaleSelection`, `ScaleProfile`, `ToleranceResolution`, `ToleranceBand`, `Classification`;
  - the pure services `ScaleSelector`, `ToleranceResolver`, `ReadingClassifier`;
  - errors and events.
  - `Quantity` gains exact `ConvertTo` and `RoundTo` (half away from zero).
- **Application** (`Application/Weighing`): `WeighingSessionService` with start or resume, get, select and read current, plus the ports `IWeighingSessionRepository` and `IScaleSelectionSource`. Session events go to the existing audit trail in the same transaction.
- **Infrastructure**: an EF Core repository (unique `batch_id`, `xmin` concurrency, a race-safe first start), the table configurations, the migration and the options-backed policy source.
- **Api**: `WeighingSessionController` under `/api/batches/{batchId}/session`, plus contracts.

### Completed Work

- [x] `src/WeighingMS.Domain/Common/Quantity.cs`: `Of`, `ConvertTo` (exact g ↔ kg), `RoundTo(step)` (WEIGH-ROUNDING), `SymbolOf`.
- [x] `src/WeighingMS.Domain/Weighing/ScaleClass.cs`, `ScaleSelectionPolicy.cs`, `ScaleSelection.cs`, `ScaleSelector.cs`: target in kg at or below the threshold → Precision, else Heavy-Duty (FR-8); the policy is validated.
- [x] `src/WeighingMS.Domain/Weighing/ScaleProfile.cs`: the scale's unit and resolution, fixed at session start (SCALE-PROFILE).
- [x] `src/WeighingMS.Domain/Weighing/ToleranceSource.cs`, `ToleranceResolution.cs`, `ToleranceResolver.cs`: recipe → configured (chemical + class) → blocked (TOL, A3).
- [x] `src/WeighingMS.Domain/Weighing/ToleranceBand.cs`, `WeightClass.cs`, `Classification.cs`, `ReadingClassifier.cs`: an inclusive band in the scale's unit (A4); net and target rounded to the resolution; the four classes; remaining = max(0, target − max(0, net)) (A5).
- [x] `src/WeighingMS.Domain/Weighing/WeighingSession.cs`, `ComponentWeighing.cs`, `ComponentState.cs`, `WeighingSessionStatus.cs`:
  - start from the snapshot with every component pending, and the scale and tolerance fixed per component (A1);
  - resume;
  - select: a pending one becomes active; the active one changes nothing; a done one is refused; switching abandons the current attempt;
  - suggested = the first pending in sequence (PD-07).
- [x] `src/WeighingMS.Domain/Weighing/WeighingErrors.cs`, `WeighingEvents.cs`: `Weighing.SessionNotFound`, `ComponentNotFound`, `ComponentDone`, `NoActiveComponent`, `ScaleNotAvailable` (503), `Concurrency.Conflict`; events Started, Resumed, ComponentSelected, AttemptAbandoned.
- [x] `src/WeighingMS.Domain/Audit/AuditEventType.cs`: `WeighingSessionStarted`, `WeighingSessionResumed`, `ComponentSelected`, `AttemptAbandoned`.
- [x] `src/WeighingMS.Application/Weighing/WeighingSessionService.cs`, `CurrentReading.cs`, `IWeighingSessionRepository.cs`, `IScaleSelectionSource.cs`, `ScaleSelectionOptions.cs`.
- [x] `src/WeighingMS.Infrastructure/Weighing/WeighingSessionRepository.cs`, `ScaleSelectionSource.cs`; `Persistence/Configurations/WeighingSessionConfiguration.cs`, `ComponentWeighingConfiguration.cs`; `Persistence/Migrations/*_AddWeighingSessions.cs` (SQL reviewed: two tables, a unique `batch_id`, a unique (session, sequence), `numeric(12,4)` with units, nullable tolerance for blocked).
- [x] `src/WeighingMS.Api/Controllers/WeighingSessionController.cs`, `Contracts/Weighing/*`:
  - `POST|GET /api/batches/{batchId}/session`;
  - `POST …/session/selection`;
  - `GET …/session/reading`.
- [x] Modified:
  - `Program.cs`: `Weighing:ScaleSelection` validated on start;
  - `appsettings.json`: threshold 3.0 kg, `scale-3kg` / `scale-32kg`;
  - `ObservabilityExtensions.IsWeightPoll`: healthy reading polls are not traced;
  - the DI registrations; `WeighingDbContext`;
  - `CLAUDE.md`;
  - the OpenAPI document and `web/…/generated/schema.ts` (`npm run api:types`).

### Key Decisions

- **The approved decisions as designed**:
  - model: WEIGH-ROUNDING, SESSION-RESUME, A1–A5;
  - design: WEIGH-READING, SESSION-START, SCALE-PROFILE;
  - Stage 3 (ADR) skipped.
- **The scale-selection policy comes in through a port** (`IScaleSelectionSource`), the same pattern as the tolerance catalogue, so Application stays free of `Microsoft.Extensions.Options`.
- **A race between two first opens**: the unique `batch_id` decides. The loser's session and its audit entries are detached, and the request becomes a resume of the winner's session.
- **Classification reuses unit 004's validity**: Valid and BelowZero readings are classified (below zero → Underweight, A5). Malformed and over-capacity readings get no class.
- **The reading's `net` is the value as classified**, rounded to the resolution, so the screen shows exactly what was judged.

### Deviations from Plan

- **`version` is not in `SessionResponse` yet.** Concurrency is enforced server-side by `xmin` (409 `Concurrency.Conflict`). Exposing a version for `If-Match` moves to bolt 008, where Confirm needs it.
- **Tolerance as stored** keeps its own unit (e.g. g on a kg target). The band converts it into the scale's unit when built.

### Dependencies Added

- None (no new packages).

### Developer Notes

- **Verification**:
  - the solution builds with 0 warnings; `dotnet format` is clean;
  - **all 293 existing tests pass**; the migration is applied to the dev database;
  - the web build and the 77 web tests pass with the regenerated types.
- **Live smoke test** on spare ports 5180 and 5190 (the developer's 5080 and 5090 untouched):
  - B-24191 → **201**: 4 components, all pending, suggested 1; DYE-2290 and DYE-4471 on `scale-3kg` (precision), AUX-0101 and AUX-0107 on `scale-32kg` (heavyDuty); tolerances from the recipe. A second POST → **200** (resume).
  - B-24187: a reading before any selection → 409 `Weighing.NoActiveComponent`; select 3 (Yellow RGB) → 200.
  - **Live classification on `scale-3kg`** (tare, then simulator scenario, after 9 s): belowRange **93.0 g → underweight** (remaining 1.0); enterRange **94.2 g → withinRange** (0); overshoot **96.4 g → overweight**; rising to target **94.0 g → targetAchieved**.
  - A below-zero −1.0 g → underweight, remaining **94.0 g** (A5).
  - Select 4 (abandons 3) → Sodium Sulphate on `scale-32kg`: **24.048 kg → withinRange**.
  - 3 kg scale faulted → 503 `Scale.Unavailable`; unknown sequence → 404 `Weighing.ComponentNotFound`; missing sequence → 422 `Request.Invalid`; no XSRF → 400; no session → 404 `Weighing.SessionNotFound`; unknown batch → 404 `Batch.NotFound`; no cookie → 401.
  - **After an Api restart**, OP-1002 resumes B-24187 → 200: started by OP-1001, component 3 still active, nothing reset (SESSION-RESUME, restart durability).
  - No error lines in the Api log.
- **Smoke-test tip**: re-taring after a simulated pour includes the poured amount in the tare. Reset the simulator (`POST /_sim/reset`) between scenarios.
- **Tests** are Stage 5:
  - exhaustive Domain boundary tables: STEP 22 93.4–94.6 g, PDF 0.980–1.020 kg, FR-8 2.999 / 3.000 / 3.001 kg, rounding 93.45 / 94.55;
  - Application tests with fakes;
  - Api integration tests (Testcontainers plus the in-process simulator), including restart durability and concurrent first starts;
  - the evidence file `STEP-22-tolerance-tests.txt`.
