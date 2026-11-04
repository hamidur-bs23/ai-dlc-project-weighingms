---
id: 004-printer-unavailable-print-pending
unit: 007-ticket-printing
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-24, FR-21]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 004-printer-unavailable-print-pending

**Printer unavailable: print pending**

## User Story

**As a** operator
**I want** the batch to complete even if the printer is down
**So that** a printer fault never blocks production

## Unit & Requirements

- **Unit**: `007-ticket-printing` (`../unit-brief.md`)
- **Requirements**: FR-24, FR-21 (`../../../requirements.md`)
- **Mockup / source reference**: system-context failure boundaries

## Acceptance Criteria

- [ ] **Given** the printer is faulted at completion, **Then** the batch still completes and the ticket is held as "print pending"
- [ ] **Given** the status strip, **Then** it shows PRINTER OFFLINE
- [ ] **Given** the printer recovers, **When** I press Print (F1), **Then** the ticket prints; there is no auto-print

## UI/UX Impact

Print-pending state and PRINTER strip item (L622) via 008-011.

## API Dependencies

- `IPrinter` error code from the dummy

## Test Expectations

- Integration test with `/_sim/faults` printer

## Dependencies

### Requires
- 007-ticket-printing / 002-print-ticket
- 001-station-simulators / 006-fault-injection-and-seed-reset

### Enables
- 008-assisted-weighing-station-ui / 011-error-and-offline-states

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Auto-print on recovery (not chosen)
- Implementation tasks (planned in bolts, STEP 12+)
