---
id: 009-confirm-and-print-screen
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-18, FR-20]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 009-confirm-and-print-screen

**Confirm & print screen: summary, write-back status, Next batch F3**

## User Story

**As a** operator
**I want** the completion screen after the last component
**So that** I see the batch result and move on

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-18, FR-20 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §1 view 3 (L567–586)

## Acceptance Criteria

- [ ] **Given** completion, **Then** the summary rows (components weighed, all within tolerance, total dispensed, elapsed, operator, Written to OrgaTEX / Pending sync / Sync failed) and the per-chemical lines are shown
- [ ] **Given** Next batch (F3), **Then** the queue opens
- [ ] **Given** "Sync failed", **Then** a Retry action is shown

## UI/UX Impact

View 3 summary and keys (L570–586).

## API Dependencies

- Api summary (005-013), write-back status (006-004), retry (006-003)

## Test Expectations

- Vitest component tests

## Dependencies

### Requires
- 005-weighing-station / 013-complete-batch-all-confirmed
- 006-consumption-writeback / 004-write-back-status-visible
- 008-assisted-weighing-station-ui / 001-app-shell-top-bar-status-strip

### Enables
- 008-assisted-weighing-station-ui / 010-ticket-preview-and-print
- 008-assisted-weighing-station-ui / 012-completed-batch-review-list
- 008-assisted-weighing-station-ui / 013-end-to-end-batch-flow

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Ticket (008-010)
- Implementation tasks (planned in bolts, STEP 12+)
