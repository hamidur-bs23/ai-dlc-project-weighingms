---
id: 006-classify-reading-four-classes
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-9, FR-10, FR-11, FR-12]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 006-classify-reading-four-classes

**Classify each reading into 4 classes, with remaining**

## User Story

**As a** operator
**I want** each reading classified and the remaining amount shown
**So that** I know whether to pour more, stop, or ask for help

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-9, FR-10, FR-11, FR-12 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §4 weight zones, §10 R1–R4 (L729–780)

## Acceptance Criteria

- [ ] **Given** target t and tolerance ±d, **Then** net < t−d → Underweight; t−d ≤ net ≤ t+d and net ≠ t → Within range; net = t at the scale resolution → Target achieved; net > t+d → Overweight
- [ ] **Given** 1.000 ± 0.010 kg, **Then** 0.980 Underweight, 0.995 Within, 1.000 Target achieved, 1.008 Within, 1.020 Overweight
- [ ] **Given** Yellow RGB 94.0 ± 0.5 g, **Then** 93.5, 94.0 and 94.5 are confirmable classes; 93.4 and 94.6 are not
- [ ] **Given** any reading, **Then** remaining = max(0, target − net), and the class is computed on the server with fixed-point decimals
- [ ] **Given** the response, **Then** target, ±tolerance and range (e.g. "TOLERANCE ±0.5 g (93.5 – 94.5)") are included

## UI/UX Impact

Readout, bar colour, status line (L515–541) via 008-005.

## API Dependencies

- Api weight endpoint enriched with class and remaining for the active component

## Test Expectations

- Domain tests on every PDF §7 and STEP 22 boundary value (exact decimals)

## Dependencies

### Requires
- 005-weighing-station / 007-resolve-tolerance-source
- 004-device-integration / 001-live-scale-reading

### Enables
- 005-weighing-station / 008-confirm-valid-weighing
- 005-weighing-station / 010-overshoot-warn-and-block
- 005-weighing-station / 011-supervisor-override
- 008-assisted-weighing-station-ui / 005-weigh-screen-readout-bar-status

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Reading at 93.45 on a 0.1 g scale | Rounded to resolution first, per scale |

## Out of Scope

- Near-target amber hint (UI only, 008-005, OQ-4)
- Implementation tasks (planned in bolts, STEP 12+)
