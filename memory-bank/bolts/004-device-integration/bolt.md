---
id: 004-device-integration
unit: 004-device-integration
intent: 001-assisted-weighing-station
type: simple-construction-bolt
status: complete
stories:
  - 001-live-scale-reading
  - 002-tare-a-scale
  - 003-scale-offline-and-invalid-readings
  - 004-scan-delivery-to-station
  - 005-scanner-offline-manual-fallback
  - 006-device-health-for-status-strip
created: '2026-11-04T17:00:00Z'
started: '2026-11-04T21:50:00Z'
completed: '2026-11-05T12:25:12Z'
current_stage: null
stages_completed:
  - name: plan
    completed: '2026-11-04T22:10:00Z'
    artifact: implementation-plan.md
  - name: implement
    completed: '2026-11-04T22:25:00Z'
    artifact: implementation-walkthrough.md
  - name: test
    completed: '2026-11-05T12:15:00Z'
    artifact: test-walkthrough.md
requires_bolts:
  - 001-station-simulators
  - 002-station-simulators
enables_bolts:
  - 005-batch-recipe
  - 006-assisted-weighing-station-ui
  - 007-weighing-station
requires_units:
  - 001-station-simulators
blocks: false
complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 004-device-integration

## Overview

Simple construction bolt for unit `004-device-integration`, covering workbook STEP 21, STEP 30.

## Objective (expected outcome)

`IScaleDevice` and `IScanner` adapters with resilience (timeout, retry with jitter, circuit breaker): live readings polled at a configurable 250 ms, tare, invalid-reading rules, scale-offline handling, scan delivery to the station, scanner-offline fallback and aggregated device health.

## Stories Included

- **001-live-scale-reading**: Live scale reading at ≤ 250 ms (Must)
- **002-tare-a-scale**: Tare a scale (Must)
- **003-scale-offline-and-invalid-readings**: Scale offline and invalid readings handled safely (Must)
- **004-scan-delivery-to-station**: Scans delivered from the scanner to the station (Must)
- **005-scanner-offline-manual-fallback**: Scanner offline: manual fallback (Must)
- **006-device-health-for-status-strip**: Device health for the status strip (Must)

Story files: `memory-bank/intents/001-assisted-weighing-station/units/004-device-integration/stories/`

## Bolt Type

**Type**: Simple Construction Bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- ✅ **1. plan**: Complete → implementation-plan.md
- ✅ **2. implement**: Complete → src/, implementation-walkthrough.md
- ✅ **3. test**: Complete → test-walkthrough.md

Every stage stops at a human validation checkpoint (workbook STEP 15, CHECKPOINT 5).

## Dependencies

### Requires
- 001-station-simulators
- 002-station-simulators

### Enables
- 005-batch-recipe
- 006-assisted-weighing-station-ui
- 007-weighing-station

## Test Expectations

- Integration tests against the simulator with `/_sim/faults` for each scale and the scanner
- Unit tests for every invalid-reading rule (missing, non-numeric, below zero band, over capacity, error flag)
- Poll interval test with a fake clock

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEP 21, STEP 30.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
