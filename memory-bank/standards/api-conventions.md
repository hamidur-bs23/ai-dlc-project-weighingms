# API Conventions

## Overview
API design conventions and response formats for **WeighingMS.Api** (consumed by the Angular SPA) and **WeighingMS.Simulators.Host** (training contracts consumed by WeighingMS.Api). The simulator contracts are **training contracts, not claims about the real OrgaTEX interface**. The brief's unresolved integration questions stay unresolved, and real contracts are never invented.

## API Style
**Resource-oriented REST over HTTPS/JSON**, implemented with ASP.NET Core Controllers and documented with OpenAPI 3.1.

- Base path `/api`. Resources are plural nouns, path segments kebab-case, identifiers as path parameters in camelCase in docs (`{batchId}`)
- **Workbook contract paths are preserved exactly** (both in the simulator and in WeighingMS.Api's UI-facing surface):

| Integration | Endpoints (minimum) |
|---|---|
| OrgaTEX | `GET /api/batches` · `GET /api/batches/{batchId}` · `GET /api/batches/{batchId}/recipe` · `POST /api/batches/{batchId}/consumption` |
| Scale | `GET /api/scales` · `GET /api/scales/{scaleId}/status` · `GET /api/scales/{scaleId}/weight` · `POST /api/scales/{scaleId}/tare` |
| Printer | `POST /api/tickets` |
| Scanner | **contract TBD** (defined during Requirements/System Context) |

- WeighingMS-specific operations are additional resources/sub-resources defined during Inception (e.g. weighing sessions and component confirmations). Their names are not fixed here
- Methods: `GET` safe/idempotent; `POST` for commands/creation; `PUT`/`PATCH` only for true updates; `DELETE` not used for business records (records are append-only)
- Commands are expressed as sub-resource POSTs (e.g. `POST …/tare`, `POST …/consumption`), not RPC verbs in query strings

## Versioning
- **No version segment in URLs in V1.** The OpenAPI document is `v1` (`/openapi/v1.json`)
- Changes are **additive only** (new optional fields, new endpoints). Breaking changes require a new document version plus an ADR
- The simulator contracts follow the same rule; a contract change is recorded in the decision index

## Response Format
- `Content-Type: application/json; charset=utf-8`; property names **camelCase**; enums as **strings** (`"inTolerance"`, `"precision"`)
- Timestamps **ISO-8601 UTC** with `Z` (`2026-11-04T09:15:30.123Z`); display formatting is a UI concern
- **Quantities are never bare numbers.** They use `{ "value": 94.0, "unit": "g" }`, where value is a JSON number serialised from `decimal` (no float math server-side); precision follows the scale resolution
- Single resources are returned as the object itself (no envelope); collections as `{ "items": [ … ] }` (room for metadata later)
- `201 Created` + `Location` for created resources; `200 OK` with body for commands that return state; `204 No Content` when nothing is returned
- `Cache-Control: no-store` on all `/api/*` responses

Example: `GET /api/scales/scale-3kg/weight`
```json
{
  "scaleId": "scale-3kg",
  "gross": { "value": 94.6, "unit": "g" },
  "tare":  { "value": 0.0,  "unit": "g" },
  "net":   { "value": 94.6, "unit": "g" },
  "stable": true,
  "readAt": "2026-11-04T09:15:30.123Z"
}
```

## Error Format
**ProblemDetails (RFC 9457)**, `Content-Type: application/problem+json`.

```json
{
  "type": "Weighing.OutOfTolerance",
  "title": "Weight outside tolerance",
  "status": 422,
  "detail": "YEL-RGB: 94.6 g outside 93.5–94.5 g",
  "instance": "/api/…",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errorCode": "Weighing.OutOfTolerance",
  "batchId": "B-24187",
  "net": 94.6, "min": 93.5, "max": 94.5
}
```

| ErrorOr type | HTTP | Typical codes |
|---|---|---|
| Validation | 422 (`ValidationProblemDetails` with `errors{}` for field errors) | `Weighing.OutOfTolerance`, `Request.Invalid` |
| Conflict | 409 | `Weighing.AlreadyConfirmed`, `Weighing.OutOfOrder`, `Batch.Incomplete` |
| NotFound | 404 | `Batch.NotFound`, `Scale.NotFound` |
| Unauthorized | 401 | `Auth.SignInRequired` |
| Forbidden | 403 | `Auth.SupervisorRequired`, `Auth.StepUpFailed` |
| Unavailable (custom) | 503 (+ `Retry-After` when known) | `Scale.Unavailable`, `OrgaTex.Unavailable`, `Printer.Unavailable` |
| TooManyRequests | 429 (+ `Retry-After`) | `Auth.LockedOut` |
| Unexpected | 500 | `Server.Unexpected` (no internal details leaked) |

- `type` and `errorCode` carry the stable error code. The UI maps the code to a translated message (Transloco) and never displays `detail` verbatim to operators
- `traceId` is always present (shown as "Ref" in the UI)

## Pagination
**None in V1.** The batch queue and recipes are small (station-scoped). If needed later: cursor-based (`?cursor=&limit=`) returning `{ items, nextCursor }`, added as an additive change.

## Idempotency & Concurrency
- `POST /api/batches/{batchId}/consumption` and `POST /api/tickets` accept an **`Idempotency-Key`** header (UUID). Retries with the same key return the original result and never duplicate a write-back or a print. The outbox reuses the key on every retry
- Mutable aggregates expose an `ETag`/version where concurrent edits are possible; mismatches return 409 `Concurrency.Conflict`

## Headers
| Header | Direction | Purpose |
|---|---|---|
| `traceparent` / `tracestate` | in/out | W3C trace context (OTel) |
| `X-XSRF-TOKEN` | in | Antiforgery for cookie-authenticated state changes |
| `Idempotency-Key` | in | Safe retries of write-back and print |
| `Retry-After` | out | On 503/429 |

## Simulator Conventions (`WeighingMS.Simulators.Host`)
- Exposes the workbook contract paths above, plus **simulation control** under `/_sim/*`, enabled only in Development/Test:
  - `POST /_sim/reset`: reload the JSON seed (scenario B-24187 …)
  - `POST /_sim/faults`: `{ "target": "orgatex|scale:{id}|printer|scanner", "mode": "unavailable|timeout|error500|slow", "durationMs"?: n }`; `DELETE /_sim/faults` clears them
  - `POST /_sim/scales/{scaleId}/scenario`: `{ "scenario": "rising|belowRange|enterRange|overshoot|stable", "target": {…} }` (PDF simulation scenarios; readings respect the scale resolution)
  - `POST /_sim/scanner/scan`: emit a scan (`{ "code": "…" }`)
  - `GET /_sim/orgatex/consumption` and `GET /_sim/printer/tickets`: inspect what was received (evidence for STEP-26/27)
- Simulator responses use the same JSON, quantity and ProblemDetails conventions
