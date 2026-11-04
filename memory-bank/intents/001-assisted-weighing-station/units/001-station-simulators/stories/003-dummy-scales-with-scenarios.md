---
id: 003-dummy-scales-with-scenarios
unit: 001-station-simulators
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-26, FR-9, FR-7]
created: 2026-11-04T16:00:00Z
assigned_bolt: 001-station-simulators
implemented: false
---

# Story: 003-dummy-scales-with-scenarios

**Two dummy scales with weighing scenarios and resolution**

## User Story

**As a** tester
**I want** two simulated scales whose readings follow chosen scenarios
**So that** every weighing state (rising, below range, in range, overshoot, stable) can be produced on demand

## Unit & Requirements

- **Unit**: `001-station-simulators` (`../unit-brief.md`)
- **Requirements**: FR-26, FR-9, FR-7 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §9, §10 R14 (L787–809)

## Acceptance Criteria

- [ ] **Given** the simulator, **When** `GET /api/scales` is called, **Then** it lists the Precision scale (3 kg) and the Heavy-Duty scale (32 kg) with capacity and resolution
- [ ] **Given** a scale, **When** `POST /_sim/scales/{scaleId}/scenario` sets rising / belowRange / enterRange / overshoot / stable for a target, **Then** `GET /api/scales/:scaleId/weight` returns readings following that scenario
- [ ] **Given** any reading, **When** it is returned, **Then** it respects the scale's resolution and carries value, unit, stable flag and timestamp
- [ ] **Given** a scale, **When** `POST /api/scales/:scaleId/tare` is called, **Then** the net reading returns to zero and the tare value is reported
- [ ] **Given** `GET /api/scales/:scaleId/status`, **Then** it reports READY or OFFLINE

## UI/UX Impact

None directly (backend). Makes the mockup data real for the live readout and bar (L515–541).

## API Dependencies

- Serves `GET /api/scales`, `/api/scales/:scaleId/status`, `/api/scales/:scaleId/weight`, `POST /api/scales/:scaleId/tare`
- Serves `POST /_sim/scales/{scaleId}/scenario`

## Test Expectations

- Contract tests per scenario (sequence shape, landing value, resolution)
- Rising scenario ends stable

## Dependencies

### Requires
- None

### Enables
- 001-station-simulators / 006-fault-injection-and-seed-reset
- 004-device-integration / 001-live-scale-reading
- 004-device-integration / 002-tare-a-scale

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Scenario target above capacity | Readings capped; over-capacity flag set |
| Tare while unstable | Tare still reported; stable flag false |

## Out of Scope

- Classification of readings (005)
- Implementation tasks (planned in bolts, STEP 12+)
