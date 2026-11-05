---
unit: 005-weighing-station
bolt: 009-weighing-station
stage: test
status: complete
updated: 2026-11-06T01:10:00Z
---

# Test Report - Weighing Station (bolt 009)

## Summary

- **Tests**: **582/582 passed** across the solution, the same in 3 consecutive runs. **58 are new**:
  - Domain: 29 (`OvershootAndOverrideTests`, `CompletionTests`);
  - Application: 13 (`WeighingOutcomeServiceTests`);
  - Api integration: 16 (`WeighingOutcomeEndpointsTests`) against **PostgreSQL 17 (Testcontainers)** and the **real simulator in-process**.
- **Coverage** (hand-written lines; migrations and generated code excluded):

  | Project | Coverage |
  |---|---|
  | `WeighingMS.Domain` | **99.5%** (799/803) |
  | `WeighingMS.Application` | 95.8% (682/712) |
  | `WeighingMS.Infrastructure` | 95.9% (544/567) |
  | `WeighingMS.Api` | 95.4% (706/740) |

  The uncovered controller lines are the per-command "missing `sequence` / `grantId`" 422 branches; the same rule is tested on other commands.
- **Build**: 0 warnings, 0 errors. **Format**: clean.
- **Evidence**:
  - `evidence/STEP-24-overshoot-decision.md`: the ADR, problem → rule → implementation → test;
  - `evidence/STEP-24-overshoot-tests.txt`: the 58 named passing STEP 24 / override / completion cases and the whole-solution total. It is the basis for `STEP-24-overshoot-test.png`.

## Test Files

- [x] `tests/WeighingMS.Domain.Tests/OvershootAndOverrideTests.cs` (**STEP 24 / PD-05 option 1**):
  - **an overshoot blocks confirm and reports the surplus** (96.4 g → +2.4 g, +2.6%; `OutOfTolerance`; still active);
  - recovery (a): **brought back into range at 94.3 g → normal confirm** (Within range, no override);
  - recovery (b): **quit and re-weigh** → a fresh attempt with no record;
  - recovery (c): **override → Overweight + surplus + OVR** (grant, supervisor, trimmed reason, time), next active;
  - **a large surplus (+50%) can still be overridden** (no limit, by decision);
  - **no other component's target or band changes**;
  - an Underweight override → no surplus;
  - in range (94.0 / 93.5 / 94.5) → `OverrideNotNeeded`;
  - **four eyes** → `Auth.FourEyesRequired`;
  - no reason (null / empty / blank) → field `reason`, nothing changes; 500 characters accepted, 501 not;
  - **A11**: still needs lot, container, tare and a stable reading; no usable reading → `reading`; a **blocked tolerance can't be overridden**;
  - a component that isn't active, unknown, or already done → rejected;
  - surplus values (94.6 → +0.6 g / 0.6%; 429.6 vs 420 → +9.6 g / 2.3%; floor scale +0.300 kg / 1.3%); no surplus unless Overweight.
- [x] `tests/WeighingMS.Domain.Tests/CompletionTests.cs`:
  - **pending → `Batch.Incomplete` (1 of 5)**;
  - **the last record completes automatically**: completed at the last record's time and by its operator, nothing active, **one `BatchCompleted` with all 5 records**;
  - an override as the last record also completes;
  - explicit completion afterwards is idempotent (no new event);
  - **a completed session is frozen**: select, lot, container, tare, confirm, override and quit → `Weighing.BatchCompleted`; leave and resume add nothing.
- [x] `tests/WeighingMS.Application.Tests/WeighingOutcomeServiceTests.cs`:
  - **override records OVR and consumes the grant in the same single save**; the audit line is "Overweight 96.4 g authorised by SV-2001: …";
  - **fixable rejections (no reason, in range) and four eyes leave the grant unused**;
  - unknown, used, other-action and expired grants → `Auth.GrantInvalid` **without reading the scale**;
  - a component that isn't active is rejected before the grant is looked at; no session → not found;
  - **the last confirm calls the completion handler once, before the save**, with the header SO-9931 / JET-04 / Teal 4C and the `BatchCompleted` audit entry;
  - **the summary matches the mockup's view 3**: 5 of 5, YES, 0 overrides, **35.322 kg** (1241.5 g + 385.4 g + 94.2 g + 23.980 kg + 9.621 kg), operators "R. Karim, S. Akter", elapsed, OrgaTEX and ticket `notAvailable`, the actuals per line;
  - with an override → NO, 1 override, the line Overweight + OVR, in progress with no elapsed time;
  - complete while pending → `Batch.Incomplete`; after auto-completion → the summary, with no extra save;
  - the review list: empty, page size 20, page 0 → 1, then B-24187 (5 of 5, YES).
