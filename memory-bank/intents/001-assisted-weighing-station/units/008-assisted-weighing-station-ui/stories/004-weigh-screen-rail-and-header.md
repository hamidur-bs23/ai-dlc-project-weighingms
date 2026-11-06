---
id: 004-weigh-screen-rail-and-header
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-3
  - FR-4
  - FR-5
  - FR-6
  - FR-7
  - FR-8
  - FR-17
created: '2026-11-04T16:00:00Z'
assigned_bolt: 010-assisted-weighing-station-ui
implemented: true
---

# Story: 004-weigh-screen-rail-and-header

**Weigh screen: recipe rail and component header**

## User Story

**As a** operator
**I want** the recipe rail and the active component header
**So that** I always know what I'm weighing and how far the recipe has got

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-3, FR-4, FR-5, FR-6, FR-7, FR-8, FR-17 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §2 (L494–512, L696–716)

## Acceptance Criteria

- [ ] **Given** a session, **Then** the rail shows each component with colour swatch, name, code, scale, target + unit and state (✓ actual / WEIGHING / PENDING), and the counter "n of N components weighed"
- [ ] **Given** I touch a pending component, **Then** it becomes active; done ones are not selectable
- [ ] **Given** the active component, **Then** the header shows name, code, LOT, CONTAINER, TARE and the scale badge "automatic"

## UI/UX Impact

Rail and header (L494–512).

## API Dependencies

- Api session state, select command (005-002)

## Test Expectations

- Vitest component tests

## Dependencies

### Requires
- 005-weighing-station / 002-select-pending-component
- 005-weighing-station / 003-automatic-scale-selection
- 008-assisted-weighing-station-ui / 001-app-shell-top-bar-status-strip

### Enables
- 008-assisted-weighing-station-ui / 005-weigh-screen-readout-bar-status
- 008-assisted-weighing-station-ui / 007-lot-and-container-entry

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Lot / container entry (008-007)
- Implementation tasks (planned in bolts, STEP 12+)
