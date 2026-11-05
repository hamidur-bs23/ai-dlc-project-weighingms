---
id: 002-tare-a-scale
unit: 004-device-integration
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-7
created: '2026-11-04T16:00:00Z'
assigned_bolt: 004-device-integration
implemented: true
---

# Story: 002-tare-a-scale

**Tare a scale**

## User Story

**As a** operator
**I want** to tare the empty container on the selected scale
**So that** the reading shows only the chemical I pour

## Unit & Requirements

- **Unit**: `004-device-integration` (`../unit-brief.md`)
- **Requirements**: FR-7 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §3 Re-tare F2 (L558, L811)

## Acceptance Criteria

- [ ] **Given** a READY scale, **When** tare is requested, **Then** the net reading becomes zero and the tare value reported by the scale is returned
- [ ] **Given** an OFFLINE scale, **When** tare is requested, **Then** it fails with ScaleUnavailable

## UI/UX Impact

Tare value in the header (L506).

## API Dependencies

- Api `POST /api/scales/:scaleId/tare` → `IScaleDevice`

## Test Expectations

- Integration tests: tare ok, tare offline

## Dependencies

### Requires
- 001-station-simulators / 003-dummy-scales-with-scenarios

### Enables
- 005-weighing-station / 005-tare-required-and-re-tare

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Tare during an unstable reading | Allowed; reported tare stored |

## Out of Scope

- The rule that tare is required (005-005)
- Implementation tasks (planned in bolts, STEP 12+)
