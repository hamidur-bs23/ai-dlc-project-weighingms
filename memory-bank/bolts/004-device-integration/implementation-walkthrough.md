---
stage: implement
bolt: 004-device-integration
created: 2026-11-04T22:20:00Z
---

## Implementation Walkthrough: device-integration (bolt 004)

### Summary

WeighingMS.Api now reaches the station devices through ports and resilient adapters. It serves:
- the workbook scale endpoints, with readings converted to the Api conventions (`{ value, unit }` quantities, `{ items }`) and a validity on every reading;
- a tare endpoint;
- scale and scanner status;
- scan collection by cursor;
- one device-health view for the status strip.

The Api and the simulator now emit OpenTelemetry traces and metrics, with export switched on only when a collector is configured.

### Structure Overview

- **Application** defines the device ports (`IScaleDevice`, `IScanner`, `IDeviceHealthProbe`) and these pieces:
  - a pure validity rule (`ScaleReadingRules`);
  - a tracker that remembers each device's last status and when it changed (the "since" time);
  - `DeviceService`, the use cases;
  - the `WeighingMS` activity source and meter.
- **Infrastructure** holds typed HttpClients to the simulator. Each sits behind a resilience pipeline (retry only for safe status reads, circuit breaker, attempt timeout). Every transport failure is translated into an `Area.Unavailable` error at the adapter.
- **The Api controllers** map the use cases to HTTP, and errors to ProblemDetails, through the shared base controller.

### Completed Work

