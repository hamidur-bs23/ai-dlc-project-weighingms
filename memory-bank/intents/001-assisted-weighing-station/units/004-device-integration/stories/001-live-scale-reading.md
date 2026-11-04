---
id: 001-live-scale-reading
unit: 004-device-integration
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-9]
created: 2026-11-04T16:00:00Z
assigned_bolt: 004-device-integration
implemented: false
---

# Story: 001-live-scale-reading

**Live scale reading at ≤ 250 ms**

## User Story

**As a** operator
**I want** the scale reading to update continuously while I pour
**So that** I can stop pouring at the right moment

## Unit & Requirements

- **Unit**: `004-device-integration` (`../unit-brief.md`)
- **Requirements**: FR-9 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §2 readout (L515–528), §9

## Acceptance Criteria

- [ ] **Given** the selected scale, **When** weighing, **Then** readings are polled at the configured interval (default 250 ms)
- [ ] **Given** a reading, **Then** it carries value (fixed-point), unit, stable flag and timestamp at the scale's resolution
- [ ] **Given** an unstable reading, **Then** it is marked unstable

## UI/UX Impact

Live readout and bar (L515–535) via 008-005.

## API Dependencies

- Api `GET /api/scales/:scaleId/weight` → `IScaleDevice` → dummy

## Test Expectations

- Integration test with the rising scenario; interval test with a fake clock

## Dependencies

### Requires
- 001-station-simulators / 003-dummy-scales-with-scenarios

### Enables
- 004-device-integration / 003-scale-offline-and-invalid-readings
- 004-device-integration / 006-device-health-for-status-strip
- 005-weighing-station / 006-classify-reading-four-classes

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Reading arrives out of order | Older timestamps are discarded |

## Out of Scope

- Classification (005-006)
- Implementation tasks (planned in bolts, STEP 12+)
