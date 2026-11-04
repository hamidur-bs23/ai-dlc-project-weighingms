---
id: 002-dummy-orgatex-receives-consumption
unit: 001-station-simulators
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-25, FR-20]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 002-dummy-orgatex-receives-consumption

**Dummy OrgaTEX receives and exposes consumption**

## User Story

**As a** tester
**I want** the Dummy OrgaTEX to accept and record consumption posts
**So that** I can prove the write-back happened exactly once (STEP 26 evidence)

## Unit & Requirements

- **Unit**: `001-station-simulators` (`../unit-brief.md`)
- **Requirements**: FR-25, FR-20 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §9 (L577, L580)

## Acceptance Criteria

- [ ] **Given** a completed batch, **When** `POST /api/batches/:batchId/consumption` is called, **Then** it answers 200 OK and stores the payload
- [ ] **Given** a POST repeats an `Idempotency-Key` already received, **When** it arrives, **Then** the original result is returned and nothing is stored twice
- [ ] **Given** consumption was received, **When** `GET /_sim/orgatex/consumption` is called, **Then** each received payload is listed once with its key and time

## UI/UX Impact

None directly (backend). Makes the mockup data real for "Written to OrgaTEX" on the summary (L577).

## API Dependencies

- Serves `POST /api/batches/:batchId/consumption` (+ `Idempotency-Key`)
- Serves `GET /_sim/orgatex/consumption`

## Test Expectations

- Contract tests: 200 OK, idempotent replay, inspection list

## Dependencies

### Requires
- 001-station-simulators / 001-dummy-orgatex-serves-batches-and-recipes

### Enables
- 006-consumption-writeback / 002-dispatch-with-idempotency-and-retry

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| POST for an unknown batch | 404 ProblemDetails, nothing stored |
| Missing Idempotency-Key | 400 ProblemDetails |

## Out of Scope

- Outbox and retry logic (006)
- Implementation tasks (planned in bolts, STEP 12+)
