---
stage: plan
bolt: 004-device-integration
created: 2026-11-04T22:00:00Z
---

## Implementation Plan: device-integration (bolt 004)

### Objective

Connect WeighingMS.Api to the station devices through ports and adapters (boundary B1):
- live scale readings (fixed-point value, unit, stable flag, validity);
- tare;
- scale and scanner status;
- scan delivery;
- one aggregated **device health** view for the status strip.

The Api translates the simulator's foreign shapes into WeighingMS conventions (decision SIM-CONTRACTS):
- `{ value, unit }` quantities;
- `{ items }` collections;
- `Area.Reason` ProblemDetails, including 503 `Scale.Unavailable`.

This is the first Api → simulator call, so **OpenTelemetry** is added here too (decision OTEL-TIMING).

Workbook: STEP 21 (connect the scale API; `GET /api/scales/scale-3kg/weight`); preparation for STEP 30 (scale and scanner unavailable).

### Stories in scope

1. `001-live-scale-reading` (FR-9, ADR-004)
2. `002-tare-a-scale` (FR-7)
3. `003-scale-offline-and-invalid-readings` (FR-23)
4. `004-scan-delivery-to-station` (FR-27, FR-2, FR-5)
5. `005-scanner-offline-manual-fallback` (FR-24 b)
6. `006-device-health-for-status-strip` (FR-23, FR-24)

### What will change

1. **Application** (`Application/Devices`):
   - ports `IScaleDevice` (list, status, read, tare), `IScanner` (scans after a cursor, status) and `IDeviceHealthProbe`;
   - `ScaleReadingRules`, a pure function that classifies a raw reading as **valid / overCapacity / belowZero / malformed**, using a per-scale zero band from configuration;
   - `DeviceHealthService`, which runs every probe and tracks **since-times** (when each device last changed state) with `TimeProvider`;
   - `DevicesOptions`: poll interval (default 250 ms), zero band per scale, timeouts;
   - errors `Scale.Unavailable` (503), `Scale.NotFound` (404), `Scanner.Unavailable` (503).
2. **Infrastructure** (`Integrations/Scales`, `/Scanner`, `/Health`):
   - typed HttpClients for the simulator (`Integrations:Simulators:BaseUrl`) with **resilience**, using `Microsoft.Extensions.Http.Resilience`:
     - scale reads get a short attempt timeout and no retry, because the next poll is the retry;
     - status calls get one retry;
     - tare (POST) gets no retry;
     - every client gets a circuit breaker;
   - the adapters **translate** HTTP 503, 500, timeouts, connection errors and an open circuit into `IntegrationErrors.*Unavailable`, so `HttpRequestException` never leaks upward;
   - malformed JSON becomes a `malformed` reading;
   - health probes for scales and the scanner, plus two light probes for status-strip completeness:
     - **printer** status via `GET /api/printer/status`;
     - **OrgaTEX** reachability via `GET /api/batches`.

     The full printer and OrgaTEX adapters belong to bolts 012 and 005.
3. **Api**:
   - `ScalesController` keeps the workbook paths: `GET /api/scales`, `GET /api/scales/{scaleId}/status`, `GET /api/scales/{scaleId}/weight`, `POST /api/scales/{scaleId}/tare`;
   - `ScannerController`: `GET /api/scanner/scans?after={sequence}` and `GET /api/scanner/status`;
   - `DevicesController`: `GET /api/devices/health`;
   - every endpoint requires a signed-in operator; tare also requires the XSRF token.
4. **OpenTelemetry** in Api and Simulators.Host:
   - tracing for ASP.NET Core, HttpClient and Npgsql (Api);
   - metrics for ASP.NET Core, HttpClient and runtime;
   - a custom `ActivitySource("WeighingMS")` and `Meter("WeighingMS")` with the histogram `wms.scale.read.duration`;
   - **healthy scale-poll requests are not traced** (coding-standards); failed polls are;
   - the OTLP exporter is enabled **only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set**, so development without a collector stays quiet;
   - service names `weighingms-api` and `weighingms-simulators`;
   - W3C trace context flows Api → simulator.

   The Grafana and Collector stack remains in the separate observability bolt.

