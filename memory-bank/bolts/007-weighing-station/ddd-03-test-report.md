---
unit: 005-weighing-station
bolt: 007-weighing-station
stage: test
status: complete
updated: 2026-11-06T00:00:00Z
---

# Test Report - Weighing Station (bolt 007)

## Summary

- **Tests**: **440/440 passed** across the solution, the same in 3 consecutive runs. **147 are new**:
  - Domain: 79 (`ReadingClassifierTests`, `ScaleSelectorTests`, `ToleranceResolverTests`, `WeighingSessionTests`, `QuantityConversionTests`);
  - Application: 28 (`WeighingSessionServiceTests`);
  - Api integration: 26 (`WeighingSessionEndpointsTests`) against **PostgreSQL 17 (Testcontainers)** and the **real simulator in-process**;
  - the remainder are parameterised cases.
- **Coverage** (hand-written lines; migrations and generated code excluded):

  | Project | Coverage |
  |---|---|
  | `WeighingMS.Domain` | **99.3%** (417/420) |
  | `WeighingMS.Application` | **98.5%** (466/473) |
  | `WeighingMS.Infrastructure` | 95.3% (470/493) |
  | `WeighingMS.Api` | 95.2% (517/543) |

  - **Bolt 007 files**: `ReadingClassifier`, `ScaleSelector`, `ToleranceResolver`, `ToleranceBand`, `ToleranceResolution`, `ScaleSelectionPolicy`, `ComponentWeighing`, `Quantity`, `WeighingSessionController`, `WeighingSessionRepository` and the configurations are all at **100%**; `WeighingSessionService` 97.6%; `WeighingSession` 97.0%; `WeighingErrors` 83.3%.
  - The uncovered domain lines are the **"done component" branch** of `Select` and `WeighingErrors.ComponentDone`. They can't be reached before Confirm exists (bolt 008), which adds their tests.
- **Build**: 0 warnings, 0 errors. **Format**: clean.
- **Evidence**: `evidence/STEP-22-tolerance-tests.txt`, 79 named passing cases: the STEP 22 and PDF §7 tables, rounding, units, Sodium Sulphate, below zero, the tolerance source, FR-8 scale selection, the Api endpoints, plus the whole-solution total.

## Test Files

- [x] `tests/WeighingMS.Domain.Tests/ReadingClassifierTests.cs`:
  - **STEP 22**, Yellow RGB 94.0 ± 0.5 g: 93.4 → Underweight · **93.5 → Within (confirmable)** · 94.0 → Target achieved · **94.5 → Within (confirmable)** · 94.6 → Overweight, each with remaining;
  - **PDF §7**, 1.000 ± 0.010 kg: 0.980 / 0.995 / 1.000 / 1.008 / 1.020, plus the inclusive bounds 0.990 / 1.010 and 0.989 / 1.011;
  - **WEIGH-ROUNDING**: 93.45 → 93.5 Within · 93.44 → 93.4 Underweight · 94.55 → 94.6 Overweight · 94.54 → 94.5 Within · 94.04 and 93.96 → 94.0 Target achieved;
  - **A4**: a 2.500 kg target on a g scale → band 2490.0 – 2510.0 g; a reading in g on a kg scale is converted; a tolerance in g on a kg target;
  - Sodium Sulphate 24.000 ± 0.120 kg on the floor scale: 23.879 / 23.880 / 24.000 / 24.120 / 24.121;
  - **A5**: −0.3, −1.0 and 0.0 g → Underweight with **94.0 g remaining**; a rising pour gives 55.3 g → 38.7 g remaining;
  - band text values.
- [x] `tests/WeighingMS.Domain.Tests/ScaleSelectorTests.cs`:
  - **FR-8**: 2.999 / **3.000** → Precision, 3.001 → Heavy-Duty, in kg and in g;
  - **Yellow RGB 94.0 g → `scale-3kg`; Sodium Sulphate 24.000 kg → `scale-32kg`**;
  - a configured 2 kg threshold;
  - an invalid policy (threshold ≤ 0, missing assignment) is rejected; IDs are trimmed.
- [x] `tests/WeighingMS.Domain.Tests/ToleranceResolverTests.cs`:
  - **the recipe tolerance is used and the configuration not consulted**;
  - otherwise the configured one;
  - **neither → blocked**; a configured 0 → blocked, not a default;
  - a zero tolerance can't be created.
- [x] `tests/WeighingMS.Domain.Tests/WeighingSessionTests.cs`:
  - start: all pending, suggested 1, none active, the Started event, UUIDv7;
  - scale per component (B-24187); scale profile and band kept;
  - recipe tolerance with its source; a configured one asked for **chemical + selected class** (`AUX-0107`, HeavyDuty);
  - **blocked component**: no band and no class, the other four unaffected;
  - an assigned scale missing → 503 `Weighing.ScaleNotAvailable`;
  - select: pending → active, the others stay pending, suggestion unchanged; any order allowed;
  - **switching abandons the attempt with no record** (Abandoned + Selected events);
  - re-selecting the active one changes nothing; an unknown sequence → not found;
  - a blocked component is selectable;
  - resume resets nothing (another operator; SESSION-RESUME);
  - the active component classifies on its own scale;
  - `Components` is a copy.
