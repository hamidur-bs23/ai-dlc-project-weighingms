---
unit: 005-weighing-station
bolt: 008-weighing-station
stage: design
status: complete
updated: 2026-11-05T18:30:00Z
---

# Technical Design - Weighing Station (bolt 008)

## Architecture Pattern

Unchanged from bolt 007: Clean modular monolith with the rules in the pure Domain. This bolt extends the same `WeighingSession` aggregate, the same `WeighingSessionService`, repository and controller. No new project or package.

## Layer Structure

```text
┌──────────────────────────────────────────────────────────────┐
│ Api          WeighingSessionController (+ attempt/lot, attempt/container, tare,
│              confirmation, quit, leave) · reading response + confirmation check
├──────────────────────────────────────────────────────────────┤
│ Application  WeighingSessionService (+ CaptureLot, CaptureContainer, Tare, Confirm, Quit, Leave)
│              uses DeviceService.TareAsync / ReadAsync (unit 004), IAuditTrail, IPlantClock
├──────────────────────────────────────────────────────────────┤
│ Domain       WeighingAttempt · TareTaken · Lot · ContainerId · WeighingRecord
│              ConfirmationRules (one rule for button + server) · NextComponentRule (NEXT-COMPONENT)
│              WeighingSession: SetLot · SetContainer · RecordTare · Confirm · Quit · Leave
├──────────────────────────────────────────────────────────────┤
│ Infra        attempt columns on weighing_session_components · weighing_records table
│              (unique per component) · record immutability guard · migration AddWeighingAttemptsAndRecords
└──────────────────────────────────────────────────────────────┘
```

### Flow: tare / re-tare (story 005)
1. Load the session; the command names the component (`sequence`), which must be the active one.
2. Tare **the component's own scale** through the scale port (`DeviceService.TareAsync`, so device health is tracked). Scale down → 503, nothing stored.
3. `session.RecordTare(sequence, TareTaken(scaleId, reportedTare, takenAt = plant clock UTC))`. The first tare raises `ScaleTared`; a later one raises `ScaleReTared` and writes the existing `ReTare` audit entry. No record either way.

### Flow: confirm (story 008, CONFIRM-SERVER-READ)
1. Load the session. `sequence` is done → 409 `Weighing.AlreadyConfirmed` (A8); not active → 409 `Weighing.ComponentNotActive`.
2. **Read the component's scale on the server** (`DeviceService.ReadAsync`). Scale down → 503.
3. `session.Confirm(sequence, reading, operator, now)` runs `ConfirmationRules.Check`:
   - tolerance resolved (not blocked);
   - lot and container present;
   - tare present, on this scale, and `takenAt ≥ reading.onlineSince` (a scale that went offline and returned needs a new tare);
   - reading valid and **stable** (A10);
   - class Within range or Target achieved.
   
   Any failure → 422 with the list of what's missing, and nothing changes.
4. Success: an immutable `WeighingRecord` (actual = the server reading rounded to resolution); the component is Done; `NextComponentRule` activates the next pending one after it, wrapping (NEXT-COMPONENT), with a fresh attempt; or nothing is active and `readyToComplete` is true.
5. Save the record, the session change and the `WeighingConfirmed` audit entry in **one transaction** under `xmin`. A unique (session, sequence) index on records is the database backstop against two records.

### Flow: quit / leave (story 012, LEAVE-DISCARDS)
- **Quit** (`sequence` must be active): discard the attempt, component → Pending, nothing active, `Quit` audit entry.
- **Leave**: discard any active attempt the same way. If components are still pending, write a `BatchLeft` audit entry. The session stays in progress, and resume (bolt 007) continues.

### The confirmation check on the reading poll
`GET …/session/reading` (bolt 007) also returns `confirmation: { canConfirm, missing[] }`, computed by the **same** `ConfirmationRules.Check` on the same reading. The screen enables F1 from it, so the button and the server can't disagree.

## API Design

All endpoints require a signed-in operator. Commands need `X-XSRF-TOKEN`, and every command on a component names it (`sequence`, A8).

