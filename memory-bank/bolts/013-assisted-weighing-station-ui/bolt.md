---
id: 013-assisted-weighing-station-ui
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
type: simple-construction-bolt
status: complete
stories:
  - 009-confirm-and-print-screen
  - 010-ticket-preview-and-print
  - 012-completed-batch-review-list
created: 2026-11-04T17:00:00Z
started: 2026-11-06T06:31:50Z
completed: '2026-11-06T09:03:14Z'
current_stage: null
stages_completed:
  - name: plan
    completed: '2026-11-06T06:34:50Z'
    artifact: implementation-plan.md
  - name: implement
    completed: '2026-11-06T06:44:00Z'
    artifact: implementation-walkthrough.md
  - name: test
    completed: '2026-11-06T09:03:14Z'
    artifact: test-walkthrough.md

requires_bolts: [010-assisted-weighing-station-ui, 011-consumption-writeback, 012-ticket-printing]
enables_bolts: [014-assisted-weighing-station-ui]
requires_units: [006-consumption-writeback, 007-ticket-printing]
blocks: true

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 013-assisted-weighing-station-ui

## Overview

Simple construction bolt for unit `008-assisted-weighing-station-ui`, covering workbook STEPs 26–28 (UI).

## Objective (expected outcome)

**Screen 3 · Confirm & print**: the completion summary with Written to OrgaTEX / Pending sync / Sync failed + Retry, the ticket preview, Print and Reprint, Next batch (F3), and the completed-batch review list.

## Stories Included

- **009-confirm-and-print-screen**: Confirm & print screen: summary, write-back status, Next batch F3 (Must)
- **010-ticket-preview-and-print**: Ticket preview and print / reprint (Must)
- **012-completed-batch-review-list**: Completed-batch review list (Must)

Story files: `memory-bank/intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/`

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
- 010-assisted-weighing-station-ui
- 011-consumption-writeback
- 012-ticket-printing

### Enables
- 014-assisted-weighing-station-ui

## Test Expectations

- Vitest component tests for each write-back and print state
- Evidence for STEP 26–28 screens

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEPs 26–28 (UI).
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
