---
stage: test
bolt: 014-assisted-weighing-station-ui
created: 2026-11-10T18:45:00Z
---

## Test Report: assisted-weighing-station-ui (bolt 014)

### Summary

- **E2E (Playwright)**: **15/15 passed**, twice, each from a clean stack (Compose `full`, isolated project). That is the B-24187 flow, the STEP 30 suite, two smoke tests and accessibility checks on four screens.
- **Web tests**: **205/205 passed**, 11 new (194 before).
- **Backend tests**: **717/717 passed**, 11 new (706 before): 9 for the Api serving the web build, 2 for the new simulator endpoint.
- **Coverage**: web 97.87% of lines overall, 98% for `features/weigh` and 99.53% for `features/batch-summary`. Backend line coverage was not re-measured.
- **Other checks**: backend build 0 warnings, format clean, lint clean, web production build, E2E typecheck, `specsmd:validate` 0 issues, `specsmd:integrity` 0 inconsistencies. The development volume `weighingms_pgdata` was never touched, and no E2E containers or volumes are left.

### Test Files

- [x] `web/src/app/features/weigh/weighing-session.store.spec.ts` - 4 new: a failed read clears the weight; tare stops on any failed read, "offline" only for `Scale.Unavailable`; tare returns with readings; another component starts with no old failure
- [x] `web/src/app/features/weigh/weigh.component.spec.ts` - 4 new: "—" and SCALE OFFLINE with Confirm and Tare disabled; F1 and F2 do nothing offline; Quit still works; OVER CAPACITY
- [x] `web/src/app/features/batch-summary/confirm-and-print.spec.ts` - 2 new: the header named from the summary on a direct URL and from the review list, cleared on Back
- [x] `web/src/app/features/batch-summary/batch-summary.component.spec.ts` - 1 new and 1 changed: a failed Retry keeps the screen without reloading; a refused one keeps it and catches up
- [x] `web/src/app/features/batch-review/batch-review.component.spec.ts` - 1 changed: Retry asks again for the page that failed
- [x] `tests/WeighingMS.Api.IntegrationTests/StationWebHostingTests.cs` - UI routes get `index.html`, build files are served, unknown `/api` paths stay a non-HTML 404, the Api still answers, nothing is served without a build
- [x] `tests/WeighingMS.Simulators.Tests/ScaleScenarioTests.cs` - 2 new: an empty container after a pour, and an unknown scale
- [x] `playwright/tests/01-smoke.e2e.ts` - the packaged app serves the UI and stays an Api
- [x] `playwright/tests/02-batch-b24187.e2e.ts` - STEP 29: sign in, open B-24187, weigh all five on the real Weigh screen (scale switch to the floor scale), complete, OrgaTEX write-back, print, Next batch, review read-only
- [x] `playwright/tests/03-failures.e2e.ts` - STEP 30: scale, OrgaTEX (on load and at completion) and printer unavailable, invalid weight, over and under tolerance, re-tare, quit, incomplete batch, scanner down, the 3.0 kg boundary
- [x] `playwright/tests/support/station.ts` - the seed's components, the simulator client, sign-in, weighing and accessibility helpers

### Acceptance Criteria Validation

- ✅ **Scale offline**: "—", SCALE OFFLINE, Confirm and Tare disabled (web specs and live).
- ✅ **OrgaTEX down on load**: an error with Retry, live.
- ✅ **Scanner down**: SCANNER OFFLINE, batch opened by touch (SCANNER-FALLBACK-SCOPE), live.
- ✅ **Printer down**: print pending, live.
- ✅ **No tolerance defined / Over capacity**: both messages with Confirm disabled. Over capacity live; no tolerance in the web specs only, because no seed chemical lacks one.
- ✅ **Translated message by code, never raw details**: every code the Api can send has text; the two simulator-only codes cannot reach the UI.
- ✅ **B-24187 completes on the full stack**: all five components weighed on the real screens.
- ✅ **One post at OrgaTEX and the ticket at the printer**: exactly one consumption post with the five lots, and one ticket naming all five chemicals.
- ✅ **Each STEP 30 behaviour verified**: see `evidence/STEP-30-failure-tests.md`.

### Test Quality Check

Six deliberate faults were put in one at a time and reverted, with checksums confirming the source was restored. Every one made tests fail:

- [x] Keeping the stale reading after a failed read: 1 test failed.
- [x] Letting Tare ignore a failed read: 4 failed.
- [x] Leaving the Tare button on the old rule: 1 failed.
- [x] A refused Retry replacing the screen: 2 failed.
- [x] Not naming the header from the summary: 2 failed.
- [x] Unknown `/api` paths falling through to `index.html`: 2 failed.

The E2E tests were not fault-checked this way, but their first runs failed for real defects (below), which shows they can fail.

### Issues Found

Found and fixed during this stage:

- **The dummy scale could not change containers** (SIM-EMPTY-CONTAINER, decided with you). After one pour, the next component's tare included it, so on the real Weigh screen only the first component per scale could be weighed. `POST /_sim/scales/{scaleId}/container` now puts an empty container on.
- **Text contrast below WCAG AA, found by axe**: `--hmi-dim` was 2.9–3.6:1 on the dark panels (station name, table headers, rail meta) and is now #8193a5 (≥ 4.6:1). The faded F1 hint on the green OK key was 3.8:1 and is now at full strength (6.9:1).
- **Two of my own test mistakes**: a smoke test looked for a "Sign in" button (the key is "OK F1"), and the quit test expected the attempt to stay on screen (the app correctly clears it).
- **npm in the image** (Implement stage): the Node image's npm 11 was refused by `engine-strict`; the build installs npm 12.2.1.

Known and not fixed:

- **Mixed units for a kg target on the precision scale**: B-24190's Remazol Black B shows Target 3.000 kg but the readout and tolerance in g. The values are right.
- **Dim text is lighter than the mockup** because of the contrast fix, and sits closer to the muted text.
- **CI**: no pipeline yet (E2E-CI-DEFERRED), so "the suite in CI" is still a gap.
- **The barcode** has been decoded by software (bolt 013), never by a physical scanner.

### Evidence

- [x] `evidence/STEP-29-playwright-report/` - the HTML report of the second full run (15/15), with traces and videos
- [x] `evidence/STEP-29-e2e-video.mp4` - the B-24187 run (converted from Playwright's WebM to H.264 in a Debian container, since Playwright's own ffmpeg can't write MP4)
- [x] `evidence/STEP-30-failure-tests.md` - Scenario | Expected | Actual | Pass, all passing
- [x] `evidence/STEP-28-ui-validation/screen-1-comparison.png`, `screen-2-comparison.png`, `screen-3-comparison.png` - mockup beside the app (screen 3 retaken with the green title)
- [x] `evidence/STEP-28-ui-validation/validation.md` - the comparison and the known differences

### Notes

- **The pictures were not checked by me**: `.claudeignore` excludes images, so the comparison in `validation.md` is from each screen's text and behaviour. The PNGs are for you to judge.
- **Run time**: the suite takes about 3.5 minutes after the stack is built; three tests weigh whole batches at the simulator's 8-second pour.
- **Order matters**: the tests share one stack and four seed batches, so they run in file order with one worker; `03-failures` is serial.
- `playwright/evidence/capture-step28.mjs` reproduces the STEP 28 sheets against a running E2E stack.