### Files to be created / modified

- **Created**:
  - `src/WeighingMS.Application/Devices/*` (ports, records, `ScaleReadingRules`, `DeviceHealthService`, `DevicesOptions`, `DeviceErrors`)
  - `src/WeighingMS.Infrastructure/Integrations/Scales/ScaleDeviceClient.cs`
  - `src/WeighingMS.Infrastructure/Integrations/Scanner/ScannerClient.cs`
  - `src/WeighingMS.Infrastructure/Integrations/Health/*Probe.cs`
  - `src/WeighingMS.Infrastructure/Integrations/SimulatorOptions.cs`
  - `src/WeighingMS.Infrastructure/Observability/Telemetry.cs`
  - `src/WeighingMS.Api/Controllers/ScalesController.cs`, `ScannerController.cs`, `DevicesController.cs`
  - `src/WeighingMS.Api/Contracts/Devices/*`
  - `src/WeighingMS.Api/Common/ObservabilityExtensions.cs`
  - Tests (Stage 3): new classes in `WeighingMS.Application.Tests` and `WeighingMS.Api.IntegrationTests`
- **Modified**:
  - the Api and Simulators `Program.cs` (OpenTelemetry, typed clients)
  - `src/WeighingMS.Infrastructure/DependencyInjection.cs`
  - the `appsettings*.json` files
  - `Directory.Packages.props`, `CLAUDE.md`

### APIs involved

**Exposed by WeighingMS.Api (:5080, to the SPA):**

| Method | Path | Response |
|---|---|---|
| GET | `/api/scales` | `{ items: [{ scaleId, name, scaleClass, capacity{value,unit}, resolution{value,unit}, status }], pollIntervalMs }` |
| GET | `/api/scales/{scaleId}/status` | `{ scaleId, status: ready \| offline, since }` |
| GET | `/api/scales/{scaleId}/weight` | `{ scaleId, net{value,unit}, stable, validity: valid \| overCapacity \| belowZero \| malformed, readAt, onlineSince }`, or 503 `Scale.Unavailable`, or 404 `Scale.NotFound` |
| POST | `/api/scales/{scaleId}/tare` | `{ scaleId, tare{value,unit}, taredAt }`, or 503 `Scale.Unavailable` |
| GET | `/api/scanner/scans?after={sequence}` | `{ items: [{ sequence, code, scannedAt }], cursor }`, or 503 `Scanner.Unavailable` |
| GET | `/api/scanner/status` | `{ status, since }` |
| GET | `/api/devices/health` | `{ items: [{ device, kind: scale \| scanner \| printer \| orgatex, status, since }] }` |

**Consumed from the simulator (:5090), the training contracts from bolts 001 and 002:**
- `/api/scales`, `/api/scales/{id}/status`, `/api/scales/{id}/weight`, `POST /api/scales/{id}/tare`
- `/api/scanner/scans`, `/api/scanner/status`
- `/api/printer/status`
- `/api/batches`, used only as the reachability probe

Contract notes:
- **`onlineSince`** on a reading is when the scale last came back from offline. Bolt 008 uses it to require a new tare after a reconnect: a tare older than `onlineSince` doesn't count. That meets this bolt's "the scale comes back → a new tare is required" criterion without putting weighing rules in the device layer.
- **Scan routing by screen** (batch ID on the queue screen, lot on the weigh screen) belongs to the SPA (bolts 006 and 010). This bolt delivers each scan exactly once per cursor.
- **No caching**: every read goes to the device (tech-stack).

### Dependencies

- **Bolts 001 and 002** (complete): the simulator contracts and fault injection.
- **Bolt 003** (complete): sign-in, ProblemDetails mapping, XSRF.
- **New packages**, pinned:
  - `Microsoft.Extensions.Http.Resilience`
  - `OpenTelemetry.Extensions.Hosting`
  - `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime`
  - `OpenTelemetry.Exporter.OpenTelemetryProtocol`
  - `Npgsql.OpenTelemetry`
- **Docker** must be running for the integration tests.

### Technical approach

