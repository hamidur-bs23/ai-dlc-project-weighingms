---
unit: 007-ticket-printing
intent: 001-assisted-weighing-station
phase: inception
status: stories-defined
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-11-04T15:00:00Z
updated: 2026-11-04T15:00:00Z
---

# Unit Brief: Ticket & Printing

## Purpose

Produce the weighing ticket for a completed batch and get it printed, without blocking completion (PD-02, PD-04).

## Scope

### In Scope
- Ticket composition, fixed V1 layout (80 mm): batch, order, machine, shade, date (plant zone), operator, components target/actual, OVR marks, Code 128 barcode of the batch ID (FR-21)
- On-screen preview data
- Print queue: print pending → printed; manual print (F1)
- REPRINT marking for every print after the first
- Printer unavailable (FR-24 criterion d)
- `IPrinter` adapter

### Out of Scope
- Configurable ticket templates (deviation from brief IR-16, noted)
- Completion itself (005)

---

## Assigned Requirements

| FR | Requirement | Priority |
|---|---|---|
| FR-21 | Ticket generation | Must |
| FR-24 (d) | Printer unavailable | Must |

Full acceptance criteria: `../../requirements.md`. Unit decomposition: `../../units.md`.

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|---|---|---|
| Ticket | Composed ticket | batchId, header fields, lines[], result, barcode, version |
| PrintJob | One print attempt | ticketId, isReprint, state, idempotencyKey, atUtc |

### Key Operations
| Operation | Description | Inputs | Outputs |
|---|---|---|---|
| Compose | Build the ticket from records | BatchCompleted | Ticket |
| Print | Send to the printer | ticketId | PrintJob |
| Reprint | Print again, marked REPRINT | ticketId, operator | PrintJob |

---

## Story Summary

- **Total Stories**: 4
- **Must Have**: 4
- **Should Have**: 0
- **Could Have**: 0

### Stories

- [ ] **001-compose-ticket**: Compose the 80 mm ticket (STEP 27 fields, Code 128, OVR) - Must - Planned
- [ ] **002-print-ticket**: Print the ticket - Must - Planned
- [ ] **003-reprint-marked**: Reprints marked REPRINT - Must - Planned
- [ ] **004-printer-unavailable-print-pending**: Printer unavailable: print pending - Must - Planned

---

## Dependencies

### Depends On
| Unit | Reason |
|---|---|
| 005-weighing-station | BatchCompleted, records |
| 002-station-access | Plant time zone, reprint audit |
| 001-station-simulators | Dummy printer |

### Depended By
| Unit | Reason |
|---|---|
| 008-assisted-weighing-station-ui | Ticket preview, print |

### External Dependencies
| System | Purpose | Risk |
|---|---|---|
| Ticket printer (dummy in lab) | Paper ticket | Medium: model and language unconfirmed (Q18) |

---

## Technical Context

### Suggested Technology
Application service + typed HttpClient adapter; `POST /api/tickets` with `Idempotency-Key`.

### Integration Points
| Integration | Type | Protocol |
|---|---|---|
| Dummy Printer | API | REST/JSON |

### Data Storage
| Data | Type | Volume | Retention |
|---|---|---|---|
| Tickets, print jobs | SQL (PostgreSQL) | Per batch | Kept with the batch |

---

## Constraints

- Completion is never rolled back by a print failure
- Accepted risk BR-15 (a container may leave before the ticket prints)

---

## Success Criteria

### Functional
- [ ] The B-24187 ticket shows all STEP 27 fields and the barcode
- [ ] With the printer faulted, the batch completes and the ticket is print pending
- [ ] A second print is marked REPRINT

### Quality
- [ ] All acceptance criteria of the assigned FRs met
- [ ] Tests per tech-stack standards pass
- [ ] Code reviewed and approved

---

## Bolt Suggestions

Planned bolts (STEP 12) in `memory-bank/bolts/`:

| Bolt | Type | Stories | Workbook |
|---|---|---|---|
| 012-ticket-printing | Simple | 001, 002, 003, 004 | STEP 27 |
