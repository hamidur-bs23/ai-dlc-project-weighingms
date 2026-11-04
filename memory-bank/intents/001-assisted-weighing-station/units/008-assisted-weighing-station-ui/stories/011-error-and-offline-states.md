---
id: 011-error-and-offline-states
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-23, FR-24]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 011-error-and-offline-states

**Error and offline states across screens**

## User Story

**As a** operator
**I want** clear, safe messages when a device or OrgaTEX fails
**So that** I always know what's wrong and what I can still do

## Unit & Requirements

- **Unit**: `008-assisted-weighing-station-ui` (`../unit-brief.md`)
- **Requirements**: FR-23, FR-24 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §12 Q-UI-4

## Acceptance Criteria

- [ ] **Given** scale offline, **Then** the readout shows "—" and "Scale offline" and Confirm/Tare are disabled
- [ ] **Given** OrgaTEX down on load, **Then** an error with Retry; **given** scanner down, **Then** SCANNER OFFLINE with touch/typed fallback; **given** printer down, **Then** print pending
- [ ] **Given** "no tolerance defined" or "Over capacity", **Then** the message is shown and Confirm stays disabled
- [ ] **Given** any integration error, **Then** a translated message by error code; raw details never shown

## UI/UX Impact

Offline variants of the readout, queue and strip (new, mockup style).

## API Dependencies

- Api ProblemDetails error codes from 003, 004, 006, 007

## Test Expectations

- Vitest per error state; Playwright fault scenarios (STEP 30)

## Dependencies

### Requires
- 004-device-integration / 003-scale-offline-and-invalid-readings
- 003-batch-recipe / 005-orgatex-unavailable-on-load
- 004-device-integration / 005-scanner-offline-manual-fallback
- 007-ticket-printing / 004-printer-unavailable-print-pending

### Enables
- None

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Device repair flows
- Implementation tasks (planned in bolts, STEP 12+)
