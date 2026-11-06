---
id: 007-lot-and-container-entry
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-5
  - FR-6
created: '2026-11-04T16:00:00Z'
assigned_bolt: 010-assisted-weighing-station-ui
implemented: true
---

# Story: 007-lot-and-container-entry

**Lot and container entry**

## User Story

**As a** operator
**I want** to scan or type the lot and accept or type the container
**So that** Confirm becomes possible only with full traceability

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-5, FR-6 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §8 gap (L506)

## Acceptance Criteria

- [ ] **Given** the weigh screen, **Then** lot and container fields are shown; a scan fills the lot
- [ ] **Given** a suggested container, **Then** it is pre-filled and editable
- [ ] **Given** either field is empty, **Then** Confirm stays disabled

## UI/UX Impact

New fields in the header area (L503–507), mockup style.

## API Dependencies

- Api set lot / container (005-004); scans (004-004)

## Test Expectations

- Vitest component tests

## Dependencies

### Requires
- 005-weighing-station / 004-capture-lot-and-container
- 008-assisted-weighing-station-ui / 004-weigh-screen-rail-and-header

### Enables
- None

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Lot identity verification
- Implementation tasks (planned in bolts, STEP 12+)
