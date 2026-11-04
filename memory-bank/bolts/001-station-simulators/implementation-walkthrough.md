---
stage: implement
bolt: 001-station-simulators
created: 2026-11-04T18:25:00Z
---

## Implementation Walkthrough: station-simulators

### Summary

The repository now builds as a .NET 10 solution. It contains five source projects in the Clean modular-monolith layout and a working `WeighingMS.Simulators.Host`. That host serves the Dummy OrgaTEX and the two dummy scales over HTTP on port 5090:
- Dummy OrgaTEX: batch queue, batch, recipe, consumption receiver;
- dummy scales: list, status, weight, tare, and five timed weighing scenarios.

All of it is driven by a JSON training seed.

### Structure Overview

The **simulator host** is a separate ASP.NET Core Controllers process. It never references the WeighingMS business projects. It is organised by area:
- **Seed**: loading and validating the training data;
- **OrgaTex**: the batch, recipe and consumption contracts;
- **Scales**: the scale contracts and a time-driven scale simulator;
- **Common**: shared helpers.

Simulation-control routes under `/_sim/*` are removed at start-up unless the environment is Development or Test.

**Domain, Application and Infrastructure** are empty shells for now. Each holds only an assembly marker, and the project references already enforce the dependency rule. **WeighingMS.Api** is a minimal host with health, OpenAPI, ProblemDetails and Serilog.

### Completed Work

- [x] `WeighingMS.sln`, `global.json` - solution and pinned SDK (10.0.101)
- [x] `Directory.Build.props` - shared strict build settings: nullable, warnings as errors, latest-recommended analysis, Roslynator and Sonar analyzers
- [x] `Directory.Packages.props` - Central Package Management with every version pinned
- [x] `.editorconfig` - formatting and naming rules from coding-standards
- [x] `.gitignore`, `.dockerignore`, `.env.example` - build output, secrets and container context rules
- [x] `docker-compose.yml` - `postgres` for development; `simulators` under the `full` profile
- [x] `package.json` - npm scripts `sim`, `api`, `build:backend`, `test:backend`, `format:backend`
- [x] `CLAUDE.md` - command table replaced with the real commands; planned ones moved to a "later bolts" line
- [x] `src/WeighingMS.Domain/`, `src/WeighingMS.Application/`, `src/WeighingMS.Infrastructure/` - layer projects with assembly markers and the dependency direction wired
- [x] `src/WeighingMS.Api/Program.cs` - Api host: `/health/live`, OpenAPI with Scalar in Development, ProblemDetails, Serilog; listens on :5080
- [x] `src/WeighingMS.Simulators.Host/Program.cs` - simulator host composition: options validated on start, eager seed load, simulation-control gating, camelCase enum JSON
- [x] `src/WeighingMS.Simulators.Host/Seed/seed.json` - training seed:
  - B-24187, taken from the mockup;
  - training-only recipes for B-24188, B-24190 and B-24191 (SEED-01);
  - two scales.
- [x] `src/WeighingMS.Simulators.Host/Seed/*` - seed records, simulator options, and a loader that rejects an invalid seed
- [x] `src/WeighingMS.Simulators.Host/OrgaTex/BatchesController.cs` - `GET /api/batches`, `GET /api/batches/{id}`, `GET /api/batches/{id}/recipe`, `POST /api/batches/{id}/consumption`
- [x] `src/WeighingMS.Simulators.Host/OrgaTex/ConsumptionStore.cs` - idempotent store of received consumption, keyed by `Idempotency-Key`
- [x] `src/WeighingMS.Simulators.Host/OrgaTex/OrgatexInspectionController.cs` - `GET /_sim/orgatex/consumption`
- [x] `src/WeighingMS.Simulators.Host/OrgaTex/*Response.cs`, `*Request.cs` - training contracts
- [x] `src/WeighingMS.Simulators.Host/Scales/ScaleSimulator.cs` - per-scale state:
  - gross = container + poured amount, and net = gross − tare;
  - easing pour, so each scenario lands on its own point;
  - rounding to the scale's resolution;
  - over-capacity flag.
