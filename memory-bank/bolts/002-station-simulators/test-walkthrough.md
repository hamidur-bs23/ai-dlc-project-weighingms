---
stage: test
bolt: 002-station-simulators
created: 2026-11-04T19:50:00Z
---

## Test Report: station-simulators (bolt 002)

### Summary

- **Tests**: 122/122 passed across the solution: 113 in `WeighingMS.Simulators.Tests` (50 new) and 9 in `WeighingMS.Architecture.Tests`.
  - The suite was run 3 times with the same result. The timing-based fault tests are stable because they use a `FakeTimeProvider`.
- **Coverage**: 96.3% of hand-written lines in `WeighingMS.Simulators.Host` (1207/1253), up from 94.4%. Every new scanner, printer and fault file is at 100%.
  - The files below 100% are bolt-001 code: `SeedLoader` 81.7%, `ScaleScenarioController` 81.8%, `Program.cs` 85.1%. Their uncovered lines are defensive branches and Development-only wiring.
- **Build**: 0 warnings, 0 errors. **Format**: `dotnet format --verify-no-changes` is clean.

### Test Files

- [x] `tests/WeighingMS.Simulators.Tests/ScannerTests.cs`:
  - a scan returns 202 with a sequence number that increases;
  - collection from 0 returns scans in emission order;
  - the cursor returns each scan once, and polling again with the same cursor is safe;
  - an empty code → 400; a negative cursor → 400 `Scanner.InvalidCursor`;
  - status is READY.
- [x] `tests/WeighingMS.Simulators.Tests/PrinterTests.cs`:
  - a print returns 201 with its receipt (batch, reprint flag, barcode);
  - a repeated key prints once and returns the original receipt;
  - a missing key → 400 `Printer.IdempotencyKeyMissing`; no batch ID or no lines → 400;
  - the inspection list shows each ticket with its content;
  - status is READY.
- [x] `tests/WeighingMS.Simulators.Tests/FaultInjectionTests.cs`:
  - **every target × mode**:
    - `unavailable` → 503 with `OrgaTex/Scale/Printer/Scanner.Unavailable`;
    - `error500` → 500;
    - `timeout` → still pending, then 504 after 30 s of fake time;
    - `slow` → still pending, then a normal answer after 2 s;
  - a scale fault affects only that scale, for both weight and tare;
  - status endpoints and the scale list report OFFLINE;
  - a fault with a duration expires by itself; the list shows the expiry;
  - a new fault replaces the previous one; DELETE clears all;
  - an unknown target or scale → 400 `Fault.UnknownTarget`; an unknown mode → 400.
- [x] `tests/WeighingMS.Simulators.Tests/ResetTests.cs`:
  - after tare, a pour, consumption, a print, a scan and a fault, reset restores the start state: the untared 214.0 g reading, and empty consumption, tickets, faults and scans;
  - the scanner sequence restarts at 1;
  - the seed batches are kept.
- [x] `tests/WeighingMS.Simulators.Tests/SimulationControlTests.cs` (extended): in Production, every new `/_sim/*` endpoint (scanner/scan, reset, faults, printer/tickets) returns 404.
- [x] All 72 bolt-001 tests are unchanged and passing.

### Acceptance Criteria Validation

- ✅ **The scan is queued, collected once per cursor in emission order, and an empty code → 400** (`ScannerTests`).
- ✅ **Scanner status is READY, and OFFLINE under an `unavailable` fault** (`ScannerTests`, `FaultInjectionTests`). There is no keyboard-wedge path; the only input is `POST /_sim/scanner/scan` (PD-03).
- ✅ **Printer: 201; a repeated key prints nothing twice; no batch → 400; inspection lists the content; a faulted printer → 503 `Printer.Unavailable`** (`PrinterTests`, `FaultInjectionTests`).
- ✅ **Each target × mode behaves as in the plan's table, and an unknown scale → 400** (`FaultInjectionTests`, 16 target × mode cases plus extras).
- ✅ **A fault with `durationMs` clears by itself, and DELETE clears all** (`FaultInjectionTests`).
- ✅ **Reset restores scales, tares, consumption, scans, prints and faults** (`ResetTests`).
- ✅ **`/_sim/*` is absent in Production** (`SimulationControlTests`).

### Issues Found

- **CA1861 fired on constant arrays in the test request bodies.** It is a performance rule that doesn't apply to tests, so it is relaxed only in `tests/Directory.Build.props`, alongside the existing test-only relaxations.
- **No defects were found in the Implement-stage code.**
- **Still open from bolt 001: the Docker image build is unverified.** The Docker daemon wasn't running.

### Notes

- Timeout and slow faults are tested without real waiting. Each test checks that the request is still pending, then steps the `FakeTimeProvider` in 250 ms increments until the response arrives.
- The cursor, the status endpoints and the generic ticket body are now covered by tests. Bolts 004 (devices) and 012 (tickets) can rely on them.
