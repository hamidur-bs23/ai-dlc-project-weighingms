---
id: 001-view-batch-queue
unit: 003-batch-recipe
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-1
created: '2026-11-04T16:00:00Z'
assigned_bolt: 005-batch-recipe
implemented: true
---

# Story: 001-view-batch-queue

**View the OrgaTEX batch queue**

## User Story

**As a** operator
**I want** to see the batches released for weighing
**So that** I know what to weigh next

## Unit & Requirements

- **Unit**: `003-batch-recipe` (`../unit-brief.md`)
- **Requirements**: FR-1 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §1 view 1, §2 queue table (L448–484)

## Acceptance Criteria

- [ ] **Given** the dummy seed, **When** the queue loads, **Then** it shows B-24187, B-24188, B-24190, B-24191 with batch, order, machine, fabric/lot, shade, component count and status
- [ ] **Given** a completed batch with print or sync pending, **Then** its row shows "print pending" or "sync pending"
- [ ] **Given** a batch removed from the dummy, **When** the queue reloads, **Then** it disappears; no batch data exists in UI code

## UI/UX Impact

Queue table (L458–484) via 008-003.

## API Dependencies

- Api `GET /api/batches` → `IOrgatexGateway` (read) → dummy `GET /api/batches`

## Test Expectations

- Integration test against the simulator (Testcontainers + simulator host)
- Architecture test: no batch literals in the web project

## Dependencies

### Requires
- 001-station-simulators / 001-dummy-orgatex-serves-batches-and-recipes

### Enables
- 003-batch-recipe / 002-select-batch-by-touch-or-scan
- 003-batch-recipe / 005-orgatex-unavailable-on-load
- 008-assisted-weighing-station-ui / 003-select-batch-screen

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Empty queue | "No batches released" message |

## Out of Scope

- Selecting a batch (003-002)
- Implementation tasks (planned in bolts, STEP 12+)
