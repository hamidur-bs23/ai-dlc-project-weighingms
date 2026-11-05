---
id: 005-batch-recipe
unit: 003-batch-recipe
intent: 001-assisted-weighing-station
type: simple-construction-bolt
status: complete
stories:
  - 001-view-batch-queue
  - 002-select-batch-by-touch-or-scan
  - 003-load-recipe-snapshot
  - 004-configured-tolerance-fallback
  - 005-orgatex-unavailable-on-load
created: '2026-11-04T17:00:00Z'
started: '2026-11-05T12:20:00Z'
completed: '2026-11-05T13:48:25Z'
current_stage: null
stages_completed:
  - name: plan
    completed: '2026-11-05T12:35:00Z'
    artifact: implementation-plan.md
  - name: implement
    completed: '2026-11-05T13:15:00Z'
    artifact: implementation-walkthrough.md
  - name: test
    completed: '2026-11-05T13:47:44Z'
    artifact: test-walkthrough.md
requires_bolts:
  - 002-station-simulators
  - 004-device-integration
enables_bolts:
  - 006-assisted-weighing-station-ui
  - 007-weighing-station
requires_units:
  - 001-station-simulators
  - 004-device-integration
blocks: false
complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 005-batch-recipe

## Overview

Simple construction bolt for unit `003-batch-recipe`, covering workbook STEP 19.

## Objective (expected outcome)

The `IOrgatexGateway` read operations; the batch queue, selection by touch or scanned ID, the immutable recipe snapshot, the configured tolerance per chemical + scale, and error + Retry when OrgaTEX is unavailable while loading.

## Stories Included

- **001-view-batch-queue**: View the OrgaTEX batch queue (Must)
- **002-select-batch-by-touch-or-scan**: Select a batch by touch or scanned batch ID (Must)
- **003-load-recipe-snapshot**: Load an immutable recipe snapshot (Must)
- **004-configured-tolerance-fallback**: Configured tolerance per chemical + scale (Must)
- **005-orgatex-unavailable-on-load**: OrgaTEX unavailable while loading: error + Retry (Must)

Story files: `memory-bank/intents/001-assisted-weighing-station/units/003-batch-recipe/stories/`

## Bolt Type

**Type**: Simple Construction Bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- ✅ **1. plan**: Complete → implementation-plan.md
- ✅ **2. implement**: Complete → src/, implementation-walkthrough.md
- ⏳ **3. test**: In Progress → test-walkthrough.md

Every stage stops at a human validation checkpoint (workbook STEP 15, CHECKPOINT 5).

## Dependencies

### Requires
- 002-station-simulators
- 004-device-integration

### Enables
- 006-assisted-weighing-station-ui
- 007-weighing-station

## Test Expectations

- Integration tests against the simulator: queue content, selection, snapshot persisted and still usable while OrgaTEX is faulted
- Architecture test: no batch literals in the web project
- Configuration validation tests for tolerances

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEP 19.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
