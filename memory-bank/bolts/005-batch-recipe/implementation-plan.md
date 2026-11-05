---
stage: plan
bolt: 005-batch-recipe
created: 2026-11-05T12:30:00Z
---

## Implementation Plan: batch-recipe (bolt 005)

### Objective

Give WeighingMS the batch side of the workflow, read from OrgaTEX through the single **`IOrgatexGateway`** port (CHECKPOINT 3; read operations here, the consumption operation in bolt 011):
- the **batch queue**;
- **batch selection** by ID, which a touch and a scan both use;
- an **immutable recipe snapshot** persisted in PostgreSQL;
- the **configured tolerance per chemical + scale class** (the TOL fallback).

When OrgaTEX is down, the Api returns 503 with a code the UI can offer Retry on, and never invents data. A recipe already snapshotted is still served (FR-24a, FR-3).

Workbook: **STEP 19** (backend). The UI fetches the queue from `GET /api/batches`, then `GET /api/batches/B-24187` and `GET /api/batches/B-24187/recipe`, with no `const batches = [...]`. The screen itself is bolt 006.

### Stories and requirements in scope

1. `001-view-batch-queue`: FR-1
2. `002-select-batch-by-touch-or-scan`: FR-2, FR-27
3. `003-load-recipe-snapshot`: FR-3
4. `004-configured-tolerance-fallback`: FR-12 (TOL), NFR Configurability
5. `005-orgatex-unavailable-on-load`: FR-24(a)

### What will change

1. **Domain** (`Domain/Batches`):
   - `RecipeSnapshot` (aggregate) and `SnapshotComponent`;
   - `Quantity` (value object: fixed-point value plus a unit, g or kg; positive where required);
   - a factory that **validates OrgaTEX data**: at least one component, sequences unique, target > 0, a known unit, recipe tolerance > 0 when present. It returns ErrorOr, so a bad recipe is rejected and no snapshot or session is created;
   - once created, a snapshot never changes.
2. **Application** (`Application/Batches`):
   - the **`IOrgatexGateway`** port with read operations `GetBatchesAsync`, `GetBatchAsync` and `GetRecipeAsync`;
   - `IRecipeSnapshotStore`;
   - `BatchService`:
     - the queue, with WeighingMS status merged in;
     - batch detail;
     - recipe: return the stored snapshot if there is one, otherwise fetch it, validate it, store it and return it;
   - `ToleranceSettings` (options) and `IToleranceCatalogue.Find(chemicalId, scaleClass)`, which returns a setting or **none**;
   - errors `Batch.NotFound` (404), `OrgaTex.Unavailable` (503), `OrgaTex.InvalidRecipe` (502).
3. **Infrastructure**:
   - `OrgatexGatewayClient`: a typed HttpClient on the same resilience pipeline as the devices. It translates the simulator shapes (SIM-CONTRACTS) and transport failures.
   - EF Core configuration for `recipe_snapshots` and `recipe_snapshot_components`: `numeric(12,4)` quantities and the unit stored alongside; a unique `batch_id`.
   - Migration **`AddRecipeSnapshots`**.
   - `ToleranceCatalogue` over `IOptionsMonitor<ToleranceSettings>`, so changes reload without a restart. Settings are validated on start; a zero or negative tolerance stops start-up.
4. **Api**: `BatchesController` with the **workbook paths**, all requiring a signed-in operator:
   - `GET /api/batches`
   - `GET /api/batches/{batchId}`
   - `GET /api/batches/{batchId}/recipe`

### Files to be created / modified

- **Created**:
  - `src/WeighingMS.Domain/Batches/RecipeSnapshot.cs`, `SnapshotComponent.cs`, `BatchErrors.cs`
  - `src/WeighingMS.Domain/Common/Quantity.cs`
  - `src/WeighingMS.Application/Batches/*` (port, store port, service, records, tolerance catalogue port and options)
  - `src/WeighingMS.Infrastructure/Integrations/OrgaTex/OrgatexGatewayClient.cs`
  - `src/WeighingMS.Infrastructure/Batches/RecipeSnapshotStore.cs`, `ToleranceCatalogue.cs`
  - `src/WeighingMS.Infrastructure/Persistence/Configurations/RecipeSnapshot*Configuration.cs`
  - `src/WeighingMS.Infrastructure/Persistence/Migrations/*_AddRecipeSnapshots.cs`
  - `src/WeighingMS.Api/Controllers/BatchesController.cs`
  - `src/WeighingMS.Api/Contracts/Batches/*`
  - Tests (Stage 3): Domain, Application and Api integration tests against the in-process simulator
- **Modified**:
  - `Infrastructure/DependencyInjection.cs`, `Integrations/IntegrationsRegistration.cs`
  - `Persistence/WeighingDbContext.cs`
  - `Api/Program.cs`, `appsettings*.json` (the `Tolerances` section)
  - `CLAUDE.md`

### APIs involved

**WeighingMS.Api (:5080), the workbook contract paths:**

| Method | Path | Response |
|---|---|---|
| GET | `/api/batches` | `{ items: [{ batchId, orderId, machine, fabric, batchWeight{value,unit:"kg"}, shade, componentCount, status, trainingOnly }] }`. An empty queue gives `items: []`. OrgaTEX down → 503 `OrgaTex.Unavailable` |
| GET | `/api/batches/{batchId}` | The same fields for one batch. Unknown → 404 `Batch.NotFound` (the "batch not found" message, for both scan and touch) |
| GET | `/api/batches/{batchId}/recipe` | `{ batchId, shade, loadedAt, trainingOnly, components: [{ sequence, chemicalId, code, name, displayColour, target{value,unit}, tolerance{value,unit} \| null, suggestedContainer }] }` in sequence. The snapshot, once taken, is served even when OrgaTEX is down |

