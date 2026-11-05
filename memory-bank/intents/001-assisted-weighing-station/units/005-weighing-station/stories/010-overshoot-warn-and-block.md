---
id: 010-overshoot-warn-and-block
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-16
created: '2026-11-04T16:00:00Z'
assigned_bolt: 009-weighing-station
implemented: true
---

# Story: 010-overshoot-warn-and-block

**Overshoot: warn and block (no compensation)**

## User Story

**As a** operator
**I want** a clear warning when I've poured too much, and normal confirmation blocked
**So that** an overweight dose is never recorded as normal

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-16 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §10 R4, R15 (L544–554, L770–778)

## Acceptance Criteria

- [ ] **Given** net > target + tolerance, **Then** the status shows "OVER TOLERANCE — SURPLUS +x (+y%)" in a distinct state
- [ ] **Given** overshoot, **Then** Confirm is disabled and the actions offered are re-tare and re-weigh, quit, or supervisor override
- [ ] **Given** overshoot, **Then** no recalculation of other components happens (PD-05 is decided at STEP 24)

## UI/UX Impact

Status line and surplus panel (L541, L544–554): excess only, no recalculation table.

## API Dependencies

- Api weight endpoint (class Overweight + surplus)

## Test Expectations

- Domain tests: surplus value and percentage; no recalculation

## Dependencies

### Requires
- 005-weighing-station / 006-classify-reading-four-classes

### Enables
- 008-assisted-weighing-station-ui / 005-weigh-screen-readout-bar-status

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Any compensation policy (PD-05, STEP 24)
- Implementation tasks (planned in bolts, STEP 12+)
