---
id: 010-ticket-preview-and-print
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-21]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 010-ticket-preview-and-print

**Ticket preview and print / reprint**

## User Story

**As a** operator
**I want** to see the ticket and print it
**So that** I can check it before it goes with the container

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-21 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §2 ticket (L590–613)

## Acceptance Criteria

- [ ] **Given** a completed batch, **Then** the ticket preview shows the same content as the printed ticket, including the barcode
- [ ] **Given** Print (F1), **Then** it prints; later prints show REPRINT
- [ ] **Given** print pending, **Then** the preview shows that state

## UI/UX Impact

Ticket preview (L590–613) and Print ticket F1 (L584).

## API Dependencies

- Api ticket query and print (007-001…003)

## Test Expectations

- Vitest component tests

## Dependencies

### Requires
- 007-ticket-printing / 001-compose-ticket
- 007-ticket-printing / 002-print-ticket
- 008-assisted-weighing-station-ui / 009-confirm-and-print-screen

### Enables
- 008-assisted-weighing-station-ui / 013-end-to-end-batch-flow

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Editing the ticket
- Implementation tasks (planned in bolts, STEP 12+)
