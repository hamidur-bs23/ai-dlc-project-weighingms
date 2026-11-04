---
id: 002-station-simulators
unit: 001-station-simulators
intent: 001-assisted-weighing-station
type: simple-construction-bolt
status: in-progress
stories:
  - 004-dummy-scanner-emits-scans
  - 005-dummy-printer-accepts-tickets
  - 006-fault-injection-and-seed-reset
created: 2026-11-04T17:00:00Z
started: 2026-11-04T19:10:00Z
completed: null
current_stage: test
stages_completed:
  - name: plan
    completed: 2026-11-04T19:20:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-11-04T19:40:00Z
    artifact: implementation-walkthrough.md

requires_bolts: [001-station-simulators]
enables_bolts: [004-device-integration, 005-batch-recipe, 011-consumption-writeback, 012-ticket-printing]
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 002-station-simulators

## Overview

Simple construction bolt for unit `001-station-simulators`, covering workbook STEP 16, STEP 30 preparation.

## Objective (expected outcome)

Dummy Scanner and Dummy Printer; fault injection (`/_sim/faults`), seed reset (`/_sim/reset`) and the inspection endpoints. `/_sim/*` is available only in Development/Test.

## Stories Included

- **004-dummy-scanner-emits-scans**: Dummy scanner emits scans (Must)
- **005-dummy-printer-accepts-tickets**: Dummy printer accepts and exposes tickets (Must)
- **006-fault-injection-and-seed-reset**: Fault injection and seed reset (Must)

Story files: `memory-bank/intents/001-assisted-weighing-station/units/001-station-simulators/stories/`

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
- 001-station-simulators

### Enables
- 004-device-integration
- 005-batch-recipe
- 011-consumption-writeback
- 012-ticket-printing

## Test Expectations

- Contract tests for each fault target × mode (unavailable / timeout / error500 / slow)
- Idempotent replay tests for the printer
- Environment test: `/_sim/*` absent outside Development/Test

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEP 16, STEP 30 preparation.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
