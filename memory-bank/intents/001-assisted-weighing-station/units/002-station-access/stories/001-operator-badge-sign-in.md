---
id: 001-operator-badge-sign-in
unit: 002-station-access
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-19, NFR-Security]
created: 2026-11-04T16:00:00Z
assigned_bolt: 003-station-access
implemented: false
---

# Story: 001-operator-badge-sign-in

**Operator badge sign-in and session**

## User Story

**As a** operator
**I want** to sign in with my badge number at the start of my session
**So that** every weighing I confirm carries my identity

## Unit & Requirements

- **Unit**: `002-station-access` (`../unit-brief.md`)
- **Requirements**: FR-19, NFR-Security (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §2 operator chip (L444, pin Q37)

## Acceptance Criteria

- [ ] **Given** a seeded operator badge, **When** I sign in, **Then** a session starts and the top bar shows my name and shift
- [ ] **Given** an unknown badge, **When** I sign in, **Then** I see "badge not recognised" and no session starts
- [ ] **Given** a session, **When** I sign out, **Then** the station returns to sign-in and in-progress weighing is not lost (resumable)
- [ ] **Given** no session, **When** any weighing API is called, **Then** it is rejected (401)

## UI/UX Impact

Sign-in screen (008-002) and the operator chip in the top bar (L444).

## API Dependencies

- Api: sign-in / sign-out / current session (cookie auth + XSRF; route fixed in the bolt design)

## Test Expectations

- Integration tests (Testcontainers Postgres): sign-in, unknown badge, 401 without session
- Rate limiting on repeated failures

## Dependencies

### Requires
- None

### Enables
- 002-station-access / 002-supervisor-pin-step-up
- 002-station-access / 003-immutable-audit-trail
- 005-weighing-station / 001-start-or-resume-weighing-session
- 008-assisted-weighing-station-ui / 001-app-shell-top-bar-status-strip
- 008-assisted-weighing-station-ui / 002-sign-in-screen

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Badge typed with spaces | Trimmed before lookup |
| Second sign-in on the same station | Replaces the previous session; audited |

## Out of Scope

- Badge scanning (PD-03: not in V1)
- Real identity provider
- Implementation tasks (planned in bolts, STEP 12+)
