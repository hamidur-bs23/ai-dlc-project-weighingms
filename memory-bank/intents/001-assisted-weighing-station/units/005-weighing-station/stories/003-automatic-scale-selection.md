---
id: 003-automatic-scale-selection
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-8]
created: 2026-11-04T16:00:00Z
assigned_bolt: null
implemented: false
---

# Story: 003-automatic-scale-selection

**Automatic scale selection by the ≤ 3 kg threshold**

## User Story

**As a** operator
**I want** the right scale chosen for me
**So that** small quantities are weighed precisely and I never pick the wrong scale

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-8 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §10 R10 (scale badge L508–512)

## Acceptance Criteria

- [ ] **Given** a required quantity, **When** converted to kg, **Then** ≤ threshold → Precision and > threshold → Heavy-Duty
- [ ] **Given** the default threshold, **Then** 2.999 kg → Precision, 3.000 kg → Precision, 3.001 kg → Heavy-Duty
- [ ] **Given** B-24187, **Then** Yellow RGB 94.0 g → "3 kg precision" and Sodium Sulphate 24.000 kg → "32 kg floor"
- [ ] **Given** the threshold or assignments change in configuration, **Then** the new values apply without code change

## UI/UX Impact

Scale badge "automatic" (L508–512).

## API Dependencies

- None external; uses scale assignment config

## Test Expectations

- Domain tests on the boundary values; config reload test

## Dependencies

### Requires
- 005-weighing-station / 001-start-or-resume-weighing-session

### Enables
- 005-weighing-station / 007-resolve-tolerance-source
- 008-assisted-weighing-station-ui / 004-weigh-screen-rail-and-header

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Unit g vs kg | Compared after conversion |

## Out of Scope

- Manual scale reselection (not in V1)
- Implementation tasks (planned in bolts, STEP 12+)
