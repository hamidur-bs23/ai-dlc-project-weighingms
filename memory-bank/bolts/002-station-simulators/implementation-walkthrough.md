---
stage: implement
bolt: 002-station-simulators
created: 2026-11-04T19:30:00Z
---

## Implementation Walkthrough: station-simulators (bolt 002)

### Summary

The simulator host now has all four training dummies, with fault injection for each:
- **Dummy Scanner**: scans are queued, and WeighingMS collects them with a cursor;
- **Dummy Printer**: prints are idempotent, and what was printed can be inspected;
- **Fault injection**: OrgaTEX, each scale, the printer and the scanner can be faulted on demand, in four modes;
- **Reset**: one call returns every dummy to its start state.

Every STEP 30 failure scenario can now be reproduced through `/_sim/*`.

### Structure Overview

The new areas follow the bolt-001 layout: **Scanner**, **Printer** and **Faults**.

Faults are applied by one action filter that the training-contract controllers opt into: OrgaTEX, scales, scanner scans and tickets. The filter reads the target's active fault from a registry timed by `TimeProvider`. It then answers with 503 or 500, holds the response, adds a delay, or does nothing, depending on the mode.

Status endpoints are exempt from the filter. They report OFFLINE instead of failing, which is what the status strip needs.

### Completed Work

- [x] `src/WeighingMS.Simulators.Host/Faults/FaultRegistry.cs` - active faults per target; optional expiry; a new fault replaces the old one; clear all
- [x] `src/WeighingMS.Simulators.Host/Faults/FaultInjectionAttribute.cs`, `FaultInjectionFilter.cs`, `FaultExemptAttribute.cs` - the per-controller fault filter, including resolving the scale from its route value, and the opt-out for status endpoints
- [x] `src/WeighingMS.Simulators.Host/Faults/FaultMode.cs`, `FaultTargets.cs`, `FaultRequest.cs`, `ActiveFaultResponse.cs` - modes, target names, `Area.Unavailable` error codes, contracts
- [x] `src/WeighingMS.Simulators.Host/Faults/FaultsController.cs` - `POST`, `GET` and `DELETE /_sim/faults`; an unknown target or scale returns 400 `Fault.UnknownTarget`
- [x] `src/WeighingMS.Simulators.Host/Faults/ResetController.cs` - `POST /_sim/reset`
- [x] `src/WeighingMS.Simulators.Host/Scanner/ScanBuffer.cs` - ordered scan buffer with increasing sequence numbers and cursor reads
- [x] `src/WeighingMS.Simulators.Host/Scanner/ScannerScansController.cs` - `GET /api/scanner/scans?after={sequence}` (faultable)
- [x] `src/WeighingMS.Simulators.Host/Scanner/ScannerStatusController.cs` - `GET /api/scanner/status`
- [x] `src/WeighingMS.Simulators.Host/Scanner/ScannerSimulationController.cs` - `POST /_sim/scanner/scan` → 202 with the sequence number
- [x] `src/WeighingMS.Simulators.Host/Printer/TicketsController.cs` - `POST /api/tickets` → 201; a repeated `Idempotency-Key` returns the original receipt; a missing key → 400 (faultable)
- [x] `src/WeighingMS.Simulators.Host/Printer/PrintStore.cs`, `TicketRequest.cs`, `PrintReceipt.cs`, `BarcodeDto.cs` - the idempotent print record and the generic ticket contract
- [x] `src/WeighingMS.Simulators.Host/Printer/PrinterStatusController.cs`, `PrinterInspectionController.cs` - `GET /api/printer/status`, `GET /_sim/printer/tickets`
- [x] `src/WeighingMS.Simulators.Host/Common/DeviceStatus.cs`, `DeviceStatusResponse.cs` - the READY / OFFLINE values and the status contract
- [x] Modified `OrgaTex/BatchesController.cs` - faultable as `orgatex`
- [x] Modified `Scales/ScalesController.cs` - weight and tare are faultable per scale; the scale list and scale status report OFFLINE for a faulted scale
- [x] Modified `Scales/ScaleSimulator.cs`, `OrgaTex/ConsumptionStore.cs` - added `Reset()`
- [x] Modified `Seed/SimulatorOptions.cs` - `FaultTimeout` (30 s) and `SlowDelay` (2 s), validated on start
- [x] Modified `Program.cs` - registers the scan buffer, print store and fault registry
- [x] Modified `CLAUDE.md` - simulator control endpoints listed

### Key Decisions

- **Scan collection with a cursor** (`after={sequence}`) rather than a "next scan" call that removes it. Reading never changes state, so a failed poll can't lose a scan, and the call stays a plain GET.
- **Status endpoints report OFFLINE rather than failing.** The UI's status strip needs a readable status while the device itself is down (FR-23, FR-24). The OFFLINE scale also shows in `GET /api/scales`.
- **`timeout` holds for 30 s, then answers 504 `Simulator.Timeout`.** WeighingMS adapters will time out well before that, so they see a real timeout. The final 504 only matters to callers without a timeout.
- **The printer contract is generic** (text lines plus a barcode value), so the ticket layout stays a WeighingMS decision (bolt 012, PD-04).
- **Reset clears runtime state; it doesn't re-read the seed.** The seed is immutable for the life of the process, so clearing scales, tares, consumption, scans, prints and faults restores the start state.

### Deviations from Plan

- **The scanner controller was split in two**, `ScannerScansController` and `ScannerStatusController`, as the Sonar S6960 analyzer requires. The status endpoint therefore needs no fault-exemption marker.
- **`ScannerQueue` was renamed `ScanBuffer`**: analyzer rule CA1711 forbids a type name ending in `Queue` unless it is a real queue type.
- Otherwise none.

### Dependencies Added

- None.

### Developer Notes

- **Verification so far**:
  - `dotnet build`: 0 warnings, 0 errors.
  - Format check: clean.
  - All 72 existing tests pass.
  - A live smoke test of every new endpoint passed:
    - scan cursor;
    - print 201;
    - scale unavailable → OFFLINE and 503, with the other scale unaffected;
    - printer `error500` expiring after 1.5 s;
    - `slow` adding 2 s;
    - unknown target → 400;
    - reset clearing everything.
- **Gotcha**: a simulator left running on port 5090 (for example from `npm run sim`) makes a new instance fail to bind, and calls then reach the old build. Stop the old one first.
