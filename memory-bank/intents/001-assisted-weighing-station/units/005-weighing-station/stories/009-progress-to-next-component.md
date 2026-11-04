---
id: 009-progress-to-next-component
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-17]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 009-progress-to-next-component

**Progress to the next pending component**

## User Story

**As a** operator
**I want** the next pending chemical pre-selected after each confirmation
**So that** I keep a steady rhythm through the recipe

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-17 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §10 R8–R9 (L818–821)

## Acceptance Criteria

- [ ] **Given** a confirmation, **Then** the next pending component in recipe sequence is pre-selected
- [ ] **Given** progress, **Then** each component shows done (actual), weighing or pending and the counter shows "n of N"
- [ ] **Given** no pending component remains, **Then** the batch moves to completion (005-013)

## UI/UX Impact

Rail and counter (L497, L696–716).

## API Dependencies

- Api session state query

## Test Expectations

- Domain tests: suggestion order, counter, move to completion

## Dependencies

### Requires
- 005-weighing-station / 008-confirm-valid-weighing

### Enables
- 005-weighing-station / 013-complete-batch-all-confirmed

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Completion rules (005-013)
- Implementation tasks (planned in bolts, STEP 12+)
