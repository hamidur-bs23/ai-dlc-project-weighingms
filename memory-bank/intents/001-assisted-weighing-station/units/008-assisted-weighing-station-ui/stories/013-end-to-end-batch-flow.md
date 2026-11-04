---
id: 013-end-to-end-batch-flow
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-1, FR-13, FR-18, FR-20, FR-21]
created: 2026-11-04T16:00:00Z
assigned_bolt: 014-assisted-weighing-station-ui
implemented: false
---

# Story: 013-end-to-end-batch-flow

**End-to-end batch B-24187 (Playwright)**

## User Story

**As a** product owner
**I want** an automated run of the whole B-24187 workflow against the dummies
**So that** the top acceptance criterion (the complete workflow works) is proven on every build

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-1, FR-13, FR-18, FR-20, FR-21 (`../../../requirements.md`)
- **Mockup / source reference**: workbook STEP 29

## Acceptance Criteria

- [ ] **Given** the full stack (`docker compose --profile full`), **When** the test signs in, selects B-24187 and weighs all 5 components using scale scenarios, **Then** the batch completes
- [ ] **Then** `GET /_sim/orgatex/consumption` shows one post and `GET /_sim/printer/tickets` shows the ticket
- [ ] **Given** the fault scenarios of STEP 30, **Then** each defined behaviour is verified

## UI/UX Impact

Covers views 1–3 end to end.

## API Dependencies

- All Api endpoints; `/_sim/*` for scenarios and inspection

## Test Expectations

- Playwright (TS) suite in CI

## Dependencies

### Requires
- 008-assisted-weighing-station-ui / 003-select-batch-screen
- 008-assisted-weighing-station-ui / 005-weigh-screen-readout-bar-status
- 008-assisted-weighing-station-ui / 006-weigh-actions-and-keys
- 008-assisted-weighing-station-ui / 009-confirm-and-print-screen
- 008-assisted-weighing-station-ui / 010-ticket-preview-and-print

### Enables
- None

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Load testing
- Implementation tasks (planned in bolts, STEP 12+)