- **Ports in Application, adapters in Infrastructure** (system-architecture). Business code never sees HTTP. The architecture tests still apply.
- **Validity is decided in one pure function** (`ScaleReadingRules`):
  - `overCapacity` comes from the device flag or from `net > capacity`;
  - `belowZero` means `net < −zeroBand`, with a configurable zero band per scale: 0.5 g for `scale-3kg`, 0.002 kg for `scale-32kg`;
  - `malformed` means a value is missing or not a number.
  
  An invalid reading is returned with HTTP 200 and its validity, because it is a measurement, not an error. Bolt 007 never enables Confirm on one.
- **Since-time tracking**: a singleton tracker records each device's last status and the time it changed. A status change is visible on the next health call, which is within one poll interval.
- **Testing the adapters against the real simulator**: the integration tests host `WeighingMS.Simulators.Host` in-process and point the Api's typed HttpClients at its test server. The adapters are then tested against the real training contracts and the real fault injection, not hand-written stubs. The two `Program` types are kept apart with an `extern alias`.

### Acceptance criteria (from the stories)

- [ ] `GET /api/scales/scale-3kg/weight` returns the simulator's reading as `{ net: { value, unit } }`, in fixed-point and at the scale's resolution, with `stable` and `readAt`.
- [ ] During a rising scenario the reading is `stable: false`, and `stable: true` once it settles.
- [ ] The poll interval (default 250 ms) is configurable and published in `GET /api/scales`.
- [ ] Tare on a READY scale returns the tare value, and the next reading is 0. Tare on a faulted scale returns 503 `Scale.Unavailable`.
- [ ] A scale that is unavailable, times out or returns 500 gives 503 `Scale.Unavailable` (never an unhandled 500). Its status reports `offline`.
- [ ] When the scale comes back, the reading's `onlineSince` moves forward.
- [ ] An over-capacity reading → `validity: overCapacity`; a reading below −zeroBand → `belowZero`; a malformed body → `malformed`.
- [ ] A scan emitted on the dummy is returned once by `GET /api/scanner/scans?after=…`, and the returned `cursor` excludes it next time. A faulted scanner gives 503 `Scanner.Unavailable` and status `offline`.
- [ ] `GET /api/devices/health` lists both scales, the scanner, the printer and OrgaTEX with status and since-time. A fault shows on the next call.
- [ ] Every device endpoint without a session → 401. Tare without XSRF → 400.
- [ ] OpenTelemetry: Api → simulator calls carry `traceparent`; healthy weight polls are not traced. With no OTLP endpoint configured, nothing is exported and nothing is logged as an error.
- [ ] Build has 0 warnings; format clean; all 188 existing tests pass.

### How this will be tested (Stage 3)

1. **Application unit tests**:
   - `ScaleReadingRules` boundary table: −0.5 g valid, −0.6 g belowZero, capacity valid, over capacity, missing value;
   - `DeviceHealthService` since-time transitions, with `FakeTimeProvider`.
2. **Api integration tests** (Testcontainers Postgres plus the in-process simulator):
   - every endpoint above, with a real scenario: tare, then `enterRange` landing at 94.2 g;
   - every fault mode for scales and the scanner, through `/_sim/faults`, giving 503 and `offline`;
   - the scale coming back gives a new `onlineSince`;
   - the cursor delivers each scan once;
   - the health aggregate, including the printer and OrgaTEX probes;
   - 401 without a session, 400 without XSRF.
3. **A tracing test**: an `ActivityListener` checks that a failed weight poll creates a span and a healthy one doesn't, and that `traceparent` reaches the simulator.
4. **A live smoke test** with `npm run sim` and `npm run api`, which also gives STEP 21 evidence: Yellow RGB on the 3 kg precision scale and Sodium Sulphate on the 32 kg floor scale.

### Out of scope

- Classifying a reading against tolerance, and choosing the scale for a component: bolt 007.
- Requiring a tare before confirm: bolt 008.
- The status-strip UI and scan routing by screen: bolts 006 and 010.
- The full printer and OrgaTEX adapters: bolts 012 and 005.
- The Grafana and Collector stack: the observability bolt.
