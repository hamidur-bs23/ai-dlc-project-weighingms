---
id: 004-write-back-status-visible
unit: 006-consumption-writeback
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-20, FR-24]
created: 2026-11-04T16:00:00Z
assigned_bolt: 011-consumption-writeback
implemented: false
---

# Story: 004-write-back-status-visible

**Write-back status and unsynced count visible**

## User Story

**As a** operator
**I want** to see whether each batch has reached OrgaTEX
**So that** I trust the hand-off to OrgaTEX

## Unit & Requirements

- **Unit**: `006-consumption-writeback` (`../unit-brief.md`)
- **Requirements**: FR-20, FR-24 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §2 (L577, L623)

## Acceptance Criteria

- [ ] **Given** a sent entry, **Then** the summary shows "Written to OrgaTEX hh:mm:ss" (plant time)
- [ ] **Given** pending entries, **Then** the summary shows "Pending sync" and the status strip shows OrgaTEX offline and the unsynced count
- [ ] **Given** the queue, **Then** completed rows show "sync pending" when applicable

## UI/UX Impact

"Written to OrgaTEX" (L577) and the ORGATEX strip item (L623).

## API Dependencies

- Api: write-back status query

## Test Expectations

- Integration tests for sent / pending / failed states

## Dependencies

### Requires
- 006-consumption-writeback / 002-dispatch-with-idempotency-and-retry
- 002-station-access / 004-plant-time-zone

### Enables
- 008-assisted-weighing-station-ui / 009-confirm-and-print-screen

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- OrgaTEX health beyond write-back (004-006)
- Implementation tasks (planned in bolts, STEP 12+)
