---
unit: 005-weighing-station
bolt: 009-weighing-station
stage: design
status: complete
updated: 2026-11-05T19:00:00Z
---

# Technical Design - Weighing Station (bolt 009)

## Architecture Pattern

Unchanged (Clean modular monolith, rules in the pure Domain). This bolt extends the same aggregate, service, repository and controller. It also adds one **in-process domain-event seam**, `IBatchCompletedHandler`, so that bolts 011 (OrgaTEX outbox) and 012 (ticket) can join the completing transaction without changing this unit.

## Layer Structure

```text
┌──────────────────────────────────────────────────────────────┐
│ Api          WeighingSessionController (+ override, completion, summary)
│              WeighingReviewController  GET /api/weighing-sessions?status=completed
│              reading response + surplus + override.offered
├──────────────────────────────────────────────────────────────┤
│ Application  WeighingSessionService (+ Override, Complete, GetSummary, ListCompleted)
│              IBatchCompletedHandler[] (none yet; 011/012 add them) · IStepUpGrantStore (unit 002)
├──────────────────────────────────────────────────────────────┤
│ Domain       Surplus · OverrideDetails · OverrideRules · BatchSummary(Builder)
│              WeighingSession: Override · Complete (auto with the last record) · frozen when completed
│              BatchCompleted, WeighingOverridden events · PD-05 slot (ADR stage)
├──────────────────────────────────────────────────────────────┤
│ Infra        override + surplus columns on weighing_records · completed_at/by on sessions
│              ListCompleted query · migration AddOverridesAndCompletion
└──────────────────────────────────────────────────────────────┘
```

### Flow: override (story 011, OVR, OVR-FOUR-EYES)
1. **Step-up first (existing, ADR-009):** the screen collects the supervisor's badge and PIN and calls `POST /api/session/step-up {badgeId, pin, action: "override"}` → `{ grantId, supervisorName, expiresAt }`. A wrong PIN gives the existing 403 `Auth.StepUpFailed`, with rate limiting and lockout. Nothing in the session changes.
2. `POST …/session/override { sequence, grantId, reason }`:
   - load the session (completed → 409 `Weighing.BatchCompleted`); the component must be active;
   - **read the scale on the server** (CONFIRM-SERVER-READ);
   - `OverrideRules`: class must be Under or Over (in range → 409 `Weighing.OverrideNotNeeded`: use Confirm); all other gaps must be clear (A11 → 422 with `missing`); the reason is 1–500 characters after trimming (422, field `reason`);
   - the grant must exist, have action `override`, be unused and unexpired (else 403 `Auth.GrantInvalid`), and the **supervisor ≠ the signed-in operator** (403 `Auth.FourEyesRequired`);
   - success: a record with the **true class**, `OverrideDetails` and `Surplus` (if Overweight); the **grant is consumed in the same transaction** (single use); audit `Override` (reason, supervisor); next component active or auto-completion.
   - Any failure stores nothing, and **the grant stays unused**, so the supervisor needn't re-enter the PIN for a fixable reason (for example a missing lot).

### Flow: completion (story 013, AUTO-COMPLETE)
- In the domain, `Confirm` and `Override` call `Complete(operator, now)` when no component is pending. That sets `Completed`, `completedAt` and `completedBy`, and raises `BatchCompleted` (header from the recipe snapshot, plus the N records).
- The service then calls every registered `IBatchCompletedHandler` **before** the save, so whatever they add (bolt 011: an outbox row; bolt 012: a print-pending ticket) is saved **in the same transaction**, along with the `BatchCompleted` audit entry.
- `POST …/session/completion` is the explicit, idempotent call: already completed → 200 with the summary; components pending → 409 `Batch.Incomplete` (with done and total).

### Flow: summary and review (story 014, A12–A18)
- `GET …/session/summary` → `BatchSummary`, available for any session (`status` says whether it is complete). Built by `BatchSummaryBuilder` from the session, its records and the recipe snapshot header.
- `GET /api/weighing-sessions?status=completed&page=1` → newest first, 20 per page, read-only.

