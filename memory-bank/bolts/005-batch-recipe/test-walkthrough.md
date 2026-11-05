---
stage: test
bolt: 005-batch-recipe
created: 2026-11-05T13:40:00Z
---

## Test Report: batch-recipe (bolt 005)

### Summary

- **Tests**: 293/293 passed across the solution, the same in 3 consecutive runs. 53 are new:
  - Domain: 28 (`QuantityTests`, `RecipeSnapshotTests`);
  - Application: 18 (`BatchServiceTests`, `ToleranceSettingsTests`);
  - Api integration: 21 (`BatchEndpointsTests`, `ToleranceCatalogueTests`, `PersistenceTests` extended), against **PostgreSQL 17 (Testcontainers)** and the **real simulator in-process**;
  - parameterised cases make up the remainder.
- **Coverage** (hand-written lines; migrations and generated code excluded):

  | Project | Coverage |
  |---|---|
  | `WeighingMS.Domain` | **100%** (209/209) |
  | `WeighingMS.Application` | 98.8% (340/344) |
  | `WeighingMS.Infrastructure` | 94.4% (388/411) |
  | `WeighingMS.Api` | 93.5% (387/414) |

  New files: `BatchService` 94.6%, `OrgatexGatewayClient` 90.2%, batch contracts 100%.
- **Build**: 0 warnings, 0 errors. **Format**: clean. No test containers left running; the Ryuk reaper exits by itself.
- **Evidence**: `evidence/STEP-19-api-flow.txt`, a live run through WeighingMS.Api:
  - the queue shows the 4 batches, all `queued`;
  - the B-24187 header;
  - the B-24187 recipe with 5 components, snapshotted on that first call;
  - the queue again, now with B-24187 `weighing`;
  - an unknown batch → 404 `Batch.NotFound`.

### Test Files

- [x] `tests/WeighingMS.Domain.Tests/QuantityTests.cs`:
  - g and kg; unknown units (`lb`, `G`, null) → `Quantity.UnknownUnit`;
  - 0 and negative values rejected as `Quantity.NotPositive`;
  - exact kilograms (94.0 g → 0.094 kg, 3000 g → 3 kg);
  - equality and text form.
- [x] `tests/WeighingMS.Domain.Tests/RecipeSnapshotTests.cs`:
  - B-24187's header and 5 components come out in sequence, even from reversed input;
  - Yellow RGB keeps 94.0 g ± 0.5 g and container C-12;
  - a missing tolerance becomes null, and a blank container becomes null;
  - `LoadedAt` is converted to UTC;
  - `Components` is a copy;
  - **every rejection is a 502 `OrgaTex.InvalidRecipe`**: no components, duplicate sequence, unknown unit, target 0 or negative, tolerance 0.
- [x] `tests/WeighingMS.Application.Tests/BatchServiceTests.cs`:
  - status goes from queued to weighing after the recipe is opened;
  - **the second recipe call doesn't reach OrgaTEX** and returns the same `loadedAt`;
  - OrgaTEX down after a snapshot → still served, while the queue is 503;
  - OrgaTEX down without a snapshot → 503 and nothing stored;
  - invalid data → 502 and nothing stored;
  - not found is passed through; the training-only flag is kept.
- [x] `tests/WeighingMS.Application.Tests/ToleranceSettingsTests.cs` - valid entries, empty configuration, zero, negative, bad unit, bad scale class, missing chemical, duplicates
- [x] `tests/WeighingMS.Api.IntegrationTests/BatchEndpointsTests.cs`:
  - **the STEP 19 flow**: queue (4 batches, every column, all `queued`) → batch → recipe (5 components, targets, units, tolerance, container) → B-24187 `weighing`;
  - the snapshot is stored once (1 row, 5 components) and a later call returns the same `loadedAt`;
  - **OrgaTEX faulted**: queue → 503; the snapshotted recipe is still served unchanged; B-24188 → 503 with **no database row**; after the fault clears, Retry → 200;
  - unknown batch or recipe → 404 `Batch.NotFound`;
  - B-24190's Black B is stored exactly as **3.000 kg**;
  - **6 concurrent first calls → 1 snapshot**, all with the same `loadedAt`;
  - 401 without a session on all three endpoints.
- [x] `tests/WeighingMS.Api.IntegrationTests/ToleranceCatalogueTests.cs`:
  - lookup hit; no entry → null, not a default;
  - **a configuration change applies without a restart**;
  - **an invalid entry stops the Api at start-up**; a valid configuration starts and resolves.
- [x] `tests/WeighingMS.Api.IntegrationTests/PersistenceTests.cs` (extended) - both migrations applied, `_InitialStationAccess` and `_AddRecipeSnapshots`

### Acceptance Criteria Validation

- ✅ **The queue shows the 4 batches with order, machine, fabric, weight, shade, count and status. A batch removed from the dummy disappears, because the queue is read live with no cache. An empty queue → `items: []`.** (`Step19Flow…`; `BatchService` returns OrgaTEX's list as-is.)
- ✅ **`GET /api/batches/B-24187` returns the header; an unknown or scanned ID → 404 `Batch.NotFound`** (`UnknownBatch_…`).
- ✅ **The recipe snapshot holds 5 components in sequence; calling again returns the same snapshot; nothing changes after load** (`Recipe_IsSnapshottedOnce…`, `RecipeSnapshotTests`).
- ✅ **OrgaTEX down: the snapshotted batch is still served; the queue and un-snapshotted batches → 503; Retry works after recovery** (`OrgatexDown_…`).
- ✅ **Invalid OrgaTEX data → 502 `OrgaTex.InvalidRecipe` and nothing stored** (`RecipeSnapshotTests`, `BatchServiceTests`).
- ✅ **Tolerance lookup returns a hit or none; reloads without a restart; an invalid entry fails start-up** (`ToleranceCatalogueTests`, `ToleranceSettingsTests`).
- ✅ **401 without a session; errors are ProblemDetails with `type` and `errorCode`** (`BatchEndpoints_WithoutSession…` and the error tests).
- ✅ **Build has 0 warnings; format clean; all earlier tests still pass; the migration applies** (`Migrations_AreAllApplied`).

### Issues Found

- **No defects found in the Implement-stage code.** Every new test passed on its first run.
- **Evidence hygiene**: the first STEP 19 capture showed B-24188 as `weighing`, left over from the Implement-stage smoke test in the development database. I cleared the development database's recipe snapshots (manual-run data only) and captured again.

### Notes

- **The STEP 19 screenshots** (`STEP-19-batch-screen.png`, `STEP-19-network.png`, `STEP-19-test.png`) need the Select-batch screen from bolt 006. `STEP-19-api-flow.txt` is the API part.
- **The "no `const batches = [...]` in the UI" check** is enforced in bolt 006, when `web/` exists.
- **Smoke and evidence runs** now use ports 5180 and 5190 with `ASPNETCORE_ENVIRONMENT=Development`. They never touch a developer's instances on 5080 and 5090, and stop only the processes they started.
