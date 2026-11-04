---
id: 006-device-health-for-status-strip
unit: 004-device-integration
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-23, FR-24]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 006-device-health-for-status-strip

**Device health for the status strip**

## User Story

**As a** operator
**I want** to see at a glance whether scales, scanner, printer and OrgaTEX are up
**So that** I know why something isn't working

## Unit & Requirements

- **Unit**: `004-device-integration` (`../unit-brief.md`)
- **Requirements**: FR-23, FR-24 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §2 status strip (L618–627)

## Acceptance Criteria

- [ ] **Given** the devices, **When** health is requested, **Then** each scale, the scanner, the printer and OrgaTEX report READY / OFFLINE with a since-time
- [ ] **Given** a status change, **Then** it is visible within one polling interval

## UI/UX Impact

Status strip (L618–627) via 008-001.

## API Dependencies

- Api: aggregated health query (scales, scanner, printer, OrgaTEX)

## Test Expectations

- Integration tests toggling each fault

## Dependencies

### Requires
- 004-device-integration / 001-live-scale-reading
- 001-station-simulators / 006-fault-injection-and-seed-reset

### Enables
- 008-assisted-weighing-station-ui / 001-app-shell-top-bar-status-strip

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Simulator host down | Everything shows OFFLINE |

## Out of Scope

- Unsynced count (006-004)
- Implementation tasks (planned in bolts, STEP 12+)
