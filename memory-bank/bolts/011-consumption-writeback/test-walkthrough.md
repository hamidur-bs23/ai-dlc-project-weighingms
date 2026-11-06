---
stage: test
bolt: 011-consumption-writeback
created: 2026-11-06T04:08:00Z
---

## Test Report: consumption-writeback (bolt 011)

### Summary

- **Backend tests**: **647/647 passed**, 65 of them new (582 before this bolt).
- **Web tests**: **144/144 passed**, 13 of them new (131 before).
- **Stability**: the web suite and the 25 integration tests passed on two more repeat runs.
- **Coverage**: 86.6% of write-back lines from the integration tests alone (292/337; files named WriteBack plus `OrgatexGatewayClient`, migrations excluded). The domain and application tests add to this, but no combined figure or web coverage figure was measured.
- **Other checks**: build 0 warnings, format clean, lint clean, web production build passes, `specsmd:validate` 0 issues, `specsmd:integrity` 0 inconsistencies.
- **Live run**: the real dispatcher, Api, simulator, Postgres and web terminal on a scratch database, for both STEP 26 and the OrgaTEX outage in STEP 30.

### Test Files

- [x] `tests/WeighingMS.Domain.Tests/ConsumptionWriteBackTests.cs` - 17 tests: state machine, 5/10/20/40 s schedule, cap and no overflow, rejection, manual retry only from sync failed, key never changes
- [x] `tests/WeighingMS.Application.Tests/WriteBackTests.cs` - 20 tests: dispatcher outcomes, due-time selection, audit on sync failed, one failure not stopping the next entry, status view, unsynced count, retry rules, options validation
- [x] `tests/WeighingMS.Application.Tests/BatchServiceTests.cs` - 3 new cases: queue status comes from the write-back
- [x] `tests/WeighingMS.Api.IntegrationTests/WriteBackEndpointsTests.cs` - 25 tests through the Api and the real dummy OrgaTEX, on PostgreSQL, including 2 that run the real background loop
- [x] `web/src/app/features/batch-summary/batch-summary.component.spec.ts` - 8 new: confirmed, pending and failed states, following until settled, no timer after the screen closes, Retry, refused Retry
- [x] `web/src/app/core/devices/device-health.service.spec.ts` - 2 new: unsynced count followed, cleared on stop, kept when the Api is unreachable
- [x] `web/src/app/features/shell/status-strip.component.spec.ts` - 2 new: unsynced count hidden at zero, shown on OrgaTEX only
- [x] `web/src/app/features/batch-queue/batch-queue.component.spec.ts` - 1 new: COMPLETE, SYNC PENDING and SYNC FAILED tags

### Acceptance Criteria Validation

- ✅ **Outbox row in the completing transaction**: a failing handler leaves no row and no completion; completion makes no call to OrgaTEX.
- ✅ **OrgaTEX down, completion still succeeds**: tested with an injected fault, and seen live on B-24188.
- ✅ **Posted once with its key, sent on 200 OK**: dispatching twice gives one receipt; reposting the same key returns the first receipt.
- ✅ **Automatic retry with back-off**: tested for 503, 500 and timeout. Live gaps between attempts were 6, 10, 20 and 40 s.
- ✅ **Pending entries resume after a restart**: the real loop delivered a row left pending by an earlier host.
- ✅ **Limit reached gives sync failed plus an audit entry**: live after 74 s and 5 attempts, with one `SyncFailed` entry.
- ✅ **A 4xx goes straight to sync failed**: tested for 400 and 404 through the Api; 409 at application level only.
- ✅ **Retry after recovery posts exactly once**: live through the UI, with the same key, one receipt and a `WriteBackRetried` audit entry for OP-1001.
- ✅ **Summary states**: seen live as CONFIRMED 10:02:41 and as Sync failed with Retry. "Pending sync" is covered by the web spec, and the Api reported pending live.
- ✅ **Status strip**: seen live as ORGATEX · OFFLINE · 1 UNSYNCED.
- ✅ **Queue tags**: COMPLETE seen live and opens the summary; all three covered by the web spec.
- ✅ **Override lines included**: the overweight 96.4 g reached the dummy OrgaTEX.
- ✅ **Build, format, earlier tests and migration**: all pass; the migration applied to a clean database.

### Test Quality Check

Two deliberate faults were put in and then reverted. Removing the closed-screen timer guard failed 1 web test. Making retry generate a new key failed 2 backend tests.

### Issues Found

Found and fixed during this bolt:

- The web build was broken by a missing `DestroyRef` import.
- A summary response arriving after the screen closed could start a timer nothing would clear.
- `BatchService` had an optional dependency that could have hidden a missing registration.
- The summary label read "Pending sync (attempt 0)".

Known and not fixed:

- While OrgaTEX is down, `GET /api/batches` returns 503, so a batch can't be reached from the queue. This also blocks reaching Retry that way, but Retry is only useful after recovery, and the summary still opens by direct URL during the outage.
- Opening the summary by direct URL leaves the header without the batch, order and machine line. This comes from how the active batch is set, not from this bolt.
- The dispatcher's own HTTP exchange isn't logged, so the request and 200 OK in the evidence are a hand replay with the stored key and lines. The replay also proved a repeated key stores nothing new.

### Evidence

- [x] `evidence/STEP-26-writeback.txt` - the run, stored data, outbox row, request and 200 OK
- [x] `evidence/STEP-26-ui.png` - Written to OrgaTEX · CONFIRMED
- [x] `evidence/STEP-30-orgatex-outage.txt` - the schedule, sync failed, audit and recovery
- [x] `evidence/STEP-30-orgatex-sync-failed.png`, `evidence/STEP-30-orgatex-retry-confirmed.png` - both UI states
- [ ] `STEP-26-request.png`, `STEP-26-response.png`, `STEP-26-data.png` - the workbook's named screenshots, still to be taken by the user

### Notes

The evidence run used a scratch database, now dropped, and separate ports. The station Api and simulators already running on :5080 and :5090 predate this bolt, so they need a restart and `npm run db:update` before showing write-back.