- [x] `src/WeighingMS.Application/Devices/IScaleDevice.cs`, `IScanner.cs`, `IDeviceHealthProbe.cs` - device ports (boundary B1)
- [x] `src/WeighingMS.Application/Devices/ScaleReadingRules.cs` - the single place validity is decided: malformed, then over capacity, then below the zero band, otherwise valid
- [x] `src/WeighingMS.Application/Devices/DeviceHealthTracker.cs` - last status and since-time per device (also gives the scale's `onlineSince`)
- [x] `src/WeighingMS.Application/Devices/DeviceService.cs` - list, status, classified read (records offline and traces failed reads), tare, scans with cursor, scanner status, aggregated health
- [x] `src/WeighingMS.Application/Devices/*` (records, enums, `DeviceErrors`, `DevicesOptions`) - contracts, `Scale.Unavailable`, `Scale.NotFound` and `Scanner.Unavailable` errors, configuration
- [x] `src/WeighingMS.Application/Abstractions/WmsTelemetry.cs` - `ActivitySource` and `Meter` "WeighingMS", histogram `wms.scale.read.duration`
- [x] `src/WeighingMS.Infrastructure/Integrations/IntegrationCall.cs` - translates connection errors, timeouts, an open circuit and 5xx into unavailable errors, and 404 into not found
- [x] `src/WeighingMS.Infrastructure/Integrations/IntegrationsRegistration.cs` - typed clients and resilience (retry only for GETs that aren't weight polls, circuit breaker, attempt timeout)
- [x] `src/WeighingMS.Infrastructure/Integrations/Scales/ScaleDeviceClient.cs` - Dummy Scale API adapter; malformed JSON becomes a malformed reading
- [x] `src/WeighingMS.Infrastructure/Integrations/Scanner/ScannerClient.cs` - scanner cursor adapter
- [x] `src/WeighingMS.Infrastructure/Integrations/Health/PrinterStatusProbe.cs`, `OrgatexReachabilityProbe.cs` - status-strip probes
- [x] `src/WeighingMS.Infrastructure/Integrations/SimulatorIntegrationOptions.cs` - base URL, attempt timeout, circuit-breaker settings
- [x] `src/WeighingMS.Api/Controllers/ScalesController.cs` - `GET /api/scales` (with `pollIntervalMs`), `/status`, `/weight`, `POST /tare`
- [x] `src/WeighingMS.Api/Controllers/ScannerController.cs` - `GET /api/scanner/scans?after=`, `GET /api/scanner/status`
- [x] `src/WeighingMS.Api/Controllers/DevicesController.cs` - `GET /api/devices/health`
- [x] `src/WeighingMS.Api/Contracts/Devices/*` - wire contracts (`QuantityResponse`, collections as `items`)
- [x] `src/WeighingMS.Api/Common/ObservabilityExtensions.cs` - OpenTelemetry tracing (ASP.NET Core, HttpClient, Npgsql) and metrics; weight polls filtered; OTLP export only when configured
- [x] `src/WeighingMS.Api/Common/WireNames.cs`, `RequestLogLevels.cs` - camelCase enum wire names; 503 logged as Warning
- [x] Modified `src/WeighingMS.Simulators.Host/Program.cs` - OpenTelemetry (`weighingms-simulators`), weight polls filtered
- [x] Modified `src/WeighingMS.Api/Program.cs`, `appsettings.json` - device options, telemetry, request log levels, `Integrations:Simulators`, zero bands, quieter HttpClient and Polly logging
- [x] Modified `Directory.Packages.props` and project files - resilience and OpenTelemetry packages

### Key Decisions

- **Validity is reported, not raised.** An over-capacity, below-zero or malformed reading returns 200 with its `validity`. It is a measurement, so the UI can show "Over capacity" while Confirm stays disabled (bolt 007). Only a scale that can't be reached returns 503.
- **Zero bands** come from configuration: 0.5 g for `scale-3kg`, 0.002 kg for `scale-32kg`. A scale without an entry gets 0. **Over capacity** relies on the device's flag, because the simulator caps the reading at capacity.
- **`onlineSince`** is published on every reading. Bolt 008 uses it to require a new tare after a reconnect, which keeps weighing rules out of the device layer.
- **Resilience**:
  - attempt timeout of 1 s, so a simulator `timeout` fault (a 30 s hold) is reported as offline after about 1 s;
  - a `slow` fault (2 s) also counts as offline for scales, because a reading that late is useless;
  - weight polls and tare are never retried; status reads get one retry;
  - circuit breaker settings are configurable (10 calls minimum, 5 s break).
- **Failed weight reads produce a `wms.scale.read` error span.** Healthy polls are filtered out of tracing and measured by `wms.scale.read.duration` instead, as coding-standards requires.
- **Log levels**: an unavailable integration (503) is a Warning, not an Error, and per-poll HttpClient information logs are turned off. The smoke test showed 19 noisy exception traces per fault burst beforehand; afterwards there were 0 ERR lines.

### Deviations from Plan

- **Wire enum `orgatex`**: the enum value is named `Orgatex`, so its wire name is `orgatex` rather than `orgaTex`.
- **Over capacity** comes from the device flag only. The simulator never reports a net above capacity, so the capacity comparison in the plan isn't needed.
- **Serilog's OTLP log sink is not added.** Logs stay on the console; OTLP log export can come with the observability bolt. Traces and metrics are exported over OTLP as planned.

### Dependencies Added

- [x] `Microsoft.Extensions.Http.Resilience` 10.11.0 - timeouts, retry, circuit breaker
- [x] `OpenTelemetry.Extensions.Hosting`, `.Instrumentation.AspNetCore`, `.Instrumentation.Http`, `.Instrumentation.Runtime`, `.Exporter.OpenTelemetryProtocol` 1.20.1 - tracing, metrics, OTLP
- [x] `Npgsql.OpenTelemetry` 10.0.3 - database spans

### Developer Notes

- **Run order**:
  1. `POSTGRES_PASSWORD=change-me docker compose up -d postgres`
  2. `npm run sim`
  3. `npm run api`
  4. Sign in as `OP-1001`.

  Then `GET /api/scales/scale-3kg/weight` works through the Api; the UI never calls :5090.
- **Smoke test results**, with all three processes running:
  - tare, then `enterRange` → 55.3 g unstable, then 94.2 g stable and valid;
  - Sodium Sulphate on the 32 kg scale → 24.000 kg;
  - over capacity → `overCapacity`;
  - unavailable fault → 503 `Scale.Unavailable` and status `offline`, with the other scale unaffected;
  - timeout fault → 503 after 1.02 s;
  - fault cleared → a new `onlineSince`;
  - scan cursor → each scan delivered once; faulted scanner → 503;
  - health lists all 5 devices, with offline ones correct;
  - no session → 401; tare without XSRF → 400.
- **Verification**: `dotnet build` 0 warnings, format clean, all 188 existing tests pass.
