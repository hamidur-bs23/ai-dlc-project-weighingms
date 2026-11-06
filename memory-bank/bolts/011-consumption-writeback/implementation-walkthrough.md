---
stage: implement
bolt: 011-consumption-writeback
created: 2026-11-06T03:51:00Z
---

## Implementation Walkthrough: consumption-writeback (bolt 011)

### Summary

Every completed batch now writes its consumption to an outbox row in the completing transaction, and a background dispatcher delivers it to OrgaTEX later with a fixed idempotency key. Transient failures retry on a configurable schedule, then the entry becomes "sync failed" and offers a manual Retry. The state shows on the summary, the queue and the status strip.

### Structure Overview

The Domain owns the write-back state machine and the retry schedule. The Application layer holds the completion handler (adds the row, never calls OrgaTEX), the dispatcher (one testable pass over due entries) and the status and retry service. Infrastructure provides persistence, the gateway operation and the hosted loop, which only waits and calls the dispatcher. The Api exposes the state and Retry, and the web screens read them.

### Completed Work

- [x] `src/WeighingMS.Domain/WriteBack/ConsumptionWriteBack.cs` - the aggregate and its Pending, Sent and SyncFailed transitions
- [x] `src/WeighingMS.Domain/WriteBack/RetryPolicy.cs` - the attempt limit and capped exponential back-off
- [x] `src/WeighingMS.Domain/WriteBack/ConsumptionLine.cs` - one chemical's actual quantity, unit and lot
- [x] `src/WeighingMS.Domain/WriteBack/WriteBackStatus.cs` - the status values
- [x] `src/WeighingMS.Domain/Audit/AuditEventType.cs` - adds the manual-retry audit event
- [x] `src/WeighingMS.Application/WriteBack/ConsumptionWriteBackHandler.cs` - adds the outbox row to the completing unit of work
- [x] `src/WeighingMS.Application/WriteBack/WriteBackDispatcher.cs` - posts due entries, records outcomes and audits sync failed
- [x] `src/WeighingMS.Application/WriteBack/WriteBackService.cs` - per-batch state, unsynced count and manual retry
- [x] `src/WeighingMS.Application/WriteBack/WriteBackOptions.cs` - the validated `WriteBack:*` settings
- [x] `src/WeighingMS.Application/WriteBack/IWriteBackRepository.cs` - the persistence and wake-up ports
- [x] `src/WeighingMS.Application/Batches/IOrgatexGateway.cs` - adds the consumption post to the port
- [x] `src/WeighingMS.Application/Batches/OrgatexErrors.cs` - the rejection error and the transient check
- [x] `src/WeighingMS.Application/Batches/BatchService.cs`, `BatchStatus.cs` - queue statuses derived from the write-back
- [x] `src/WeighingMS.Application/Weighing/BatchSummary.cs`, `WeighingSessionService.cs` - the summary carries the real write-back state
- [x] `src/WeighingMS.Infrastructure/Integrations/OrgaTex/OrgatexGatewayClient.cs` - the HTTP consumption post and failure translation
- [x] `src/WeighingMS.Infrastructure/WriteBack/WriteBackRepository.cs` - EF Core persistence with `xmin` concurrency
- [x] `src/WeighingMS.Infrastructure/WriteBack/WriteBackHostedService.cs` - the dispatch loop, woken by a completion or the poll interval
- [x] `src/WeighingMS.Infrastructure/WriteBack/WriteBackSignal.cs` - the in-process wake-up
- [x] `src/WeighingMS.Infrastructure/Persistence/Configurations/ConsumptionWriteBackConfiguration.cs` - the table mapping
- [x] `src/WeighingMS.Infrastructure/Persistence/Migrations/20261106033843_AddConsumptionWriteBack.cs` - the `orgatex_write_backs` table
- [x] `src/WeighingMS.Api/Controllers/WriteBackController.cs` - the state and manual Retry endpoints
- [x] `src/WeighingMS.Api/Controllers/DevicesController.cs` and contract - the unsynced count on device health
- [x] `src/WeighingMS.Api/Contracts/Weighing/BatchSummaryResponse.cs` - the OrgaTEX state on the summary
- [x] `src/WeighingMS.Api/Program.cs`, `appsettings.json` - options validated on start and the default schedule
- [x] `src/WeighingMS.Api/openapi/WeighingMS.Api.json`, `web/src/app/core/api/generated/schema.ts` - the regenerated contract and types
- [x] `web/src/app/features/batch-summary/batch-summary.component.ts` - the confirmed, pending and sync-failed states, Retry, and refresh while pending
- [x] `web/src/app/features/shell/status-strip.component.ts`, `web/src/app/core/devices/device-health.service.ts` - the unsynced count
- [x] `web/src/app/features/batch-queue/batch-queue.component.html`, `.scss` - the COMPLETE, SYNC PENDING and SYNC FAILED tags
- [x] `web/src/app/core/weighing/weighing-api.service.ts`, `web/src/app/core/api/api.types.ts`, `web/public/i18n/en.json` - the retry call, types and text
- [x] `CLAUDE.md` - the new endpoints and settings
- [x] Existing test doubles and fixtures - updated to the new constructors and response shapes (no new tests; those are Stage 3)

### Key Decisions

- **Plan decisions D1 to D3 applied**: 5 attempts from 5 s capped at 5 min; only unreachable, timeout and 5xx are retried; any signed-in operator may Retry (WRITEBACK-RETRY, WRITEBACK-TRANSIENT, WRITEBACK-RETRY-ANYONE).
- **Queue status comes from the write-back row**: the row exists only once a batch has completed, so its status also answers "is this batch complete".
- **The wake-up waits briefly before dispatching**: the handler signals before the completing transaction commits, so a settle delay lets the row become visible. If the commit takes longer, the next poll picks the row up.
- **`BatchService` requires the write-back repository**: an optional dependency would have hidden a missing registration, and the queue would silently never show sync states.

### Deviations from Plan

- `WriteBackErrors.cs` and `ConsumptionPost.cs` were not created: the errors are defined where they're raised and the gateway takes the domain lines directly.
- `IWriteBackSignal` lives in `IWriteBackRepository.cs`, not its own file.
- `RetryPolicy.cs` was added so the back-off rule sits in the Domain.
- `GET …/session/write-back` was added alongside Retry; the plan listed only Retry.
- The summary shows plain "Pending sync", as the acceptance criterion says. The Api still returns attempts and the next attempt time.

### Dependencies Added

None.

### Developer Notes

- Integration tests run with `WriteBack:Enabled=false` and drive the dispatcher directly, except the two background-loop tests, which turn the loop on.
- A dispatcher pass stops at an unexpected exception, such as an unreadable 200 response. The loop logs it and tries again at the next poll, and that entry stays due.
- Reopening a completed batch from the queue still reads the batch from OrgaTEX first, so during an outage it can't be opened that way. A summary already on screen still works.
