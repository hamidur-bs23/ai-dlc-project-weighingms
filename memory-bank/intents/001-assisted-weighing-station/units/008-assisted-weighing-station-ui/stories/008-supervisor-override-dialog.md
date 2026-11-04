---
id: 008-supervisor-override-dialog
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-15]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 008-supervisor-override-dialog

**Supervisor override dialog**

## User Story

**As a** supervisor
**I want** a dialog to enter my badge, PIN and a reason
**So that** I can authorise an exception at the station

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-15 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §3 (L560, L824–826)

## Acceptance Criteria

- [ ] **Given** the dialog, **Then** badge, PIN (masked) and a mandatory reason are required
- [ ] **Given** a wrong PIN or empty reason, **Then** an error is shown and nothing is recorded
- [ ] **Given** success, **Then** the component shows as done with an OVR mark

## UI/UX Impact

New dialog in the mockup style; replaces "OVERRIDE REQUESTED — AWAITING SUPERVISOR" (L825).

## API Dependencies

- Api step-up (002-002) and override (005-011)

## Test Expectations

- Vitest component tests; Playwright override path

## Dependencies

### Requires
- 005-weighing-station / 011-supervisor-override
- 008-assisted-weighing-station-ui / 006-weigh-actions-and-keys

### Enables
- None

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Overshoot compensation (PD-05)
- Implementation tasks (planned in bolts, STEP 12+)
