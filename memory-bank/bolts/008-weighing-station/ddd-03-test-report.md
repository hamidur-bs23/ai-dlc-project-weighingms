---
unit: 005-weighing-station
bolt: 008-weighing-station
stage: test
status: complete
updated: 2026-11-06T00:35:00Z
---

# Test Report - Weighing Station (bolt 008)

## Summary

- **Tests**: **524/524 passed** across the solution, the same in 3 consecutive runs. **84 are new**:
  - Domain: 39 (`WeighingAttemptTests`, `NextComponentRuleTests`);
  - Application: 27 (`WeighingAttemptServiceTests`);
  - Api integration: 18 (`WeighingConfirmationEndpointsTests`) against **PostgreSQL 17 (Testcontainers)** and the **real simulator in-process**.
- **Coverage** (hand-written lines; migrations and generated code excluded):

  | Project | Coverage |
  |---|---|
  | `WeighingMS.Domain` | **99.7%** (684/686) |
  | `WeighingMS.Application` | **97.7%** (541/554) |
  | `WeighingMS.Infrastructure` | 95.8% (523/546) |
  | `WeighingMS.Api` | 94.7% (613/647) |

  - All new Domain files (`ConfirmationRules`, `NextComponentRule`, `WeighingRecord`, `AttemptText`, …) are at 100%; `WeighingSession` 98.8%.
  - The uncovered lines are defensive branches: a "missing sequence" 422 on each individual command (one is tested), and a tare the device reports with an unknown unit.
- **Build**: 0 warnings, 0 errors. **Format**: clean.
- **Evidence**: `evidence/STEP-25-transaction.json`, the live confirm transaction for B-24187 / Remazol Yellow RGB:
  - request `POST …/session/confirmation {"sequence":3}` → **201**;
  - the record with every STEP 25 field: actual weight 94.2 g, operator OP-1001, component 3 DYE-4471, lot 2026-0387, container C-12, tare 214.0 g, scale `scale-3kg`, timestamp; plus required, class, tolerance and source;
  - the session afterwards: 3 done, 4 active;
  - the repeated request → 409 `Weighing.AlreadyConfirmed`.

## Test Files

- [x] `tests/WeighingMS.Domain.Tests/WeighingAttemptTests.cs`:
  - selection starts an empty attempt; pending and done components have none;
  - **lot trimmed** (`"  2026-0387 "` → `2026-0387`); 32 characters accepted; empty, blank or 33 characters → `Request.Invalid` with field `lot` / `container` (A6);
  - commands on a non-active component → `ComponentNotActive` (unknown → not found);
  - **tare then re-tare**: the second replaces the first, both events, no record; **a 0 g tare is accepted** (A7); a tare from another scale is a programming error;
  - **confirm writes every STEP 25 field** (UUIDv7, session, batch, sequence, chemical, required, actual, class, tolerance and source, scale and class, lot, container, tare, operator, time);
  - Target achieved; extra digits recorded at resolution (94.25 → 94.3 g);
  - **double confirm → `AlreadyConfirmed`, nothing changes** (A8);
  - 93.4 and 94.6 → `OutOfTolerance` with the single gap `inTolerance`; **93.5 and 94.5 accepted**;
  - **unstable at 94.0 → `stable`** and no value → `reading` (A10);
  - nothing captured lists `lot, container, tare, stable, inTolerance`;
  - **a tare before the scale came online → `tareStale`**; at the same instant → valid;
  - a blocked tolerance → `tolerance` (never confirmable);
  - **next active is 4 with a fresh attempt**; **the last confirm leaves nothing active and the session ready to complete** (all five Target achieved);
  - selecting a done component → `ComponentDone`;
  - **quit discards with no record**; **leave discards the attempt, keeps the record, `BatchLeft` (1 of 5)**; switching discards the captured attempt.
- [x] `tests/WeighingMS.Domain.Tests/NextComponentRuleTests.cs` (NEXT-COMPONENT): {1,2,4,5} after 3 → **4**; {1,2} after 5 → **1** (wraps); {2,5} after 3 → 5; {2} after 3 → 2; nothing pending → null.
- [x] `tests/WeighingMS.Application.Tests/WeighingAttemptServiceTests.cs`:
  - capture lot and container (trimmed, saved, **not audited**); invalid → not saved; no session;
  - **tare uses the component's own scale and stores the reported 214.0 g at the Api's clock time**; **re-tare audited as `ReTare` "tare 214.0 g → 215.5 g"**; scale down → 503, nothing saved; a pending or unknown component → rejected **without touching the scale**;
  - **confirm reads the scale on the server and records that value**, next active, one save, `WeighingConfirmed` audited;
  - **double confirm → `AlreadyConfirmed` without a second scale read**;
  - 94.6 / 93.4 → `OutOfTolerance`; unstable 94.0 → `NotConfirmable`; nothing stored;
  - **scale outage after the tare → `tareStale`**; scale down → 503; concurrent change → `Concurrency.Conflict`; pending component → `ComponentNotActive`; no session;
  - **the reading poll reports the same gaps** (`lot, container, tare`) and `canConfirm` once ready;
  - quit → `Quit` audited; leave → `BatchLeft` "0 of 5 done";
  - validity mapping: Valid → classifiable and confirmable; BelowZero → classifiable, not confirmable; OverCapacity and Malformed → neither.
