---
stage: test
bolt: 004-device-integration
created: 2026-11-04T22:45:00Z
---

## Test Report: device-integration (bolt 004)

### Summary

- **Tests**: 240/240 passed across the solution, the same result in every run. 52 tests are new:
  - 17 Application unit tests;
  - 15 Api integration tests against the **real simulator in-process**;
  - 3 tracing tests over **real loopback sockets**;
  - 8 resilience and adapter tests;
  - plus parameterised cases.
- **Coverage** (hand-written lines; generated code and migrations excluded):

  | Project | Coverage |
  |---|---|
  | `WeighingMS.Application` | **99.2%** (262/264) |
  | `WeighingMS.Infrastructure` | **93.7%** (281/300) |
  | `WeighingMS.Api` | **92.6%** (336/363) |
  | `WeighingMS.Domain` | unchanged; 100% in bolt 003 |

  `IntegrationCall` is at 77.8%. Its uncovered lines are the narrow branch for a caller-cancelled request.
- **Build**: 0 warnings, 0 errors. **Format**: clean.
- **Evidence**: `evidence/STEP-21-scale-api.txt`, a live run through WeighingMS.Api:
  - Yellow RGB on `scale-3kg` (3 kg precision): tare, then 55.3 g still moving, then 94.2 g stable and valid;
  - Sodium Sulphate on `scale-32kg` (32 kg floor): 24.000 kg;
  - a faulted scale: 503 `Scale.Unavailable` and `offline` in the health list.

### Test Files

- [x] `tests/WeighingMS.Application.Tests/ScaleReadingRulesTests.cs` - zero-band boundaries (−0.5 g valid, −0.6 g below zero); over capacity; missing value or unit → malformed; malformed takes priority; a negative band is treated as its size
- [x] `tests/WeighingMS.Application.Tests/DeviceHealthTrackerTests.cs` - since-time on first sighting; kept while the status is unchanged; moved on every change; unknown device
- [x] `tests/WeighingMS.Application.Tests/DeviceServiceTests.cs` - a classified reading has `onlineSince`; `onlineSince` moves forward after an outage; below zero; malformed has no net value; an unknown scale is not marked offline; the scan cursor; the health list order and status; an offline tare is recorded
- [x] `tests/WeighingMS.Api.IntegrationTests/DeviceEndpointsTests.cs`, through the Api against the real simulator:
  - the scale list as `{ value, unit }` plus `pollIntervalMs` 250;
  - **tare → 214.0 g, then net 0, then `enterRange` → unstable mid-pour, then 94.2 g stable and valid**;
  - **24.000 kg on the floor scale**;
  - over capacity reported as a validity, not an error;
  - unavailable → 503 for both read and tare, status offline, the other scale unaffected;
  - `error500` → 503, while the device status stays as the device reports it;
  - a timeout fault is cut off by the 1 s attempt timeout (well under 5 s) → 503;
  - the scale coming back moves `onlineSince` forward by exactly the elapsed time;
  - an unknown scale → 404;
  - scans delivered once per cursor; a faulted scanner → 503 and offline; a negative cursor → 422;
  - health lists all 5 devices and shows printer, OrgaTEX and scale faults on the next call, with the exact since-time;
  - 401 without a session; tare without XSRF → 400.
- [x] `tests/WeighingMS.Api.IntegrationTests/TracingTests.cs`, using an in-memory OTel exporter and the simulator on Kestrel:
  - a healthy weight poll leaves **no** span;
  - a failed read leaves a `wms.scale.read` **error** span tagged `wms.scale.id`;
  - a status call is **one trace**: Api server span → outgoing client span → the simulator's server span, which proves `traceparent` propagation.
- [x] `tests/WeighingMS.Api.IntegrationTests/ResilienceTests.cs`:
  - connection refused (nothing listening) → 503, and the health list shows everything offline;
  - **the circuit breaker opens** after repeated failures and then fails fast, even once the device is healthy again;
  - the adapter turns a malformed body or a missing value into a *malformed* reading, not an error;
  - 503, 500 and 404 are translated; a transport exception becomes `Scanner.Unavailable`.
- [x] `tests/WeighingMS.Api.IntegrationTests/SimulatorHost.cs` - the real `WeighingMS.Simulators.Host` in-process (`extern alias sim`) with its own fake clock; it can also start on a loopback port

### Acceptance Criteria Validation

- ✅ **The weight is returned as `{ net: { value, unit } }`, fixed-point and at the scale's resolution, with `stable` and `readAt`** (`DeviceEndpointsTests`).
- ✅ **Unstable while rising, stable once settled** (`TareThenEnterRange…`).
- ✅ **The poll interval is configurable (250 ms) and published** (`Scales_ListsBoth…`).
- ✅ **Tare returns the tare and the net becomes 0; a faulted scale → 503 `Scale.Unavailable`** (`DeviceEndpointsTests`).
- ✅ **Unavailable, timeout and 500 → 503, never an unhandled 500; status offline** (`DeviceEndpointsTests`, `ResilienceTests`). Status shows offline for an `unavailable` fault; with an `error500` fault the device still reports READY and the read returns 503.
- ✅ **`onlineSince` moves forward when the scale comes back** (`ScaleComingBack…`, `DeviceServiceTests`).
- ✅ **overCapacity, belowZero and malformed validities** (`ScaleReadingRulesTests`, `OverCapacity…`, `ResilienceTests`).
- ✅ **A scan is returned once per cursor; a faulted scanner → 503 and offline** (`Scanner_*`).
- ✅ **Health lists both scales, the scanner, the printer and OrgaTEX with status and since-time; faults show on the next call** (`Health_*`).
- ✅ **401 without a session; tare without XSRF → 400** (`DeviceEndpoints_WithoutSession…`, `Tare_WithoutXsrf…`).
- ✅ **OpenTelemetry: `traceparent` carried to the simulator; healthy polls not traced; failed reads traced; no export without an OTLP endpoint** (`TracingTests`; the exporter is only registered when the endpoint is set).
- ✅ **Build 0 warnings; format clean; all earlier tests still pass.**

### Issues Found (fixed in this stage)

1. **Resilience waits used the application's injected business clock.** Under the test's fake clock, retry delays never ended (the tests hung for 100 s). Resilience pipelines now use `TimeProvider.System`: timeouts, retry delays and circuit breaks are wall-clock waits on I/O. Production behaviour is unchanged, since production already used the system clock.
2. **Failed weight polls got no error span.** Filtering polls at request start also suppressed the child `wms.scale.read` span. Healthy polls are now dropped **after** they finish, by `HealthyWeightPollFilter` in the Api and the simulator. Failed polls keep their full trace.
3. **Test-harness limit**: the in-memory test server bypasses `SocketsHttpHandler`, which creates the HTTP client span and injects `traceparent`. The tracing tests therefore run the simulator on a real loopback port (Kestrel through `WebApplicationFactory.UseKestrel`).

### Notes

- Docker (OrbStack) was running; the integration tests use Testcontainers `postgres:17`.
- With an `error500` fault the simulator's status endpoint still reports READY, as specified in bolt 002 (only `unavailable` reports OFFLINE). The Api reports reads as unavailable in both cases.
- For the STEP 21 screenshots:
  - `STEP-21-scale-api.png` can be a screenshot of `evidence/STEP-21-scale-api.txt`, or of a re-run of those curl calls.
  - `STEP-21-scale-switch.png` needs the Weigh screen, which arrives in bolt 010.
