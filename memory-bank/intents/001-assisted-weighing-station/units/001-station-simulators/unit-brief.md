---
unit: 001-station-simulators
intent: 001-assisted-weighing-station
phase: inception
status: in-progress
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-11-04T15:00:00Z
updated: 2026-11-04T15:00:00Z
---

# Unit Brief: Station Simulators

## Purpose

Provide realistic, controllable **training substitutes** for OrgaTEX, the two scales, the scanner and the printer, in a separate process (`WeighingMS.Simulators.Host`, :5090). These are training contracts, not claims about the real systems.

## Scope

### In Scope
- Dummy OrgaTEX API: `GET /api/batches`, `GET /api/batches/:batchId`, `GET /api/batches/:batchId/recipe`, `POST /api/batches/:batchId/consumption`
- JSON seed: B-24187 / SO-9931 / JET-04 / Teal 4C (5 components) plus B-24188, B-24190, B-24191
- Dummy Scale API: Precision 3 kg + Heavy-Duty 32 kg; status, weight, tare; resolution per scale
- Scale scenarios via `POST /_sim/scales/{scaleId}/scenario`: rising, belowRange, enterRange, overshoot, stable
- Dummy Scanner: `POST /_sim/scanner/scan`, status
- Dummy Printer: `POST /api/tickets`
- `/_sim/reset`, `/_sim/faults` (unavailable / timeout / error500 / slow), `GET /_sim/orgatex/consumption`, `GET /_sim/printer/tickets`

### Out of Scope
- Any business rule (tolerance, classification, completion), which belongs to 005
- The WeighingMS Api adapters, which belong to 003, 004, 006 and 007
- Real OrgaTEX or device contracts (none are invented)

---

## Assigned Requirements

| FR | Requirement | Priority |
|---|---|---|
| FR-25 | OrgaTEX dummy | Must |
| FR-26 | Scale dummy | Must |
| FR-27 | Scanner dummy | Must |
| FR-28 | Printer dummy | Must |

Full acceptance criteria: `../../requirements.md`. Unit decomposition: `../../units.md`.

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|---|---|---|
| SeedBatch | Training batch with its recipe | batchId, orderId, machine, shade, fabric, components[] |
| SimScale | Simulated scale | scaleId, capacity, resolution, status, scenario, currentReading |
| Fault | Injected failure | target, mode, durationMs |
| ReceivedConsumption / PrintedTicket | What the dummies received (for evidence) | payload, idempotencyKey, receivedAt |

### Key Operations
| Operation | Description | Inputs | Outputs |
|---|---|---|---|
| Serve batches / recipe | Return seed data | batchId | Batch / recipe JSON |
| Produce reading | Next reading per scenario and resolution | scaleId | value, unit, stable, ts |
| Inject / clear fault | Make a dummy fail | target, mode | — |
| Emit scan | Queue a scan code | code | — |

---

## Story Summary

- **Total Stories**: 6
- **Must Have**: 6
- **Should Have**: 0
- **Could Have**: 0

### Stories

- [ ] **001-dummy-orgatex-serves-batches-and-recipes**: Dummy OrgaTEX serves the batch queue and recipes from seed - Must - Planned
- [ ] **002-dummy-orgatex-receives-consumption**: Dummy OrgaTEX receives and exposes consumption - Must - Planned
- [ ] **003-dummy-scales-with-scenarios**: Two dummy scales with weighing scenarios and resolution - Must - Planned
- [ ] **004-dummy-scanner-emits-scans**: Dummy scanner emits scans - Must - Planned
- [ ] **005-dummy-printer-accepts-tickets**: Dummy printer accepts and exposes tickets - Must - Planned
- [ ] **006-fault-injection-and-seed-reset**: Fault injection and seed reset - Must - Planned

---

## Dependencies

### Depends On
None.

### Depended By
| Unit | Reason |
|---|---|
| 003-batch-recipe | OrgaTEX inbound contract |
| 004-device-integration | Scale and scanner contracts |
| 006-consumption-writeback | Consumption endpoint |
| 007-ticket-printing | Printer endpoint |

### External Dependencies
None (training substitutes only).

---

## Technical Context

### Suggested Technology
ASP.NET Core (.NET 10) minimal host, JSON seed, OpenAPI; xUnit v3 + Shouldly contract tests (tech-stack, api-conventions).

### Integration Points
| Integration | Type | Protocol |
|---|---|---|
| WeighingMS.Api adapters | API | REST/JSON |

### Data Storage
| Data | Type | Volume | Retention |
|---|---|---|---|
| Seed + scenario state | In-memory (JSON seed) | Small | Reset on `/_sim/reset` |

---

## Constraints

- `/_sim/*` enabled in Development/Test only (api-conventions)
- Readings respect each scale's resolution (PDF §9)
- Separate process; never referenced by the Api at compile time (ADR-003)

---

## Success Criteria

### Functional
- [ ] All four STEP 16 dummies respond with the seed data
- [ ] Each scenario produces the expected reading sequence
- [ ] Each fault mode makes the target fail as configured
- [ ] Received consumption and printed tickets can be inspected

### Quality
- [ ] All acceptance criteria of the assigned FRs met
- [ ] Tests per tech-stack standards pass
- [ ] Code reviewed and approved

---

## Bolt Suggestions

Planned bolts (STEP 12) in `memory-bank/bolts/`:

| Bolt | Type | Stories | Workbook |
|---|---|---|---|
| 001-station-simulators | Simple | 001, 002, 003 | STEPs 16–18 |
| 002-station-simulators | Simple | 004, 005, 006 | STEP 16, STEP 30 preparation |
