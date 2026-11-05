---
id: 008-confirm-valid-weighing
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-13
  - FR-19
created: '2026-11-04T16:00:00Z'
assigned_bolt: 008-weighing-station
implemented: true
---

# Story: 008-confirm-valid-weighing

**Confirm a valid weighing**

## User Story

**As a** operator
**I want** to confirm a weighing that is within tolerance
**So that** the chemical is recorded as done and I can move on

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-13, FR-19 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §8 (L557, L779, L814)

## Acceptance Criteria

- [ ] **Given** class Within range or Target achieved, a stable reading, and lot, container and tare present, **Then** Confirm (F1) is enabled
- [ ] **Given** any of those missing, **Then** Confirm is disabled, and the server rejects a direct call
- [ ] **Given** a confirmation, **Then** exactly one immutable record is created and the rail shows "✓ actual"

## UI/UX Impact

OK · Confirm F1 (L557) and rail state (L709).

## API Dependencies

- Api: confirm command (server re-validates)

## Test Expectations

- Domain tests on the guard; integration test: one record per confirm, duplicate submit rejected

## Dependencies

### Requires
- 005-weighing-station / 004-capture-lot-and-container
- 005-weighing-station / 005-tare-required-and-re-tare
- 005-weighing-station / 006-classify-reading-four-classes

### Enables
- 005-weighing-station / 009-progress-to-next-component
- 005-weighing-station / 014-weighing-records-and-review
- 008-assisted-weighing-station-ui / 006-weigh-actions-and-keys

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Double-tap Confirm | Second request rejected; one record |

## Out of Scope

- Override (005-011)
- Implementation tasks (planned in bolts, STEP 12+)
