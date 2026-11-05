---
id: 008-weighing-station
unit: 005-weighing-station
intent: 001-assisted-weighing-station
type: ddd-construction-bolt
status: in-progress
stories:
  - 004-capture-lot-and-container
  - 005-tare-required-and-re-tare
  - 008-confirm-valid-weighing
  - 009-progress-to-next-component
  - 012-quit-and-incomplete-batch
created: 2026-11-04T17:00:00Z
started: 2026-11-05T18:08:29Z
completed: null
current_stage: implement
stages_completed:
  - name: model
    completed: '2026-11-05T18:20:00Z'
    artifact: ddd-01-domain-model.md
  - name: design
    completed: '2026-11-05T18:35:00Z'
    artifact: ddd-02-technical-design.md
  - name: adr
    completed: '2026-11-05T18:35:00Z'
    artifact: skipped (T1–T3 in decision-index)

requires_bolts: [007-weighing-station]
enables_bolts: [009-weighing-station]
requires_units: []
blocks: true

complexity:
  avg_complexity: 3
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 008-weighing-station

## Overview

DDD construction bolt for unit `005-weighing-station`, covering workbook STEP 23, STEP 25.

## Objective (expected outcome)

The session lifecycle: lot and container capture, tare required and re-tare, confirm (in tolerance + stable + lot + container + tare, re-checked on the server) writing exactly one immutable record, progression to the next pending component, and quit / leave / resume.

## Stories Included

- **004-capture-lot-and-container**: Capture lot and container for the component (Must)
- **005-tare-required-and-re-tare**: Tare required before weighing; re-tare (Must)
- **008-confirm-valid-weighing**: Confirm a valid weighing (Must)
- **009-progress-to-next-component**: Progress to the next pending component (Must)
- **012-quit-and-incomplete-batch**: Quit, leaving the batch incomplete (Must)

Story files: `memory-bank/intents/001-assisted-weighing-station/units/005-weighing-station/stories/`

## Bolt Type

**Type**: DDD Construction Bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/ddd-construction-bolt.md`

## Stages

- ✅ **1. model**: Complete → ddd-01-domain-model.md
- ✅ **2. design**: Complete → ddd-02-technical-design.md
- ✅ **3. adr**: Skipped (approved; T1–T3 recorded in decision-index)
- ⏳ **4. implement**: In Progress → implementation-walkthrough.md
- [ ] **5. test**: Pending → ddd-03-test-report.md

Every stage stops at a human validation checkpoint (workbook STEP 15, CHECKPOINT 5).

## Dependencies

### Requires
- 007-weighing-station

### Enables
- 009-weighing-station

## Test Expectations

- Domain tests for every confirm guard and for quit creating no record
- Integration tests: one record per confirm, duplicate submit rejected, records survive an API restart
- `STEP-25-transaction.json` evidence

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEP 23, STEP 25.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
