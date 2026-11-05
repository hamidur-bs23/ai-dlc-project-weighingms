---
stage: implement
bolt: 005-batch-recipe
created: 2026-11-05T12:50:00Z
---

## Implementation Walkthrough: batch-recipe (bolt 005)

### Summary

WeighingMS.Api now serves the batch side of the workflow on the workbook paths:
- the batch queue, read live from OrgaTEX, with a WeighingMS status of queued or weighing;
- one batch, for selection by touch or scan;
- the recipe, as a validated, **immutable snapshot** stored in PostgreSQL on first request.

When OrgaTEX is down, a snapshotted batch can still be weighed and anything else returns 503 for Retry, with no data invented. A configured tolerance per chemical and scale class is available as the fallback for recipes that give none.

### Structure Overview

- **Domain** gets its first shared value object, `Quantity` (fixed-point value with g or kg), and the `RecipeSnapshot` aggregate. The snapshot's factory checks the OrgaTEX data and returns errors rather than storing anything bad.
- **Application** adds the single `IOrgatexGateway` port (read operations), the snapshot store port, `BatchService` and the tolerance catalogue port and settings.
- **Infrastructure** implements the gateway as a typed HttpClient on the existing resilience pipeline. It also stores snapshots through EF Core: two tables, `numeric(12,4)` with the unit stored alongside, and a unique batch ID.
- **The Api** maps the use cases to the three workbook endpoints.

### Completed Work

- [x] `src/WeighingMS.Domain/Common/Quantity.cs`, `MassUnit.cs` - fixed-point mass with unit; positive creation; conversion to kg
- [x] `src/WeighingMS.Domain/Batches/RecipeSnapshot.cs` - validated, immutable recipe copy: no components, duplicate sequence, unknown unit, target ≤ 0 or tolerance ≤ 0 → `OrgaTex.InvalidRecipe` (502)
- [x] `src/WeighingMS.Domain/Batches/SnapshotComponent.cs`, `RecipeLine.cs`, `BatchHeader.cs`, `BatchErrors.cs` - recipe lines, inputs, `Batch.NotFound` and `OrgaTex.InvalidRecipe`
- [x] `src/WeighingMS.Application/Batches/IOrgatexGateway.cs` - the single OrgaTEX port (read operations now; consumption in bolt 011)
- [x] `src/WeighingMS.Application/Batches/BatchService.cs` - queue with status, batch, recipe (stored snapshot first, else fetch → validate → store)
- [x] `src/WeighingMS.Application/Batches/IRecipeSnapshotStore.cs`, `BatchStatus.cs`, `QueuedBatch.cs`, `OrgatexBatch.cs`, `OrgatexRecipe.cs`, `OrgatexErrors.cs` - ports, read model, `OrgaTex.Unavailable` (503)
- [x] `src/WeighingMS.Application/Batches/ToleranceSettings.cs`, `ToleranceEntry.cs`, `IToleranceCatalogue.cs` - TOL fallback configuration and validation (positive, g or kg, precision or heavyDuty, no duplicates)
- [x] `src/WeighingMS.Infrastructure/Integrations/OrgaTex/OrgatexGatewayClient.cs` - Dummy OrgaTEX adapter (SIM-CONTRACTS translation; 404 → `Batch.NotFound`; transport failures → `OrgaTex.Unavailable`)
- [x] `src/WeighingMS.Infrastructure/Batches/RecipeSnapshotStore.cs` - load with components; "which batches have a snapshot"; store or return the existing one if two requests race (unique conflict → the first wins)
- [x] `src/WeighingMS.Infrastructure/Batches/ToleranceCatalogue.cs` - lookup over `IOptionsMonitor`, so changes apply without a restart
- [x] `src/WeighingMS.Infrastructure/Persistence/Configurations/RecipeSnapshotConfiguration.cs`, `SnapshotComponentConfiguration.cs` - `recipe_snapshots` and `recipe_snapshot_components`
- [x] `src/WeighingMS.Infrastructure/Persistence/Migrations/*_AddRecipeSnapshots.cs` - migration (SQL reviewed)
- [x] `src/WeighingMS.Api/Controllers/BatchesController.cs` - `GET /api/batches`, `GET /api/batches/{id}`, `GET /api/batches/{id}/recipe`, all requiring a signed-in operator
- [x] `src/WeighingMS.Api/Contracts/Batches/*` - wire contracts (`{ items }`; quantities `{ value, unit }`)
- [x] Modified `WeighingDbContext.cs`, `DependencyInjection.cs` (Application and Infrastructure), `IntegrationsRegistration.cs` - registration; the OrgaTEX client on the resilience pipeline
- [x] Modified `src/WeighingMS.Api/Program.cs` - `Tolerances` options validated on start
- [x] Modified `appsettings.json` (empty `Tolerances`; Polly logs silenced), `appsettings.Development.json` (three lab fallback tolerances)

### Key Decisions

- **The snapshot is taken on the first `GET …/recipe`** (RECIPE-SNAPSHOT). Later calls read only the database, so OrgaTEX can't change a recipe mid-batch.
- **Two requests snapshotting the same batch**: the unique `batch_id` decides. The second insert fails, and the store returns the first snapshot.
- **Queue status `weighing`** means a snapshot exists. The completed, print-pending and sync-pending states are left to bolts 009, 011 and 012.
- **The training-only flag** is set if either the batch or the recipe is marked training-only (SEED-01).
- **Polly's own logs are silenced** (`"Polly": "Fatal"`). Each failed integration call is already logged once as a Warning by the request log, so Polly's last-retry `ERR` line was a duplicate that broke the coding-standards levels.

### Deviations from Plan

- **None in behaviour.**
- **The STEP 19 "no `const batches`" check** (an architecture or lint rule over `web/`) moves to bolt 006, because the web project doesn't exist yet.

### Dependencies Added

- None.

### Developer Notes

- **Smoke test**, run on separate ports 5180 and 5190 so it can't touch a developer's running instances on 5080 and 5090:
  - 401 without a session;
  - the queue lists the 4 mockup batches with order, machine, fabric, weight, shade, count and the training flag;
  - unknown batch → 404 `Batch.NotFound`;
  - the B-24187 recipe gives 5 components in sequence with mockup targets, tolerances and containers, and the queue then shows B-24187 as `weighing`;
  - B-24190's 7 components are stored, including the exact 3.000 kg Black B;
  - **OrgaTEX faulted**: queue → 503 `OrgaTex.Unavailable`; the snapshotted B-24187 is still served unchanged; B-24188 → 503 and **no snapshot stored**; after the fault clears, Retry → 200.
- **Quantity scale on the wire**:
  - values read back from the database carry `numeric(12,4)` scale (for example `24.0000`); the first response, built in memory, shows `24.000`;
  - the numbers are equal, and the UI formats by unit (g to 1 dp, kg to 3 dp).
- **Smoke-test tip**: running `dotnet run --no-launch-profile --environment X` does not set the environment; use `ASPNETCORE_ENVIRONMENT=Development`, or the simulator starts in Production without `/_sim/*`.
- **Verification**: build 0 warnings, format clean, all 240 existing tests pass, migration applied to Postgres 17, specsmd validators clean.
