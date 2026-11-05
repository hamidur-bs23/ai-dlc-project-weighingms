---
id: 003-select-batch-screen
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-1
  - FR-2
created: '2026-11-04T16:00:00Z'
assigned_bolt: 006-assisted-weighing-station-ui
implemented: true
---

# Story: 003-select-batch-screen

**Select-batch screen (scan prompt + queue table)**

## User Story

**As a** operator
**I want** the Select batch screen as in the mockup
**So that** I can start a batch by scan or touch

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-1, FR-2 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §1 view 1 (L448–487)

## Acceptance Criteria

- [ ] **Given** the queue, **Then** the scan prompt and the table (Batch, Order, Machine, Fabric / lot, Shade, Comp., Status tag) are shown from the Api
- [ ] **Given** I touch a row or a scan arrives, **Then** the batch opens (Weigh or read-only summary)
- [ ] **Given** "batch not found", **Then** it is shown without leaving the screen

## UI/UX Impact

View 1 (L448–487).

## API Dependencies

- Api `GET /api/batches`, `GET /api/batches/:batchId`; scan delivery (004-004)

## Test Expectations

- Vitest component tests with a mocked Api

## Dependencies

### Requires
- 003-batch-recipe / 001-view-batch-queue
- 003-batch-recipe / 002-select-batch-by-touch-or-scan
- 008-assisted-weighing-station-ui / 001-app-shell-top-bar-status-strip

### Enables
- 008-assisted-weighing-station-ui / 013-end-to-end-batch-flow

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Queue filtering
- Implementation tasks (planned in bolts, STEP 12+)
