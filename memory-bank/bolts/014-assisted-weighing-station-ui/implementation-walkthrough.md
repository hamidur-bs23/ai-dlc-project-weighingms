---
stage: implement
bolt: 014-assisted-weighing-station-ui
created: 2026-11-10T17:15:00Z
---

## Implementation Walkthrough: assisted-weighing-station-ui (bolt 014)

### Summary

The Weigh screen now treats a failed scale read as "no known weight": it clears the readout, says "SCALE OFFLINE", and disables Confirm, Tare and their keys until readings return. Over capacity and the remaining error codes now have messages. The four issues carried over from bolts 011 and 013 are fixed. The whole app now runs as the Compose `full` profile, and a Playwright workspace starts its own isolated copy of that stack.

### Structure Overview

The web fixes stay inside the existing screens and the session store, which still decides nothing itself: "scale offline" comes from the error code the Api returns. The Api serves the Angular build only when a `wwwroot` folder exists, so native development with `ng serve` is unchanged. One Dockerfile builds the Api with the web build inside it, and the migrations bundle as a separate target. Compose starts them in order: Postgres, then the migrator, then the Api once the migrator has finished and the simulators are healthy. The E2E workspace runs that same Compose file under its own project name, with its own ports and volume, plus a small override for test timings.

### Completed Work

- [x] `web/src/app/features/weigh/weighing-session.store.ts` - a failed read clears the reading; "scale offline" and "can tare" states; a new active component starts with no old failure
- [x] `web/src/app/features/weigh/weigh.component.ts`, `.html` - SCALE OFFLINE and OVER CAPACITY status lines; Tare and F2 follow the scale
- [x] `web/public/i18n/en.json` - the two status lines and text for `Scale.NotFound`, `Quantity.NotPositive`, `Quantity.UnknownUnit` and `Weighing.InvalidScalePolicy`
- [x] `web/src/app/core/batches/active-batch.service.ts` - the header holds only batch, order and machine, which the summary also carries
- [x] `web/src/app/features/batch-summary/batch-summary.component.ts`, `.html`, `.scss` - a refused Retry shows in the banner and keeps the screen; the header's batch line is filled on direct URLs and from the review list, and cleared on Back; the green title
- [x] `web/src/app/features/batch-review/batch-review.component.ts`, `.html` - Retry asks again for the page that failed
- [x] `web/src/app/features/batch-review/batch-review.component.spec.ts`, `batch-summary/batch-summary.component.spec.ts` - two existing tests changed to the approved behaviour (new tests are Stage 3)
- [x] `src/WeighingMS.Api/Common/StationWebHosting.cs`, `Program.cs` - serving the web build, sending UI routes to `index.html`, and keeping unknown `/api` paths a 404
- [x] `src/WeighingMS.Api/Dockerfile` - the web build, the Api publish and the EF migrations bundle, with `api` and `migrator` targets
- [x] `docker-compose.yml` - the `migrator` and `api` services in the `full` profile, with configurable ports
- [x] `.dockerignore`, `.gitignore` - build output, test output and tool folders kept out of images and git
- [x] `playwright/` - the `weighingms-e2e` workspace: config, isolated-stack script, Compose override, global setup and teardown, a smoke test
- [x] `package.json`, `package-lock.json` - the workspace and the `e2e`, `e2e:stack:up` and `e2e:stack:down` scripts
- [x] `memory-bank/standards/decision-index.md`, `CLAUDE.md` - D1 to D4 and the new commands

### Key Decisions

- **A failed read means no known weight**: keeping the last good reading avoided flicker, but it looked live. The server re-reads the scale on confirm, so this was never a data fault; it was a wrong display.
- **Any failed read disables Tare, but only `Scale.Unavailable` says "SCALE OFFLINE"**: other failures, such as the Api itself being unreachable, keep their own translated banner instead of a misleading label.
- **The header comes from the summary, not a second request**: the summary already carries batch, order and machine, so the header also works during an OrgaTEX outage.
- **A refused Retry reloads only when the refusal says the state changed** (`WriteBack.NotFailed`), the same way a stale print press is handled.
- **The workspace is named `weighingms-e2e`, not `playwright`**: a workspace called `playwright` would shadow the real `playwright` package in `node_modules`.
- **The E2E stack ignores `.env`**: its throwaway database password lives in the stack script, so a test run never reads or depends on your development secrets.
- **Test timings in an override file**: sign-in rate limit and write-back retry timings are shortened only for E2E, so the outage test reaches "sync failed" in seconds.

### Deviations from Plan

- **Two simulator-only error codes get no text**: `Printer.IdempotencyKeyMissing` and `Scanner.InvalidCursor` come from the dummy devices and are never passed on by the Api, so the screen can't show them.
- **A failed read on another component's scale**: the old failure is now cleared when the active component changes, so a second scale that works isn't shown as offline.
- **npm inside the image**: the official Node 26.13.0 image ships npm 11, which the workspace's `engine-strict` refuses. The web build stage installs npm 12.2.1, the version used locally. Found by the first real build.
- **Smoke test only**: the stack was built and started for real, and `npm run e2e` passed two smoke tests from a clean stack and then removed it. The B-24187 flow, the STEP 30 suite and the accessibility checks are written in Stage 3, as the plan says.

### Dependencies Added

- [x] `@playwright/test` 1.65.0 - the E2E runner (named in the tech-stack standard)
- [x] `@axe-core/playwright` 4.14.0 - accessibility checks in E2E (ux-guide)
- [x] `@types/node` 24.19.2 - types for the stack script and config, so the workspace typechecks

All are pinned exactly, in the `weighingms-e2e` workspace only.

### Developer Notes

- `npm run e2e` builds and starts the stack each run, then removes it. Use `npm run e2e:stack:up` and `E2E_REUSE_STACK=1 npm run e2e` for a faster loop, and `npm run e2e:stack:down` afterwards.
- The E2E stack uses ports 5280 (app), 5290 (simulators) and 5434 (Postgres), so it can run next to a native development stack.
- The image runs in the Development environment, because the lab users and `/_sim/*` exist only there. It is not a production image.
