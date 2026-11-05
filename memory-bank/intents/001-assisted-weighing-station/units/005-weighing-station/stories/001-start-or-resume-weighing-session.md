---
id: 001-start-or-resume-weighing-session
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-4
  - FR-17
  - NFR-Reliability
created: '2026-11-04T16:00:00Z'
assigned_bolt: 007-weighing-station
implemented: true
---

# Story: 001-start-or-resume-weighing-session

**Start or resume a weighing session for a batch**

## User Story

**As a** operator
**I want** to start weighing a batch, or pick up where it was left
**So that** an interruption never loses confirmed work

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-4, FR-17, NFR-Reliability (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §7 screen flow

## Acceptance Criteria

- [ ] **Given** a loaded recipe snapshot and a signed-in operator, **When** the batch opens, **Then** a session starts with every component pending and the first pending one suggested
- [ ] **Given** a batch left incomplete, **When** reopened, **Then** confirmed components stay done and weighing resumes at those not yet done
- [ ] **Given** an API restart, **When** the batch is reopened, **Then** progress is restored exactly

## UI/UX Impact

Weigh view entry (L490) via 008-004.

## API Dependencies

- Api: open/resume session (route fixed in the bolt design)

## Test Expectations

- Domain tests (xUnit) for the session aggregate; integration test for restart durability

## Dependencies

### Requires
- 003-batch-recipe / 003-load-recipe-snapshot
- 002-station-access / 001-operator-badge-sign-in

### Enables
- 005-weighing-station / 002-select-pending-component
- 005-weighing-station / 003-automatic-scale-selection

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Two stations open the same batch | Second is rejected (optimistic concurrency) |

## Out of Scope

- Batch selection (003-002)
- Implementation tasks (planned in bolts, STEP 12+)
