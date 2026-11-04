---
id: 002-select-batch-by-touch-or-scan
unit: 003-batch-recipe
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-2, FR-27]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 002-select-batch-by-touch-or-scan

**Select a batch by touch or scanned batch ID**

## User Story

**As a** operator
**I want** to open a batch by touching its row or scanning its card
**So that** I start weighing the right batch quickly

## Unit & Requirements

- **Unit**: `003-batch-recipe` (`../unit-brief.md`)
- **Requirements**: FR-2, FR-27 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §3 (L453, L463–482)

## Acceptance Criteria

- [ ] **Given** the queue, **When** I touch B-24187 or scan "B-24187", **Then** the Weigh view opens for B-24187 in both cases
- [ ] **Given** a scanned ID not in the queue, **Then** "batch not found" is shown and nothing changes
- [ ] **Given** a completed batch, **When** selected, **Then** its read-only summary opens instead (005-014)
- [ ] **Given** a selection, **Then** the header shows batch, order, machine, shade and the signed-in operator

## UI/UX Impact

Queue rows and scan prompt (L450–484); top-bar crumb (L442).

## API Dependencies

- Api `GET /api/batches/:batchId`; scan events from 004-004

## Test Expectations

- Integration tests: touch vs scan equivalence, unknown ID, completed batch

## Dependencies

### Requires
- 003-batch-recipe / 001-view-batch-queue
- 004-device-integration / 004-scan-delivery-to-station

### Enables
- 003-batch-recipe / 003-load-recipe-snapshot
- 008-assisted-weighing-station-ui / 003-select-batch-screen

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Scan arrives while on the Weigh view | Treated as a lot, not a batch (004-004) |

## Out of Scope

- Loading the recipe (003-003)
- Implementation tasks (planned in bolts, STEP 12+)
