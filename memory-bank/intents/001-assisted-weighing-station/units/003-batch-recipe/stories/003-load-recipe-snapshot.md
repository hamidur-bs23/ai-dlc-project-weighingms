---
id: 003-load-recipe-snapshot
unit: 003-batch-recipe
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-3]
created: 2026-11-04T16:00:00Z
assigned_bolt: 005-batch-recipe
implemented: false
---

# Story: 003-load-recipe-snapshot

**Load an immutable recipe snapshot**

## User Story

**As a** operator
**I want** the recipe of the selected batch loaded and kept for the session
**So that** I can finish the batch even if OrgaTEX goes offline later

## Unit & Requirements

- **Unit**: `003-batch-recipe` (`../unit-brief.md`)
- **Requirements**: FR-3 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §2 recipe rail (L494–499)

## Acceptance Criteria

- [ ] **Given** B-24187 is selected, **When** the recipe loads, **Then** a snapshot of all 5 components in recipe sequence is stored with name, code, required quantity, unit and recipe tolerance
- [ ] **Given** a snapshot, **When** OrgaTEX later becomes unavailable, **Then** the batch can still be weighed and completed
- [ ] **Given** the snapshot, **Then** it never changes after load (job → recipe → chemical kept)
- [ ] **Given** the recipe can't be loaded, **Then** an error with Retry is shown and no weighing starts

## UI/UX Impact

Recipe rail content (L494–499) via 008-004.

## API Dependencies

- Api `GET /api/batches/:batchId/recipe` → `IOrgatexGateway` (read)

## Test Expectations

- Integration tests: snapshot persisted, survives OrgaTEX fault, immutability

## Dependencies

### Requires
- 003-batch-recipe / 002-select-batch-by-touch-or-scan

### Enables
- 005-weighing-station / 001-start-or-resume-weighing-session
- 005-weighing-station / 007-resolve-tolerance-source

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Recipe with an unknown unit | Rejected with an error; no session |
| Target ≤ 0 | Rejected |

## Out of Scope

- Weighing state (005)
- Implementation tasks (planned in bolts, STEP 12+)
