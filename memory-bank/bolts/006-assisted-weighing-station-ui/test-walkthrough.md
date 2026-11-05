---
stage: test
bolt: 006-assisted-weighing-station-ui
created: 2026-11-05T17:02:00Z
---

## Test Report: assisted-weighing-station-ui (bolt 006)

### Summary

- **Frontend tests**: **77/77 passed** (Vitest 5 + Testing Library, jsdom), the same in 3 consecutive runs, ~1.6 s. 17 spec files:
  - 10 for core services;
  - 6 for components;
  - 1 for the shell.
- **Coverage** (v8, `ng test --coverage`): **98.4 % lines**, 95.1 % branches, 95.6 % functions.
  - Every file is at least 92 % lines, apart from the one-line default factory in `station-config.ts`.
  - The remaining gaps: the scale-name failure fallback, the interceptor's non-HTTP pass-through, and a not-yet-shown error branch on the queue template.
- **Lint**: `npm run lint` is clean. That covers ESLint (strict-type-checked, template a11y, `wms/no-batch-literals`) on app and spec code, and Stylelint. The rule self-check **fails a `const batches = [...]` fixture in app code** and passes the same code in a spec.
- **Build**: `ng build` (production) passes with 0 warnings.
- **Backend**: 293/293 tests pass with the Implement-stage strict-number change; `dotnet format` is clean.
- **Evidence**: `evidence/STEP-19-web-test.txt` holds the verbose test run plus the lint run. The live run is in the Implement walkthrough. The STEP 19 screenshots are taken by the developer (guide in the bolt checkpoint).

### Test Files

- [x] `src/testing/test-providers.ts`, `fixtures.ts`: the real `en.json` through `TranslocoTestingModule`; HttpClient with the app interceptor on `HttpTestingController`; ProblemDetails bodies; the seeded scenario as the Api returns it. Specs and fixtures may name batches; the lint rule exempts them.
- [x] `core/errors/app-error.spec.ts`: the Ref comes from the **trace-id part** of a W3C trace context, or the first 8 characters of any other ID; no trace means no Ref; anything else becomes `Unknown`.
- [x] `core/errors/error.interceptor.spec.ts`:
  - ProblemDetails → code, status and trace ID;
  - no error code → `Unknown`;
  - status 0 → `Network.Unavailable`;
  - **a 401 elsewhere ends the session and goes to sign-in**;
  - a 401 from `/api/session` is left to the caller.
- [x] `core/auth/session.service.spec.ts`:
  - restore; a 401 on restore means no session;
  - sign in with the badge and shift;
  - **the XSRF cookie is fetched first if start-up could not**;
  - sign-out forgets the session even if the Api fails.
- [x] `core/auth/auth.guards.spec.ts`: no session → `/sign-in`; signed in → allowed, and sign-in → `/batches`.
- [x] `core/auth/station-startup.spec.ts`: antiforgery, then restore; with the Api unreachable the station still opens.
- [x] `core/scanner/scanner.service.spec.ts`, with fake timers:
  - **scans made before the screen opened are skipped**; later scans come once each, in order and trimmed;
  - when the scanner is offline, polling continues and primes once it's back;
  - blank codes are ignored;
  - polling stops on unsubscribe.
- [x] `core/devices/device-health.service.spec.ts`:
  - all 5 devices with scale names;
  - **a faulted device is offline on the next poll**;
  - an unreachable Api → NO DATA, never READY;
  - stop ends polling.
- [x] `core/clock/plant-clock.service.spec.ts`:
  - **the Api's time is shown in the plant zone (Asia/Dhaka), not the terminal's clock or zone**;
  - it ticks every second;
  - any instant can be formatted;
  - no clock rather than a wrong one.
- [x] `core/keyboard/function-keys.service.spec.ts`:
  - a registered key runs its action and suppresses the browser's default;
  - unused keys are left alone; disabled actions and modified keys don't run;
  - the action is dropped when its screen is destroyed, and the newer screen's action wins.
- [x] `core/format/weight.pipe.spec.ts`: g to 1 decimal (94.0 g, 1240.0 g), kg to 3 (24.000 kg, 9.600 kg); explicit decimals; a dash for nothing.
- [x] `features/sign-in/sign-in.component.spec.ts`:
  - **a known badge (trimmed) plus shift → `/batches`**;
  - **an unknown badge → "Badge not recognised." with Ref, and the screen stays**;
  - an empty badge → a prompt and no Api call;
  - **F1 signs in**.
