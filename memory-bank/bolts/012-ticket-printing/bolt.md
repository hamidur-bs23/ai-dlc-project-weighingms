---
id: 012-ticket-printing
unit: 007-ticket-printing
intent: 001-assisted-weighing-station
type: simple-construction-bolt
status: complete
stories:
  - 001-compose-ticket
  - 002-print-ticket
  - 003-reprint-marked
  - 004-printer-unavailable-print-pending
created: '2026-11-04T17:00:00Z'
started: '2026-11-06T04:20:00Z'
completed: '2026-11-06T06:28:31Z'
current_stage: null
stages_completed:
  - name: plan
    completed: '2026-11-06T04:24:00Z'
    artifact: implementation-plan.md
  - name: implement
    completed: '2026-11-06T04:32:00Z'
    artifact: implementation-walkthrough.md
  - name: test
    completed: '2026-11-06T06:28:31Z'
    artifact: test-walkthrough.md
requires_bolts:
  - 002-station-simulators
  - 003-station-access
  - 009-weighing-station
enables_bolts:
  - 013-assisted-weighing-station-ui
requires_units:
  - 001-station-simulators
  - 002-station-access
  - 005-weighing-station
blocks: true
complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 012-ticket-printing

## Overview

Simple construction bolt for unit `007-ticket-printing`, covering workbook STEP 27.

## Objective (expected outcome)

The 80 mm weighing ticket (STEP 27 fields, plant time, OVR marks, "ALL WITHIN TOLERANCE" or "OVERRIDES: n", Code 128 barcode of the batch ID), the print queue, manual print, REPRINT marking and print pending when the printer is unavailable.

## Stories Included

- **001-compose-ticket**: Compose the 80 mm ticket (STEP 27 fields, Code 128, OVR) (Must)
- **002-print-ticket**: Print the ticket (Must)
- **003-reprint-marked**: Reprints marked REPRINT (Must)
- **004-printer-unavailable-print-pending**: Printer unavailable: print pending (Must)

Story files: `memory-bank/intents/001-assisted-weighing-station/units/007-ticket-printing/stories/`

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
- 002-station-simulators
- 003-station-access
- 009-weighing-station

### Enables
- 013-assisted-weighing-station-ui

## Test Expectations

- Unit tests on ticket composition (field order, OVR, totals line, barcode value)
- Integration tests with the dummy printer and the printer fault
- STEP 27 evidence (ticket, printer API, test)

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEP 27.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
