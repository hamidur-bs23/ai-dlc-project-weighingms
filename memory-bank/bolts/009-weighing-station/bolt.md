---
id: 009-weighing-station
unit: 005-weighing-station
intent: 001-assisted-weighing-station
type: ddd-construction-bolt
status: in-progress
stories:
  - 010-overshoot-warn-and-block
  - 011-supervisor-override
  - 013-complete-batch-all-confirmed
  - 014-weighing-records-and-review
created: 2026-11-04T17:00:00Z
started: 2026-11-05T18:37:11Z
completed: null
current_stage: implement
stages_completed:
  - name: model
    completed: '2026-11-05T18:50:00Z'
    artifact: ddd-01-domain-model.md
  - name: design
    completed: '2026-11-05T19:05:00Z'
    artifact: ddd-02-technical-design.md
  - name: adr
    completed: '2026-11-05T18:42:00Z'
    artifact: adr-001-overshoot.md (PD-05 option 1)

requires_bolts: [003-station-access, 008-weighing-station]
enables_bolts: [010-assisted-weighing-station-ui, 011-consumption-writeback, 012-ticket-printing]
requires_units: []
blocks: true

complexity:
  avg_complexity: 3
  avg_uncertainty: 3
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 009-weighing-station

## Overview

DDD construction bolt for unit `005-weighing-station`, covering workbook STEP 24 (decision), STEP 23, STEP 26 preparation.

## Objective (expected outcome)

Out-of-tolerance handling and completion. **The ADR stage holds the PD-05 overshoot decision: present 2–3 safe training options and stop for the human decision (workbook STEP 24); the prototype's recalculation is not adopted.** Supervisor override with PIN + reason (true class kept, OVR flag, audited); completion only when all N components are confirmed, raising `BatchCompleted`; records and the completed-batch review query.

## Stories Included

- **010-overshoot-warn-and-block**: Overshoot: warn and block (no compensation) (Must)
- **011-supervisor-override**: Supervisor override of an out-of-tolerance weighing (Must)
- **013-complete-batch-all-confirmed**: Complete the batch when all N are confirmed (Must)
- **014-weighing-records-and-review**: Weighing records with traceability, and completed-batch review (Must)

Story files: `memory-bank/intents/001-assisted-weighing-station/units/005-weighing-station/stories/`

## Bolt Type

**Type**: DDD Construction Bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/ddd-construction-bolt.md`

## Stages

- ✅ **1. model**: Complete → ddd-01-domain-model.md
- ⏳ **2. design**: In Progress → ddd-02-technical-design.md
- ✅ **3. adr**: Complete → adr-001-overshoot.md (PD-05: option 1, human decision)
- ⏳ **4. implement**: In Progress → implementation-walkthrough.md
- [ ] **5. test**: Pending → ddd-03-test-report.md

Every stage stops at a human validation checkpoint (workbook STEP 15, CHECKPOINT 5).

## Dependencies

### Requires
- 003-station-access
- 008-weighing-station

### Enables
- 010-assisted-weighing-station-ui
- 011-consumption-writeback
- 012-ticket-printing

## Test Expectations

- Domain tests: overshoot blocks confirm and shows the surplus; override success, wrong PIN, missing reason
- Completion guard: rejected while any component is pending
- Integration test: `BatchCompleted` raised in the completion transaction
- `STEP-24-overshoot-decision.md` and test evidence

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEP 24 (decision), STEP 23, STEP 26 preparation.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
- PD-05 must not be implemented before the human decision is recorded in `decision-index.md`.
