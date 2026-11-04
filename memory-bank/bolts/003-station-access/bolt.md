---
id: 003-station-access
unit: 002-station-access
intent: 001-assisted-weighing-station
type: simple-construction-bolt
status: in-progress
stories:
  - 001-operator-badge-sign-in
  - 002-supervisor-pin-step-up
  - 003-immutable-audit-trail
  - 004-plant-time-zone
created: 2026-11-04T17:00:00Z
started: 2026-11-04T20:00:00Z
completed: null
current_stage: implement
stages_completed:
  - name: plan
    completed: 2026-11-04T20:20:00Z
    artifact: implementation-plan.md

requires_bolts: [001-station-simulators]
enables_bolts: [006-assisted-weighing-station-ui, 007-weighing-station, 009-weighing-station, 012-ticket-printing]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 003-station-access

## Overview

Simple construction bolt for unit `002-station-access`, covering workbook STEP 25 preparation.

## Objective (expected outcome)

Badge sign-in with a cookie session (roles Operator and Supervisor), supervisor badge + PIN step-up per action (hashed, rate-limited, single-use grant), the append-only audit trail and the plant time zone (`Asia/Dhaka` default).

## Stories Included

- **001-operator-badge-sign-in**: Operator badge sign-in and session (Must)
- **002-supervisor-pin-step-up**: Supervisor badge + PIN step-up for one action (Must)
- **003-immutable-audit-trail**: Immutable audit trail (Should)
- **004-plant-time-zone**: Plant time zone for display and print (Must)

Story files: `memory-bank/intents/001-assisted-weighing-station/units/002-station-access/stories/`

## Bolt Type

**Type**: Simple Construction Bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- ✅ **1. plan**: Complete → implementation-plan.md
- ⏳ **2. implement**: In Progress → src/
- [ ] **3. test**: Pending → test-report.md

Every stage stops at a human validation checkpoint (workbook STEP 15, CHECKPOINT 5).

## Dependencies

### Requires
- 001-station-simulators

### Enables
- 006-assisted-weighing-station-ui
- 007-weighing-station
- 009-weighing-station
- 012-ticket-printing

## Test Expectations

- Integration tests with Testcontainers PostgreSQL: 401 without a session, unknown badge, wrong PIN, lockout, single-use grant
- Audit entries cannot be updated or deleted
- Time-zone test: 2026-11-03T21:48:22Z → 2026-11-04 03:48, independent of the server OS zone

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEP 25 preparation.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
