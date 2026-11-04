---
id: 004-dummy-scanner-emits-scans
unit: 001-station-simulators
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-27
created: '2026-11-04T16:00:00Z'
assigned_bolt: 002-station-simulators
implemented: true
---

# Story: 004-dummy-scanner-emits-scans

**Dummy scanner emits scans**

## User Story

**As a** tester
**I want** to emit a barcode scan through the Dummy Scanner
**So that** batch and lot scanning can be exercised without hardware

## Unit & Requirements

- **Unit**: `001-station-simulators` (`../unit-brief.md`)
- **Requirements**: FR-27 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §9 (L453, L621)

## Acceptance Criteria

- [ ] **Given** the simulator, **When** `POST /_sim/scanner/scan {"code":"B-24187"}` is called, **Then** the scan is queued for WeighingMS to collect
- [ ] **Given** the scanner, **When** its status is requested, **Then** it reports READY or OFFLINE
- [ ] **Given** no keyboard-wedge mode, **Then** scans arrive only through the device-style HTTP contract (PD-03)

## UI/UX Impact

None directly (backend). Makes the mockup data real for the scan prompt (L450–455) and lot capture.

## API Dependencies

- Serves `POST /_sim/scanner/scan`, scanner status and the collection endpoint used by 004 (route fixed in the bolt design)

## Test Expectations

- Contract tests: emit → collect once, status, FIFO order

## Dependencies

### Requires
- None

### Enables
- 001-station-simulators / 006-fault-injection-and-seed-reset
- 004-device-integration / 004-scan-delivery-to-station

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Two scans in quick succession | Both delivered in order |
| Empty code | 400 ProblemDetails |

## Out of Scope

- Routing a scan by view (004, 008)
- Implementation tasks (planned in bolts, STEP 12+)