**Consumed from the Dummy OrgaTEX (:5090):** `GET /api/batches`, `GET /api/batches/{id}`, `GET /api/batches/{id}/recipe`.

**Queue status.** The status shown is the WeighingMS status:
- **`queued`**: no recipe snapshot yet;
- **`weighing`**: a snapshot exists, so the batch has been opened at the station.

`complete`, `print pending` and `sync pending` (story 001, criterion 2) are added to this same read model by bolts 009, 011 and 012, which own those states.

### Decisions at this checkpoint: resolved (APPROVE PLAN, 2026-11-05)

- **D1: option 1.** The snapshot is taken on the first `GET /api/batches/{id}/recipe` (RECIPE-SNAPSHOT).
- **D2: option 1.** Invalid OrgaTEX recipe data → 502 `OrgaTex.InvalidRecipe`.

#### Options as presented

**D1. When is the recipe snapshot taken?**
1 - **(Recommended)** On the first `GET /api/batches/{id}/recipe`, which the UI calls when the batch is selected (STEP 19). Each later call returns the stored snapshot. The request is idempotent and safe to retry: the first call stores, and every call returns the same data. It keeps the workbook's three-call flow, so the UI needs nothing extra.
2 - Through an explicit command, `POST /api/batches/{id}/snapshot`, before `GET /recipe`. That's purer REST, but it adds a call to the workbook flow.

**D2. What status does a recipe OrgaTEX sent with bad data return** (unknown unit, target ≤ 0)?
1 - **(Recommended)** 502 `OrgaTex.InvalidRecipe`. The fault lies upstream, not with the operator, and no snapshot is stored.
2 - 422 `Recipe.Invalid`.

### Dependencies

- Bolt 003 (complete): the session, ProblemDetails, persistence.
- Bolt 004 (complete): the resilience pipeline, `IntegrationCall`, the in-process simulator test host.
- No new packages.

### Technical approach

- **Quantities** are `Quantity(decimal, unit)` in the Domain, stored as `numeric(12,4)` plus a unit column (data-stack). The Domain gets its first shared value object here; bolt 007 reuses it for tolerance classification.
- **The recipe is read from the snapshot only**, whenever one exists, so OrgaTEX can't change a recipe mid-batch, which keeps job → recipe → chemical intact. The simulator recipe call happens once per batch.
- **No caching** of the queue (tech-stack): the queue is read live from OrgaTEX each time.
- **Tolerance catalogue**:
  - configuration `Tolerances:Entries[]` holds `{ chemicalId, scaleClass: precision | heavyDuty, plusMinus, unit }`;
  - a lookup returns **none** when there's no entry (TOL: no universal default);
  - development seeds a few entries for chemicals whose recipes could omit a tolerance; the training seed always carries one.

### Acceptance criteria (from the stories)

- [ ] `GET /api/batches` returns B-24187, B-24188, B-24190 and B-24191, each with order, machine, fabric, batch weight, shade, component count and status. A batch removed from the dummy disappears. An empty queue → `items: []`.
- [ ] `GET /api/batches/B-24187` returns the header data. An unknown or scanned-but-absent ID → 404 `Batch.NotFound`.
- [ ] `GET /api/batches/B-24187/recipe` stores a snapshot of all 5 components in sequence, with name, code, target, unit, tolerance and container. The same call again returns the same snapshot, and nothing changes after load.
- [ ] With OrgaTEX faulted, a batch already snapshotted is still served. The queue, and a batch not yet snapshotted, → 503 `OrgaTex.Unavailable`. Once the fault clears, the same request succeeds (Retry).
- [ ] A recipe with an unknown unit or a target ≤ 0 is rejected (502 `OrgaTex.InvalidRecipe`) and no snapshot is stored.
- [ ] The tolerance catalogue returns the matching entry for chemical + scale class, or none. It reloads without a restart, and a zero or negative entry fails start-up.
- [ ] Every endpoint needs a session (401). Errors are ProblemDetails with `type` and `errorCode`, never raw detail.
- [ ] Build 0 warnings; format clean; all 240 existing tests pass; the migration applies.

### How this will be tested (Stage 3)

1. **Domain**: `Quantity` (positive, units, equality) and the `RecipeSnapshot` factory (each invalid case; valid B-24187; immutability).
2. **Application**, with fakes: snapshot taken on the first call and reused afterwards; OrgaTEX down with and without a snapshot; status merging; tolerance lookup hit and miss.
3. **Api integration** (Testcontainers Postgres plus the in-process simulator):
   - the full STEP 19 flow;
   - the snapshot persisted and reused while `/_sim/faults orgatex` is active;
   - 503, 404 and 401;
   - the seed batches and training-only flags;
   - `IOptionsMonitor` reload of the tolerances;
   - start-up validation failure.
4. **A live smoke test**, which also gives STEP 19 API evidence: a `curl` transcript of the three calls. The STEP 19 screenshots and network evidence come with the UI in bolt 006.

### Out of scope

- The Select-batch screen and the scan routing in the UI: bolt 006.
- Weighing sessions and choosing between recipe and configured tolerance at weighing time: bolt 007.
- The completed, print-pending and sync-pending statuses: bolts 009, 011 and 012.
- The consumption operation of `IOrgatexGateway`: bolt 011.
