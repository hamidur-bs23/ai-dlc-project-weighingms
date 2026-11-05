---
id: 005-scanner-offline-manual-fallback
unit: 004-device-integration
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-24
created: '2026-11-04T16:00:00Z'
assigned_bolt: 004-device-integration
implemented: true
---

# Story: 005-scanner-offline-manual-fallback

**Scanner offline: manual fallback**

## User Story

**As a** operator
**I want** to keep working by touch and typing when the scanner is down
**So that** a broken scanner never stops weighing

## Unit & Requirements

- **Unit**: `004-device-integration` (`../unit-brief.md`)
- **Requirements**: FR-24 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §2 status strip SCANNER (L621)

## Acceptance Criteria

- [ ] **Given** the scanner is faulted, **Then** the status strip shows SCANNER OFFLINE
- [ ] **Given** the scanner is offline, **Then** batch selection by touch and typed lot entry still work

## UI/UX Impact

Status strip and fallbacks (008-011).

## API Dependencies

- Api scanner status ← `IScanner`

## Test Expectations

- Integration test with `/_sim/faults` target scanner

## Dependencies

### Requires
- 004-device-integration / 004-scan-delivery-to-station
- 001-station-simulators / 006-fault-injection-and-seed-reset

### Enables
- 008-assisted-weighing-station-ui / 011-error-and-offline-states

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Scanner repair flows
- Implementation tasks (planned in bolts, STEP 12+)
