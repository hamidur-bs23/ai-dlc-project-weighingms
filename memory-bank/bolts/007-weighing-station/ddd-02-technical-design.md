---
unit: 005-weighing-station
bolt: 007-weighing-station
stage: design
status: complete
updated: 2026-11-05T17:50:00Z
---

# Technical Design - Weighing Station (bolt 007)

## Architecture Pattern

**Clean modular monolith (existing)**, with the weighing rules in a pure Domain module:
- **Domain** (`WeighingMS.Domain/Weighing`): the `WeighingSession` aggregate, its value objects and the three pure domain services (`ScaleSelector`, `ToleranceResolver`, `ReadingClassifier`). No I/O, no ASP.NET, no EF Core (enforced by the architecture test).
- **Application** (`WeighingMS.Application/Weighing`): use cases `StartOrResumeSession`, `GetSession`, `SelectComponent`, `ClassifyCurrentReading`. They orchestrate the existing ports: the recipe snapshot (unit 003, through `BatchService`), the tolerance catalogue (unit 003), the scale port (unit 004), the plant clock and audit trail (unit 002), plus the new session repository.
- **Infrastructure**: EF Core persistence of the session (`xmin` concurrency) and the scale-selection options.
- **Api**: `WeighingSessionController`, the session sub-resource of a batch.

Rationale: the boundary tables of STEP 22 and FR-8 run as plain unit tests on the Domain; the HTTP layer only maps results (ErrorOr → ProblemDetails), as in bolts 003–005.

## Layer Structure

```text
┌──────────────────────────────────────────────────────────────┐
│ Api          WeighingSessionController  (/api/batches/{id}/session…)
│              contracts: SessionResponse, ComponentResponse, ReadingResponse
├──────────────────────────────────────────────────────────────┤
│ Application  WeighingSessionService: StartOrResume · Get · Select · ClassifyReading
│              ports: IWeighingSessionRepository (new) · BatchService · IToleranceCatalogue
│                     IScaleDevice · IPlantClock · IAuditTrail
│              options: ScaleSelectionOptions (validated on start)
├──────────────────────────────────────────────────────────────┤
│ Domain       WeighingSession (root) · ComponentWeighing
│              Quantity (+ConvertTo, RoundTo) · ScaleClass · ScaleSelectionPolicy · ScaleSelection
│              ScaleProfile · ToleranceBand · ToleranceResolution · WeightClass · Classification
│              ScaleSelector · ToleranceResolver · ReadingClassifier · WeighingErrors · events
├──────────────────────────────────────────────────────────────┤
│ Infra        WeighingSessionRepository (EF Core, xmin) · configurations · migration AddWeighingSessions
└──────────────────────────────────────────────────────────────┘
```

### Flow: start or resume (story 001)
1. Load the session for the batch. If one exists → **resume**: record `WeighingSessionResumed`, return it (no device or OrgaTEX call needed).
2. Otherwise get the recipe snapshot through `BatchService.GetRecipeAsync` (snapshots on first call; 404 / 502 / 503 pass through).
3. Read the scale list once from the scale port (unit 004) to get each assigned scale's **profile** (unit, resolution). Scale port down → 503 `Scale.Unavailable` (Retry); nothing stored.
4. For every recipe line: `ScaleSelector` picks the class with the current `ScaleSelectionPolicy` (threshold + assignments); `ToleranceResolver` picks recipe → configured (chemical + class) → blocked. Both are **fixed** on the component from here (A1).
5. Create the session (all Pending, suggested = first in sequence), store it, record `WeighingSessionStarted`. Two first opens racing → the unique `batch_id` keeps one; the loser reloads and resumes.

### Flow: classify the current reading (story 006)
1. Load the session; there must be an Active component (else 409 `Weighing.NoActiveComponent`).
2. Read the weight **from that component's scale only** (FR-8) through the scale port.
3. If the tolerance is blocked → return the reading with `tolerance.status = blocked`, no class (A3).
4. Otherwise `ReadingClassifier` rounds the net to the stored resolution (half away from zero, WEIGH-ROUNDING) and classifies in the scale's unit (A4). Remaining = `max(0, target − max(0, net))` (A5). Malformed or over-capacity readings get no class (validity is returned as is).
5. Nothing is stored and no event is raised: classification is a read. (Confirm, in bolt 008, re-reads and re-classifies on the server; it never trusts a class sent by the UI.)

## API Design

All endpoints require a signed-in operator (`RequireOperator`). Commands need `X-XSRF-TOKEN`. Errors are ProblemDetails with `errorCode` and `traceId`. Quantities are `{ value, unit }`.

