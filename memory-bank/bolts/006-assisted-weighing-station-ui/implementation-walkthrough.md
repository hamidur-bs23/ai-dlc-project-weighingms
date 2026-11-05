---
stage: implement
bolt: 006-assisted-weighing-station-ui
created: 2026-11-05T17:00:00Z
---

## Implementation Walkthrough: assisted-weighing-station-ui (bolt 006)

### Summary

The Angular operator terminal now exists as the `web/` npm workspace (Angular 22, zoneless, OnPush, signals). It delivers:
- the **shell**: top bar, status strip, plant clock and F-key routing;
- **badge sign-in**;
- **screen 1 · Select batch**;
- the **opened-batch view** (decision D1).

Every value on screen comes from WeighingMS.Api through the `/api` dev proxy. The UI holds no scenario data, and a lint rule enforces that (workbook Rule 5, STEP 19).

### Structure Overview

- **`core/`**: the Api access layer and cross-cutting services:
  - generated API types plus a typed surface (`api.types.ts`);
  - `AppError` and the error interceptor;
  - session and guards; start-up (XSRF and session restore);
  - device health, plant clock, scanner, function keys, `WeightPipe` and `PlantTimePipe`.
- **`features/`**: the screens (shell, sign-in, batch-queue, batch-opened).
- **`shared/ui/`**: `wms-banner`.
- **`src/styles/`**: the prototype tokens, mixins and base rules.

### Completed Work

- [x] `web/` workspace (Angular CLI 22.2.6, Node 26.13.0), with the root `package.json` turned into npm workspaces. Scripts `web`, `dev` (concurrently), `lint`, `test:web`, `api:types`.
- [x] `web/proxy.conf.mjs`: `/api` goes to `:5080`; `WMS_API_URL` overrides it.
- [x] `web/eslint.config.js`:
  - strict-type-checked typescript-eslint;
  - angular-eslint plus template accessibility rules;
  - `prefer-on-push`, `no-explicit-any`, the `wms` selector prefix;
  - **the local rule `wms/no-batch-literals`** (`web/eslint/no-batch-literals.mjs`) on TypeScript and templates, with specs exempt.
- [x] `web/scripts/verify-lint-rule.mjs`: proves that the rule fails `const batches = [{ batchId: 'B-…' }]` in app code and allows it in a spec. It runs as part of `npm run lint`.
- [x] Stylelint (standard-scss, `color-no-hex` outside `_tokens.scss`) and Prettier (2 spaces, single quotes, trailing commas, width 100).
- [x] `src/WeighingMS.Api/openapi/WeighingMS.Api.json` and `web/src/app/core/api/generated/schema.ts`: the build-time OpenAPI document and the generated types (`npm run api:types`).
- [x] `core/errors/`: `AppError` (code, status, traceId; "Ref" = the first 8 characters of the **trace-id part** of the W3C trace context). The interceptor maps ProblemDetails; a 401 outside `/api/session` drops the session and goes to sign-in.
- [x] `core/auth/`: `SessionService` (restore, sign in, sign out), `signedInGuard` and `signedOutGuard`, and the start-up initializer (`GET /api/antiforgery`, then restore).
- [x] `core/devices/device-health.service.ts`: polls `/api/devices/health` every 5 s while signed in (configurable through `STATION_UI_CONFIG`). Scale names come from `/api/scales`; with the Api unreachable, devices show NO DATA.
- [x] `core/clock/`: `PlantClockService` (skew measured against the Api's UTC time, shown in the station's time zone) and `PlantTimePipe`.
- [x] `core/scanner/scanner.service.ts`: the scan stream through `?after={cursor}`. **The first successful poll only finds the current cursor**, so earlier scans are never replayed.
- [x] `core/keyboard/function-keys.service.ts`: F1–F4 run the action the current screen registered, removed when the screen is destroyed. Disabled actions don't run; the browser's own F-key behaviour is suppressed only when a screen uses that key.
- [x] `features/shell/`:
  - top bar: WeighingMS · STATION 01, the BATCH · ORDER · MACHINE crumb, the operator chip (name · shift) with **Sign out** (D2);
  - status strip: both scales by name, scanner, printer and OrgaTEX, each READY or OFFLINE as text plus a dot or square; EN; the plant clock.
- [x] `features/sign-in/`: badge and optional shift. The badge is trimmed and required. Errors are translated, with Ref. OK is also F1.
- [x] `features/batch-queue/`:
  - the scan prompt ("Queue received from OrgaTEX at HH:mm", plant time);
  - the table from `GET /api/batches`, with all 7 columns, QUEUED / WEIGHING tags (text) and a TRAINING tag;
  - **touch or scan → `GET /api/batches/{id}`, then open**;
  - not found → "Batch X not found…" and the screen stays;
  - OrgaTEX down → banner with Retry;
  - empty queue → info banner.
