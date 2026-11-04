---
id: 003-scale-offline-and-invalid-readings
unit: 004-device-integration
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-23]
created: 2026-11-04T16:00:00Z
assigned_bolt: 004-device-integration
implemented: false
---

# Story: 003-scale-offline-and-invalid-readings

**Scale offline and invalid readings handled safely**

## User Story

**As a** operator
**I want** the station to stop me safely when the scale fails or reports nonsense
**So that** nothing wrong is ever recorded

## Unit & Requirements

- **Unit**: `004-device-integration` (`../unit-brief.md`)
- **Requirements**: FR-23 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §8 (invalid reading gap), §12 Q-UI-4

## Acceptance Criteria

- [ ] **Given** the selected scale is offline or times out, **Then** the reading shows "—", the message "Scale offline" appears, Confirm and Tare are disabled and nothing is recorded
- [ ] **Given** the scale comes back, **Then** a new tare is required
- [ ] **Given** a reading that is missing, non-numeric, below zero beyond the zero band, above capacity or error-flagged, **Then** it is invalid and never enables Confirm; above capacity shows "Over capacity"

## UI/UX Impact

Offline / invalid states of the readout (008-011).

## API Dependencies

- Api weight endpoint returning `IntegrationErrors.ScaleUnavailable` (503) or an invalid flag

## Test Expectations

- Integration tests with `/_sim/faults` scale:{id}; unit tests for each invalid-reading rule

## Dependencies

### Requires
- 004-device-integration / 001-live-scale-reading
- 001-station-simulators / 006-fault-injection-and-seed-reset

### Enables
- 008-assisted-weighing-station-ui / 011-error-and-offline-states

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Scale flaps offline/online | Re-tare required after each return |

## Out of Scope

- What the operator does next (005)
- Implementation tasks (planned in bolts, STEP 12+)
