---
unit: 006-consumption-writeback
intent: 001-assisted-weighing-station
phase: inception
status: stories-defined
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-11-04T15:00:00Z
updated: 2026-11-04T15:00:00Z
---

# Unit Brief: Consumption Write-back

## Purpose

Deliver the actual consumption of every completed batch to OrgaTEX **exactly once from the operator's point of view**, without ever blocking the station (PD-01).

## Scope

### In Scope
- Outbox entry written in the completion transaction (handles `BatchCompleted`)
- Background dispatcher: `POST /api/batches/:batchId/consumption` with `Idempotency-Key`
- Bounded automatic retry with back-off, then "sync failed" and manual retry (FR-20)
- Sync status and unsynced count for the UI
- OrgaTEX unavailable at completion (FR-24 criterion c)
- `IOrgatexGateway` consumption operation (shares the typed OrgaTEX HttpClient)

### Out of Scope
- Deciding completion (005)
- Reading batches (003)

---

## Assigned Requirements

| FR | Requirement | Priority |
|---|---|---|
| FR-20 | OrgaTEX write-back | Must |
| FR-24 (c) | OrgaTEX unavailable at completion | Must |

Full acceptance criteria: `../../requirements.md`. Unit decomposition: `../../units.md`.

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|---|---|---|
| OutboxEntry | Pending write-back | batchId, payload, idempotencyKey, attempts, state, lastError, nextAttemptAt |

### Key Operations
| Operation | Description | Inputs | Outputs |
|---|---|---|---|
| Enqueue | Create the outbox entry with completion | BatchCompleted | OutboxEntry |
| Dispatch | Send pending entries | — | sent / retry / failed |
| Manual retry | Re-queue a failed entry | entryId | state |

---

## Story Summary

- **Total Stories**: 4
- **Must Have**: 4
- **Should Have**: 0
- **Could Have**: 0

### Stories

- [ ] **001-enqueue-consumption-on-completion**: Enqueue consumption atomically with completion - Must - Planned
- [ ] **002-dispatch-with-idempotency-and-retry**: Dispatch with idempotency and bounded retry - Must - Planned
- [ ] **003-sync-failed-and-manual-retry**: Sync failed and manual retry - Must - Planned
- [ ] **004-write-back-status-visible**: Write-back status and unsynced count visible - Must - Planned

---

## Dependencies

### Depends On
| Unit | Reason |
|---|---|
| 005-weighing-station | BatchCompleted and record data |
| 001-station-simulators | Dummy consumption endpoint |

### Depended By
| Unit | Reason |
|---|---|
| 008-assisted-weighing-station-ui | Sync status |

### External Dependencies
| System | Purpose | Risk |
|---|---|---|
| OrgaTEX (dummy in lab) | Consumption receiver | High: real write-back contract unconfirmed (Q11) |

---

## Technical Context

### Suggested Technology
Hosted background service, EF Core outbox table, typed HttpClient with resilience; retry policy in config.

### Integration Points
| Integration | Type | Protocol |
|---|---|---|
| Dummy OrgaTEX | API | REST/JSON |
| 005 | Domain event | BatchCompleted |

### Data Storage
| Data | Type | Volume | Retention |
|---|---|---|---|
| Outbox | SQL (PostgreSQL) | One per batch | Kept after send for audit |

---

## Constraints

- Completion never waits for OrgaTEX
- Never posts twice (idempotency key)
- Sync failures are audited (002)

---

## Success Criteria

### Functional
- [ ] With OrgaTEX online, consumption arrives once (`GET /_sim/orgatex/consumption`)
- [ ] With OrgaTEX faulted, the batch completes and shows write-back pending, then sync failed
- [ ] Manual retry after recovery posts exactly once

### Quality
- [ ] All acceptance criteria of the assigned FRs met
- [ ] Tests per tech-stack standards pass
- [ ] Code reviewed and approved

---

## Bolt Suggestions

Planned bolts (STEP 12) in `memory-bank/bolts/`:

| Bolt | Type | Stories | Workbook |
|---|---|---|---|
| 011-consumption-writeback | Simple | 001, 002, 003, 004 | STEP 26 |
