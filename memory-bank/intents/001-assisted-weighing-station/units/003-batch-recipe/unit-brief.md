---
unit: 003-batch-recipe
intent: 001-assisted-weighing-station
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-11-04T15:00:00Z
updated: 2026-11-04T15:00:00Z
---

# Unit Brief: Batch & Recipe

## Purpose

Present the OrgaTEX batch queue, let the operator select a batch (touch or scanned batch ID), and load an immutable **recipe snapshot**. Also own the configured tolerance per chemical + scale (the TOL fallback). OrgaTEX owns this data; WeighingMS only reads it.

## Scope

### In Scope
- Batch queue (FR-1) and selection (FR-2)
- Recipe load and snapshot: ordered components, target, unit, optional recipe tolerance (FR-3)
- Tolerance configuration per chemical + scale (read model for TOL)
- `IOrgatexBatchReader` adapter (typed HttpClient + resilience)
- OrgaTEX unavailable on load: error + Retry; never invented data (FR-24 criterion a)

### Out of Scope
- Weighing, classification, completion (005)
- Consumption write-back (006)
- Editing batches or recipes (OrgaTEX owns them)

---

## Assigned Requirements

| FR | Requirement | Priority |
|---|---|---|
| FR-1 | Batch queue | Must |
| FR-2 | Batch selection | Must |
| FR-3 | Recipe loading | Must |
| FR-24 (a) | OrgaTEX unavailable while loading the queue or a recipe | Must |

Full acceptance criteria: `../../requirements.md`. Unit decomposition: `../../units.md`.

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|---|---|---|
| BatchSummary | Queue row | batchId, orderId, machine, fabric, shade, componentCount, status |
| RecipeSnapshot | Immutable copy of the recipe at load | batchId, components[], loadedAtUtc |
| RecipeComponent | One recipe line | sequence, chemicalId, code, name, target, unit, recipeTolerance? |
| ToleranceSetting | Configured tolerance | chemicalId, scaleClass, plusMinus |

### Key Operations
| Operation | Description | Inputs | Outputs |
|---|---|---|---|
| List queue | Get the OrgaTEX queue | — | BatchSummary[] |
| Select batch | Load and snapshot a batch's recipe | batchId (touch or scan) | RecipeSnapshot or error |
| Resolve configured tolerance | Look up the fallback | chemicalId, scaleClass | ToleranceSetting or none |

---

## Story Summary

*To be filled at STEP 11 (stories).*

---

## Dependencies

### Depends On
| Unit | Reason |
|---|---|
| 001-station-simulators | Dummy OrgaTEX contract |

### Depended By
| Unit | Reason |
|---|---|
| 005-weighing-station | Recipe snapshot, tolerance config |
| 008-assisted-weighing-station-ui | Queue and recipe views |

### External Dependencies
| System | Purpose | Risk |
|---|---|---|
| OrgaTEX (dummy in lab) | Batch and recipe source | High: real contract unconfirmed |

---

## Technical Context

### Suggested Technology
Application query handlers, typed HttpClient adapter with `Microsoft.Extensions.Http.Resilience`, EF Core snapshot tables, `IOptionsMonitor` for tolerance config.

### Integration Points
| Integration | Type | Protocol |
|---|---|---|
| Dummy OrgaTEX | API | REST/JSON |
| 005 weighing-station | In-process | Application service |

### Data Storage
| Data | Type | Volume | Retention |
|---|---|---|---|
| Recipe snapshots | SQL (PostgreSQL) | Per batch | Kept with the batch records |

---

## Constraints

- No batch data hard-coded in the UI or Api
- The snapshot never changes after load
- Real OrgaTEX contract not invented (training contract only)

---

## Success Criteria

### Functional
- [ ] The queue shows B-24187, B-24188, B-24190, B-24191 from the dummy
- [ ] Selecting B-24187 loads 5 components in recipe order
- [ ] With OrgaTEX faulted, the queue shows an error with Retry and no data

### Quality
- [ ] All acceptance criteria of the assigned FRs met
- [ ] Tests per tech-stack standards pass
- [ ] Code reviewed and approved

---

## Bolt Suggestions

Default bolt type: **Simple**. Final bolts are planned at STEP 12.

| Bolt | Type | Objective |
|---|---|---|
| bolt-batch-recipe-1 | Simple | OrgaTEX reader + queue + selection + snapshot |
