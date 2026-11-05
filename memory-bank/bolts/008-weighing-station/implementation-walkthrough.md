---
stage: implement
bolt: 008-weighing-station
created: 2026-11-06T00:20:00Z
---

## Implementation Walkthrough: weighing-station (bolt 008)

### Summary

The weighing attempt and the record now exist end to end:
- the operator captures **lot and container**, **tares** the component's own scale (and re-tares), and **confirms**;
- the server reads the scale itself, re-checks the same rule the Confirm button uses, and writes **exactly one immutable record** with every STEP 25 field;
- the **next pending component** after it becomes active;
- **Quit** discards an attempt with no record; **Leave** exits the batch and keeps every record.

### Structure Overview

- **Domain** (`Domain/Weighing`):
  - new: `WeighingAttempt`, `TareTaken`, `MeasuredReading`, `WeighingRecord` (immutable), `ConfirmationRules` with `ConfirmationGap` / `ConfirmationCheck` (one rule for button and server), `NextComponentRule`, `AttemptText`;
  - `WeighingSession` gains `SetLot`, `SetContainer`, `RecordTare`, `Confirm`, `Quit`, `Leave` and `ReadyToComplete`; `ComponentWeighing` carries the attempt and the record.
- **Application**: `WeighingSessionService` gains the commands (`CaptureLot`, `CaptureContainer`, `Tare`, `Confirm`, `Quit`, `Leave`) and a confirmation check on the reading poll. Audit: `WeighingConfirmed` (new), `ReTare`, `Quit`, `BatchLeft`.
- **Infrastructure**: attempt columns on `weighing_session_components`; the `weighing_records` table (unique per component and per session + sequence); an immutability guard in `WeighingDbContext`; the repository loads records and maps a duplicate record or concurrent change to `Concurrency.Conflict`.
- **Api**: six commands under `/api/batches/{id}/session/`, record and attempt contracts, and `confirmation { canConfirm, missing[] }` on the reading.

### Completed Work

- [x] `src/WeighingMS.Domain/Weighing/AttemptText.cs`: lot and container trimmed, 1–32 characters, field errors (A6).
- [x] `src/WeighingMS.Domain/Weighing/TareTaken.cs`, `WeighingAttempt.cs`, `MeasuredReading.cs`: the attempt state and the domain view of a reading (A7, A10).
- [x] `src/WeighingMS.Domain/Weighing/ConfirmationGap.cs`, `ConfirmationCheck.cs`, `ConfirmationRules.cs`: tolerance, lot, container, tare (on this scale), `tareStale` (the scale came online after the tare), reading valid, stable, in tolerance.
- [x] `src/WeighingMS.Domain/Weighing/NextComponentRule.cs`: the next pending after the confirmed one, wrapping (NEXT-COMPONENT).
- [x] `src/WeighingMS.Domain/Weighing/WeighingRecord.cs`: the immutable record (required, actual, class, tolerance and source, scale and class, lot, container, tare, operator, UTC time).
- [x] Modified `ComponentWeighing.cs`, `WeighingSession.cs`, `WeighingErrors.cs` (`AlreadyConfirmed`, `ComponentNotActive`, `NotConfirmable` / `OutOfTolerance` with one field per gap), `WeighingEvents.cs` (7 events), `Audit/AuditEventType.cs` (`WeighingConfirmed`).
- [x] Modified `src/WeighingMS.Application/Weighing/WeighingSessionService.cs`, `CurrentReading.cs` (now carries the check).
- [x] `src/WeighingMS.Infrastructure/Persistence/Configurations/WeighingRecordConfiguration.cs`; modified `ComponentWeighingConfiguration.cs`, `WeighingDbContext.cs` (`WeighingRecords`, `RejectRecordChanges`), `Weighing/WeighingSessionRepository.cs`.
- [x] `src/WeighingMS.Infrastructure/Persistence/Migrations/*_AddWeighingAttemptsAndRecords.cs` (SQL reviewed: 8 nullable attempt columns, the `weighing_records` table, unique `component_id`, unique (`session_id`, `sequence`), RESTRICT foreign keys).
- [x] `src/WeighingMS.Api/Contracts/Weighing/AttemptRequests.cs`, `WeighingRecordResponse.cs`; modified the session, component and reading responses (`readyToComplete`, `attempt`, `record`, `confirmation`); `WeighingSessionController.cs` (`attempt/lot`, `attempt/container`, `tare`, `confirmation` → 201, `quit`, `leave`).
- [x] Modified `CLAUDE.md`; the OpenAPI document and `web/…/generated/schema.ts`.

