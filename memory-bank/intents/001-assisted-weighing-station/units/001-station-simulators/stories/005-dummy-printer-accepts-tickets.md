---
id: 005-dummy-printer-accepts-tickets
unit: 001-station-simulators
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-28, FR-21]
created: 2026-11-04T16:00:00Z
assigned_bolt: 002-station-simulators
implemented: false
---

# Story: 005-dummy-printer-accepts-tickets

**Dummy printer accepts and exposes tickets**

## User Story

**As a** tester
**I want** the Dummy Printer to accept tickets and let me inspect them
**So that** printing and reprinting can be proven (STEP 27 evidence)

## Unit & Requirements

- **Unit**: `001-station-simulators` (`../unit-brief.md`)
- **Requirements**: FR-28, FR-21 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §9 (L584, L622)

## Acceptance Criteria

- [ ] **Given** a ticket, **When** `POST /api/tickets` is called, **Then** it is stored as printed and 201 is returned
- [ ] **Given** a repeated `Idempotency-Key`, **When** it arrives, **Then** nothing is printed twice
- [ ] **Given** printed tickets, **When** `GET /_sim/printer/tickets` is called, **Then** each ticket is listed with its content
- [ ] **Given** the printer is faulted, **When** a ticket is posted, **Then** it fails with a defined error code

## UI/UX Impact

None directly (backend). Makes the mockup data real for the ticket (L590–610).

## API Dependencies

- Serves `POST /api/tickets` (+ `Idempotency-Key`), `GET /_sim/printer/tickets`

## Test Expectations

- Contract tests: accept, idempotent replay, inspection, fault error code

## Dependencies

### Requires
- None

### Enables
- 001-station-simulators / 006-fault-injection-and-seed-reset
- 007-ticket-printing / 002-print-ticket

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Ticket without batch ID | 400 ProblemDetails |

## Out of Scope

- Ticket composition (007)
- Implementation tasks (planned in bolts, STEP 12+)
