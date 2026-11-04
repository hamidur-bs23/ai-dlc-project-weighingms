---
unit: 005-weighing-station
intent: 001-assisted-weighing-station
phase: inception
status: stories-defined
unit_type: backend
default_bolt_type: ddd-construction-bolt
created: 2026-11-04T15:00:00Z
updated: 2026-11-04T15:00:00Z
---

# Unit Brief: Weighing Station

## Purpose

The **guided-weighing domain**: everything that decides whether a weighing is valid and what is recorded. It is more than a screen: every rule is enforced on the server, and the UI only renders the state and classification this unit computes.

## Scope

### In Scope
- Component selection: free choice, sequence suggested (PD-07; FR-4, FR-17)
- Lot and container capture (FR-5, FR-6)
- Tare required before confirm; re-tare (FR-7)
- Automatic scale selection, configurable ≤ 3 kg inclusive threshold (FR-8)
- Server-side classification into 4 classes, inclusive bounds, fixed-point decimals (FR-9 to FR-12)
- Tolerance source: recipe, then config, otherwise blocked (TOL)
- Confirm guard: in tolerance, stable, tared (FR-13)
- Quit / incomplete batch (FR-14)
- Supervisor override: PIN + reason, true class kept, OVR flag (FR-15)
- Overshoot warn/block; further policy pending PD-05 (FR-16)
- Completion when all N components are confirmed; summary and review list (FR-18)
- Weighing record and traceability (FR-19); resume after interruption
- Raises `BatchCompleted`

### Out of Scope
- Device I/O (004)
- Posting to OrgaTEX (006)
- Ticket printing (007)
- Overshoot compensation policy (PD-05, decided at STEP 24)

---

## Assigned Requirements

| FR | Requirement | Priority |
|---|---|---|
| FR-4 | Component identification and selection | Must |
| FR-5 | Dyestuff lot | Must |
| FR-6 | Container | Must |
| FR-7 | Tare and re-tare | Must |
| FR-8 | Scale selection | Must |
| FR-9 | Live weight | Must |
| FR-10 | Target quantity | Must |
| FR-11 | Remaining quantity | Must |
| FR-12 | Tolerance | Must |
| FR-13 | Confirmation | Must |
| FR-14 | Quit and incomplete batch | Must |
| FR-15 | Supervisor override | Must |
| FR-16 | Overshoot | Must |
| FR-17 | Component progression | Must |
| FR-18 | Batch completion and summary | Must |
| FR-19 | Weighing record and traceability | Must |

Full acceptance criteria: `../../requirements.md`. Unit decomposition: `../../units.md`.

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|---|---|---|
| WeighingSession | Aggregate: one batch being weighed | batchId, operator, status, components[] |
| ComponentWeighing | One recipe line in the session | sequence, chemical, target, scale, state, lot, container, tare |
| Quantity | Value object: fixed-point amount + unit | value, unit |
| ToleranceBand | Value object: inclusive band and classification | target, plusMinus, source |
| ScaleSelection | Value object: chosen scale by threshold | threshold, scaleClass |
| WeighingRecord | Immutable confirmed result | batch, chemical, required, actual, class, scale, lot, container, tare, operator, overridden, reason, supervisor, confirmedAtUtc |
| Override | Authorised exception | supervisor, reason, trueClass |

### Key Operations
| Operation | Description | Inputs | Outputs |
|---|---|---|---|
| Start / resume session | Open the batch for weighing | batchId, operator | WeighingSession |
| Select component | Make a pending component active | componentId | state |
| Classify reading | Underweight / Within / Target / Overweight | Reading | class + remaining |
| Confirm | Record a valid weighing | componentId, reading | WeighingRecord |
| Override | Record an authorised out-of-tolerance weighing | grant, reason | WeighingRecord [OVR] |
| Quit | Leave the batch incomplete | — | session status |
| Complete | Finish when all N are confirmed | — | BatchCompleted |

---

## Story Summary

- **Total Stories**: 14
- **Must Have**: 14
- **Should Have**: 0
- **Could Have**: 0

