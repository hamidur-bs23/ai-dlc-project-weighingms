---
id: 007-weighing-station
unit: 005-weighing-station
intent: 001-assisted-weighing-station
type: ddd-construction-bolt
status: in-progress
stories:
  - 001-start-or-resume-weighing-session
  - 002-select-pending-component
  - 003-automatic-scale-selection
  - 006-classify-reading-four-classes
  - 007-resolve-tolerance-source
created: 2026-11-04T17:00:00Z
started: 2026-11-05T17:29:20Z
completed: null
current_stage: implement
stages_completed:
  - name: model
    completed: '2026-11-05T17:40:00Z'
    artifact: ddd-01-domain-model.md
  - name: design
    completed: '2026-11-05T17:55:00Z'
    artifact: ddd-02-technical-design.md
  - name: adr
    completed: '2026-11-05T17:55:00Z'
    artifact: skipped (no ADR-worthy decision; T1–T3 in decision-index)

requires_bolts: [003-station-access, 004-device-integration, 005-batch-recipe]
enables_bolts: [008-weighing-station]
requires_units: [002-station-access, 003-batch-recipe, 004-device-integration]
blocks: true

complexity:
  avg_complexity: 3
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 007-weighing-station

## Overview

DDD construction bolt for unit `005-weighing-station`, covering workbook STEPs 21–22.

## Objective (expected outcome)

The weighing domain model: `WeighingSession`, `ComponentWeighing`, `Quantity` (fixed-point), `ToleranceBand`, `ScaleSelection`. Sessions start or resume, any pending component can be selected, the scale is chosen by the configurable ≤ 3 kg threshold, and each reading is classified on the server into Underweight / Within range / Target achieved / Overweight with the tolerance source recipe → config → blocked.

## Stories Included

- **001-start-or-resume-weighing-session**: Start or resume a weighing session for a batch (Must)
- **002-select-pending-component**: Select any pending component, sequence suggested (Must)
- **003-automatic-scale-selection**: Automatic scale selection by the ≤ 3 kg threshold (Must)
- **006-classify-reading-four-classes**: Classify each reading into 4 classes, with remaining (Must)
- **007-resolve-tolerance-source**: Resolve the tolerance source (recipe → config → blocked) (Must)

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
- 003-station-access
- 004-device-integration
- 005-batch-recipe

### Enables
- 008-weighing-station

## Test Expectations

- Exhaustive xUnit domain tests: 93.4 / 93.5 / 94.0 / 94.5 / 94.6 g (STEP 22); 0.980 / 0.995 / 1.000 / 1.008 / 1.020 kg (PDF §7); 2.999 / 3.000 / 3.001 kg (FR-8)
- Tolerance-source tests: recipe, config, none → blocked
- `STEP-22-tolerance-tests.txt` evidence

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEPs 21–22.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
