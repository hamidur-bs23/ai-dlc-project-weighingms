---
stage: implement
bolt: 009-weighing-station
created: 2026-11-06T00:52:00Z
---

## Implementation Walkthrough: weighing-station (bolt 009)

### Summary

Unit 005 is functionally complete:
- **overshoot** warns and blocks, showing the surplus;
- per **PD-05 option 1** (STEP 24), it ends only by bringing the weight back into range, quitting and re-weighing, or a **supervisor override**;
- the override uses a single-use step-up grant from **another** supervisor (four eyes) plus a reason, and records the true class, surplus and OVR;
- the **last record completes the batch automatically**, raising `BatchCompleted` in the same transaction;
- a completed batch is frozen, has a **summary**, and is listed in the **review list**.

### Structure Overview

- **Domain**:
  - `Surplus`, `OverrideDetails` / `OverrideAuthority`;
  - `WeighingSession.Override`, `Complete` (explicit), automatic completion in `AdvanceAfter`, frozen-session guards;
  - `WeighingRecord` gains surplus and override;
  - events `WeighingOverridden` and `BatchCompleted`;
  - `StepUpGrant.CheckUsable` (non-consuming check).
- **Application**:
  - `WeighingSessionService.OverrideAsync`, `CompleteAsync`, `GetSummaryAsync`, `ListCompletedAsync`;
  - the `IBatchCompletedHandler` seam, called inside the completing save (COMPLETION-HANDLERS);
  - `BatchSummary` / `BatchSummaryBuilder`.
- **Infrastructure**: override and surplus columns, completion columns, a unique grant per record, the review query, migration `AddOverridesAndCompletion`.
- **Api**:
  - `POST …/session/override`, `POST …/session/completion`, `GET …/session/summary`;
  - `WeighingReviewController` (`GET /api/weighing-sessions?status=completed`);
  - the reading adds `surplus` and `override.offered`.

### Completed Work

- [x] `src/WeighingMS.Domain/Weighing/Surplus.cs`: amount = net − target, % of target to 1 dp (A16), only for Overweight.
- [x] `src/WeighingMS.Domain/Weighing/OverrideDetails.cs`: the override on a record, and the supervisor authority from the grant.
- [x] Modified `WeighingSession.cs`:
  - `Override` (completed or done checks, active, **four eyes**, in range → `OverrideNotNeeded`, every other gap required (A11), reason 1–500 characters, a record with true class + surplus + OVR);
  - `Complete` (idempotent; `Batch.Incomplete`);
  - **auto-completion** after the last record;
  - **frozen** when completed (select, commands → `Weighing.BatchCompleted`; resume and leave are no-ops).
- [x] Modified `WeighingRecord.cs` (surplus, override, `IsOverride`), `Classification.cs` (`Surplus`), `WeighingSessionStatus.cs` (`Completed`), `WeighingErrors.cs` (`OverrideNotNeeded`, `BatchCompleted`, `Batch.Incomplete`), `WeighingEvents.cs`, `AttemptText.cs` (a maximum length per field).
- [x] Modified `src/WeighingMS.Domain/Access/StepUpGrant.cs` (`CheckUsable`; `Consume` uses it) and `AccessErrors.cs` (`Auth.FourEyesRequired`); `Audit/AuditEventType.cs` (`BatchCompleted`).
- [x] `src/WeighingMS.Application/Weighing/IBatchCompletedHandler.cs`, `BatchSummary.cs`; modified `WeighingSessionService.cs`, `IWeighingSessionRepository.cs`.
- [x] Modified `src/WeighingMS.Infrastructure/Persistence/Configurations/WeighingRecordConfiguration.cs`, `WeighingSessionConfiguration.cs`, `Weighing/WeighingSessionRepository.cs`; `Persistence/Migrations/*_AddOverridesAndCompletion.cs` (SQL reviewed: 8 nullable record columns, 2 session columns, a review index on (`status`, `completed_at` desc), a filtered **unique `override_grant_id`**).
- [x] `src/WeighingMS.Api/Controllers/WeighingReviewController.cs`, `Contracts/Weighing/BatchSummaryResponse.cs`; modified `WeighingSessionController.cs` and the contracts (`OverrideRequest`, surplus and override on records, `completedAt` / `completedBy`, `override.offered`).
- [x] `memory-bank/bolts/009-weighing-station/adr-001-overshoot.md` (accepted: option 1), `evidence/STEP-24-overshoot-decision.md`, PD-05 resolved in `decision-index.md`, FR-16 updated, `CLAUDE.md`, the OpenAPI document and the web types.

