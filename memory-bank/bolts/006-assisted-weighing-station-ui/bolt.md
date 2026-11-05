---
id: 006-assisted-weighing-station-ui
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
type: simple-construction-bolt
status: complete
stories:
  - 001-app-shell-top-bar-status-strip
  - 002-sign-in-screen
  - 003-select-batch-screen
created: '2026-11-04T17:00:00Z'
started: '2026-11-05T13:54:37Z'
completed: '2026-11-05T17:24:48Z'
current_stage: null
stages_completed:
  - name: plan
    completed: '2026-11-05T15:55:00Z'
    artifact: implementation-plan.md
  - name: implement
    completed: '2026-11-05T16:55:00Z'
    artifact: implementation-walkthrough.md
requires_bolts:
  - 003-station-access
  - 004-device-integration
  - 005-batch-recipe
enables_bolts:
  - 010-assisted-weighing-station-ui
requires_units:
  - 002-station-access
  - 003-batch-recipe
  - 004-device-integration
blocks: false
complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 006-assisted-weighing-station-ui

## Overview

Simple construction bolt for unit `008-assisted-weighing-station-ui`, covering workbook STEP 19.

## Objective (expected outcome)

The Angular shell (top bar, status strip, plant-time clock, Transloco EN, F-key routing), the sign-in screen and **screen 1 · Select batch** fed only by `GET /api/batches`.

## Stories Included

- **001-app-shell-top-bar-status-strip**: App shell: top bar, status strip, clock, Transloco, function keys (Must)
- **002-sign-in-screen**: Sign-in screen (Must)
- **003-select-batch-screen**: Select-batch screen (scan prompt + queue table) (Must)

Story files: `memory-bank/intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/`

## Bolt Type

**Type**: Simple Construction Bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- ✅ **1. plan**: Complete → implementation-plan.md
- ✅ **2. implement**: Complete → implementation-walkthrough.md
- ⏳ **3. test**: In Progress → test-walkthrough.md

Every stage stops at a human validation checkpoint (workbook STEP 15, CHECKPOINT 5).

## Dependencies

### Requires
- 003-station-access
- 004-device-integration
- 005-batch-recipe

### Enables
- 010-assisted-weighing-station-ui

## Test Expectations

- Vitest + Testing Library component tests with a mocked Api
- STEP 19 evidence: batch screen, network calls, test run; no `const batches = [...]` in components

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEP 19.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