- [x] `tests/WeighingMS.Api.IntegrationTests/WeighingOutcomeEndpointsTests.cs`:
  - **STEP 24 over HTTP**: overshoot → `class: overweight`, `surplus` +2.4 g / 2.6%, `override.offered: true`, confirm 422 `OutOfTolerance`;
  - **step-up (SV-2001 + PIN) → override 201** with Overweight 96.4 g, surplus, supervisor M. Rahman, reason, and the rail flag; **the grant reused → 403**; in the database: the override record, the grant `used_at`, the audit `Override`;
  - **targets and tolerances of all components unchanged** after the overshoot and override;
  - a blank reason → 422 and **the same grant succeeds on retry**;
  - in range → 409 `OverrideNotNeeded`;
  - **a supervisor signed in as the operator → 403 `FourEyesRequired`**;
  - a wrong PIN → 403 and no record;
  - **six concurrent overrides with one grant → one record**;
  - no `grantId` → 422;
  - **completion**: 409 `Batch.Incomplete` before; **the last confirm → `completed`**; explicit completion → 200; `BatchCompleted` audited once;
  - **a completion handler's work is saved in the same transaction**;
  - **a failing handler rolls back the last record and the completion** (4 records, in progress, no `BatchCompleted`);
  - a completed batch is frozen (409) and reopening it returns `completed`;
  - **the summary after an Api restart** (OP-1002): 5 of 5, YES, 35.320 kg, R. Karim, `notAvailable` states, header, all lines Target achieved;
  - **the review list**: empty, then B-24187 with 1 override; `status=open` → 422;
  - the migration is applied.

## Acceptance Criteria Validation

- ✅ **010**: net > target + tolerance → a distinct Overweight state with the surplus +x (+y%); Confirm disabled; the actions offered are re-tare and re-weigh, quit, or override; **no recalculation** (PD-05 option 1, ADR-001).
- ✅ **011**: an Underweight or Overweight reading + a valid grant from another supervisor + a reason → **one record with the true class**, flagged with supervisor, reason and time; a wrong PIN or no reason → no record and nothing changes; without an override the operator can't confirm; the override is audited and shown in the summary and on the rail. The "OVR" ticket line is bolt 012. In range → not offered.
- ✅ **013**: pending → rejected; all N recorded → completed, with `BatchCompleted` raised **in the same transaction** (proved by an auditing and a failing handler); the summary has n of N, within tolerance (YES / NO + override count), total dispensed, elapsed, operators, OrgaTEX and ticket state, and per-chemical lines.
- ✅ **014**: the record holds every field including override details and surplus; it survives restarts and is immutable (bolt 008 guard); a completed batch reopens read-only from the review list.
- ✅ **Build has 0 warnings; format clean; all 524 earlier tests still pass; the migration applies.**

## Issues Found

- **No defects in the Implement-stage code.** Every new test passed on its first run. One test helper (the completion handler's save counter) wasn't wired at first, so its assertion couldn't fail; it was wired before the run.
- **Observation**: a completion handler that throws surfaces as a 500 (and everything rolls back). That's correct for a programming or infrastructure fault. Bolts 011 and 012 should keep their handlers to writing rows only (outbox, print-pending ticket) and do the external calls later, outside the transaction (PD-01, PD-02).

## Notes

- **`STEP-24-overshoot-test.png`**: a screenshot of `evidence/STEP-24-overshoot-tests.txt`, or of the test run in a terminal.
- **`STEP-24-overshoot-ui.png`** needs the Weigh screen (bolt 010).
- `ApiFixture.CreateApi` gained an optional service hook, used here for the test completion handlers.