| Endpoint | Method | Request | Response |
|----------|--------|---------|----------|
| `/api/batches/{batchId}/session` | POST | — | **201** `SessionResponse` (started, `Location` = the GET) · **200** `SessionResponse` (resumed) |
| `/api/batches/{batchId}/session` | GET | — | 200 `SessionResponse` · 404 `Weighing.SessionNotFound` |
| `/api/batches/{batchId}/session/selection` | POST | `{ "sequence": 3 }` | 200 `SessionResponse` (component 3 Active) |
| `/api/batches/{batchId}/session/reading` | GET | — | 200 `ReadingResponse` |

`SessionResponse`:
```json
{
  "batchId": "B-24187",
  "status": "inProgress",
  "startedAt": "2026-11-05T01:12:31Z",
  "startedBy": "OP-1001",
  "version": 3,
  "progress": { "done": 0, "total": 5 },
  "suggestedSequence": 1,
  "activeSequence": 3,
  "components": [
    {
      "sequence": 3, "chemicalId": "DYE-4471", "code": "DYE-4471", "name": "Remazol Yellow RGB",
      "target": { "value": 94.0, "unit": "g" },
      "state": "active",
      "scale": { "scaleId": "scale-3kg", "scaleClass": "precision", "unit": "g", "resolution": { "value": 0.1, "unit": "g" }, "automatic": true },
      "tolerance": {
        "status": "resolved", "source": "recipe",
        "plusMinus": { "value": 0.5, "unit": "g" },
        "lower": { "value": 93.5, "unit": "g" }, "upper": { "value": 94.5, "unit": "g" }
      },
      "suggestedContainer": "C-12"
    }
  ]
}
```
A blocked component has `"tolerance": { "status": "blocked", "source": null, … null }` and the UI shows `errors.Weighing.NoTolerance` ("No tolerance defined: ask a supervisor").

`ReadingResponse` (polled by the Weigh screen at the configured interval, default 250 ms):
```json
{
  "sequence": 3, "scaleId": "scale-3kg",
  "net": { "value": 93.5, "unit": "g" }, "stable": true, "validity": "valid", "readAt": "2026-11-05T01:15:02Z",
  "class": "withinRange",
  "remaining": { "value": 0.5, "unit": "g" },
  "band": { "target": { "value": 94.0, "unit": "g" }, "plusMinus": { "value": 0.5, "unit": "g" }, "lower": { "value": 93.5, "unit": "g" }, "upper": { "value": 94.5, "unit": "g" } },
  "toleranceStatus": "resolved"
}
```
`class` is one of `underweight | withinRange | targetAchieved | overweight`, or `null` (blocked tolerance, malformed or over-capacity reading). Values are in the **scale's unit**, which for every seeded component is also the recipe unit (g on the precision scale, kg on the floor scale).

## Data Persistence

Migration **`AddWeighingSessions`** (snake_case, UUIDv7 keys, `numeric(12,4)` + unit, `timestamptz` UTC):

| Table | Columns | Relationships |
|-------|---------|---------------|
| `weighing_sessions` | `id` uuid PK · `batch_id` text **unique** · `status` text · `started_at` timestamptz · `started_by` text (badge) · `active_sequence` int null · `xmin` (concurrency token) | 1 → n components; `batch_id` matches `recipe_snapshots.batch_id` (natural key) |
| `weighing_session_components` | `id` uuid PK · `session_id` uuid FK · `sequence` int · `chemical_id`, `code`, `name` text · `target_value` numeric(12,4), `target_unit` text · `scale_id`, `scale_class`, `scale_unit` text · `scale_resolution` numeric(12,4) · `tolerance_value` numeric(12,4) null, `tolerance_unit` text null, `tolerance_source` text null (`recipe` / `configured`; null = blocked) · `state` text · unique (`session_id`, `sequence`) | n → 1 session (cascade delete only for tests; no API deletes) |

Bolt 008 adds attempt columns (lot, container, tare) and the records table; bolt 009 adds completion. All additive.

## Configuration

```json
"Weighing": {
  "ScaleSelection": {
    "ThresholdKg": 3.0,
    "Assignments": { "Precision": "scale-3kg", "HeavyDuty": "scale-32kg" }
  }
}
```
- Validated on start (`ValidateOnStart`): threshold > 0; both classes assigned to non-empty scale IDs. Invalid → the Api does not start (same pattern as `Tolerances`).
- Read through `IOptionsMonitor` **when a session starts**, so a change applies to the next batch without a restart, never to an open one (A1).
- The STEP 21 scale switch (Yellow RGB → `scale-3kg`, Sodium Sulphate → `scale-32kg`) is the default.

## Security Design

