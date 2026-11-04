---
stage: test
bolt: 001-station-simulators
created: 2026-11-04T18:40:00Z
---

## Test Report: station-simulators

### Summary

- **Tests**: 72/72 passed: 63 in `WeighingMS.Simulators.Tests` and 9 in `WeighingMS.Architecture.Tests`. Command: `dotnet test --solution WeighingMS.sln`.
- **Coverage**: 94.4% of the hand-written lines in `WeighingMS.Simulators.Host` (770/816), measured with Microsoft Testing Platform code coverage (Cobertura).
  - The raw figure the tool reports is 48.7%. It includes a 776-line file in `obj/` that the build's OpenAPI source generator produces (`OpenApiXmlCommentSupport.generated.cs`); that is not our code.
- **Build**: `dotnet build` passes with 0 warnings and 0 errors (warnings are errors).
- **Format**: `dotnet format --verify-no-changes` is clean.

### Test Files

- [x] `tests/WeighingMS.Simulators.Tests/BatchQueueTests.cs` - the four seed batches in order, the B-24187 mockup fields, the training-only flags and the mockup rows for B-24188/90/91, and 404 `Batch.NotFound`
- [x] `tests/WeighingMS.Simulators.Tests/RecipeTests.cs` - the B-24187 recipe sequence, and each component's exact target, unit, tolerance and container (Theory); B-24190 has a component of exactly 3.000 kg; unknown batch → 404
- [x] `tests/WeighingMS.Simulators.Tests/ConsumptionTests.cs` - 200 OK and the receipt; a repeated key returns the original receipt and stores it once; missing key → 400; unknown batch → 404; a key reused on another batch → 409; empty lines → 400; the inspection list
- [x] `tests/WeighingMS.Simulators.Tests/ScaleTests.cs` - scale list (class, unit, capacity, resolution), READY status, an untared reading equals the empty container, tare → net 0, unknown scale → 404
- [x] `tests/WeighingMS.Simulators.Tests/ScaleScenarioTests.cs` - scenario landing points for Yellow RGB 94.0 ± 0.5 g and Sodium Sulphate 24.000 ± 0.120 kg; mid-pour readings are unstable, rising and on the 0.1 g grid; `stable` holds the reading; a kg target is converted to g; a missing tolerance → 400 `Scenario.Invalid`; over capacity is flagged and capped
- [x] `tests/WeighingMS.Simulators.Tests/SimulationControlTests.cs` - in Production, `/_sim/*` returns 404 while `/api/*` is still served; the simulator references no WeighingMS business assembly
- [x] `tests/WeighingMS.Simulators.Tests/SeedLoaderTests.cs` - the repository seed loads; a missing file, duplicate batch, non-positive or unknown-unit component, duplicate sequence and missing scales are all rejected
- [x] `tests/WeighingMS.Simulators.Tests/MassUnitsTests.cs` - exact g↔kg conversion; rounding to resolution with midpoints away from zero; invalid input throws
- [x] `tests/WeighingMS.Simulators.Tests/SimulatorFactory.cs` - in-process host (`WebApplicationFactory<Program>`) with a `FakeTimeProvider`, in a Test or Production environment
- [x] `tests/WeighingMS.Architecture.Tests/LayerDependencyTests.cs` - Domain has no dependency on Application, Infrastructure, Api, ASP.NET Core or EF Core; Application has none on Infrastructure or Api; Infrastructure has none on Api; no business assembly references the simulator host

### Acceptance Criteria Validation

- ✅ **The queue returns B-24187, B-24188, B-24190 and B-24191 with their fields**: `BatchQueueTests`. Removing a batch from the seed removes it, because the response is built only from `seed.json` (there is no other data source).
- ✅ **B-24187 recipe**: 5 components in order, with mockup targets, tolerances and suggested containers (`RecipeTests`).
- ✅ **Unknown batch → 404 ProblemDetails** (`BatchQueueTests`, `RecipeTests`).
- ✅ **Consumption**: 200 OK; a repeated key stores nothing new; missing key → 400; unknown batch → 404 (`ConsumptionTests`). Also checked live in STEP 18.
- ✅ **`/_sim/orgatex/consumption` lists each post once** (`ConsumptionTests`).
- ✅ **Scale list**: Precision 3 kg nominal at 0.1 g; Heavy-Duty 32 kg at 0.001 kg (`ScaleTests`).
- ✅ **Scenarios land as specified, at the scale resolution, with value, unit, stable flag and timestamp** (`ScaleScenarioTests`).
- ✅ **Tare zeroes the net reading and reports the tare; status is READY** (`ScaleTests`).
- ✅ **`/_sim/*` is absent outside Development/Test** (`SimulationControlTests`).
- ✅ **`dotnet build` passes with 0 warnings, and the format check is clean.**
- ✅ **Architecture rule holds** (`LayerDependencyTests`).

### Issues Found

1. **`dotnet test` failed on .NET 10 with the VSTest runner.** Fixed by opting into Microsoft.Testing.Platform in `global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`). The npm script and the `CLAUDE.md` commands now use `dotnet test --solution …`, `--filter-class` and `--filter-method`.
2. **Private static fields in the tests broke the `_camelCase` naming rule.** The format check caught this, and the fields were renamed.
3. **The Docker image was not built.** `docker compose --profile full build simulators` could not reach the Docker daemon: OrbStack's socket is not running. The Dockerfile and Compose file pass `docker compose config`, but the image build is **unverified**. Start Docker or OrbStack and run that command once.

### Notes

- STEP 17 evidence: `evidence/STEP-17-dummy-data.json` is a copy of `Seed/seed.json`.
- STEP 18 evidence: `evidence/STEP-18-api-tests.txt` is a live curl transcript against `npm run sim`. It covers:
  - the queue, B-24187 and its recipe;
  - an unknown batch → 404;
  - the scales, status, the untared reading and tare;
  - the `enterRange` scenario: mid-pour 55.0 g unstable, then settled 94.2 g stable;
  - consumption 200 OK, its replay with the same receipt, and the inspection list.
- Added test packages, versions pinned centrally:
  - `Microsoft.Testing.Extensions.CodeCoverage` 18.12.0, for coverage;
  - xUnit v3, Shouldly, Mvc.Testing 10.0.14, TimeProvider.Testing and NetArchTest, as planned.
- Test-only analyzer relaxations are in `tests/Directory.Build.props`: CA1707 (underscored test names, as the coding standards require), CA1515 and CA2007.
