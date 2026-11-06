---
id: 005-weigh-screen-readout-bar-status
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-9
  - FR-10
  - FR-11
  - FR-12
  - FR-16
created: '2026-11-04T16:00:00Z'
assigned_bolt: 010-assisted-weighing-station-ui
implemented: true
---

# Story: 005-weigh-screen-readout-bar-status

**Weigh screen: readout, progress bar with band and target line, status line**

## User Story

**As a** operator
**I want** the live readout, progress bar and status line
**So that** I can pour to target by eye

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-9, FR-10, FR-11, FR-12, FR-16 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §2, §4 (L515–554)

## Acceptance Criteria

- [ ] **Given** readings, **Then** "Weighed now", "Still to weigh" and "Target" are shown with the unit (1 dp for g, 3 dp for kg)
- [ ] **Given** the bar, **Then** it shows fill, the tolerance band and the target line, with caption "TOLERANCE ±0.5 g (93.5 – 94.5)"
- [ ] **Given** each class, **Then** a distinct state with text: DISPENSE — x REMAINING / WITHIN TOLERANCE — PRESS OK / TARGET ACHIEVED / OVER TOLERANCE — SURPLUS +x (+y%)
- [ ] **Given** ≥ 90% of target but below range, **Then** an amber hint only (not a class; Could, OQ-4)
- [ ] **Given** overshoot, **Then** only the excess is shown; no recalculation table (PD-05)
- [ ] **Given** an unstable reading, **Then** it is labelled unstable

## UI/UX Impact

Readout, bar, caption, status line, surplus (L515–554).

## API Dependencies

- Api weight endpoint with class (005-006), polled at the configured interval

## Test Expectations

- Vitest: rendering per class and boundary readings; reduced-motion respected

## Dependencies

### Requires
- 005-weighing-station / 006-classify-reading-four-classes
- 005-weighing-station / 010-overshoot-warn-and-block
- 008-assisted-weighing-station-ui / 004-weigh-screen-rail-and-header

### Enables
- 008-assisted-weighing-station-ui / 006-weigh-actions-and-keys
- 008-assisted-weighing-station-ui / 013-end-to-end-batch-flow

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Choosing the class (server only)
- Implementation tasks (planned in bolts, STEP 12+)
