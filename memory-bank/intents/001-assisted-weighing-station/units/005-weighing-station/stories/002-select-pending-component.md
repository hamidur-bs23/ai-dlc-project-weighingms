---
id: 002-select-pending-component
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-4, FR-17]
created: 2026-11-04T16:00:00Z
assigned_bolt: 007-weighing-station
implemented: false
---

# Story: 002-select-pending-component

**Select any pending component, sequence suggested**

## User Story

**As a** operator
**I want** to choose which pending chemical to weigh
**So that** I can work around a temporarily unavailable chemical

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-4, FR-17 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §10 R6–R8 (L718–724)

## Acceptance Criteria

- [ ] **Given** pending components, **When** I select any of them, **Then** it becomes active and the others stay pending
- [ ] **Given** a done component, **When** I select it, **Then** nothing changes
- [ ] **Given** an attempt in progress, **When** I switch component, **Then** the attempt is abandoned like Quit (no record)

## UI/UX Impact

Recipe rail selection (L696–724) via 008-004.

## API Dependencies

- Api: select component command

## Test Expectations

- Domain tests: free selection, done not selectable, switching abandons

## Dependencies

### Requires
- 005-weighing-station / 001-start-or-resume-weighing-session

### Enables
- 005-weighing-station / 004-capture-lot-and-container
- 005-weighing-station / 005-tare-required-and-re-tare
- 005-weighing-station / 012-quit-and-incomplete-batch
- 008-assisted-weighing-station-ui / 004-weigh-screen-rail-and-header

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Strict sequence (not chosen, PD-07)
- Implementation tasks (planned in bolts, STEP 12+)