- [x] `tests/WeighingMS.Api.IntegrationTests/WeighingConfirmationEndpointsTests.cs`:
  - **STEP 25**: `canConfirm` → **201**, every record field over HTTP, the session (3 done, 4 active with an empty attempt), the row in `weighing_records`, the `WeighingConfirmed` audit entry;
  - **the whole batch**: five confirms (1240.0 g, 386.0 g, 94.0 g on `scale-3kg`; 24.000 kg, 9.600 kg on `scale-32kg`), each Target achieved → `readyToComplete`, nothing active, 5 records;
  - **six concurrent confirms → one 201, five 409, one record, one audit entry**; a sequential double tap → 409 `AlreadyConfirmed`;
  - nothing captured → 422 with `lot, container, tare, inTolerance`; overshoot and below range → 422 `OutOfTolerance` and no record; still pouring → `stable` on both the reading and the 422;
  - **a tare through `/api/scales/{id}/tare` doesn't count** → 422 `tare` (SESSION-TARE);
  - re-tare after the pour stores 308.2 g (gross) and audits `ReTare`, with no record;
  - **a scale outage after the tare → 422 `tareStale`**; a faulted scale → 503 and no record;
  - a pending component → 409; a 33-character lot → 422 (`lot`); a missing sequence → 422;
  - **quit** → no record and a `Quit` audit entry;
  - **leave, then an Api restart, then OP-1002 resumes**: 1 done, the record by OP-1001 kept, component 4 back to pending, `BatchLeft` audited;
  - **records can't be modified or deleted** (the DbContext guard), and the value is unchanged;
  - no XSRF → 400; the migration is applied.

## Acceptance Criteria Validation

- ✅ **004**: the lot (scanned or typed) is stored for the attempt and shown (`attempt.lot`); the container is accepted or typed; an empty lot or container keeps Confirm disabled (`missing` on the reading) and the server rejects it; there is no identity check; the lot is trimmed.
- ✅ **005**: no tare in the attempt on this scale → Confirm disabled; the stored tare is the value the scale reports (214.0 g) and is shown; re-tare replaces it with no record; **a scale that went offline and returned needs a new tare** (`tareStale`).
- ✅ **008**: Within range or Target achieved, stable, with lot, container and tare → `canConfirm`; anything missing → disabled, and **the server rejects a direct call**; a confirm creates **exactly one immutable record** and the rail shows "✓ actual" (`record.actual`); **a double tap → the second is rejected, one record** (including 6 concurrent requests).
- ✅ **009**: after a confirm the next pending component is pre-selected (NEXT-COMPONENT, wrapping); progress shows done (actual), active or pending and "n of N"; with none pending the session is **ready to complete** (completion: bolt 009).
- ✅ **012**: quit discards the attempt with no record and the chemical returns to pending; leave keeps every record and the batch resumes later (also after an Api restart); quit and leave are audited. "Cannot be completed" while incomplete: `readyToComplete` is false until all are done; the completion command arrives in bolt 009.
- ✅ **FR-19**: records survive an Api restart and can't be edited or deleted through the app (or the DbContext).
- ✅ **Build has 0 warnings; format clean; all 440 earlier tests still pass; the migration applies.**

## Issues Found

- **No defects in the Implement-stage code.** Every new test passed on its first run. One test helper used `.Result` (sync over async) and was made asynchronous.
- **Observation**: re-taring **after** a pour stores the gross with the poured amount (308.2 g), so the net reads zero again. That is the defined behaviour of re-tare (FR-7, "zeroes the net reading again"). The Weigh screen (bolt 010) should label F2 clearly so operators re-tare only with an empty container.

## Notes

- **Evidence hygiene**: `STEP-25-transaction.json` comes from the live Implement-stage smoke run (2026-11-05T18:18Z, the same code), because that run had already confirmed B-24187 Yellow RGB in the development database. Recreating it would need clearing development data. The integration tests prove the same transaction against a clean database on every run.
- **The STEP 25 screenshot** (`STEP-25-confirmation.png`) needs the Weigh screen (bolt 010).
- The integration tests drive both fake clocks: the simulator's for the 8 s pour, and the Api's for `onlineSince` and tare times.
