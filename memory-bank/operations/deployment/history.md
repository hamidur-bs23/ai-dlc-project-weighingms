# Deployment history

Whole-system releases (OPS-SCOPE-SYSTEM). The environments are defined in OPS-ENVIRONMENTS (`standards/decision-index.md`). Newest first.

---

```yaml
version: 1.0.0
environment: prod
deployed: not deployed (handoff package, OPS-ENVIRONMENTS)
packaged: 2026-11-12T20:00:00Z
deployed_by: Hamidur-BS23 (with Claude Code)
status: handoff-not-production-ready
```

## Deployment: 1.0.0 → prod (handoff package)

### Details

- **Version:** `1.0.0` (tag `v1.0.0`, commit `586bf5f`)
- **Environment:** production is one on-premise station or site server per site (brief.md A-11, **unconfirmed**). No such server is available, so production is a **handoff package**, not a deployment (OPS-ENVIRONMENTS, Checkpoint 3).
- **Package:** `deploy/` (committed: `docker-compose.prod.yml`, `production.env.template`, `README.md` runbook) plus `dist/release/` (gitignored image tarballs: arm64 as built, and linux/amd64 built with buildx from the same tag; checksums in `SHA256SUMS`).
- **Previous version:** none

### Production rehearsal (local, Production environment, arm64 images)

`deploy/docker-compose.prod.yml` was started exactly as written, as project `weighingms-prodrehearsal`, then removed with `down --volumes`.

| Check | Result |
|---|---|
| Stack up | ✅ 24 s: Postgres, simulators and Api healthy, migrator exited 0. Only the Api is published, on `127.0.0.1` |
| `/_sim/reset` on the simulators | ✅ 404, off outside Development |
| `GET /api/antiforgery` over plain HTTP | ❌ **500**: "SecurePolicy = Always, but the current request is not an SSL request" |
| The same through a simulated TLS proxy (`X-Forwarded-Proto: https`) **before** the fix | ❌ 500, because the Api has no forwarded-headers handling |
| The same **after** adding `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` | ✅ 204, with `__Host-wms.af` and `XSRF-TOKEN` set as `Secure` and `SameSite=Strict` |
| `POST /api/session` with `OP-1001` | ❌ **401 `Auth.BadgeNotRecognised`**: `station_users` has 0 rows (OPS-PROD-USERS confirmed) |

### Status

**Not production-ready.** The package installs and runs, but no one can sign in (OPS-PROD-USERS). OPS-FORWARDED-HEADERS was found and mitigated in the stack configuration. OPS-ARCH is resolved: the linux/amd64 tarballs were boot-tested with this runbook (containers report x86_64, healthy in 24 s). The full list is in `deploy/README.md` → Known blockers.

### Rollback

Nothing is deployed. The runbook's rollback (database backup, then the previous `WMS_VERSION`) applies from the second release onwards.

---

```yaml
version: 1.0.0
environment: staging
deployed: 2026-11-12T19:40:41Z
deployed_by: Hamidur-BS23 (with Claude Code)
status: success
```

## Deployment: 1.0.0 → staging

### Details

- **Version:** `1.0.0`. These are the exact release images; nothing was rebuilt (`up --no-build`): `weighingms/api` `4ea6093b7d98`, `weighingms/migrator` `73f4b56a11e2`, `weighingms/simulators` `725670276a82`, `postgres:17`.
- **Environment:** staging, the Compose project `weighingms-staging` (profile `full`) with its own fresh volume. Ports: Api and web **5380**, simulators **5390**, Postgres **5435**. The env file has a newly generated database password and is kept outside the repo.
- **ASP.NET environment:** `Development`, for both the Api and the simulators (OPS-STAGING-DEV). This seeds the lab users and is the same as the E2E stack. **Staging therefore proves the images and the workflow, not the production settings.**
- **Timestamp:** 2026-11-12T19:40:41Z (01:40 Asia/Dhaka)
- **Duration:** 12 s to healthy (migrator exited 0; Postgres, simulators and Api healthy)
- **Previous version:** none

### Changes

This is the first staging deployment. Database: all 7 migrations applied to an empty database.

### Commands

```bash
# env file: WMS_VERSION=1.0.0, API_ENVIRONMENT=Development, SIMULATORS_ENVIRONMENT=Development,
#           API_PORT=5380, SIMULATORS_PORT=5390, POSTGRES_PORT=5435, POSTGRES_DB/USER=weighingms_staging,
#           POSTGRES_PASSWORD=<generated>
docker compose --project-name weighingms-staging --env-file staging.env --profile full up --no-build --detach --wait
```

### Verification

[`verification-1.0.0-staging.md`](verification-1.0.0-staging.md): passed (smoke checks, plus E2E 15/15 against the same images).

### Rollback

There's no earlier version. To remove it: `docker compose --project-name weighingms-staging --env-file staging.env --profile full down --volumes`.

---

```yaml
version: 1.0.0
environment: dev
deployed: 2026-11-12T19:38:05Z
deployed_by: Hamidur-BS23 (with Claude Code)
status: success
```

## Deployment: 1.0.0 → dev

### Details

- **Version:** `1.0.0` (tag `v1.0.0`, commit `586bf5f`)
- **Environment:** dev, i.e. the developer machine. `npm run dev` runs the simulators (:5090), the Api (:5080, `Development`) and the Angular dev server (:4200, proxying `/api`), all from source. The dev Postgres runs in Docker (`docker compose up -d postgres`, host port 5433).
- **Timestamp:** 2026-11-12T19:38:05Z (01:38 Asia/Dhaka)
- **Duration:** about 1 min, including the migration
- **Previous version:** none; this is the first recorded deployment

### Changes

- `npm run db:update` applied the pending migration `20261106042726_AddTickets` (bolt 012). The dev database had been one migration behind, because the bolt 012–014 tests run on Testcontainers and the isolated E2E stack, never on the dev database.
- Dev runs from source, not from the release images. The images are first used at staging.

### Verification

[`verification-1.0.0-dev.md`](verification-1.0.0-dev.md): passed.

### Rollback

Dev has no earlier version to roll back to. To return to the pre-release tree, run `git checkout b7c6a24` (the release commit changed only versions and image names). The `AddTickets` migration is additive, and the bolt 012 code needs it.
