---
id: 012-completed-batch-review-list
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements: [FR-18, FR-19]
created: 2026-11-04T16:00:00Z
assigned_bolt: 013-assisted-weighing-station-ui
implemented: true
---

# Story: 012-completed-batch-review-list

**Completed-batch review list**

## User Story

**As a** supervisor
**I want** a list of completed batches I can reopen read-only
**So that** completed jobs stay reviewable

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-18, FR-19 (`../../../requirements.md`)
- **Mockup / source reference**: PDF §6 Step 9; ui-analysis §12 Q-UI-5

## Acceptance Criteria

- [ ] **Given** completed batches, **Then** a list shows them with time, operator and override count
- [ ] **Given** I open one, **Then** its summary and ticket preview are shown read-only

## UI/UX Impact

New list in the mockup style.

## API Dependencies

- Api completed-batch query (005-014)

## Test Expectations

- Vitest component tests

## Dependencies

### Requires
- 005-weighing-station / 014-weighing-records-and-review
- 008-assisted-weighing-station-ui / 009-confirm-and-print-screen

### Enables
- None

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Search and export
- Implementation tasks (planned in bolts, STEP 12+)
