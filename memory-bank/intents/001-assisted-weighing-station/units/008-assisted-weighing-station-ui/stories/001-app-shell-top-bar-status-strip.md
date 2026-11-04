---
id: 001-app-shell-top-bar-status-strip
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-22, NFR-Usability]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 001-app-shell-top-bar-status-strip

**App shell: top bar, status strip, clock, Transloco, function keys**

## User Story

**As a** operator
**I want** a consistent frame around every screen
**So that** I always see where I am, who is signed in and what is working

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-22, NFR-Usability (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §1 top bar and strip (L440–445, L618–627)

## Acceptance Criteria

- [ ] **Given** any screen, **Then** the top bar shows WeighingMS, the station, the active batch / order / machine and the operator chip
- [ ] **Given** any screen, **Then** the status strip shows both scales, scanner, printer, OrgaTEX (with unsynced count), language and the plant-time clock
- [ ] **Given** F1–F4, **Then** each key triggers the action labelled on the current screen
- [ ] **Given** any operator text, **Then** it comes from Transloco (EN); no state is shown by colour alone

## UI/UX Impact

Top bar (L440–445), status strip (L618–627).

## API Dependencies

- Api: session, health (004-006), write-back status (006-004), plant time (002-004)

## Test Expectations

- Vitest + Testing Library: shell rendering, key routing; no hard-coded scenario data

## Dependencies

### Requires
- 002-station-access / 001-operator-badge-sign-in
- 004-device-integration / 006-device-health-for-status-strip

### Enables
- 008-assisted-weighing-station-ui / 002-sign-in-screen
- 008-assisted-weighing-station-ui / 003-select-batch-screen
- 008-assisted-weighing-station-ui / 004-weigh-screen-rail-and-header
- 008-assisted-weighing-station-ui / 009-confirm-and-print-screen

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Languages other than EN
- Implementation tasks (planned in bolts, STEP 12+)
