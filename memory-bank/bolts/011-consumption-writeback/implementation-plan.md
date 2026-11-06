---
stage: plan
bolt: 011-consumption-writeback
created: 2026-11-06T03:35:00Z
---

## Implementation Plan: consumption-writeback (bolt 011)

### Objective

Deliver every completed batch's actual consumption to OrgaTEX **exactly once from the operator's point of view, without ever blocking the station** (PD-01: store-and-forward outbox).
- When the batch completes, an outbox entry is written **in the same transaction**. This uses bolt 009's `IBatchCompletedHandler` seam.
- A background dispatcher posts it to `POST /api/batches/{batchId}/consumption` with an `Idempotency-Key`, retries automatically with back-off, and after a configured limit marks it **sync failed** for a manual Retry.
- The state is visible on the summary, the queue and the status strip.

Workbook **STEP 26**: 5/5 → Complete Batch → `POST /api/batches/B-24187/consumption` → Dummy OrgaTEX. The evidence is the UI ("Written to OrgaTEX · CONFIRMED"), the request, the 200 OK response, and the stored data. The OrgaTEX-unavailable part of **STEP 30** is covered too.

### Stories in scope

1. `001-enqueue-consumption-on-completion`: FR-20, FR-24(c)
2. `002-dispatch-with-idempotency-and-retry`: FR-20
3. `003-sync-failed-and-manual-retry`: FR-20
4. `004-write-back-status-visible`: FR-20, FR-22

### What will change

1. **Domain** (`Domain/WriteBack`): `ConsumptionWriteBack` (aggregate):
   - batch, idempotency key (UUIDv7, fixed for life), payload lines (chemical, actual quantity + unit, lot), status `Pending | Sent | SyncFailed`, attempts, next attempt time, last error code, sent time, the OrgaTEX receipt time;
   - rules: `RecordSent`, `RecordFailure(retryable, now)` (back-off or sync failed at the limit), `RetryManually` (only from sync failed; same key, attempts reset).
