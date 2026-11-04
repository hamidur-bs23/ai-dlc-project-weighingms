---
id: 001-dummy-orgatex-serves-batches-and-recipes
unit: 001-station-simulators
intent: 001-assisted-weighing-station
status: complete
priority: must
requirements:
  - FR-25
  - FR-1
  - FR-3
created: '2026-11-04T16:00:00Z'
assigned_bolt: 001-station-simulators
implemented: true
---

# Story: 001-dummy-orgatex-serves-batches-and-recipes

**Dummy OrgaTEX serves the batch queue and recipes from seed**

## User Story

**As a** trainee developer / tester
**I want** the Dummy OrgaTEX API to serve the training batches and recipes
**So that** WeighingMS can load real-looking OrgaTEX data over HTTP without the real system

## Unit & Requirements

- **Unit**: `001-station-simulators` (`../unit-brief.md`)
- **Requirements**: FR-25, FR-1, FR-3 (`../../../requirements.md`)
- **Mockup / source reference**: ui-analysis §6, §9 (L454, L677–683)

## Acceptance Criteria

- [ ] **Given** the simulator has started with the seed, **When** `GET /api/batches` is called, **Then** it returns B-24187, B-24188, B-24190 and B-24191 with order, machine, fabric, shade, component count and status
- [ ] **Given** B-24187, **When** `GET /api/batches/B-24187/recipe` is called, **Then** it returns 5 components in order Blue RSPL, Red RB, Yellow RGB, Sodium Sulphate, Soda Ash with target, unit, tolerance (Yellow RGB 94.0 g ± 0.5 g) and suggested containers C-09, C-11, C-12, D-03, D-05
- [ ] **Given** an unknown batch ID, **When** it is requested, **Then** the response is 404 ProblemDetails
- [ ] **Given** a batch is removed from the seed, **When** the queue is requested, **Then** it no longer appears

## UI/UX Impact

None directly (backend). Makes the mockup data real for the queue (view 1) and the recipe rail (view 2).

## API Dependencies

- Serves `GET /api/batches`, `GET /api/batches/:batchId`, `GET /api/batches/:batchId/recipe` (simulator :5090)

## Test Expectations

- Contract tests (xUnit v3 + Shouldly) on the simulator: seed content, order, 404
- The JSON seed is the only source of the training data

## Dependencies

### Requires
- None

### Enables
- 001-station-simulators / 002-dummy-orgatex-receives-consumption
- 001-station-simulators / 006-fault-injection-and-seed-reset
- 003-batch-recipe / 001-view-batch-queue

## Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Seed file missing or invalid | Simulator fails to start with a clear error |
| Batch with zero components | Not in the seed; contract allows it but WeighingMS rejects it (003) |

## Out of Scope

- Real OrgaTEX contract (not invented)
- Consumption receiving (002)
- Implementation tasks (planned in bolts, STEP 12+)