### Stories

- [ ] **001-start-or-resume-weighing-session**: Start or resume a weighing session for a batch - Must - Planned
- [ ] **002-select-pending-component**: Select any pending component, sequence suggested - Must - Planned
- [ ] **003-automatic-scale-selection**: Automatic scale selection by the ≤ 3 kg threshold - Must - Planned
- [ ] **004-capture-lot-and-container**: Capture lot and container for the component - Must - Planned
- [ ] **005-tare-required-and-re-tare**: Tare required before weighing; re-tare - Must - Planned
- [ ] **006-classify-reading-four-classes**: Classify each reading into 4 classes, with remaining - Must - Planned
- [ ] **007-resolve-tolerance-source**: Resolve the tolerance source (recipe → config → blocked) - Must - Planned
- [ ] **008-confirm-valid-weighing**: Confirm a valid weighing - Must - Planned
- [ ] **009-progress-to-next-component**: Progress to the next pending component - Must - Planned
- [ ] **010-overshoot-warn-and-block**: Overshoot: warn and block (no compensation) - Must - Planned
- [ ] **011-supervisor-override**: Supervisor override of an out-of-tolerance weighing - Must - Planned
- [ ] **012-quit-and-incomplete-batch**: Quit, leaving the batch incomplete - Must - Planned
- [ ] **013-complete-batch-all-confirmed**: Complete the batch when all N are confirmed - Must - Planned
- [ ] **014-weighing-records-and-review**: Weighing records with traceability, and completed-batch review - Must - Planned

---

## Dependencies

### Depends On
| Unit | Reason |
|---|---|
| 002-station-access | Operator identity, step-up, audit, clock |
| 003-batch-recipe | Recipe snapshot, tolerance config |
| 004-device-integration | Scale port |

### Depended By
| Unit | Reason |
|---|---|
| 006-consumption-writeback | BatchCompleted |
| 007-ticket-printing | BatchCompleted, records |
| 008-assisted-weighing-station-ui | Weighing API |

### External Dependencies
None (training substitutes only).

---

## Technical Context

### Suggested Technology
Pure Domain project (no I/O), ErrorOr results, Application commands/queries, EF Core with optimistic concurrency; exhaustive xUnit boundary tests (PDF §7, STEP 22: 93.4 / 93.5 / 94.0 / 94.5 / 94.6; 2.999 / 3.000 / 3.001 kg).

### Integration Points
| Integration | Type | Protocol |
|---|---|---|
| 004 device-integration | In-process | Port |
| 006 / 007 | Domain event | BatchCompleted |
| 008 UI | API | REST/JSON |

### Data Storage
| Data | Type | Volume | Retention |
|---|---|---|---|
| Sessions, records, overrides | SQL (PostgreSQL) | Per batch | Records immutable, kept indefinitely |

---

## Constraints

- A failed weighing is never recorded as successful without an authorised exception (PDF §6 Step 7)
- No universal tolerance
- PD-05 must not be implemented before STEP 24

---

## Success Criteria

### Functional
- [ ] All PDF §7 and STEP 22 boundary values classify correctly
- [ ] Confirm is impossible unless in tolerance, stable and tared
- [ ] An override records PIN-verified supervisor, reason, true class and OVR
- [ ] B-24187 completes only after all 5 components are confirmed

### Quality
- [ ] All acceptance criteria of the assigned FRs met
- [ ] Tests per tech-stack standards pass
- [ ] Code reviewed and approved

---

## Bolt Suggestions

Planned bolts (STEP 12) in `memory-bank/bolts/`:

| Bolt | Type | Stories | Workbook |
|---|---|---|---|
| 007-weighing-station | DDD | 001, 002, 003, 006, 007 | STEPs 21–22 |
| 008-weighing-station | DDD | 004, 005, 008, 009, 012 | STEP 23, STEP 25 |
| 009-weighing-station | DDD | 010, 011, 013, 014 | STEP 24 (decision), STEP 23, STEP 26 preparation |