### Key Decisions

- **The approved decisions**:
  - model: AUTO-COMPLETE, OVR-FOUR-EYES, A11–A18;
  - design: OVR-GRANT-FLOW, REVIEW-LIST, COMPLETION-HANDLERS;
  - **ADR-001 = PD-05 option 1** (no surplus limit, no recalculation).
- **The grant is checked before and consumed after**: `CheckUsable` first. The domain decides, and only then does `Consume` run in the record's transaction. A fixable rejection (missing lot, no reason, in range, four eyes) leaves the grant unused. Two concurrent uses are stopped by `xmin` on the grant and the unique `override_grant_id`.
- **The override can't bypass a blocked tolerance**: "no tolerance defined" is still a gap (A3). Only "in tolerance" is waived (A11).
- **`override.offered` on the reading** is true only when "in tolerance" is the single gap, so the screen shows Override exactly when it would succeed (apart from the supervisor's PIN and the reason).
- **The audit text for an override** is capped at 500 characters; the full reason is on the record.

### Deviations from Plan

- **None in behaviour.**
- The summary is built in the Application layer (`BatchSummaryBuilder`, a pure static) rather than the Domain, because it needs the recipe header and operator display names from other contexts.

### Dependencies Added

- None.

### Developer Notes

- **Verification**:
  - build 0 warnings; format clean;
  - **all 524 existing tests pass**; the migration is applied to the dev database;
  - web build and 77 web tests pass with the regenerated types.
- **Live smoke test** on spare ports 5180 and 5190 (5080 and 5090 untouched), B-24191:
  - component 1 (420.0 ± 2.0 g) overshoots to **429.6 g → `overweight`, surplus +9.6 g (+2.3%), `override.offered: true`**, `missing: [inTolerance]`;
  - confirm → 422 `Weighing.OutOfTolerance`; completion now → 409 `Batch.Incomplete`;
  - step-up SV-2001 → grant; override with a blank reason → 422 (`reason`) and the grant still unused;
  - **override → 201: Overweight 429.6 g, surplus +9.6 g, OVR by SV-2001**, next component active;
  - **the same grant again → 403 `Auth.GrantInvalid`**;
  - components 2–4 confirmed (Target achieved); **the last confirm → `status: completed`**, nothing active;
  - select afterwards → 409 `Weighing.BatchCompleted`; completion again → 200 (idempotent);
  - **summary**: 4 of 4, all within tolerance **NO**, 1 override, total 15.668 kg (429.6 g + 38.0 g + 11.4 kg + 3.8 kg), operators R. Karim, OrgaTEX and ticket `notAvailable`, per-line status (`overweight` with override);
  - **four eyes**: SV-2001 signed in at the station, then self-approved → **403 `Auth.FourEyesRequired`**;
  - **review list**: B-24191 (SO-9947, WINCH-01, Rose 08, 4 of 4, 1 override); `status=open` → 422;
  - 0 error lines in the Api log.
- **Tests** are Stage 5:
  - the STEP 24 tests: an overshoot blocks and reports the surplus; bringing it back into range confirms normally; quit and re-weigh; an override records Overweight + surplus + OVR; **a large surplus can still be overridden (no limit, by decision)**; **other targets are unchanged**;
  - override failures (wrong or used grant, four eyes, no reason, in range, blocked tolerance);
  - completion (auto, explicit, incomplete, `BatchCompleted` in the transaction via a test handler);
  - summary values (B-24187: 35.322 kg) and the review list;
  - evidence `STEP-24-overshoot-test` (the test run).
