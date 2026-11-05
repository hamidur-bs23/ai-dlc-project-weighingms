---
id: 014-weighing-records-and-review
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-19
  - FR-18
created: '2026-11-04T16:00:00Z'
assigned_bolt: 009-weighing-station
implemented: true
---

# Story: 014-weighing-records-and-review

**Weighing records with traceability, and completed-batch review**

## User Story

**As a** supervisor
**I want** immutable weighing records and a way to review completed batches
**So that** any batch can be audited later

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-19, FR-18 (`../../../requirements.md`)
- **Mockup / source reference**: PDF §10; ui-analysis §12 Q-UI-5

## Acceptance Criteria

- [ ] **Given** a confirmation, **Then** the record holds batch, chemical ID and name, required quantity and unit, actual, tolerance and source, class, scale, lot, container, tare, operator, UTC time and override details
- [ ] **Given** records, **Then** they survive an API restart and cannot be edited or deleted through the app
- [ ] **Given** a completed batch, **Then** its summary can be reopened read-only from a review list

## UI/UX Impact

Read-only summary (L570–578) and review list (008-012).

## API Dependencies

- Api: records and completed-batch queries

## Test Expectations

- Integration tests: all fields persisted, restart durability, no update/delete endpoint

## Dependencies

### Requires
- 005-weighing-station / 008-confirm-valid-weighing
- 005-weighing-station / 013-complete-batch-all-confirmed

### Enables
- 008-assisted-weighing-station-ui / 012-completed-batch-review-list

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Exporting records
- Implementation tasks (planned in bolts, STEP 12+)