### Key Decisions

- **The approved decisions as designed**:
  - model: NEXT-COMPONENT, CONFIRM-SERVER-READ, LEAVE-DISCARDS, A6–A10;
  - design: ATTEMPT-COMMANDS, NO-CLIENT-VERSION, SESSION-TARE;
  - Stage 3 (ADR) skipped.
- **`missing` on a rejected confirm** is a validation problem with one error per gap (`errors.lot`, `errors.tare`, …), the same names as `confirmation.missing` on the reading. The error code is `Weighing.OutOfTolerance` when tolerance is the only gap, otherwise `Weighing.NotConfirmable`.
- **The tare time is the Api's clock**, the same clock that sets the scale's `onlineSince`, so `tareStale` compares like with like (not the simulator's own `taredAt`).
- **Not audited separately**: lot, container and the first tare are carried by the record. Re-tare, confirm, quit and leave are audited.
- **Two concurrency backstops**: `xmin` on the session (a confirm always changes the active component) and the unique record per component. Both map to `Concurrency.Conflict`, and nothing of the losing request is stored.

### Deviations from Plan

- **None in behaviour.** The tare and confirm endpoints answer "done" components with `Weighing.AlreadyConfirmed` before calling the scale, so a double tap never even reads or tares a device.

### Dependencies Added

- None.

### Developer Notes

- **Verification**:
  - the solution builds with 0 warnings; `dotnet format` is clean;
  - **all 440 existing tests pass**; the migration is applied to the dev database;
  - the web build and the 77 web tests pass with the regenerated types.
- **Live smoke test** on spare ports 5180 and 5190 (the developer's 5080 and 5090 untouched), B-24187 Yellow RGB:
  - the reading poll before capture lists `lot, container, tare, inTolerance`; the lot `"  2026-0387 "` is stored as `2026-0387`; container `C-12`;
  - confirm without a tare → 422 `Weighing.NotConfirmable` (`tare`, `inTolerance`);
  - tare → 214.0 g; mid-pour 55.1 g unstable → confirm 422 (`stable`, `inTolerance`);
  - **settled 94.2 g → `canConfirm: true` → confirm 201** with the full STEP 25 record (actual 94.2 g, required 94.0 g, withinRange, ±0.5 g recipe, `scale-3kg` precision, lot, container, tare 214.0 g, OP-1001, UTC time);
  - **double tap → 409 `Weighing.AlreadyConfirmed`**;
  - **progression**: component 3 done ("94.2 g withinRange" on the rail), 4 active with an empty attempt, "1 of 5";
  - Sodium Sulphate overshoot → 422 **`Weighing.OutOfTolerance`**; re-tare → 200;
  - **a 32 kg scale outage after the tare → `tareStale`**;
  - quit 4 → pending, nothing active; quit again → 409 `Weighing.ComponentNotActive`;
  - a 34-character lot → 422 `Request.Invalid` (`lot`);
  - **leave** with 5 active → 5 back to pending, the record kept;
  - **after an Api restart**, OP-1002 resumes: the 94.2 g record by OP-1001 is intact, and the suggestion is component 1.
  - 0 error lines in the Api logs.
- **Tests** are Stage 5:
  - Domain: every confirm gap, record content, progression wrap-around, quit and leave, immutability;
  - Application with fakes;
  - Api integration: one record per confirm, six concurrent confirms → one record, records survive a restart, the record guard, audit entries;
  - evidence `STEP-25-transaction.json`.
