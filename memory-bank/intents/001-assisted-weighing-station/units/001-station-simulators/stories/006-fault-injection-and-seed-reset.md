---
id: 006-fault-injection-and-seed-reset
unit: 001-station-simulators
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-25
  - FR-26
  - FR-27
  - FR-28
  - FR-23
  - FR-24
created: '2026-11-04T16:00:00Z'
assigned_bolt: 002-station-simulators
implemented: true
---

# Story: 006-fault-injection-and-seed-reset

**Fault injection and seed reset**

## User Story

**As a** tester
**I want** to make any dummy fail on demand and to reset the seed
**So that** every STEP 30 failure scenario can be tested repeatably

## Unit & Requirements

- **Unit**: `001-station-simulators` (`../unit-brief.md`)
- **Requirements**: FR-25, FR-26, FR-27, FR-28, FR-23, FR-24 (`../../../requirements.md`)
- **Mockup / source reference**: api-conventions `/_sim/*`

## Acceptance Criteria

- [ ] **Given** `POST /_sim/faults {target, mode}` with target orgatex / scale:{id} / printer / scanner and mode unavailable / timeout / error500 / slow, **When** that dummy is called, **Then** it fails in that mode
- [ ] **Given** `durationMs`, **When** it elapses, **Then** the fault clears by itself; `DELETE /_sim/faults` clears all faults
- [ ] **Given** `POST /_sim/reset`, **Then** seed data, scenarios and inspection lists return to their initial state
- [ ] **Given** a Production environment, **Then** `/_sim/*` is not exposed

## UI/UX Impact

None (test control). Drives every offline state in the UI (008-011).

## API Dependencies

- Serves `POST /_sim/faults`, `DELETE /_sim/faults`, `POST /_sim/reset`

## Test Expectations

- Contract tests for each target × mode
- Environment test: `/_sim/*` absent outside Development/Test

## Dependencies

### Requires
- 001-station-simulators / 001-dummy-orgatex-serves-batches-and-recipes
- 001-station-simulators / 003-dummy-scales-with-scenarios
- 001-station-simulators / 004-dummy-scanner-emits-scans
- 001-station-simulators / 005-dummy-printer-accepts-tickets

### Enables
- 003-batch-recipe / 005-orgatex-unavailable-on-load
- 004-device-integration / 003-scale-offline-and-invalid-readings
- 004-device-integration / 005-scanner-offline-manual-fallback
- 004-device-integration / 006-device-health-for-status-strip
- 007-ticket-printing / 004-printer-unavailable-print-pending

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Fault on an unknown scale ID | 400 ProblemDetails |
| Overlapping faults on one target | The latest replaces the earlier |

## Out of Scope

- How WeighingMS reacts to faults (003, 004, 006, 007)
- Implementation tasks (planned in bolts, STEP 12+)
