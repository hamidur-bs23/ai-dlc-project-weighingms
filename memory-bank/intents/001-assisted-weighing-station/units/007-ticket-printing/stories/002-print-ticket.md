---
id: 002-print-ticket
unit: 007-ticket-printing
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-21]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 002-print-ticket

**Print the ticket**

## User Story

**As a** operator
**I want** to print the ticket
**So that** I can attach it to the container

## Unit & Requirements

- **Unit**: `007-ticket-printing` (`../unit-brief.md`)
- **Requirements**: FR-21 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §3 Print ticket F1 (L584)

## Acceptance Criteria

- [ ] **Given** a composed ticket and a READY printer, **When** I press Print (F1), **Then** it is sent with an `Idempotency-Key` and marked printed
- [ ] **Given** the printer accepted it, **Then** `GET /_sim/printer/tickets` shows it once

## UI/UX Impact

Print ticket F1 (L584).

## API Dependencies

- `IPrinter` → dummy `POST /api/tickets`

## Test Expectations

- Integration test against the dummy printer

## Dependencies

### Requires
- 007-ticket-printing / 001-compose-ticket
- 001-station-simulators / 005-dummy-printer-accepts-tickets

### Enables
- 007-ticket-printing / 003-reprint-marked
- 007-ticket-printing / 004-printer-unavailable-print-pending
- 008-assisted-weighing-station-ui / 010-ticket-preview-and-print

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Double press | One print |

## Out of Scope

- Reprint (007-003)
- Implementation tasks (planned in bolts, STEP 12+)
