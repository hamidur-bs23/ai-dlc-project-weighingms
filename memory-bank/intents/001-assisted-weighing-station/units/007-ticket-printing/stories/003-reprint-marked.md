---
id: 003-reprint-marked
unit: 007-ticket-printing
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-21]
created: 2026-11-04T16:00:00Z
assigned_bolt: 012-ticket-printing
implemented: false
---

# Story: 003-reprint-marked

**Reprints marked REPRINT**

## User Story

**As a** supervisor
**I want** every print after the first marked REPRINT
**So that** duplicate tickets can't be mistaken for originals

## Unit & Requirements

- **Unit**: `007-ticket-printing` (`../unit-brief.md`)
- **Requirements**: FR-21 (`../../../requirements.md`)
- **Mockup / source reference**: decision PD-02

## Acceptance Criteria

- [ ] **Given** a ticket already printed, **When** printed again, **Then** the new print is marked REPRINT and audited

## UI/UX Impact

Print key on a reopened summary (008-010).

## API Dependencies

- `IPrinter`; audit (002-003)

## Test Expectations

- Integration test: first print plain, second REPRINT

## Dependencies

### Requires
- 007-ticket-printing / 002-print-ticket
- 002-station-access / 003-immutable-audit-trail

### Enables
- None

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Reprint authorisation beyond audit
- Implementation tasks (planned in bolts, STEP 12+)
