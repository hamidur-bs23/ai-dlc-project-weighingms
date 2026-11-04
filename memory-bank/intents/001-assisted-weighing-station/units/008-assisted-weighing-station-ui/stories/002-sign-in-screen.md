---
id: 002-sign-in-screen
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-19, NFR-Security]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 002-sign-in-screen

**Sign-in screen**

## User Story

**As a** operator
**I want** a sign-in screen at the start of my shift
**So that** the station knows who I am

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-19, NFR-Security (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis gaps (no sign-in in the mockup)

## Acceptance Criteria

- [ ] **Given** no session, **Then** the sign-in screen is shown before any other
- [ ] **Given** a valid badge, **Then** the queue opens; **given** an unknown badge, **Then** an error is shown

## UI/UX Impact

New screen in the mockup's style (ux-guide).

## API Dependencies

- Api sign-in (002-001)

## Test Expectations

- Vitest component tests; Playwright step in 008-013

## Dependencies

### Requires
- 002-station-access / 001-operator-badge-sign-in
- 008-assisted-weighing-station-ui / 001-app-shell-top-bar-status-strip

### Enables
- None

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Badge scan
- Implementation tasks (planned in bolts, STEP 12+)