### The PD-05 slot (decided at the ADR stage, STEP 24)
The reading endpoint already reports `surplus` and `override.offered`. Whatever rule is approved at STEP 24 is applied in `OverrideRules` and the `Overshoot` handling of the aggregate. One example would be a configurable limit beyond which an Overweight can't be overridden and must be re-weighed. The API shapes below don't change with the choice; only which requests succeed does. **No option recalculates other components** (FR-16).

## API Design

All endpoints require a signed-in operator; commands need `X-XSRF-TOKEN`.

| Endpoint | Method | Request | Response |
|----------|--------|---------|----------|
| `/api/batches/{batchId}/session/override` | POST | `{ "sequence": 3, "grantId": "…", "reason": "Lot ran out; supervisor accepts 93.3 g" }` | **201** `ConfirmationResponse` (record with `override`) · 403 · 409 · 422 · 503 |
| `/api/batches/{batchId}/session/completion` | POST | — | 200 `BatchSummaryResponse` · 409 `Batch.Incomplete` |
| `/api/batches/{batchId}/session/summary` | GET | — | 200 `BatchSummaryResponse` |
| `/api/weighing-sessions?status=completed&page=1` | GET | — | 200 `{ items: [ { batchId, orderId, machine, shade, completedAt, done, total, allWithinTolerance, overrideCount } ], page, pageSize, total }` |
| `…/session/reading` (bolts 007–008) | GET | — | + `surplus: { amount{value,unit}, percentOfTarget }` or null · + `override: { offered: bool }` |

Additive changes: the session gets `status: "completed"`, `completedAt` and `completedBy`; a record gets `override: { supervisorBadgeId, supervisorName, reason, overriddenAt } | null` and `surplus | null`.

`BatchSummaryResponse` (for B-24187, with the mockup values):
```json
{
  "batchId": "B-24187", "orderId": "SO-9931", "machine": "JET-04", "shade": "Teal 4C",
  "status": "completed", "done": 5, "total": 5,
  "allWithinTolerance": true, "overrideCount": 0,
  "totalDispensed": { "value": 35.322, "unit": "kg" },
  "startedAt": "2026-11-05T01:41:41Z", "completedAt": "2026-11-05T01:48:22Z", "elapsedSeconds": 401,
  "operators": ["R. Karim"],
  "orgatex": { "state": "notAvailable" }, "ticket": { "state": "notAvailable" },
  "lines": [ { "sequence": 1, "chemicalName": "Remazol Brill. Blue RSPL", "required": { "value": 1240.0, "unit": "g" },
               "actual": { "value": 1241.5, "unit": "g" }, "status": "withinRange", "override": false } ]
}
```

## Data Persistence

Migration **`AddOverridesAndCompletion`** (additive):

| Table | Columns | Relationships |
|-------|---------|---------------|
| `weighing_records` (+) | `override_grant_id` uuid null · `override_supervisor_badge_id` text null · `override_supervisor_name` text null · `override_reason` varchar(500) null · `overridden_at` timestamptz null · `surplus_value` numeric(12,4) null · `surplus_unit` text null · `surplus_percent` numeric(7,2) null · unique (`override_grant_id`) where not null | The unique grant ID is the database backstop for single use |
| `weighing_sessions` (+) | `completed_at` timestamptz null · `completed_by` text null · index (`status`, `completed_at` desc) | For the review list |

Records stay immutable: the override is part of the record when it is created, never added later.

## Security Design

| Concern | Approach |
|---------|----------|
| Authentication | Cookie session (operator); supervisor identity only through the existing step-up (badge + PIN, rate-limited, lockout) |
| Authorization | Override needs a valid **single-use** `override` grant from a supervisor **other than** the operator (OVR-FOUR-EYES); the grant expires after the configured lifetime (2 min) |
| Integrity | The server re-reads the scale for an override as for a confirm; the grant is consumed in the record's transaction; there is a unique grant ID on records |
| Audit | `Override` (supervisor badge, reason) and the new `BatchCompleted` type; step-up success and failure are already audited (unit 002) |

