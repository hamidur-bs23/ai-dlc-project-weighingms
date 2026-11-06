---
stage: test
bolt: 012-ticket-printing
created: 2026-11-06T06:27:00Z
---

## Test Report: ticket-printing (bolt 012)

### Summary

- **Backend tests**: **705/705 passed**, 58 of them new (647 before this bolt).
- **Web tests**: **147/147 passed**, 3 of them new (144 before).
- **Stability**: the 22 ticket integration tests passed on one more repeat run, and the full backend suite passed again after the test quality check below.
- **Coverage**: 100% of ticket lines (274/274), merged from the domain, application and integration tests. This counts the ticket and print-job domain types, the composer, handler, service, printer client, repository, controller and contracts. EF configurations, migrations and web code are not included, and no web coverage figure was measured.
- **Other checks**: build 0 warnings, format clean, lint clean, web production build passes, no EF model changes pending, `specsmd:validate` 0 issues, `specsmd:integrity` 0 inconsistencies.
- **Live run**: real Api, simulators and web terminal on a scratch database, for STEP 27 and the printer outage in STEP 30.

### Test Files

- [x] `tests/WeighingMS.Domain.Tests/TicketTests.cs` - 21 tests: field order, plant time, units and precision, OVR, result line, name wrapping, operators, barcode, print keys, stale press, reprint mark
- [x] `tests/WeighingMS.Application.Tests/TicketServiceTests.cs` - 9 tests: first print, reprint, printer down, retry with the same key, stale press, losing a race, not found, same content as the print
- [x] `tests/WeighingMS.Api.IntegrationTests/TicketEndpointsTests.cs` - 22 tests through the Api and the real dummy printer, on PostgreSQL with the migrations applied
- [x] `tests/WeighingMS.Application.Tests/BatchServiceTests.cs` - queue tag order across the write-back and ticket states
- [x] `web/src/app/features/batch-summary/batch-summary.component.spec.ts` - 3 new: not available, print pending, printed with its count
- [x] `web/src/app/features/batch-queue/batch-queue.component.spec.ts` - PRINT PENDING added to the existing tag test

### Acceptance Criteria Validation

- ✅ **Ticket composed in the required order**: every field checked by tests and seen in the live ticket for B-24187. OVR and "OVERRIDES: n" are covered by domain and integration tests only, because the live batch had no override.
- ✅ **Code 128 barcode of the batch ID**: symbology and value checked by tests and in the dummy printer's stored data. Drawing the bars with the ID beneath them belongs to the preview in bolt 013.
- ✅ **Same content available for the preview (Api)**: the ticket endpoint returns the same lines the printer receives. The preview screen itself is bolt 013.
- ✅ **Print sends once with an Idempotency-Key and marks it printed (Api)**: tested and seen live, with one stored receipt per copy.
- ✅ **Double press gives one print**: a stale press gets 409 and prints nothing; six concurrent presses print one copy, for first prints and reprints.
- ✅ **Later prints are marked REPRINT and audited**: seen live as `*** REPRINT ***`, `isReprint: true` and a `TicketReprinted` audit entry; also tested for a different operator.
- ✅ **Printer down leaves completion intact, ticket print pending**: tested for unavailable, error500 and timeout; seen live on B-24188.
- ✅ **Status strip shows the printer offline**: covered by the Api test and seen live as PRINTER · OFFLINE.
- ✅ **No auto-print after recovery, Print then works**: seen live, with 0 receipts after 5 s and one after the press, using the same key before and after the outage.
- ✅ **Build, format, earlier tests and migration**: all pass; the migration ran on a clean database in the integration fixture.

The stories say "press Print (F1)". This bolt delivers the Api behind that press. The Print key itself is bolt 013 (unit 008, stories 010 and 011).

### Test Quality Check

Three deliberate faults were put in one at a time and then reverted, with checksums confirming the source was restored:

- [x] A new print key on every first print attempt failed 7 tests.
- [x] Accepting a stale press failed 4 tests.
- [x] Auditing a reprint as a first print failed 3 tests.

### Issues Found

Known and not fixed:

- Any non-success answer from the printer, including a 4xx, maps to `Printer.Unavailable`. The dummy only returns 400 for a malformed request, which would be our own defect, but the operator would see it as a printer outage.
- Batches completed before this bolt have no ticket. Their summary shows "—" and they can't be printed.
- A refused print attempt is recorded in `print_jobs` but not in the audit trail, which holds successful prints only. This is deliberate and tested.

### Evidence

- [x] `evidence/STEP-27-ticket.txt` - the live ticket, print, stale press, reprint, stored data and database rows
- [x] `evidence/STEP-27-printer-api.png` - both copies at `GET /_sim/printer/tickets`
- [x] `evidence/STEP-27-ticket-tests.txt` - the ticket test results
- [x] `evidence/STEP-30-printer-outage.txt` - the fault, print pending, 503, recovery and manual print
- [x] `evidence/STEP-30-printer-print-pending.png` - the summary with Print pending and PRINTER · OFFLINE
- [ ] `STEP-27-ticket.png` - needs the preview, so it is taken in bolt 013 (as planned)

### Notes

- The live run used a scratch database and separate ports (:5180, :5190), and was not repeated for this report. No other ticket source file is newer than that run. `Ticket.cs` and `TicketService.cs` can't be checked this way, because the quality check reset their timestamps; the automated suites did run against their current content.
- The station Api and simulators on :5080 and :5090 need a restart and `npm run db:update` before they show tickets, if they were started before this bolt.
- The bolt's "Code reviewed" criterion has had no independent review, only the checks above.
