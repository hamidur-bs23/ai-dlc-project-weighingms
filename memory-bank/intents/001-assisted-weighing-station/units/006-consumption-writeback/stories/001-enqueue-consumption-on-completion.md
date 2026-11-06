---
id: 001-enqueue-consumption-on-completion
unit: 006-consumption-writeback
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-20
created: '2026-11-04T16:00:00Z'
assigned_bolt: 011-consumption-writeback
implemented: true
---

# Story: 001-enqueue-consumption-on-completion

**Enqueue consumption atomically with completion**

## User Story

**As a** production planner
**I want** the actual consumption of every completed batch queued for OrgaTEX
**So that** OrgaTEX always learns what was really used

## Unit & Requirements

- **Unit**: `006-consumption-writeback` (`../unit-brief.md`)
- **Requirements**: FR-20 (`../../../requirements.md`)
- **Mockup / source reference**: system-context O1

## Acceptance Criteria

- [ ] **Given** `BatchCompleted`, **Then** an outbox entry is written in the same transaction as the completion
- [ ] **Given** the entry, **Then** its payload holds the batch and, per chemical, actual quantity, unit and lot, plus an idempotency key
- [ ] **Given** OrgaTEX is down, **Then** completion still succeeds

## UI/UX Impact

None directly (backend).

## API Dependencies

- Consumes `BatchCompleted` (005-013)

## Test Expectations

- Integration test: completion and outbox commit or roll back together

## Dependencies

### Requires
- 005-weighing-station / 013-complete-batch-all-confirmed

### Enables
- 006-consumption-writeback / 002-dispatch-with-idempotency-and-retry

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Sending (006-002)
- Implementation tasks (planned in bolts, STEP 12+)
