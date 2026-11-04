---
stage: plan
bolt: 001-station-simulators
created: 2026-11-04T18:10:00Z
---

## Implementation Plan: station-simulators (bolt 001)

### Objective

Create the WeighingMS solution skeleton, then build the first two **training dummies** in `WeighingMS.Simulators.Host` (:5090):

- the **Dummy OrgaTEX**: batch queue, batch, recipe, and a consumption receiver;
- the **Dummy Scale API**: a Precision scale and a Heavy-Duty scale, each with weighing scenarios.

Both serve the B-24187 training seed over HTTP. These are training contracts, not claims about the real OrgaTEX or the real devices (FR-25, FR-26).

Workbook: STEP 16 (dummy integration layer), STEP 17 (seed), STEP 18 (test the dummy APIs).

### Stories and requirements in scope

1. `001-dummy-orgatex-serves-batches-and-recipes`: FR-25, FR-1, FR-3
2. `002-dummy-orgatex-receives-consumption`: FR-25, FR-20
3. `003-dummy-scales-with-scenarios`: FR-26, FR-9, FR-7

### What will change

The repository goes from *pre-code* to a buildable .NET 10 solution:

1. **Solution scaffold**, following the coding-standards layout:
   - `WeighingMS.sln`, plus `global.json` pinning SDK 10.0.101 with `rollForward: latestFeature`.
   - `Directory.Build.props`, containing: `Nullable`, `TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`, `ImplicitUsings`.
   - `Directory.Packages.props`, which turns on Central Package Management.
   - `.editorconfig`.
   - Additions to `.gitignore`: `bin/`, `obj/`, `.env`, `TestResults/`.
   - The source projects:
     - `WeighingMS.Domain`, `WeighingMS.Application` and `WeighingMS.Infrastructure`: empty shells with the dependency rule wired through project references.
     - `WeighingMS.Api`: a minimal host with `/health/live`, OpenAPI, ProblemDetails and Serilog. It has no business endpoints yet; those arrive with bolts 003 onwards.
     - `WeighingMS.Simulators.Host`: the dummies (detailed below).
   - Two test projects:
     - `WeighingMS.Simulators.Tests`: contract tests.
     - `WeighingMS.Architecture.Tests`: checks that Domain has no dependency on Infrastructure, ASP.NET or EF Core.
2. **The Dummy OrgaTEX inside Simulators.Host**: controllers, a seed loader that reads JSON at start-up, and an in-memory store of received consumption.
3. **The Dummy Scales inside Simulators.Host**:
   - a scale registry with capacity and resolution for each scale;
   - an engine that drives each scale through its scenario over time, using an injected `TimeProvider`;
   - tare, where net = gross − tare;
   - every reading rounded with `decimal` to the scale's resolution.
4. **Docker Compose**: the `postgres` service for development, and a `simulators` service under the `full` profile. Also a `.env.example` file.
5. **Root `package.json` scripts and `CLAUDE.md`**:
   - add `sim` (runs the simulator host) and `test:backend`;
   - replace the *planned* command table in `CLAUDE.md` with the real commands for what exists after this bolt.

### Files to be created / modified

**Created:**
- Root files: `WeighingMS.sln`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `docker-compose.yml`, `.env.example`
- `src/WeighingMS.Domain/WeighingMS.Domain.csproj`
- `src/WeighingMS.Application/WeighingMS.Application.csproj`
- `src/WeighingMS.Infrastructure/WeighingMS.Infrastructure.csproj`
- `src/WeighingMS.Api/` containing `WeighingMS.Api.csproj`, `Program.cs` and `appsettings*.json`
- `src/WeighingMS.Simulators.Host/`, containing:
  - `WeighingMS.Simulators.Host.csproj`, `Program.cs`, `appsettings*.json`
  - `Seed/seed.json`: the B-24187 training scenario
  - `OrgaTex/`: `BatchesController`, the seed models and store, `ConsumptionStore`, and the `/_sim/orgatex/consumption` inspection controller
  - `Scales/`: `ScalesController`, `ScaleRegistry`, `ScaleScenarioEngine`, `ScenarioController` for `/_sim/scales/{scaleId}/scenario`, and the reading contracts
- `tests/WeighingMS.Simulators.Tests/`, with test classes per area: OrgaTEX batches, consumption, scales, scenarios
- `tests/WeighingMS.Architecture.Tests/`, with the layer-dependency tests

**Modified:**
- `.gitignore`
- `package.json` (scripts)
- `CLAUDE.md` (command table)
- `memory-bank/bolts/001-station-simulators/bolt.md` (stage tracking)
- the construction log

**Evidence:**
- `evidence/STEP-17-dummy-data.json`: a copy of the seed
- `evidence/STEP-18-api-tests.txt`: curl transcript