| Endpoint | Method | Request | Response |
|----------|--------|---------|----------|
| `/api/batches/{batchId}/session/attempt/lot` | POST | `{ "sequence": 3, "lot": "2026-0387" }` | 200 `WeighingSessionResponse` |
| `/api/batches/{batchId}/session/attempt/container` | POST | `{ "sequence": 3, "container": "C-12" }` | 200 `WeighingSessionResponse` |
| `/api/batches/{batchId}/session/tare` | POST | `{ "sequence": 3 }` | 200 `WeighingSessionResponse` (attempt shows the tare) |
| `/api/batches/{batchId}/session/confirmation` | POST | `{ "sequence": 3 }` | **201** `ConfirmationResponse` (`{ record, session }`) · 409 · 422 · 503 |
| `/api/batches/{batchId}/session/quit` | POST | `{ "sequence": 3 }` | 200 `WeighingSessionResponse` |
| `/api/batches/{batchId}/session/leave` | POST | — | 200 `WeighingSessionResponse` |
| `/api/batches/{batchId}/session/reading` (bolt 007) | GET | — | + `confirmation: { canConfirm, missing[] }` |

Additions to `WeighingSessionResponse` (additive):
- `readyToComplete` (bool);
- per component, `attempt` (`{ lot, container, tare{value,unit}, taredAt }` or null) and `record` (`{ actual{value,unit}, class, confirmedAt, operator }` or null).

The rail can then show "✓ 94.2 g" and the counter "n of N".

`ConfirmationResponse.record`, the STEP 25 transaction (saved as `STEP-25-transaction.json` in the test stage):
```json
{
  "recordId": "0193…", "batchId": "B-24187", "sequence": 3,
  "chemicalId": "DYE-4471", "chemicalName": "Remazol Yellow RGB",
  "required": { "value": 94.0, "unit": "g" }, "actual": { "value": 94.2, "unit": "g" },
  "class": "withinRange",
  "tolerance": { "plusMinus": { "value": 0.5, "unit": "g" }, "source": "recipe" },
  "scaleId": "scale-3kg", "scaleClass": "precision",
  "lot": "2026-0387", "container": "C-12", "tare": { "value": 214.0, "unit": "g" },
  "operator": "OP-1001", "confirmedAt": "2026-11-05T01:15:02Z"
}
```

`missing[]` values (shared by the reading poll and the 422): `tolerance` (blocked), `lot`, `container`, `tare`, `tareStale` (the scale came online after the tare), `reading` (no usable value: malformed, over capacity, below zero), `stable`, `inTolerance`.

The existing device endpoint `POST /api/scales/{id}/tare` stays (STEP 16 contract), but a tare taken there **doesn't count** for an attempt. Only `…/session/tare` records it (T3).

## Data Persistence

Migration **`AddWeighingAttemptsAndRecords`** (additive):

| Table | Columns | Relationships |
|-------|---------|---------------|
| `weighing_session_components` (+ columns) | `attempt_started_at` timestamptz null · `attempt_started_by` text null · `lot` text null · `container` text null · `tare_scale_id` text null · `tare_value` numeric(12,4) null · `tare_unit` text null · `tared_at` timestamptz null | Attempt columns are null when there is no attempt (Pending or Done) |
| `weighing_records` (new) | `id` uuid PK · `weighing_session_id` uuid FK · `component_id` uuid FK **unique** · `batch_id` · `sequence` · `chemical_id` · `chemical_name` · `required_value`/`unit` · `actual_value`/`unit` · `class` · `tolerance_value`/`unit` · `tolerance_source` · `scale_id` · `scale_class` · `lot` · `container` · `tare_value`/`unit` · `operator_badge_id` · `confirmed_at` timestamptz · unique (`weighing_session_id`, `sequence`) | 1 record per component (database backstop for A8); n → 1 session. Bolt 009 adds the override columns |

**Immutability (A9):** `WeighingDbContext.SaveChanges` refuses any Modified or Deleted `WeighingRecord`, the same guard as the audit trail. There is no update or delete endpoint.

## Security Design

