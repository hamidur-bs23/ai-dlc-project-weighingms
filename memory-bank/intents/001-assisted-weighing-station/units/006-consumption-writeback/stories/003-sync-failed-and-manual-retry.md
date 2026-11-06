---
id: 003-sync-failed-and-manual-retry
unit: 006-consumption-writeback
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-20
  - FR-24
created: '2026-11-04T16:00:00Z'
assigned_bolt: 011-consumption-writeback
implemented: true
---

# Story: 003-sync-failed-and-manual-retry

**Sync failed and manual retry**

## User Story

**As a** operator
**I want** a visible "sync failed" state and a way to retry
**So that** a long outage is noticed and fixed, not silently lost

## Unit & Requirements

- **Unit**: `006-consumption-writeback` (`../unit-brief.md`)
- **Requirements**: FR-20, FR-24 (`../../../requirements.md`)
- **Mockup / source reference**: system-context failure boundaries

## Acceptance Criteria

- [ ] **Given** retries reach the configured limit, **Then** the entry becomes "sync failed" and an audit entry is written
- [ ] **Given** "sync failed", **When** I press Retry after recovery, **Then** it is posted exactly once

## UI/UX Impact

Sync failed state and Retry on the summary (008-009, 008-011).

## API Dependencies

- Api: manual retry command

## Test Expectations

- Integration test: fault until limit → failed → clear fault → retry → one post

## Dependencies

### Requires
- 006-consumption-writeback / 002-dispatch-with-idempotency-and-retry
- 002-station-access / 003-immutable-audit-trail

### Enables
- None

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Editing the payload
- Implementation tasks (planned in bolts, STEP 12+)