- [x] `features/batch-opened/` (`/batches/:batchId/weigh`): the header and the recipe snapshot from `GET …/recipe` (sequence, code, name, target, ± tolerance or "configured", container), the "Weighing controls arrive with the Weigh screen" note, and Back to queue. Opened from the queue it reuses the header, so the network shows exactly the 3 STEP 19 calls.
- [x] `shared/ui/banner/`: an error or info banner with the translated code (unknown codes fall back to `errors.Unknown`), Ref and an optional Retry.
- [x] `src/styles/_tokens.scss`, `_mixins.scss`, `_base.scss`: the prototype token names, spacing scale, `touch-target`, `mono-readout`, `focus-ring`, and the `.key` HMI button.
- [x] `public/i18n/en.json`: every operator string, plus `errors.<code>` for every Api error code.
- [x] Modified `CLAUDE.md` (web commands) and `.gitignore` (`.angular/`, `.playwright-mcp/`).

### Key Decisions

- **D1 and D2 as approved**: the opened-batch view sits on the Weigh route; Sign out is reached from the operator chip.
- **Number handling on the Api is strict** (`JsonNumberHandling.Strict` for MVC and for the OpenAPI JSON options). Without it, .NET's web defaults describe every number as `integer | string`, which leaks into the generated types. The Api now also rejects quoted numbers in requests. All 293 backend tests still pass.
- **The OpenAPI document is written only when asked** (`-p:OpenApiGenerateDocumentsOnBuild=true`, with a placeholder connection string, because generation builds the host). Ordinary `dotnet build` is unchanged. The document and the generated TypeScript are committed, so frontend work needs no running Api.
- **Times are plant times**: the clock and every time shown use the station's time zone from `/api/station`, not the browser's.
- **The scanner skips old scans**: a batch card scanned before the queue opened must not open a batch later.
- **File naming follows coding-standards** (`*.component.ts`, `*.service.ts`; CLI style guide 2016).

### Deviations from Plan

- **Api change not in the plan**: strict JSON number handling (above).
- **`openapi-typescript` runs through a pinned `npx openapi-typescript@7.13.0`** instead of a dev dependency, because its peer range (`typescript ^5`) conflicts with Angular 22's TypeScript 6.
- **`withFetch()` is not used**: in Angular 22 the fetch backend is already the default (the API is deprecated).
- **F-keys in this bolt**: only sign-in registers one (F1 = OK). The queue screen has no labelled F-key in the mockup. Confirm, Re-tare, Next batch and Quit come with bolts 010 and 013.

### Dependencies Added

- **web**: `@angular/cdk` 22.2.6, `@jsverse/transloco` 8.4.0; dev: `angular-eslint` 22.6.0, `eslint` 10.14.0, `@eslint/js` 10.0.1, `typescript-eslint` 8.72.1, `stylelint` 17.16.0, `stylelint-config-standard-scss` 17.0.0, `@testing-library/angular` 19.6.0, `@testing-library/dom` 10.4.2, `@testing-library/user-event` 14.6.8, `prettier` 3.9.10.
- **root**: `concurrently` 10.0.6.
- **NuGet**: `Microsoft.Extensions.ApiDescription.Server` 10.0.14 (build-time OpenAPI).

### Developer Notes

- **Verification**:
  - `ng build` (production) passes with 0 warnings;
  - `npm run lint` is clean and the rule self-check passes;
  - `dotnet build` has 0 warnings; `dotnet format` is clean;
  - **293/293 backend tests pass**.
- **Live smoke test** on spare ports (web 4300, Api 5180, simulator 5190; the developer's 5080 and 5090 untouched):
  - an unknown badge shows the translated error and Ref;
  - OP-1001 with shift A signs in with **F1**;
  - the queue shows 4 batches with every column;
  - a scanned `B-99999` gives "not found" and the screen stays;
  - a **scanned `B-24187`** gives `GET /api/batches/B-24187`, then `…/recipe`, and opens with 5 components in sequence and the crumb filled;
  - a touched row opens B-24188;
  - **OrgaTEX faulted** gives the error with Retry; Retry after the fault clears reloads the queue;
  - a **faulted 32 kg scale** shows OFFLINE within one poll;
  - Sign out goes to sign-in; a signed-out deep link goes to sign-in;
  - a second sign-in works with the re-issued XSRF token.
- **Browser console**: the only errors are the browser's own log lines for expected 401, 404 and 503 responses. There are no application errors.
- **Specs** (Vitest + Testing Library) are Stage 3, as planned.