- [x] `features/batch-queue/batch-queue.component.spec.ts`:
  - **the scan prompt plus a table from `GET /api/batches`**: all 7 headers; B-24187 as `SO-9931 · JET-04 · 100% CTN knit · 480 kg · Teal 4C · 5 · WEIGHING`; 4 rows in order;
  - status and training shown as text;
  - a batch removed upstream is not shown;
  - **touch → `GET /api/batches/B-24187` → Weigh route**, and the keyboard (Enter) does the same;
  - **scan → open**;
  - **an unknown scan → "Batch B-99999 not found." and the queue stays**;
  - OrgaTEX down while opening → Retry;
  - a second selection while opening is ignored;
  - **OrgaTEX down → Retry → queue**;
  - an empty queue → "No batches released".
- [x] `features/batch-opened/batch-opened.component.spec.ts`:
  - **opened from the queue → only `GET …/recipe` (the third STEP 19 call)**; 5 components in sequence; Yellow RGB `3 · DYE-4471 · 94.0 g · ± 0.5 g · C-12`; a missing tolerance → "configured";
  - after a reload the header is fetched first;
  - OrgaTEX down → Retry → loaded;
  - not found → no Retry;
  - Back to queue clears the batch.
- [x] `features/shell/top-bar.component.spec.ts`:
  - WeighingMS, STATION 01, and the chip "R. Karim · Shift A";
  - **the crumb `BATCH B-24187 · ORDER SO-9931 · MACHINE JET-04` appears only while a batch is open**;
  - **chip → Sign out → `DELETE /api/session` → sign-in** (D2);
  - Escape closes the menu.
- [x] `features/shell/status-strip.component.spec.ts`:
  - `SCALE 3 kg precision · READY`, `SCALE 32 kg floor · OFFLINE`, `SCANNER · READY`, `PRINTER · READY`, `ORGATEX · NO DATA` (**text, not colour alone**);
  - EN and the plant clock;
  - device changes are followed.
- [x] `shared/ui/banner/banner.component.spec.ts`:
  - the translated message plus Ref, never raw detail;
  - an unknown code → the generic message;
  - Retry emits;
  - info shows as a status, not an alert.
- [x] `app.component.spec.ts`:
  - top bar, main area and status strip;
  - **health and clock calls happen only while signed in** and stop after the session ends;
  - F-keys are forwarded to the current screen.

### Acceptance Criteria Validation

- ✅ **Without a session, every route goes to `/sign-in` first** (`auth.guards`, `error.interceptor` 401, the live run's deep link).
- ✅ **A seeded badge opens the queue; an unknown badge shows the translated error and stays** (`sign-in` specs, live run).
- ✅ **The top bar shows WeighingMS, the station, the operator chip and, once a batch is open, the crumb** (`top-bar` specs).
- ✅ **The status strip shows both scales, the scanner, the printer and OrgaTEX as READY or OFFLINE in text, plus EN and the plant clock; a faulted device turns OFFLINE within one poll** (`status-strip`, `device-health`, `plant-clock` specs; live 32 kg fault).
- ✅ **The queue shows the scan prompt and a table from `GET /api/batches` with every column and text status tags; a batch removed upstream disappears** (`batch-queue` specs).
- ✅ **Touching or scanning B-24187 opens it via `GET /api/batches/B-24187` then `…/recipe`** (`batch-queue` and `batch-opened` specs; the network log from the live run).
- ✅ **An unknown scanned ID shows "Batch not found" and the queue stays** (`batch-queue` spec, live run).
- ✅ **OrgaTEX down → error with Retry; Retry after recovery loads the queue** (`batch-queue` spec, live run).
- ✅ **F1–F4 go to the current screen's registered actions** (`function-keys`, `app.component`, `sign-in` F1).
- ✅ **All operator text comes from Transloco; no batch literals in `src/app`** (specs render the real `en.json`; lint plus the rule self-check).
- ✅ **`ng build`, lint and Stylelint pass with 0 errors; backend tests still pass** (293/293).

### Issues Found

- **No defects in the Implement-stage code.** Two harness fixes were needed:
  1. Testing Library's `render` configures TestBed itself, so state can't be injected first. The opened batch is now passed as a provider, and the session is restored after rendering.
  2. In Angular 22's DOM types, `textContent` on an element is non-nullable, so the specs drop optional chaining (strict lint).
- **Coverage tooling**: added `@vitest/coverage-v8` 5.1.0 (dev).

### Notes

- **STEP 19 screenshots** (`STEP-19-batch-screen.png`, `STEP-19-network.png`, `STEP-19-test.png`) are taken from a live `npm run dev` run and the `npm run test:web` output.
- Browser console during the live run: only the browser's own log lines for expected 401, 404 and 503 responses.
- `coverage/` is git-ignored.
