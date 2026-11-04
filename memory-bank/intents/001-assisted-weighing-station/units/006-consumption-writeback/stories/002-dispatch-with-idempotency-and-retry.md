---
id: 002-dispatch-with-idempotency-and-retry
unit: 006-consumption-writeback
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-20]
created: 2026-11-04T16:00:00Z
assigned_bolt: 011-consumption-writeback
implemented: false
---

# Story: 002-dispatch-with-idempotency-and-retry

**Dispatch with idempotency and bounded retry**

## User Story

**As a** production planner
**I want** queued consumption delivered automatically and never twice
**So that** OrgaTEX data stays correct

## Unit & Requirements

- **Unit**: `006-consumption-writeback` (`../unit-brief.md`)
- **Requirements**: FR-20 (`../../../requirements.md`)
- **Mockup / source reference**: system-context O1

## Acceptance Criteria

- [ ] **Given** a pending entry and OrgaTEX reachable, **Then** it is posted once with its `Idempotency-Key` and marked sent on 200 OK
- [ ] **Given** a failure, **Then** it is retried automatically with back-off
- [ ] **Given** a retry of an already-received post, **Then** OrgaTEX stores it once

## UI/UX Impact

None directly.

## API Dependencies

- `IOrgatexGateway` (consumption) → dummy `POST /api/batches/:batchId/consumption`

## Test Expectations

- Integration tests with `/_sim/faults` orgatex; `GET /_sim/orgatex/consumption` shows exactly one

## Dependencies

### Requires
- 006-consumption-writeback / 001-enqueue-consumption-on-completion
- 001-station-simulators / 002-dummy-orgatex-receives-consumption

### Enables
- 006-consumption-writeback / 003-sync-failed-and-manual-retry
- 006-consumption-writeback / 004-write-back-status-visible

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| API restarts mid-retry | Dispatcher resumes pending entries |

## Out of Scope

- Manual retry (006-003)
- Implementation tasks (planned in bolts, STEP 12+)
