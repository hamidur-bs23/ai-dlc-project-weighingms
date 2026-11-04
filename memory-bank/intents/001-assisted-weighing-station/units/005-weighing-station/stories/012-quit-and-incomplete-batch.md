---
id: 012-quit-and-incomplete-batch
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-14]
created: 2026-11-04T16:00:00Z
assigned_bolt: 008-weighing-station
implemented: false
---

# Story: 012-quit-and-incomplete-batch

**Quit, leaving the batch incomplete**

## User Story

**As a** operator
**I want** to abandon the current chemical or leave the batch unfinished
**So that** I can stop safely without corrupting the batch

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-14 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §10 R12 (L559, L812)

## Acceptance Criteria

- [ ] **Given** an attempt, **When** I Quit (F4), **Then** it is discarded, no record is created and the chemical returns to pending
- [ ] **Given** I leave the batch, **Then** every confirmed record is kept, the batch stays "weighing" and resumes later
- [ ] **Given** quit or leave, **Then** an audit entry is written
- [ ] **Given** an incomplete batch, **Then** it cannot be completed

## UI/UX Impact

Quit key F4 (L559).

## API Dependencies

- Api: quit / leave commands

## Test Expectations

- Domain tests: quit creates no record; incomplete cannot complete

## Dependencies

### Requires
- 005-weighing-station / 002-select-pending-component
- 002-station-access / 003-immutable-audit-trail

### Enables
- 008-assisted-weighing-station-ui / 006-weigh-actions-and-keys

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Cancelling a batch in OrgaTEX
- Implementation tasks (planned in bolts, STEP 12+)
