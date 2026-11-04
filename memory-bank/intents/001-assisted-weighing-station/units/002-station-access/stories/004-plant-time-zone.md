---
id: 004-plant-time-zone
unit: 002-station-access
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-22
created: '2026-11-04T16:00:00Z'
assigned_bolt: 003-station-access
implemented: true
---

# Story: 004-plant-time-zone

**Plant time zone for display and print**

## User Story

**As a** operator
**I want** times shown and printed in the plant's local time
**So that** the ticket and screen match the clock on the wall

## Unit & Requirements

- **Unit**: `002-station-access` (`../unit-brief.md`)
- **Requirements**: FR-22 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §5 clock (L626), ticket DATE (L597)

## Acceptance Criteria

- [ ] **Given** the default zone `Asia/Dhaka`, **When** a record at 2026-11-03T21:48:22Z is displayed, **Then** it shows 2026-11-04 03:48
- [ ] **Given** any browser or server OS time zone, **Then** displayed and printed times are unchanged
- [ ] **Given** all persisted timestamps, **Then** they are stored in UTC

## UI/UX Impact

Status-strip clock (L626), summary times (L577), ticket date (L597).

## API Dependencies

- Api: plant time zone exposed with session/config (route fixed in the bolt design)

## Test Expectations

- Unit tests with a fake `TimeProvider`; test with server TZ set to UTC and to another zone

## Dependencies

### Requires
- None

### Enables
- 006-consumption-writeback / 004-write-back-status-visible
- 007-ticket-printing / 001-compose-ticket

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Configured zone invalid | Start-up validation fails |

## Out of Scope

- Daylight-saving rules beyond IANA data
- Implementation tasks (planned in bolts, STEP 12+)
