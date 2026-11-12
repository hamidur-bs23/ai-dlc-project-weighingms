---
version: 1.0.0
environment: staging
verified: 2026-11-12T19:47:00Z
status: passed
---

# Verification report: 1.0.0 → staging

## Deployed images

| Service | Image | Running image ID | Matches build.md |
|---|---|---|---|
| api | `weighingms/api:1.0.0` | `sha256:4ea6093b7d98…` | ✅ |
| migrator | `weighingms/migrator:1.0.0` | `sha256:73f4b56a11e2…` | ✅ |
| simulators | `weighingms/simulators:1.0.0` | `sha256:725670276a82…` | ✅ |

The release tags still point at the same IDs after every run below, so nothing was rebuilt.

## Health and database

| Check | Result |
|---|---|
| Migrator | ✅ exited 0; 7 migrations applied, 7 rows in `__EFMigrationsHistory`, 7 migrations in source |
| Api `GET /health/ready` (:5380) | ✅ 200 |
| Simulators `GET /api/scales` (:5390) | ✅ 200 |
| Api log | ✅ 0 `ERR`/`FTL` lines after the smoke test |

## Smoke test (the Api serves the web build itself, with no dev server)

| Step | Result |
|---|---|
| `GET /` | ✅ 200, `<title>WeighingMS` (Angular build from `wwwroot`) |
| `GET /batches/B-24187/weigh` (deep link) | ✅ 200 (SPA fallback) |
| `GET /api/nope` | ✅ 404 (unknown API paths are not swallowed by the SPA fallback) |
| `GET /api/antiforgery` → `POST /api/session` with `OP-1001` | ✅ 204 → 200 |
| `GET /api/batches` | ✅ 200: B-24187, B-24188, B-24190, B-24191 |
| `GET /api/batches/B-24187/recipe` | ✅ 200: 5 components |
| `GET /api/devices/health` | ✅ 200: 5 devices ready |

## Functional: E2E against the release images

The E2E suite needs its own settings (`playwright/compose.e2e.yml`: a higher sign-in rate limit and a write-back retry policy shortened to seconds), so it ran on its isolated project `weighingms-e2e` (:5280/:5290/:5434). It used the **same 1.0.0 images**, started with `WMS_VERSION=1.0.0 … up --no-build` and run with `E2E_REUSE_STACK=1`. `stack.mjs` itself would have used `up --build`. Afterwards the project was removed with `down --volumes`.

**Result: 15 passed, 0 failed (3.2 min), Node 26.13.0.**

- Smoke (2): the UI and a route opened by URL; ready, with an unknown API path returning 404
- End to end (1): B-24187, all five components → OrgaTEX write-back → ticket printed
- Failure scenarios (12): scale unavailable; under tolerance; over tolerance (overshoot recovery); invalid weight over capacity; exactly 3.0 kg uses the precision scale; re-tare; quit; printer unavailable (print pending, then printed); OrgaTEX unavailable on load; OrgaTEX unavailable at completion (sync failed → Retry → confirmed); scanner unavailable (touch fallback); incomplete batch

## What staging does not prove

| ID | Gap |
|---|---|
| OPS-STAGING-DEV | Staging runs in `Development` (lab users seeded, plain-HTTP cookies, `/_sim/*` control endpoints and Scalar on). The production settings are untested. |
| OPS-PROD-USERS | In `Production`, nobody can sign in, because users are seeded only in Development and Test. **This blocks a real production deployment.** |
| OPS-SECURE-COOKIES | Production uses `Secure` `__Host-` cookies and needs HTTPS on a station server reached by hostname. |
| OPS-ARCH | The images are linux/arm64 only. |
| Load and soak | Not tested. The target is one station per site (A-11). |

## Decision

✅ Staging passed for the images and the workflow. Production is **blocked** by OPS-PROD-USERS and OPS-ARCH for a real deployment. Under OPS-ENVIRONMENTS the production step is a release handoff with a runbook, and these gaps belong in it.
