---
id: 001-compose-ticket
unit: 007-ticket-printing
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-21, FR-22]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 001-compose-ticket

**Compose the 80 mm ticket (STEP 27 fields, Code 128, OVR)**

## User Story

**As a** operator
**I want** a ticket generated for the completed batch
**So that** a physical record goes with the container

## Unit & Requirements

- **Unit**: `007-ticket-printing` (`../unit-brief.md`)
- **Requirements**: FR-21, FR-22 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §2 ticket preview (L590–610)

## Acceptance Criteria

- [ ] **Given** a completed batch, **Then** a ticket is composed top to bottom: batch, order, machine, shade, date/time (plant zone), operator; each component's target and actual with unit, overridden lines marked "OVR"; "ALL WITHIN TOLERANCE" or "OVERRIDES: n"; a Code 128 barcode of the batch ID with the ID beneath
- [ ] **Given** the ticket, **Then** the same content is available for the on-screen preview

## UI/UX Impact

Ticket preview (L590–610) via 008-010.

## API Dependencies

- Consumes `BatchCompleted`; Api ticket query

## Test Expectations

- Unit tests on composition (field order, OVR, totals line, barcode value)

## Dependencies

### Requires
- 005-weighing-station / 013-complete-batch-all-confirmed
- 002-station-access / 004-plant-time-zone

### Enables
- 007-ticket-printing / 002-print-ticket
- 008-assisted-weighing-station-ui / 010-ticket-preview-and-print

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Configurable layouts (deviation from IR-16)
- Implementation tasks (planned in bolts, STEP 12+)