These are captured in the Test stage.

### APIs involved (all served by Simulators.Host :5090; workbook STEP 16 contract)

| Method | Path | Purpose | Story |
|---|---|---|---|
| GET | `/api/batches` | Queue: batchId, orderId, machine, fabric, batchWeightKg, shade, componentCount, status | 001 |
| GET | `/api/batches/{batchId}` | One batch (404 ProblemDetails `Batch.NotFound` if unknown) | 001 |
| GET | `/api/batches/{batchId}/recipe` | Ordered components, each with: sequence, chemicalId, code, name, displayColour, targetQuantity, unit, tolerance, suggestedContainer | 001 |
| POST | `/api/batches/{batchId}/consumption` | Requires an `Idempotency-Key` header. Body: one line per chemical with chemicalId, actualQuantity, unit, lot. Returns **200 OK**; a repeated key returns the original result without storing the post again | 002 |
| GET | `/_sim/orgatex/consumption` | Lists each received post once, with its key and time | 002 |
| GET | `/api/scales` | `scale-3kg` (Precision, capacity 3 kg, resolution 0.1 g) and `scale-32kg` (Heavy-Duty, 32 kg, resolution 0.001 kg) | 003 |
| GET | `/api/scales/{scaleId}/status` | READY / OFFLINE (offline only through faults, which arrive in bolt 002) | 003 |
| GET | `/api/scales/{scaleId}/weight` | net, unit, stable, overCapacity, timestamp, rounded to the scale's resolution | 003 |
| POST | `/api/scales/{scaleId}/tare` | Zeroes the net reading and returns the tare value | 003 |
| POST | `/_sim/scales/{scaleId}/scenario` | Body: `{ scenario: rising \| belowRange \| enterRange \| overshoot \| stable, target: {value, unit}, tolerance }` | 003 |

Contract details:
- Every error is ProblemDetails (RFC 9457) with a `type` of the form `Area.Reason`.
- JSON uses camelCase. Quantities are JSON numbers produced from C# `decimal`, never `double`.
- `/_sim/*` is mapped only in the Development and Test environments.

**Scenario semantics.** These are deterministic and time-based, using `TimeProvider`, so tests can drive them with `FakeTimeProvider`:

