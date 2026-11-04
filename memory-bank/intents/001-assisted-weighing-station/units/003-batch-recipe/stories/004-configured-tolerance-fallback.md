---
id: 004-configured-tolerance-fallback
unit: 003-batch-recipe
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-12]
created: 2026-11-04T16:00:00Z
assigned_bolt: 005-batch-recipe
implemented: false
---

# Story: 004-configured-tolerance-fallback

**Configured tolerance per chemical + scale**

## User Story

**As a** process engineer
**I want** to configure a tolerance per chemical and scale class
**So that** chemicals without a recipe tolerance can still be weighed safely

## Unit & Requirements

- **Unit**: `003-batch-recipe` (`../unit-brief.md`)
- **Requirements**: FR-12 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §11 UA-7; decision TOL

## Acceptance Criteria

- [ ] **Given** configuration entries per chemical + scale class, **When** queried, **Then** the matching tolerance is returned
- [ ] **Given** no entry for a chemical + scale, **Then** "none" is returned (no universal default)
- [ ] **Given** the configuration changes, **Then** it is reloaded without restart (`IOptionsMonitor`)

## UI/UX Impact

None directly. Feeds the tolerance caption (L538).

## API Dependencies

- Api: internal query used by 005-007

## Test Expectations

- Unit tests: lookup hit, miss, reload; validation of negative tolerances at start-up

## Dependencies

### Requires
- None

### Enables
- 005-weighing-station / 007-resolve-tolerance-source

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Negative or zero tolerance in config | Start-up validation fails |

## Out of Scope

- Choosing between recipe and config (005-007)
- Implementation tasks (planned in bolts, STEP 12+)
