---
id: 003-immutable-audit-trail
unit: 002-station-access
intent: 001-assisted-weighing-station
status: draft
priority: should
requirements: [NFR-Security, FR-14, FR-15]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 003-immutable-audit-trail

**Immutable audit trail**

## User Story

**As a** supervisor / auditor
**I want** an append-only record of overrides, quits, re-tares, reprints and sync failures
**So that** exceptions on the station can be reviewed later

## Unit & Requirements

- **Unit**: `002-station-access` (`../unit-brief.md`)
- **Requirements**: NFR-Security, FR-14, FR-15 (`../../../requirements.md`)
- **Mockup / source reference**: requirements NFR Security (Audit)

## Acceptance Criteria

- [ ] **Given** an override, quit, leave, re-tare, reprint or sync failure, **When** it happens, **Then** an audit entry is written with type, actor, subject, reason (if any) and UTC time
- [ ] **Given** an audit entry, **Then** no API can edit or delete it

## UI/UX Impact

None directly (backend).

## API Dependencies

- Api: internal audit service used by 005, 006, 007

## Test Expectations

- Integration tests: each event type writes one entry; no update/delete path exists

## Dependencies

### Requires
- 002-station-access / 001-operator-badge-sign-in

### Enables
- 005-weighing-station / 011-supervisor-override
- 005-weighing-station / 012-quit-and-incomplete-batch
- 006-consumption-writeback / 003-sync-failed-and-manual-retry
- 007-ticket-printing / 003-reprint-marked

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Audit write fails | The originating action fails too (same transaction) |

## Out of Scope

- An audit browsing screen (not in V1)
- Implementation tasks (planned in bolts, STEP 12+)