## NFR Implementation

| Requirement | Design Approach |
|-------------|-----------------|
| Reliability | Completion and everything the handlers add happen in **one transaction**; a crash leaves either "completed with its side effects" or "not completed" |
| Correctness | Surplus and total dispensed are fixed-point; the summary is computed from the stored records, not cached |
| Performance | The review list is one indexed query; the summary is one session load plus the snapshot header |

## Error Handling

| Error Type | Code | Response |
|------------|------|----------|
| Override when the reading is in range | `Weighing.OverrideNotNeeded` | 409 |
| Grant missing, used, expired or for another action | `Auth.GrantInvalid` (exists) | 403 |
| Supervisor = operator | `Auth.FourEyesRequired` | 403 |
| Reason empty or too long | `Request.Invalid` (field `reason`) | 422 |
| Other confirm conditions not met | `Weighing.NotConfirmable` | 422 + `missing` |
| Completion with pending components | `Batch.Incomplete` | 409 (done, total) |
| Any command on a completed session | `Weighing.BatchCompleted` | 409 |
| (bolts 007–008) as before | — | — |

## External Dependencies

| Service | Purpose | Integration |
|---------|---------|-------------|
| Station access (unit 002) | Step-up grant (find, consume), audit, clock | In-process ports |
| Units 006 / 007 (bolts 011 / 012) | React to `BatchCompleted` | `IBatchCompletedHandler` in-process, same transaction |
| Dummy scales (unit 004) | Server read at override | Existing port |

## Decisions at This Checkpoint: resolved (APPROVE DESIGN, 2026-11-06)

- **T1: option 1.** Two calls: step-up grant, then override with `grantId` (OVR-GRANT-FLOW).
- **T2: option 1.** `GET /api/weighing-sessions?status=completed` (REVIEW-LIST).
- **T3: option 1.** In-process `IBatchCompletedHandler`s inside the completing transaction (COMPLETION-HANDLERS).

#### Options as presented

**T1. How does the screen authorise an override?**
1 - **(Recommended)** **Two calls**: the existing `POST /api/session/step-up` (badge + PIN → single-use grant, ADR-009), then `POST …/session/override` with the `grantId`. It reuses the tested step-up with its rate limit and lockout, and the PIN is never sent to the weighing endpoint.
2 - **One call**: send the supervisor badge and PIN inside the override request. Fewer round trips, but it duplicates the step-up checks (rate limit, lockout, audit) in a second place.

**T2. Where is the review list?**
1 - **(Recommended)** `GET /api/weighing-sessions?status=completed`: a WeighingMS resource, separate from the OrgaTEX queue.
2 - `GET /api/batches?status=completed`: next to the queue, but `/api/batches` is OrgaTEX's live queue (STEP 16 contract), and completed batches may no longer be in it.

**T3. How do bolts 011 and 012 react to completion?**
1 - **(Recommended)** In-process `IBatchCompletedHandler`s called inside the completing transaction. Bolt 011's handler writes the outbox row (PD-01) and bolt 012's the print-pending ticket, all atomic with completion. No extra infrastructure now.
2 - A generic domain-event outbox table now, drained by a background worker for every consumer. More general, but it duplicates the write-back outbox PD-01 already defines for bolt 011.

## ADR Analysis (Stage 3)

**Stage 3 is required in this bolt.** It holds the **PD-05 overshoot decision (workbook STEP 24)**:
- what is known and what is assumed;
- 2–3 safe training options with their impact;
- a stop for your choice;
- the chosen rule recorded as `adr-…-overshoot.md` and copied to `evidence/STEP-24-overshoot-decision.md`.

T1–T3 follow existing patterns and go to the decision index.
