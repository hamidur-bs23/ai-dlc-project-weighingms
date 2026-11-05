---
id: 013-complete-batch-all-confirmed
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-18
created: '2026-11-04T16:00:00Z'
assigned_bolt: 009-weighing-station
implemented: true
---

# Story: 013-complete-batch-all-confirmed

**Complete the batch when all N are confirmed**

## User Story

**As a** operator
**I want** the batch to complete once every chemical is confirmed
**So that** the batch is closed, reported and ready for write-back and ticket

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-18 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §1 view 3 (L567–586)

## Acceptance Criteria

- [ ] **Given** any pending chemical, **When** completion is requested, **Then** the server rejects it
- [ ] **Given** all N confirmed, **Then** the batch completes and `BatchCompleted` is raised in the same transaction
- [ ] **Given** completion, **Then** the summary shows n of N, all within tolerance (YES or NO with override count), total dispensed, elapsed, operator, OrgaTEX state, ticket state and per-chemical required / actual / final status

## UI/UX Impact

Confirm & print screen summary (L570–578) via 008-009.

## API Dependencies

- Api: completion + summary query

## Test Expectations

- Domain tests: completion guard; integration test: event + summary

## Dependencies

### Requires
- 005-weighing-station / 009-progress-to-next-component

### Enables
- 005-weighing-station / 014-weighing-records-and-review
- 006-consumption-writeback / 001-enqueue-consumption-on-completion
- 007-ticket-printing / 001-compose-ticket
- 008-assisted-weighing-station-ui / 009-confirm-and-print-screen

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Posting to OrgaTEX (006), printing (007)
- Implementation tasks (planned in bolts, STEP 12+)