- [x] `src/WeighingMS.Simulators.Host/Scales/ScalesController.cs` - `GET /api/scales`, `/status`, `/weight`, `POST /tare`
- [x] `src/WeighingMS.Simulators.Host/Scales/ScaleScenarioController.cs` - `POST /_sim/scales/{scaleId}/scenario`
- [x] `src/WeighingMS.Simulators.Host/Common/*` - ProblemDetails helper (`Area.Reason` codes), g/kg conversion and rounding, simulation-control attribute and feature provider
- [x] `src/WeighingMS.Simulators.Host/Dockerfile` - multi-stage image for the `full` profile

### Key Decisions

- **Seed recipes (SEED-01, CHECKPOINT 5)**: B-24188, B-24190 and B-24191 have training recipes invented for the lab and flagged `trainingOnly`.
  - B-24188 is the Teal 4C recipe scaled by 520/480.
  - B-24190 includes Remazol Black B at exactly 3.000 kg, to show the inclusive ≤ 3 kg threshold.
  - The decision is recorded in `decision-index.md`.
- **Precision scale capacity = 3200 g, checked against the net reading**: the 3 kg nominal capacity leaves no room to read a 3.000 kg target plus tolerance. The headroom is a simulator-only choice. The 3 kg *selection threshold* belongs to WeighingMS configuration (bolt 007), not to the simulator.
- **Scenario landing points**: `enterRange` lands at target + 0.4 × tolerance and `overshoot` at target + 4.8 × tolerance, as in the mockup's simulate buttons. `belowRange` lands at target − 2 × tolerance. The simulator never classifies a reading.
- **Time-driven, deterministic scenarios**: the pour progress is computed from `TimeProvider`, so tests can step time exactly with `FakeTimeProvider`.
- **Consumption returns 200 OK**, as the workbook expects at STEP 26.
  - A repeated `Idempotency-Key` returns the original receipt.
  - Reusing a key for a different batch returns 409 `Consumption.IdempotencyKeyConflict`.
- **Validation attributes sit on record constructor parameters**: MVC rejects them on record properties. The smoke test caught this (500 on the scenario endpoint), and it is fixed.

### Deviations from Plan

- **`Microsoft.AspNetCore.OpenApi` pinned to 10.0.14, not 10.0.1**: the build's NuGet audit rejected 10.0.1 because it pulls in `Microsoft.OpenApi` 2.0.0, which has a high-severity advisory (GHSA-v5pm-xwqc-g5wc). `Microsoft.AspNetCore.Mvc.Testing` is aligned to 10.0.14.
- **Test projects are not created yet**: per the Simple-bolt stages they belong to Stage 3 (Test). This covers `WeighingMS.Simulators.Tests` and `WeighingMS.Architecture.Tests`.
- **OpenTelemetry** (to bolt 003) and the **Angular `web/` workspace** (to bolt 006) remain deferred, as recorded in the plan.

### Dependencies Added

- [x] `Microsoft.AspNetCore.OpenApi` 10.0.14 - OpenAPI document
- [x] `Scalar.AspNetCore` 2.18.7 - API reference UI in Development
- [x] `Serilog.AspNetCore` 10.0.0 - structured logging
- [x] `Roslynator.Analyzers` 5.0.1, `SonarAnalyzer.CSharp` 10.35.0.4138 - static analysis (coding-standards)
- [x] Testing packages pinned but not referenced until Stage 3: `xunit.v3`, `xunit.runner.visualstudio`, `Shouldly`, `Microsoft.NET.Test.Sdk`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Extensions.TimeProvider.Testing`, `NetArchTest.Rules`

### Developer Notes

- `npm run sim` starts the dummies on http://localhost:5090. The OpenAPI document and the Scalar UI are available in Development.
- A scale reads the empty container (214.0 g / 1.850 kg) until it is tared. Tare first, then start a scenario.
- The `simulators` container image builds from the repository root. It has not been built yet; the Test stage builds it once.
- Verification so far:
  - `dotnet build`: 0 warnings, 0 errors.
  - `dotnet format --verify-no-changes`: clean.
  - Manual smoke test of every endpoint, which found and fixed the record-validation bug.
