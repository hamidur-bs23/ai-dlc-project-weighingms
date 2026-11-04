---
id: 005-tare-required-and-re-tare
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-7]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 005-tare-required-and-re-tare

**Tare required before weighing; re-tare**

## User Story

**As a** operator
**I want** to tare before weighing and re-tare whenever I need
**So that** the confirmed weight is always net of the container

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-7 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §10 R12 (L558, L811)

## Acceptance Criteria

- [ ] **Given** no tare in this attempt on the current scale, **Then** Confirm is disabled
- [ ] **Given** I tare, **Then** the stored tare is the value the scale reports and it shows in the header
- [ ] **Given** I re-tare (F2), **Then** the net reading returns to zero, the stored tare is replaced and no record is created
- [ ] **Given** the scale went offline and returned, **Then** a new tare is required

## UI/UX Impact

Re-tare key F2 (L558) and TARE in the header (L506).

## API Dependencies

- Api: tare command → 004-002

## Test Expectations

- Domain tests: tare guard, re-tare replaces, no record

## Dependencies

### Requires
- 005-weighing-station / 002-select-pending-component
- 004-device-integration / 002-tare-a-scale

### Enables
- 005-weighing-station / 008-confirm-valid-weighing
- 008-assisted-weighing-station-ui / 006-weigh-actions-and-keys

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Quit (005-012)
- Implementation tasks (planned in bolts, STEP 12+)
