---
version: 1.0.0
environment: dev
verified: 2026-11-12T19:39:00Z
status: passed
---

# Verification report: 1.0.0 → dev

## Health checks

| Check | Result |
|---|---|
| Api `GET /health/ready` (:5080) | ✅ 200 |
| Simulators `GET /api/scales` (:5090) | ✅ 200 |
| Web `GET /` (:4200) | ✅ 200 |
| Startup | Simulators listening at 01:38:03, Api at 01:38:05, no errors in the dev log |

## Smoke test (through the web proxy on :4200, the path operators use)

| Step | Result |
|---|---|
| `GET /api/antiforgery` | ✅ 204, XSRF cookie set |
| `POST /api/session` with `OP-1001` and the `X-XSRF-TOKEN` header | ✅ 200 |
| `GET /api/session` | ✅ 200: R. Karim, operator, STATION 01, Asia/Dhaka |
| `GET /api/batches` | ✅ 200: B-24187, B-24188, B-24190, B-24191 (from the dummy OrgaTEX) |
| `GET /api/batches/B-24187/recipe` | ✅ 200: 5 components |
| `GET /api/devices/health` | ✅ 200: scales ready |
| `GET /api/station` | ✅ 200 |

## Not covered here

The full end-to-end workflow (weigh, confirm, write-back, ticket) and the failure scenarios are verified at staging by the E2E suite against the release images. Dev runs from source.

## Decision

✅ Dev passed. The next step is Checkpoint 2, the staging deployment.
