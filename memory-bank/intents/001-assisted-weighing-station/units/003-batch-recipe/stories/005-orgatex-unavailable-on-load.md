---
id: 005-orgatex-unavailable-on-load
unit: 003-batch-recipe
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-24]
created: 2026-11-04T16:00:00Z
assigned_bolt: 005-batch-recipe
implemented: false
---

# Story: 005-orgatex-unavailable-on-load

**OrgaTEX unavailable while loading: error + Retry**

## User Story

**As a** operator
**I want** a clear error with Retry when OrgaTEX can't be reached
**So that** I never weigh against invented or stale data

## Unit & Requirements

- **Unit**: `003-batch-recipe` (`../unit-brief.md`)
- **Requirements**: FR-24 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §12 Q-UI-4; system-context failure boundaries

## Acceptance Criteria

- [ ] **Given** OrgaTEX is faulted, **When** the queue or a recipe loads, **Then** an error with Retry is shown and no data is invented
- [ ] **Given** the fault clears, **When** I press Retry, **Then** the data loads
- [ ] **Given** a batch whose recipe is already loaded, **Then** it can still be weighed and completed
- [ ] **Given** an integration error, **Then** the message is translated by error code; raw `detail` text is never shown

## UI/UX Impact

Queue and rail error states (008-011).

## API Dependencies

- Api `GET /api/batches…` returning 503 ProblemDetails with an error code

## Test Expectations

- Integration tests with `/_sim/faults` target orgatex (unavailable, timeout, error500)

## Dependencies

### Requires
- 003-batch-recipe / 001-view-batch-queue
- 001-station-simulators / 006-fault-injection-and-seed-reset

### Enables
- 008-assisted-weighing-station-ui / 011-error-and-offline-states

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Slow OrgaTEX | Timeout after the configured limit, then the error |

## Out of Scope

- Write-back outage (006)
- Implementation tasks (planned in bolts, STEP 12+)
