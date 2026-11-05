---
id: 004-scan-delivery-to-station
unit: 004-device-integration
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-27
  - FR-2
  - FR-5
created: '2026-11-04T16:00:00Z'
assigned_bolt: 004-device-integration
implemented: true
---

# Story: 004-scan-delivery-to-station

**Scans delivered from the scanner to the station**

## User Story

**As a** operator
**I want** a scanned code to reach the screen I'm on
**So that** scanning a batch card or lot label just works

## Unit & Requirements

- **Unit**: `004-device-integration` (`../unit-brief.md`)
- **Requirements**: FR-27, FR-2, FR-5 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §9 scanner (L453, L621)

## Acceptance Criteria

- [ ] **Given** a scan emitted on the dummy, **When** WeighingMS collects it, **Then** it is delivered to the station once
- [ ] **Given** the queue view, **Then** the code is treated as a batch ID; **given** the weigh view, **Then** it is treated as a lot
- [ ] **Given** no keyboard wedge, **Then** scans arrive only through the scanner port (PD-03)

## UI/UX Impact

Scan prompt (L450–455) and lot field (008-007).

## API Dependencies

- Api: scan delivery to the SPA (mechanism fixed in the bolt design, within ADR-004 polling) ← `IScanner` ← dummy

## Test Expectations

- Integration test: emit → delivered once; routing by view (with 008 tests)

## Dependencies

### Requires
- 001-station-simulators / 004-dummy-scanner-emits-scans

### Enables
- 003-batch-recipe / 002-select-batch-by-touch-or-scan
- 004-device-integration / 005-scanner-offline-manual-fallback

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Scan while no one is signed in | Ignored |

## Out of Scope

- Lot identity verification (not in V1)
- Implementation tasks (planned in bolts, STEP 12+)