| Concern | Approach |
|---------|----------|
| Authentication | Existing cookie session (`__Host-` cookie outside Development); every endpoint `RequireOperator` |
| Authorization | Any signed-in operator may start, resume, select and read (SESSION-RESUME). Supervisor step-up is not needed in this bolt (override: bolt 009) |
| Data Encryption | No new sensitive data; transport as today (HTTPS on the station network) |
| Forgery | `POST …/session` and `POST …/selection` pass the existing antiforgery filter (`X-XSRF-TOKEN`) |
| Audit | Started, resumed, component selected and attempt abandoned are written to the existing audit trail with badge and UTC time |

## NFR Implementation

| Requirement | Design Approach |
|-------------|-----------------|
| Performance | The reading endpoint does one indexed session load + one device call; no write. Healthy reading polls are not traced (existing `HealthyWeightPollFilter` pattern is extended to `…/session/reading`) |
| Scalability | One station in V1; nothing shared in memory between requests |
| Reliability | All state in PostgreSQL; an Api restart restores the session exactly (story 001). Resume needs neither OrgaTEX nor the scales |
| Correctness | `decimal` only; exact unit conversion; rounding rule WEIGH-ROUNDING in one place (`Quantity.RoundTo`) |
| Concurrency | `xmin` on `weighing_sessions`; two changes at once → second gets 409 `Concurrency.Conflict` (A2) |

## Error Handling

| Error Type | Code | Response |
|------------|------|----------|
| Batch unknown | `Batch.NotFound` | 404 (from `BatchService`) |
| OrgaTEX down, recipe not yet snapshotted | `OrgaTex.Unavailable` | 503 (Retry) |
| Invalid recipe from OrgaTEX | `OrgaTex.InvalidRecipe` | 502 |
| Scale list or reading unavailable | `Scale.Unavailable` | 503 (Retry) |
| Assigned scale not in the scale list | `Weighing.ScaleNotAvailable` | 503 (configuration / device mismatch; nothing stored) |
| No session for the batch | `Weighing.SessionNotFound` | 404 |
| Unknown component sequence | `Weighing.ComponentNotFound` | 404 |
| Selecting a done component | `Weighing.ComponentDone` | 409 (state unchanged, story 002) |
| Reading with no active component | `Weighing.NoActiveComponent` | 409 |
| Concurrent change | `Concurrency.Conflict` | 409 ("changed elsewhere, reload") |
| Bad request body | `Request.Invalid` | 422 |

Blocked tolerance is **not** an error on the reading endpoint (it is polled): it is `toleranceStatus: "blocked"` with `class: null`.

## External Dependencies

| Service | Purpose | Integration |
|---------|---------|-------------|
| Dummy OrgaTEX (through unit 003) | Recipe snapshot on first start | Existing `BatchService` / `IOrgatexGateway` (REST to the simulator) |
| Dummy scales (through unit 004) | Scale list (profile at start) and live reading | Existing scale port + resilience pipeline (REST to the simulator) |
| Station access (unit 002) | Operator identity, plant clock, audit | In-process ports |

## Decisions at This Checkpoint: resolved (APPROVE DESIGN, 2026-11-05)

- **T1: option 1.** `GET /api/batches/{id}/session/reading` (WEIGH-READING).
- **T2: option 1.** Explicit `POST /api/batches/{id}/session` start-or-resume (SESSION-START).
- **T3: option 1.** Scale unit and resolution stored on each component at session start (SCALE-PROFILE).
- **Stage 3 (ADR): skipped** as recommended; T1–T3 recorded in the decision index.

#### Options as presented

**T1. Where does the UI get the classified reading?**
1 - **(Recommended)** `GET /api/batches/{id}/session/reading`. The server reads the Active component's own scale, so the UI can't classify against the wrong scale (FR-8), and one call returns weight + class + remaining. The raw `/api/scales/{id}/weight` stays a pure device endpoint (STEP 21 contract unchanged).
2 - Enrich `GET /api/scales/{id}/weight?batchId=…` with the class. Fewer endpoints, but the device contract then depends on the session, and a wrong `scaleId` in the UI would be possible.

**T2. When does a session start?**
1 - **(Recommended)** Explicitly, with `POST /api/batches/{id}/session` when the Weigh screen opens (start or resume; 201 / 200). Opening the recipe view (bolt 006) stays a read, and the STEP 19 three-call flow is unchanged.
2 - Implicitly, on the first `GET …/recipe`. One call fewer, but a GET would create state.

**T3. Where do the scale unit and resolution come from?**
1 - **(Recommended)** Read once from the scale list when the session **starts** and stored on each component, like the tolerance (A1). Resume never needs the scales; a new session can't start while the scale list is unavailable (503, Retry).
2 - Read from the scale list on every reading poll. Always current, but one extra device call every 250 ms.

## ADR Analysis (Stage 3)

No ADR is proposed: T1–T3 follow existing conventions (sub-resource commands, snapshot at first use as in RECIPE-SNAPSHOT, options validated on start). They are recorded in the decision index on approval. Stage 3 can be skipped unless you want an ADR for T1.
