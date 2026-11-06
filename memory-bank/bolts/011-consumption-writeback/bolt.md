---
id: 011-consumption-writeback
unit: 006-consumption-writeback
intent: 001-assisted-weighing-station
type: simple-construction-bolt
status: complete
stories:
  - 001-enqueue-consumption-on-completion
  - 002-dispatch-with-idempotency-and-retry
  - 003-sync-failed-and-manual-retry
  - 004-write-back-status-visible
created: '2026-11-04T17:00:00Z'
started: '2026-11-06T03:31:35Z'
completed: '2026-11-06T04:11:45Z'
current_stage: null
stages_completed:
  - name: plan
    completed: '2026-11-06T03:36:00Z'
    artifact: implementation-plan.md
  - name: implement
    completed: '2026-11-06T03:53:00Z'
    artifact: implementation-walkthrough.md
  - name: test
    completed: '2026-11-06T04:11:45Z'
    artifact: test-walkthrough.md
requires_bolts:
  - 002-station-simulators
  - 009-weighing-station
enables_bolts:
  - 013-assisted-weighing-station-ui
requires_units:
  - 001-station-simulators
  - 005-weighing-station
blocks: true
complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 011-consumption-writeback

## Overview

Simple construction bolt for unit `006-consumption-writeback`, covering workbook STEP 26.

## Objective (expected outcome)

The store-and-forward write-back: outbox entry in the completion transaction, a background dispatcher posting `POST /api/batches/:batchId/consumption` with an `Idempotency-Key`, bounded automatic retry, then sync failed and manual retry; write-back status and the unsynced count.

## Stories Included

- **001-enqueue-consumption-on-completion**: Enqueue consumption atomically with completion (Must)
- **002-dispatch-with-idempotency-and-retry**: Dispatch with idempotency and bounded retry (Must)
- **003-sync-failed-and-manual-retry**: Sync failed and manual retry (Must)
- **004-write-back-status-visible**: Write-back status and unsynced count visible (Must)

Story files: `memory-bank/intents/001-assisted-weighing-station/units/006-consumption-writeback/stories/`

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
- 009-weighing-station

### Enables
- 013-assisted-weighing-station-ui

## Test Expectations

- Integration tests with `/_sim/faults` orgatex: completion never blocked, retry, sync failed, manual retry
- `GET /_sim/orgatex/consumption` shows exactly one post per batch
- STEP 26 evidence (UI, request, 200 OK response, stored data)

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEP 26.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