| Concern | Approach |
|---------|----------|
| Authentication | Existing cookie session; `RequireOperator` on every endpoint |
| Authorization | Any signed-in operator (SESSION-RESUME). The record carries the operator who confirmed |
| Integrity | The UI never sends a weight or a class (CONFIRM-SERVER-READ); the server re-reads and re-checks |
| Forgery | All commands pass the antiforgery filter |
| Audit | `WeighingConfirmed` (new), `ReTare`, `Quit`, `BatchLeft` (existing types), with badge and UTC time, in the same transaction as the change |

## NFR Implementation

| Requirement | Design Approach |
|-------------|-----------------|
| Correctness | One `ConfirmationRules` for button and server; fixed-point; the recorded actual is the classified, resolution-rounded value |
| Reliability | Records and attempts are in PostgreSQL. Records survive an Api restart. An Api restart also resets the device tracker's `onlineSince`, so a tare taken before a restart counts as stale and must be redone (safe by design) |
| Concurrency | `xmin` on the session plus the unique record per component: a double submit writes one record, and the second request gets 409 |
| Performance | Confirm: one session load, one scale read, one transaction. The reading poll's confirmation check is in memory |

## Error Handling

| Error Type | Code | Response |
|------------|------|----------|
| Component already confirmed (double tap) | `Weighing.AlreadyConfirmed` | 409 |
| Command for a component that isn't active | `Weighing.ComponentNotActive` | 409 |
| Confirm rule not met | `Weighing.NotConfirmable` | 422, `missing: [...]` |
| Reading out of tolerance (Under/Overweight) | `Weighing.OutOfTolerance` | 422, `missing: ["inTolerance"]` and `class` (override: bolt 009) |
| Lot or container empty or over 32 characters | `Request.Invalid` | 422 (field `lot` / `container`) |
| Scale down on tare or confirm | `Scale.Unavailable` | 503 (Retry) |
| Concurrent change | `Concurrency.Conflict` | 409 |
| (bolt 007) session or component not found, no active component | as before | 404 / 409 |

## External Dependencies

| Service | Purpose | Integration |
|---------|---------|-------------|
| Dummy scales (unit 004) | Tare and the confirm-time reading | Existing scale port + resilience pipeline |
| Station access (unit 002) | Operator, plant clock, audit | In-process ports |

## Decisions at This Checkpoint: resolved (APPROVE DESIGN, 2026-11-06)

- **T1: option 1.** One command per operator action, each naming the component (ATTEMPT-COMMANDS).
- **T2: option 1.** No ETag / If-Match; the deferred `version` is dropped (NO-CLIENT-VERSION).
- **T3: option 1.** `POST …/session/tare` tares the component's own scale and stores it (SESSION-TARE).
- **Stage 3 (ADR): skipped.**

#### Options as presented

**T1. Shape of the attempt commands**
1 - **(Recommended)** One small command per operator action (`attempt/lot`, `attempt/container`, `tare`, `confirmation`, `quit`, `leave`), each naming the component. It matches the F-keys and the scanner (a scanned lot is one call), and each is easy to audit and test.
2 - One `PUT …/session/attempt { lot, container }` for the details, plus the action commands. Fewer endpoints, but partial updates and nulls make "cleared" and "not sent" ambiguous.

**T2. Do clients send a version (ETag / If-Match)?**
1 - **(Recommended)** No. Double submits are stopped by naming the component (A8), server-side `xmin` and the unique record per component. This replaces the `version` deferred from bolt 007, which is dropped as unnecessary.
2 - Yes, an ETag on the session and If-Match on every command. Protects against stale screens too, but every poll and command has to carry it, and V1 has one station.

**T3. How is a tare recorded for the attempt?**
1 - **(Recommended)** `POST …/session/tare`: the server tares the component's own scale and stores the reported value in one step, so the tare can't come from another scale or another attempt.
2 - The UI calls `POST /api/scales/{id}/tare`, then tells the session. Two calls, and the session would have to trust which scale the UI tared.

## ADR Analysis (Stage 3)

No ADR is proposed: T1–T3 follow the existing sub-resource-command convention (`api-conventions.md`) and bolt 007's patterns. They are recorded in the decision index on approval, and Stage 3 can be skipped.
