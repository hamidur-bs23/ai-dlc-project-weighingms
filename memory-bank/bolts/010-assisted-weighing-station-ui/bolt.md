---
id: 010-assisted-weighing-station-ui
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
type: simple-construction-bolt
status: complete
stories:
  - 004-weigh-screen-rail-and-header
  - 005-weigh-screen-readout-bar-status
  - 006-weigh-actions-and-keys
  - 007-lot-and-container-entry
  - 008-supervisor-override-dialog
created: '2026-11-04T17:00:00Z'
started: '2026-11-05T19:07:21Z'
completed: '2026-11-06T03:29:37Z'
current_stage: null
stages_completed:
  - name: plan
    completed: '2026-11-05T19:20:00Z'
    artifact: implementation-plan.md
  - name: implement
    completed: '2026-11-06T03:15:00Z'
    artifact: implementation-walkthrough.md
requires_bolts:
  - 006-assisted-weighing-station-ui
  - 009-weighing-station
enables_bolts:
  - 013-assisted-weighing-station-ui
requires_units:
  - 005-weighing-station
blocks: true
complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 010-assisted-weighing-station-ui

## Overview

Simple construction bolt for unit `008-assisted-weighing-station-ui`, covering workbook STEPs 20–25 (UI).

## Objective (expected outcome)

**Screen 2 · Weigh**: recipe rail and counter, component header (code, lot, container, tare), scale badge "automatic", readout (weighed now / still to weigh / target), progress bar with tolerance band and target line, the four class states with text (Target achieved distinct), the amber near-target hint, surplus display without recalculation, F1 / F2 / F4, lot and container entry, and the supervisor override dialog.

## Stories Included

- **004-weigh-screen-rail-and-header**: Weigh screen: recipe rail and component header (Must)
- **005-weigh-screen-readout-bar-status**: Weigh screen: readout, progress bar with band and target line, status line (Must)
- **006-weigh-actions-and-keys**: Weigh actions and keys: Confirm F1, Re-tare F2, Quit F4, Supervisor override (Must)
- **007-lot-and-container-entry**: Lot and container entry (Must)
- **008-supervisor-override-dialog**: Supervisor override dialog (Must)

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
- 006-assisted-weighing-station-ui
- 009-weighing-station

### Enables
- 013-assisted-weighing-station-ui

## Test Expectations

- Vitest per class state and boundary reading, enabled/disabled Confirm, key handlers
- STEP 20–22 evidence: weigh screen, scale switch 3 kg precision → 32 kg floor

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEPs 20–25 (UI).
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
