---
stage: plan
bolt: 002-station-simulators
created: 2026-11-04T19:15:00Z
---

## Implementation Plan: station-simulators (bolt 002)

### Objective

Complete the training-substitute layer in `WeighingMS.Simulators.Host`:
- add the **Dummy Scanner** and the **Dummy Printer**;
- add **fault injection** for all four dummies;
- add a **seed reset**.

With these, every STEP 30 failure scenario (scale, OrgaTEX, printer and scanner unavailable) can be reproduced on demand. These are training contracts only, not claims about real devices (FR-27, FR-28).

Workbook: STEP 16 (dummy scanner and printer); preparation for STEP 30 (failure scenarios).

### Stories and requirements in scope

1. `004-dummy-scanner-emits-scans`: FR-27 (PD-03: device-style HTTP, no keyboard wedge)
2. `005-dummy-printer-accepts-tickets`: FR-28, FR-21
3. `006-fault-injection-and-seed-reset`: FR-25 to FR-28, FR-23, FR-24

### What will change

Only `WeighingMS.Simulators.Host` and its tests change. No business project is touched.

1. **Dummy Scanner** (new `Scanner/` area). A scan is emitted by test control and queued with an increasing sequence number. WeighingMS collects scans with a **cursor**: it asks for scans after the last sequence it has seen. That makes collection safe to retry and to poll: a scan is delivered once per cursor, and nothing is lost if a poll fails. This fixes the "collection endpoint" the stories left to this bolt.
2. **Dummy Printer** (new `Printer/` area). It accepts a ticket with an `Idempotency-Key` and stores it as printed; a repeated key prints nothing new. The printer stores whatever lines it receives. The ticket's layout and content belong to bolt 012 (ticket-printing), so this contract stays generic: batch ID, a reprint flag, text lines, and a Code 128 barcode value.
3. **Fault injection** (new `Faults/` area):
   - A fault registry holds the active faults per target, with optional expiry, timed by `TimeProvider` so tests are deterministic.
   - An action filter is attached to each training-contract controller. It applies the active fault before the action runs.
   - Status endpoints don't fail. They report `OFFLINE` while their device has an `unavailable` fault, which is what the status strip needs (FR-23, FR-24).
4. **Seed reset**: one endpoint returns every dummy to its start state: scale states and tares, received consumption, the scanner queue, printed tickets and active faults. The seed file itself is immutable during a run, so "reset seed" means "clear everything that changed since start-up".
5. **Status endpoints** for the scanner and printer, so the later device-health bolt (004) can report SCANNER and PRINTER as READY or OFFLINE. The scale list and scale status also start reporting `OFFLINE` for a faulted scale.

### Files to be created / modified

**Created:**
- `src/WeighingMS.Simulators.Host/Scanner/`:
  - `ScannerController` (status, scan collection);
  - `ScannerQueue`;
  - `ScanRequest`, `ScanResponse`, `ScannerStatusResponse`;
  - `ScannerSimulationController` (`/_sim/scanner/scan`).
- `src/WeighingMS.Simulators.Host/Printer/`:
  - `TicketsController` (`POST /api/tickets`);
  - `PrinterStatusController`;
  - `PrintStore`;
  - `TicketRequest`, `PrintReceipt`;
  - `PrinterInspectionController` (`/_sim/printer/tickets`).
- `src/WeighingMS.Simulators.Host/Faults/`:
  - `FaultRegistry`, `FaultTarget`, `FaultMode`, `FaultRequest`, `ActiveFaultResponse`;
  - `FaultInjectionAttribute` (action filter);
  - `FaultsController` (`/_sim/faults`);
  - `ResetController` (`/_sim/reset`).
- Tests: `ScannerTests`, `PrinterTests`, `FaultInjectionTests`, `ResetTests`, in `tests/WeighingMS.Simulators.Tests/` (Stage 3).

**Modified:**
- `OrgaTex/BatchesController`, `Scales/ScalesController`: add the fault attribute. Scale status and list report OFFLINE when faulted.
- `Scales/ScaleSimulator`, `OrgaTex/ConsumptionStore`: add a `Reset()` method.
- `Seed/SimulatorOptions`: add `FaultTimeout` (default 30 s) and `SlowDelay` (default 2 s).
- `Program.cs`: register the new services.
- `CLAUDE.md`: add the new `/_sim/*` endpoints to the command notes.

### APIs involved (Simulators.Host :5090)