| Scenario | Behaviour | Reading where it settles |
|---|---|---|
| `rising` | Pours fast at first, then slows (shape like the mockup's pour), unstable while moving | Lands on target, then stable |
| `belowRange` | Rises, then stops | target − 2 × tolerance, stable |
| `enterRange` | Rises, then stops | target + 0.4 × tolerance, stable |
| `overshoot` | Rises, then stops | target + 4.8 × tolerance, stable |
| `stable` | Holds the current reading | Current reading, stable |

The `enterRange` and `overshoot` landing points are the ones the mockup's simulate buttons use (L804, L808). They are **simulator behaviour only**: no tolerance class is computed here (that is bolt 007).

### Dependencies

- **.NET SDK 10.0.101** (installed) and **Docker 27.5** (installed, for compose `postgres`).
- **Node 26.13.0 via nvm** for the npm scripts. The current shell resolves Homebrew node 24.4.1, so the commands will run `nvm use` first.
- **NuGet packages**, pinned in `Directory.Packages.props`:
  - `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`, `Serilog.AspNetCore`
  - `Microsoft.Extensions.TimeProvider.Testing`
  - for the tests: `xunit.v3`, `Shouldly`, `Microsoft.AspNetCore.Mvc.Testing`, `NetArchTest.Rules`
  - analyzers: `Roslynator.Analyzers`, `SonarAnalyzer.CSharp`
- **No other unit is needed.** This is the first bolt.

### Technical approach

- **Simulators.Host** is a separate ASP.NET Core Controllers project. It never references Domain, Application or Infrastructure; the dummies stay independent of business logic (ADR-003, PDF "replaceable mocks").
- **The seed** is JSON loaded through the options pattern and checked with `ValidateOnStart`, so an invalid seed stops start-up.
- **The store** holds the seed in memory, immutable per run. A reset arrives with `/_sim/reset` in bolt 002.
- **Idempotency** uses an in-memory dictionary keyed by `Idempotency-Key` that returns the first result.
- **Scale model**: gross = container tare + poured amount. The scenario engine computes the poured amount from the elapsed time since the scenario started.
- **Rounding**: `decimal` rounding (`MidpointRounding.AwayFromZero`) to the scale resolution. Over capacity sets `overCapacity: true` and caps the reading at capacity.
- **Api host**: minimal only, so the solution builds and `/health/live` answers. No business endpoints yet.
- **OpenTelemetry** in both hosts is **deferred to bolt 003**, which is the first bolt with a real Api→Simulators call. That keeps this bolt focused; the deviation from tech-stack's "Bolt 1" note is recorded here.
- **The Angular workspace** (`web/`) is **deferred to bolt 006**, the first UI bolt. Scaffolding it now would ship an empty app with nothing to show. The deviation from bolt.md's "web app" note is recorded here.

### Decision at this checkpoint: resolved (CHECKPOINT 5, 2026-11-05): option 3, training recipes for all

The recipes for B-24188 (the Teal 4C chemicals scaled by 520/480), B-24190 (7 components, including one of exactly 3.000 kg to show the inclusive threshold) and B-24191 (4 components) are **invented for training** and flagged `trainingOnly: true` in the seed (SEED-01).

#### Options considered

**Seed recipes for B-24188, B-24190 and B-24191.**

The mockup gives these three only as queue rows:
- B-24188: SO-9931, JET-07, 520 kg, Teal 4C, 5 components
- B-24190: SO-9944, JET-02, 310 kg, Navy 12, 7 components
- B-24191: SO-9947, WINCH-01, 190 kg, Rose 08, 4 components

It gives no recipe for any of them, and the requirements say nothing about their recipes. I won't invent one silently.

1 - **(Recommended)** Seed the queue rows exactly as the mockup shows them. Serve a recipe **only for B-24187**. For the other three, `GET /recipe` returns 404 `Batch.RecipeNotAvailable`, which also gives a natural "recipe can't be loaded" case for FR-3.
2 - Also give B-24188 the Teal 4C recipe, scaled by batch weight (520 / 480). The arithmetic is derived, not sourced. B-24190 and B-24191 still return 404.
3 - Write training recipes for all three. Their chemicals and quantities would be invented, and marked training-only.

### Acceptance criteria (from the stories)

- [ ] `GET /api/batches` returns B-24187, B-24188, B-24190 and B-24191 with order, machine, fabric, shade, component count and status. Removing a batch from the seed removes it from the response.
- [ ] `GET /api/batches/B-24187/recipe` returns 5 components in order:
  - Blue RSPL 1240.0 g ± 6.0
  - Red RB 386.0 g ± 2.0
  - Yellow RGB 94.0 g ± 0.5
  - Sodium Sulphate 24.000 kg ± 0.120
  - Soda Ash 9.600 kg ± 0.048

  Each component also carries its suggested container: C-09, C-11, C-12, D-03 or D-05.
- [ ] An unknown batch returns 404 ProblemDetails.
- [ ] `POST /api/batches/B-24187/consumption` with an `Idempotency-Key` returns 200 OK and stores the post. A repeated key stores nothing new. A missing key returns 400. An unknown batch returns 404.
- [ ] `GET /_sim/orgatex/consumption` lists each received post once.
- [ ] `GET /api/scales` lists the Precision 3 kg scale (0.1 g resolution) and the Heavy-Duty 32 kg scale (0.001 kg resolution).
- [ ] Each scenario produces the expected sequence and the reading it settles at (table above). Every reading respects the scale's resolution and carries value, unit, stable flag and timestamp.
- [ ] Tare returns the net reading to zero and reports the tare value. Status reports READY.
- [ ] `/_sim/*` is absent outside Development/Test.
- [ ] `dotnet build` passes with zero warnings, and `dotnet format --verify-no-changes` passes.
- [ ] The architecture test passes: Domain has no forbidden references.

### How this will be tested (Test stage)

1. **Contract tests** in `WeighingMS.Simulators.Tests`, using xUnit v3 + Shouldly + `WebApplicationFactory<Program>`, cover:
   - the seed content and order;
   - 404 for an unknown batch;
   - consumption: 200, idempotent replay, 400 without a key, and the inspection list;
   - the scale list;
   - each of the five scenarios, driven by `FakeTimeProvider` (`[Theory]` tables), including resolution rounding;
   - tare;
   - over capacity;
   - `/_sim/*` absent when the environment is Production.
2. **Architecture tests** in `WeighingMS.Architecture.Tests`: the layer rules.
3. **Manual check (STEP 18):** run `npm run sim`, then `curl` `/api/batches`, `/api/batches/B-24187` and `/api/batches/B-24187/recipe`. Save the transcript to `evidence/STEP-18-api-tests.txt`, and copy the seed to `evidence/STEP-17-dummy-data.json`.
4. **Results** go in `test-walkthrough.md`.

### Out of scope (later bolts)

- Scanner, printer, fault injection and `/_sim/reset`: bolt 002.
- Any WeighingMS Api business endpoint, sign-in or OpenTelemetry: bolt 003 onwards.
- Tolerance classification: bolt 007.
- The Angular app: bolt 006.
