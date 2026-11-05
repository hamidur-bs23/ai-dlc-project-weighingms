---
id: 004-capture-lot-and-container
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-5
  - FR-6
created: '2026-11-04T16:00:00Z'
assigned_bolt: 008-weighing-station
implemented: true
---

# Story: 004-capture-lot-and-container

**Capture lot and container for the component**

## User Story

**As a** operator
**I want** to record the dyestuff lot and the container for each chemical
**So that** every weighing can be traced to its material and container

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-5, FR-6 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §8 gap (L506, pin Q28/Q29)

## Acceptance Criteria

- [ ] **Given** the active component, **When** I scan or type a lot, **Then** it is stored for the attempt and shown in the header
- [ ] **Given** a suggested container ID, **When** I accept or type one, **Then** it is stored and shown
- [ ] **Given** an empty lot or container, **Then** Confirm stays disabled
- [ ] **Given** a lot, **Then** no identity check against the recipe is made (V1)

## UI/UX Impact

Header meta LOT / CONTAINER (L506) and the entry fields (008-007).

## API Dependencies

- Api: set lot / container commands; scans from 004-004

## Test Expectations

- Domain tests: confirm guard with missing lot or container

## Dependencies

### Requires
- 005-weighing-station / 002-select-pending-component

### Enables
- 005-weighing-station / 008-confirm-valid-weighing
- 008-assisted-weighing-station-ui / 007-lot-and-container-entry

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Lot with leading/trailing spaces | Trimmed |

## Out of Scope

- Container scanning (PD-03: not in V1)
- Implementation tasks (planned in bolts, STEP 12+)