2. **Application** (`Application/WriteBack`):
   - **`ConsumptionWriteBackHandler : IBatchCompletedHandler`**: builds the payload from the `BatchCompleted` records and adds the outbox row to the completing unit of work. Saved with the completion; it never calls OrgaTEX itself (bolt 009's observation).
   - **`WriteBackDispatcher.DispatchDueAsync()`**: takes the due pending entries, posts each through **`IOrgatexGateway.PostConsumptionAsync`** (a new operation on the existing port, with the shared typed client), and records the outcome. Sync failed → audit `SyncFailed` (the type already exists).
   - **`WriteBackService`**: status per batch, the unsynced count, and manual retry.
   - `WriteBackOptions` (`WriteBack:MaxAttempts`, `BaseDelay`, `MaxDelay`, `PollInterval`, `Enabled`), validated on start.
3. **Infrastructure**:
   - the `orgatex_write_backs` table (unique `batch_id`, unique `idempotency_key`, lines as `jsonb`), migration `AddConsumptionWriteBack`;
   - `OrgatexGatewayClient.PostConsumptionAsync` (the SIM-CONTRACTS translation; 5xx / timeout → retryable; 4xx → not retryable);
   - **`WriteBackHostedService`**: wakes **immediately after a completion** (an in-process signal) and **every `PollInterval`** for due retries; it resumes pending entries after an Api restart.
4. **Api**:
   - the summary's `orgatex.state` becomes real: `pending` (with attempts and next attempt), `sent` + `writtenAt`, `syncFailed` + error code;
   - **`POST /api/batches/{batchId}/session/write-back/retry`** (manual retry from sync failed);
   - **`/api/devices/health` gains `unsyncedWriteBacks`** (additive) for the status strip;
   - **queue status** adds `complete`, `syncPending` and `syncFailed`; bolt 005's read model already reserved them.
5. **Web**:
   - the interim summary shows **"Written to OrgaTEX · CONFIRMED hh:mm:ss"** (plant time), **"Pending sync"**, or **"Sync failed · Retry"**;
   - the status strip shows **"ORGATEX · OFFLINE · 2 UNSYNCED"** when there are unsynced entries;
   - the queue shows the status tags `COMPLETE` / `SYNC PENDING` / `SYNC FAILED`.

### Files to be created / modified

- **Created**:
  - `src/WeighingMS.Domain/WriteBack/{ConsumptionWriteBack,ConsumptionLine,WriteBackStatus,WriteBackErrors}.cs`
  - `src/WeighingMS.Application/WriteBack/{ConsumptionWriteBackHandler,WriteBackDispatcher,WriteBackService,WriteBackOptions,IWriteBackRepository,IWriteBackSignal,ConsumptionPost}.cs`
  - `src/WeighingMS.Infrastructure/WriteBack/{WriteBackRepository,WriteBackHostedService,WriteBackSignal}.cs`
  - `Persistence/Configurations/ConsumptionWriteBackConfiguration.cs`, migration `*_AddConsumptionWriteBack`
  - `src/WeighingMS.Api/Controllers/WriteBackController.cs` (retry), contracts
  - tests (Stage 3)
- **Modified**:
  - `IOrgatexGateway` + `OrgatexGatewayClient` (`PostConsumptionAsync`)
  - `BatchService` / `BatchStatus` (queue statuses)
  - `WeighingSessionService` summary (OrgaTEX state)
  - `DevicesController` + contract (`unsyncedWriteBacks`)
  - DI, `Program.cs` (options), `appsettings.json` (`WriteBack`)
  - web: `batch-summary`, `status-strip`, `batch-queue`, `device-health.service`, `en.json`, generated types
  - `CLAUDE.md`

### APIs involved

| Call | Direction |
|---|---|
| `POST /api/batches/{batchId}/consumption` + `Idempotency-Key` → 200 receipt (dummy OrgaTEX; a repeated key returns the first receipt) | Api → simulator (dispatcher) |
| `GET …/session/summary` → `orgatex: { state, writtenAt?, attempts, nextAttemptAt?, errorCode? }` | UI → Api |
| `POST …/session/write-back/retry` → 200 (state back to pending) · 409 `WriteBack.NotFailed` · 404 | UI → Api |
| `GET /api/devices/health` → + `unsyncedWriteBacks` | UI → Api (status strip) |
| `GET /api/batches` → status `complete` / `syncPending` / `syncFailed` | UI → Api (queue) |

Payload sent (training contract, SIM-CONTRACTS):
```json
{ "lines": [ { "chemicalId": "DYE-4471", "actualQuantity": 94.2, "unit": "g", "lot": "2026-0387" }, … ] }
```
Override records are included with their actual quantity: the chemical was consumed.

### Decisions at this checkpoint: resolved (APPROVE PLAN, 2026-11-06)

- **D1: option 1.** 5 attempts, exponential back-off from 5 s, capped at 5 min, configurable (WRITEBACK-RETRY).
- **D2: option 1.** Only transient failures are retried; a 4xx goes straight to sync failed (WRITEBACK-TRANSIENT).
- **D3: option 1.** Any signed-in operator may retry a sync-failed write-back; audited (WRITEBACK-RETRY-ANYONE).

#### Options as presented


**D1. Retry policy before "sync failed".** FR-20 says "a configurable limit"; the values are ours to pick.
1 - **(Recommended)** **5 attempts, exponential back-off from 5 s, capped at 5 min** (5 s, 10 s, 20 s, 40 s → sync failed after about 75 s of outage). Short enough to demonstrate "sync failed → Retry" in the lab (STEP 30) with a 90 s `/_sim/faults` window. It's configuration (`WriteBack:*`), so a plant could set 10 attempts and 30 minutes without code.
2 - **10 attempts up to 30 min**: closer to a production outage window, but STEP 30's sync-failed path would take half an hour to show.

**D2. Which OrgaTEX failures are retried?**
1 - **(Recommended)** **Retry only transient failures** (unreachable, timeout, 5xx). A **4xx** (400 invalid payload, 404 batch unknown, 409 key conflict) goes **straight to sync failed** with its error code, because retrying the same request can't succeed and would only delay the operator's attention.
2 - Retry everything up to the limit.

**D3. Who may press Retry on a sync-failed write-back?**
1 - **(Recommended)** **Any signed-in operator.** Retry re-sends the **same** payload with the **same** `Idempotency-Key`, so it can never post twice or change data. Audited.
2 - Supervisor only (step-up). Safer in principle, but it adds a PIN for an action that can't do harm.

### Dependencies

- Bolt 009 (`IBatchCompletedHandler`, `BatchCompleted`, the summary), bolt 002 (the dummy consumption endpoint, `/_sim/faults orgatex`, `GET /_sim/orgatex/consumption`), bolt 010 (the summary screen, status strip, queue). All complete.
- No new packages (`BackgroundService`, `TimeProvider`, EF Core jsonb via Npgsql are already present).

### Technical approach

- **Transactional outbox**: the handler only adds a row, inside the completing transaction (proved in bolt 009: a failing handler rolls back the completion). Dispatch happens later, outside any request.
- **Exactly once from the operator's view**: one row per batch (unique `batch_id`) and one key per row (unique, never regenerated). The dummy stores a repeated key once, so an Api crash between "posted" and "marked sent" is harmless.
- **The dispatcher is testable**: `DispatchDueAsync()` is a plain method on `TimeProvider`. The hosted service only loops and waits (the signal or `PollInterval`). Integration tests disable the loop (`WriteBack:Enabled=false`) and call the dispatcher directly with the fake clock.
- **Resilience**: POSTs aren't retried by the HTTP pipeline (as configured in bolt 004); the dispatcher owns the retry schedule. The circuit breaker still protects the Api.
- **Logging**: each failed attempt is a Warning; sync failed is an Error plus the audit entry.

### Acceptance criteria (from the stories)

- [ ] `BatchCompleted` → an outbox row in the same transaction, holding the batch, each chemical's actual quantity, unit and lot, and an idempotency key; **OrgaTEX down → completion still succeeds**.
- [ ] A pending row with OrgaTEX reachable → posted once with its `Idempotency-Key`, marked sent on 200 OK; a failure → automatic retry with back-off; a repeated post → OrgaTEX stores it once (`GET /_sim/orgatex/consumption` shows exactly one per batch); after an Api restart pending rows resume.
- [ ] The limit reached → sync failed plus an audit entry; Retry after recovery → posted exactly once.
- [ ] The summary shows "Written to OrgaTEX hh:mm:ss" (plant time) / "Pending sync" / "Sync failed · Retry"; the status strip shows OrgaTEX offline and the unsynced count; queue rows show sync pending, sync failed or complete.
- [ ] Build 0 warnings, format clean, all earlier tests pass, the migration applies.

### How this will be tested (Stage 3)

1. **Domain**: the write-back state machine (sent, retryable failure with back-off times, the limit → sync failed, a non-retryable failure → sync failed at once, manual retry only from sync failed, the key never changes).
2. **Application**, with fakes: the handler builds the payload (override lines included); the dispatcher's outcomes (200 / 503 / timeout / 400 / 404 / 409), due-time selection, the audit entry on sync failed; status and unsynced count.
3. **Api integration** (Testcontainers + the in-process simulator):
   - completing B-24187 → a row in the same transaction;
   - dispatch → **exactly one receipt at `/_sim/orgatex/consumption`** with the right lines and key; dispatched twice → still one;
   - `/_sim/faults orgatex unavailable` → completion succeeds, pending, retries on schedule (fake clock), sync failed at 5, Retry after the fault clears → sent once;
   - a restart resumes pending rows;
   - the summary, queue and health fields.
4. **Web**: the summary states and Retry, the strip's unsynced count, the queue tags.
5. **STEP 26 evidence**: a live run captured to `evidence/STEP-26-writeback.txt` (the request with `Idempotency-Key`, the 200 OK receipt, the stored data from `/_sim/orgatex/consumption`), plus a guide for `STEP-26` UI screenshots.

### Out of scope

- The ticket (bolt 012) and the full Confirm & print screen (bolt 013).
- Editing or cancelling a write-back; multiple stations sharing an outbox.
