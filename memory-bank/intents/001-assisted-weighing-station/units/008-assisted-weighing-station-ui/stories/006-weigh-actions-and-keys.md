---
id: 006-weigh-actions-and-keys
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-7, FR-13, FR-14, FR-15]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 006-weigh-actions-and-keys

**Weigh actions and keys: Confirm F1, Re-tare F2, Quit F4, Supervisor override**

## User Story

**As a** operator
**I want** the action keys of the weigh screen
**So that** I can tare, confirm, quit or ask for an override by touch or key

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-7, FR-13, FR-14, FR-15 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §3 (L556–561)

## Acceptance Criteria

- [ ] **Given** the server says confirm is allowed, **Then** OK · Confirm (F1) is enabled; otherwise disabled
- [ ] **Given** Re-tare (F2) or Quit (F4), **Then** the matching command is sent and the screen updates
- [ ] **Given** an out-of-tolerance reading, **Then** Supervisor override is offered and opens the dialog (008-008)

## UI/UX Impact

Action keys (L556–561).

## API Dependencies

- Api confirm / tare / quit commands (005-005, 005-008, 005-012)

## Test Expectations

- Vitest: enabled/disabled states, key handlers

## Dependencies

### Requires
- 005-weighing-station / 005-tare-required-and-re-tare
- 005-weighing-station / 008-confirm-valid-weighing
- 005-weighing-station / 012-quit-and-incomplete-batch
- 008-assisted-weighing-station-ui / 005-weigh-screen-readout-bar-status

### Enables
- 008-assisted-weighing-station-ui / 008-supervisor-override-dialog
- 008-assisted-weighing-station-ui / 013-end-to-end-batch-flow

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Override dialog content (008-008)
- Implementation tasks (planned in bolts, STEP 12+)
