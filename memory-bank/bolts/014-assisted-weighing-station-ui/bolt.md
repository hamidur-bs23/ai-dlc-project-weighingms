---
id: 014-assisted-weighing-station-ui
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
type: simple-construction-bolt
status: complete
stories:
  - 011-error-and-offline-states
  - 013-end-to-end-batch-flow
created: '2026-11-04T17:00:00Z'
started: '2026-11-06T09:04:41Z'
completed: '2026-11-10T18:39:13Z'
current_stage: null
stages_completed:
  - name: plan
    completed: '2026-11-06T09:08:55Z'
    artifact: implementation-plan.md
  - name: implement
    completed: '2026-11-10T17:21:40Z'
    artifact: implementation-walkthrough.md
  - name: test
    completed: '2026-11-10T18:39:13Z'
    artifact: test-walkthrough.md
requires_bolts:
  - 013-assisted-weighing-station-ui
enables_bolts: []
requires_units:
  - 001-station-simulators
  - 002-station-access
  - 003-batch-recipe
  - 004-device-integration
  - 005-weighing-station
  - 006-consumption-writeback
  - 007-ticket-printing
blocks: true
complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 3
  testing_scope: 3
---

# Bolt: 014-assisted-weighing-station-ui

## Overview

Simple construction bolt for unit `008-assisted-weighing-station-ui`, covering workbook STEPs 28–30.

## Objective (expected outcome)

Every failure state visible and safe on every screen, and the **Playwright end-to-end test** of B-24187: sign in → select → five components confirmed → batch complete → OrgaTEX write-back → ticket, plus the STEP 30 failure suite (scale, OrgaTEX, printer unavailable; invalid, over and under tolerance; re-tare; quit; incomplete batch).

## Stories Included

- **011-error-and-offline-states**: Error and offline states across screens (Must)
- **013-end-to-end-batch-flow**: End-to-end batch B-24187 (Playwright) (Must)

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
- 013-assisted-weighing-station-ui

### Enables
- None (final bolt; leads to Operations)

## Test Expectations

- Playwright (TS) against `docker compose --profile full --wait`
- `STEP-29-playwright-report/` and `STEP-30-failure-tests.md`
- Vitest for each error state

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEPs 28–30.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
