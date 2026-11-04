---
id: 001-station-simulators
unit: 001-station-simulators
intent: 001-assisted-weighing-station
type: simple-construction-bolt
status: in-progress
stories:
  - 001-dummy-orgatex-serves-batches-and-recipes
  - 002-dummy-orgatex-receives-consumption
  - 003-dummy-scales-with-scenarios
created: 2026-11-04T17:00:00Z
started: 2026-11-04T17:55:00Z
completed: null
current_stage: test
stages_completed:
  - name: plan
    completed: 2026-11-04T18:20:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-11-04T18:35:00Z
    artifact: implementation-walkthrough.md

requires_bolts: []
enables_bolts: [002-station-simulators, 003-station-access, 004-device-integration]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 001-station-simulators

## Overview

Simple construction bolt for unit `001-station-simulators`, covering workbook STEPs 16–18.

## Objective (expected outcome)

**Scaffold the solution** (WeighingMS.Domain / Application / Infrastructure / Api, WeighingMS.Simulators.Host, the Angular `web` app, npm workspaces, Docker Compose `postgres`, and the CLAUDE.md command table updated with the real commands). Then the Dummy OrgaTEX (queue, batch, recipe, consumption receiver) and the two dummy scales with scenarios serve the B-24187 seed.

## Stories Included

- **001-dummy-orgatex-serves-batches-and-recipes**: Dummy OrgaTEX serves the batch queue and recipes from seed (Must)
- **002-dummy-orgatex-receives-consumption**: Dummy OrgaTEX receives and exposes consumption (Must)
- **003-dummy-scales-with-scenarios**: Two dummy scales with weighing scenarios and resolution (Must)

Story files: `memory-bank/intents/001-assisted-weighing-station/units/001-station-simulators/stories/`

## Bolt Type

**Type**: Simple Construction Bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- ✅ **1. plan**: Complete → implementation-plan.md
- ✅ **2. implement**: Complete → src/, implementation-walkthrough.md
- ⏳ **3. test**: In Progress → test-walkthrough.md

Every stage stops at a human validation checkpoint (workbook STEP 15, CHECKPOINT 5).

## Dependencies

### Requires
- None (first bolt)

### Enables
- 002-station-simulators
- 003-station-access
- 004-device-integration

## Test Expectations

- Contract tests (xUnit v3 + Shouldly) for every OrgaTEX and scale dummy endpoint
- Scenario tests: rising / belowRange / enterRange / overshoot / stable respect each scale's resolution
- curl evidence for STEP 17–18 (`STEP-17-dummy-data.json`, `STEP-18-api-tests.txt`)

## Success Criteria

- [ ] All stories implemented
- [ ] All acceptance criteria met
- [ ] Tests passing
- [ ] Code reviewed

## Notes

- Workbook: STEPs 16–18.
- Read the relevant `memory-bank/standards/` files before generating code (CLAUDE.md).
- After this bolt, replace the planned command table in CLAUDE.md with the real commands.