| Method | Path | Purpose | Story |
|---|---|---|---|
| POST | `/_sim/scanner/scan` | Test control: emit a scan `{ code }` → 202 with its sequence | 004 |
| GET | `/api/scanner/scans?after={sequence}` | WeighingMS collects scans newer than its cursor (FIFO), each `{ sequence, code, scannedAt }` | 004 |
| GET | `/api/scanner/status` | READY / OFFLINE | 004 |
| POST | `/api/tickets` | Requires an `Idempotency-Key`. Body: `{ batchId, isReprint, lines[], barcode{symbology, value} }` → **201** with the print receipt. A repeated key returns the original receipt; a missing key → 400 | 005 |
| GET | `/api/printer/status` | READY / OFFLINE | 005 |
| GET | `/_sim/printer/tickets` | Every printed ticket, once each, with its content | 005 |
| POST | `/_sim/faults` | `{ target: orgatex \| scale:{scaleId} \| printer \| scanner, mode: unavailable \| timeout \| error500 \| slow, durationMs? }` → 204 | 006 |
| GET | `/_sim/faults` | Active faults | 006 |
| DELETE | `/_sim/faults` | Clear all faults | 006 |
| POST | `/_sim/reset` | Return every dummy to its start state | 006 |

The `/_sim/*` paths follow api-conventions. `GET /_sim/faults`, the scanner and printer status endpoints, and the scan-collection endpoint are the contract details this bolt adds.

**Fault behaviour per mode** (status endpoints excepted, as described above):

| Mode | Effect on a call to the faulted dummy | Error code |
|---|---|---|
| `unavailable` | 503 ProblemDetails, and status reports OFFLINE | `OrgaTex.Unavailable`, `Scale.Unavailable`, `Printer.Unavailable`, `Scanner.Unavailable` |
| `timeout` | The response is held for `FaultTimeout` (30 s), long enough for a caller's timeout to fire | — |
| `error500` | 500 ProblemDetails | `Simulator.InternalError` |
| `slow` | The response is delayed by `SlowDelay` (2 s), then served normally | — |

A fault with `durationMs` expires by itself. A fault on a scale applies only to that scale. A new fault on a target replaces the existing one.

### Dependencies

- Bolt `001-station-simulators` (complete): the host, the seed, the OrgaTEX and scale dummies, the test host.
- No new NuGet packages.

### Technical Approach

- **One filter attribute per training-contract controller**, `[FaultInjection("orgatex")]` and so on; for scales, the target is resolved from the `scaleId` route value. Controllers stay free of fault logic, and the fault behaviour can be tested in one place.
- **Delays use `TimeProvider`**, through `Task.Delay(…, timeProvider, …)`, so tests can advance a `FakeTimeProvider` instead of waiting 30 s.
- **In-memory, thread-safe stores**: `ConcurrentDictionary` for idempotent prints and a lock-guarded list for the scan queue.
- **Reset** calls `Reset()` on each store and registry; there is no reloading from disk.
- **Validation** uses the same parameter-level attributes on records as bolt 001; `JsonRequired` covers value types.

### Acceptance criteria (from the stories)

- [ ] `POST /_sim/scanner/scan {"code":"B-24187"}` queues the scan. `GET /api/scanner/scans?after=0` returns it once; asking again with the returned cursor returns nothing new. Scans come out in emission order. An empty code returns 400.
- [ ] Scanner status reports READY, and OFFLINE while it has an `unavailable` fault. There is no keyboard-wedge path (PD-03).
- [ ] `POST /api/tickets` stores the ticket and returns 201. A repeated `Idempotency-Key` prints nothing twice. A ticket without a batch ID returns 400. `GET /_sim/printer/tickets` lists each ticket with its content. A faulted printer returns 503 `Printer.Unavailable`.
- [ ] For each target (orgatex, scale:{id}, printer, scanner), each mode (unavailable, timeout, error500, slow) behaves as in the table above. A fault on an unknown scale ID returns 400.
- [ ] A fault with `durationMs` clears by itself once the time has passed. `DELETE /_sim/faults` clears all faults.
- [ ] `POST /_sim/reset` returns scale states, tares, consumption, scans, printed tickets and faults to their start state.
- [ ] `/_sim/*` is absent in Production; this extends bolt 001's test.

### How this will be tested (Stage 3)

- **Contract tests** (xUnit v3, Shouldly, `WebApplicationFactory` with `FakeTimeProvider`):
  - scanner: emit, collect, cursor, order, validation, status;
  - printer: 201, idempotent replay, validation, inspection, fault;
  - faults: every target × mode combination (Theory), expiry by advancing the fake clock, DELETE, an unknown scale, status reported OFFLINE;
  - reset: the state after reset equals the start state.
- **Existing tests**: all 72 bolt-001 tests must still pass.
- **Checks**: `dotnet build` with 0 warnings, the format check, and coverage of the hand-written code reported again.
- **Results** go in `test-walkthrough.md`.

### Out of scope

- WeighingMS-side adapters and how they react to faults: bolts 004, 005, 011, 012.
- Ticket layout and content: bolt 012.
- Scan routing by screen: bolts 004 and 006.
- Docker image verification: still pending; it needs a running Docker daemon.
