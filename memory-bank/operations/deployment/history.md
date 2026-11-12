# Deployment history

Whole-system releases (OPS-SCOPE-SYSTEM). The environments are defined in OPS-ENVIRONMENTS (`standards/decision-index.md`). Newest first.

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
