---
id: 011-supervisor-override
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-15]
created: 2026-11-04T16:00:00Z
assigned_bolt: 009-weighing-station
implemented: false
---

# Story: 011-supervisor-override

**Supervisor override of an out-of-tolerance weighing**

## User Story

**As a** supervisor
**I want** to authorise confirming an underweight or overweight weighing with a reason
**So that** a justified exception can proceed and stays visible

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-15 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §3 (L560, L824–826)

## Acceptance Criteria

- [ ] **Given** an Underweight or Overweight reading and a valid supervisor step-up and a non-empty reason, **When** override is confirmed, **Then** one record is created with its true class, flagged with supervisor, reason and time
- [ ] **Given** a wrong PIN or empty reason, **Then** no record is created and nothing changes
- [ ] **Given** no override, **Then** the operator alone cannot confirm an out-of-tolerance weighing
- [ ] **Given** an override record, **Then** it is shown as an override in the summary and as "OVR" on the ticket, and audited

## UI/UX Impact

Supervisor override key (L560) and dialog (008-008).

## API Dependencies

- Api: override command (requires the 002-002 grant)

## Test Expectations

- Domain + integration tests: success, wrong PIN, missing reason, true class kept

## Dependencies

### Requires
- 002-station-access / 002-supervisor-pin-step-up
- 005-weighing-station / 006-classify-reading-four-classes
- 002-station-access / 003-immutable-audit-trail

### Enables
- 008-assisted-weighing-station-ui / 008-supervisor-override-dialog

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Override requested while reading is in range | Not offered; normal confirm applies |

## Out of Scope

- Overshoot compensation (PD-05)
- Implementation tasks (planned in bolts, STEP 12+)
