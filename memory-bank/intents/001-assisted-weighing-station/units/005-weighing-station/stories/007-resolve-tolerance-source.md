---
id: 007-resolve-tolerance-source
unit: 005-weighing-station
intent: 001-assisted-weighing-station
status: draft
priority: must
requirements: [FR-12]
created: 2026-11-04T16:00:00Z
assigned_bolt: 007-weighing-station
implemented: false
---

# Story: 007-resolve-tolerance-source

**Resolve the tolerance source (recipe → config → blocked)**

## User Story

**As a** operator
**I want** the right tolerance applied to each chemical
**So that** no chemical is ever weighed against an assumed universal tolerance

## Unit & Requirements

- **Unit**: `005-weighing-station` (`../unit-brief.md`)
- **Requirements**: FR-12 (`../../../requirements.md`)
- **Mockup / source reference**: decision TOL; ui-analysis §10 R11

## Acceptance Criteria

- [ ] **Given** a recipe tolerance, **Then** it is used
- [ ] **Given** none in the recipe, **Then** the configured tolerance for chemical + selected scale is used
- [ ] **Given** neither, **Then** weighing that chemical is blocked with "no tolerance defined"
- [ ] **Given** a confirmation, **Then** the tolerance applied and its source are snapshotted on the record

## UI/UX Impact

Tolerance caption (L538); blocked message (008-011).

## API Dependencies

- Uses 003-003 snapshot and 003-004 config

## Test Expectations

- Domain tests for the three paths

## Dependencies

### Requires
- 003-batch-recipe / 003-load-recipe-snapshot
- 003-batch-recipe / 004-configured-tolerance-fallback
- 005-weighing-station / 003-automatic-scale-selection

### Enables
- 005-weighing-station / 006-classify-reading-four-classes

## Edge Cases

None identified beyond the acceptance criteria.

## Out of Scope

- Editing tolerances in the UI
- Implementation tasks (planned in bolts, STEP 12+)
