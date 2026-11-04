---
id: 002-supervisor-pin-step-up
unit: 002-station-access
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-15
  - NFR-Security
created: '2026-11-04T16:00:00Z'
assigned_bolt: 003-station-access
implemented: true
---

# Story: 002-supervisor-pin-step-up

**Supervisor badge + PIN step-up for one action**

## User Story

**As a** supervisor
**I want** to authorise one privileged action with my badge and PIN
**So that** an exception is only granted by someone allowed to grant it

## Unit & Requirements

- **Unit**: `002-station-access` (`../unit-brief.md`)
- **Requirements**: FR-15, NFR-Security (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §3 Supervisor override (L560, pin Q33)

## Acceptance Criteria

- [ ] **Given** a supervisor badge and correct PIN, **When** step-up is requested for an action, **Then** a one-time grant for that action is issued
- [ ] **Given** a wrong PIN or an operator badge, **When** step-up is requested, **Then** it is refused and nothing changes
- [ ] **Given** repeated wrong PINs, **When** the limit is reached, **Then** the supervisor is locked out for a period
- [ ] **Given** a grant, **When** it is used once, **Then** it cannot be reused

## UI/UX Impact

Override dialog (008-008).

## API Dependencies

- Api: step-up verification (route fixed in the bolt design)

## Test Expectations

- Integration tests: correct PIN, wrong PIN, operator badge, lockout, single use
- PINs stored hashed only

## Dependencies

### Requires
- 002-station-access / 001-operator-badge-sign-in

### Enables
- 005-weighing-station / 011-supervisor-override

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Supervisor is also the signed-in operator | Allowed; both identities recorded |

## Out of Scope

- Deciding when an override is allowed (005-011)
- Implementation tasks (planned in bolts, STEP 12+)