- [x] `tests/WeighingMS.Domain.Tests/QuantityConversionTests.cs`: exact g ↔ kg; same unit returns the same instance; `RoundTo` half away from zero on 0.1 g and 0.001 kg, including negatives and a step in another unit; a zero step is rejected; symbols.
- [x] `tests/WeighingMS.Application.Tests/WeighingSessionServiceTests.cs`:
  - start creates and audits;
  - **the scale list is read once**;
  - the recipe is loaded through `BatchService` when not yet snapshotted;
  - OrgaTEX down or scale list down → error and **nothing stored**; an unknown assigned scale → `Weighing.ScaleNotAvailable`; an invalid policy is passed through;
  - the catalogue is asked with the wire class `heavyDuty`; blocked when there is no tolerance;
  - **resume needs neither OrgaTEX nor a scale**;
  - **A1**: a configuration change doesn't move an open session's scales;
  - **losing a first-open race resumes the winner's session**;
  - select: saves and audits, switching audits abandon then select, re-select saves nothing, not found, unknown sequence, **concurrency conflict**;
  - read: no session, no active component, **reads the active component's own scale**, classes on both scales, below zero → Underweight with 94.0 g remaining, malformed or over capacity → no class, blocked → no class, scale down → 503;
  - the wire scale-class names.
- [x] `tests/WeighingMS.Api.IntegrationTests/WeighingSessionEndpointsTests.cs`:
  - **first open → 201** with `Location`, every component pending, scales `3kg ×3 / 32kg ×2`, Yellow RGB band 93.5 – 94.5 (source `recipe`, resolution 0.1 g, automatic);
  - **second open → 200**, one row;
  - **restart durability**: a new Api host, OP-1002 resumes with component 4 still active, started by OP-1001;
  - **six concurrent first opens → one 201 and five 200**, one session, one Started audit entry;
  - unknown batch → 404 `Batch.NotFound`; scale list and OrgaTEX down → 503 and nothing stored;
  - **a configured threshold of 0.05 kg moves Yellow RGB to `scale-32kg` without a code change**;
  - no session → 404;
  - switching abandons and audits; an unknown component → 404; a missing sequence → 422; a reading with nothing active → 409;
  - **live classification from the simulator**: belowRange 93.0 g underweight (1.0 remaining) · enterRange 94.2 g withinRange · overshoot 96.4 g overweight · rising 94.0 g targetAchieved, with the band in the response;
  - **the reading comes from the active component's own scale** (24.048 kg withinRange while the 3 kg scale overshoots);
  - mid-pour: unstable, underweight, and net + remaining = target;
  - a faulted scale → 503;
  - **two stations change one session → the second gets `Concurrency.Conflict`** (xmin) and the first change is kept;
  - 401 without a session; 400 without XSRF;
  - reading polls are treated as healthy weight polls (not traced);
  - the migration is applied.

## Acceptance Criteria Validation

- ✅ **001**: a session starts with every component pending and the first one suggested; resuming keeps confirmed state (no reset); an Api restart restores progress exactly; two stations → the second is rejected (409).
- ✅ **002**: any pending component can be selected and the others stay pending; **switching abandons the attempt with no record**. The "a done component changes nothing" criterion is in place (`Weighing.ComponentDone`) and gets its test in bolt 008, which creates done components.
- ✅ **003**: in kg after conversion, ≤ threshold → Precision and > threshold → Heavy-Duty; **2.999 / 3.000 / 3.001 kg**; Yellow RGB → 3 kg precision and Sodium Sulphate → 32 kg floor; the threshold and assignments come from configuration (0.05 kg test).
- ✅ **006**: the four classes with inclusive bounds; the **PDF and STEP 22 values**; remaining = max(0, target − net) (with A5); computed on the server with fixed-point decimals; target, ± and range in the response; **93.45 rounded to the resolution first**.
- ✅ **007**: recipe → configured → **blocked "no tolerance defined"**; the applied tolerance and its source are kept on the component, ready to be snapshotted on the record in bolt 009.
- ✅ **Build has 0 warnings; format clean; all 293 earlier tests still pass; the migration applies.**

## Issues Found

- **No defects in the Implement-stage code.** One test assumption was corrected: audit entries written in **one request** share the same instant, and UUIDv7s made in the same millisecond are not ordered, so the order of "abandoned 3 / selected 5" can't be read back from the trail. The test now checks them as a set.
  - **Observation for later**: if the review list (bolt 009) has to show the order within one request, the audit entry needs a sequence column. Raised for bolt 009 and not changed here.
- **Observation**: when two first opens race, EF Core logs the losing insert's unique violation at Error level, although it is handled (the request resumes). The recipe snapshot race (bolt 005) behaves the same way. A candidate for the observability bolt (log-level filter); not changed here.

## Notes

- The **configured** and **blocked** tolerance paths aren't reachable through the seed: every seeded component has a recipe tolerance. They are covered by the Domain and Application tests. A training batch with a missing tolerance could be added to the seed later, which would be a SEED-01 change.
- The integration tests drive the simulator's own fake clock for the 8 s pour, so they run in about 1 s and are deterministic.
