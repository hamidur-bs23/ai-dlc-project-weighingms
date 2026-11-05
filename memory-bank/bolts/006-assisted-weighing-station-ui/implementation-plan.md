---
stage: plan
bolt: 006-assisted-weighing-station-ui
created: 2026-11-05T15:50:00Z
---

## Implementation Plan: assisted-weighing-station-ui (bolt 006)

### Objective

Create the Angular operator terminal and deliver its first working screens, using **only WeighingMS.Api** (boundary B0) and holding no scenario data (workbook Rule 5):
- the **app shell**: top bar, status strip, plant clock, function-key routing, Transloco EN;
- **badge sign-in**;
- **screen 1 · Select batch**: the scan prompt and the queue table, opening a batch by touch or scan.

Workbook **STEP 19** (UI): the screen gets its data from `GET /api/batches`. Selecting B-24187 calls `GET /api/batches/B-24187`, then `GET /api/batches/B-24187/recipe`. There is no `const batches = [...]`.

### Stories in scope

1. `001-app-shell-top-bar-status-strip`: FR-22, NFR Usability
2. `002-sign-in-screen`: FR-19, NFR Security
3. `003-select-batch-screen`: FR-1, FR-2

### What will change

1. **Scaffold the `web/` npm workspace**:
   - Angular CLI 22.2.6 on Node 26.13.0 (nvm): standalone, **zoneless**, SCSS, Vitest test runner, no SSR;
   - the selector prefix `wms`;
   - path aliases `@core/*`, `@features/*`, `@shared/*`;
   - a dev proxy that sends `/api` to `http://localhost:5080`, so the SPA and the Api share one origin and need no CORS (tech-stack).
2. **Tooling** (coding-standards):
   - angular-eslint (flat config, template a11y rules, `prefer-on-push`, `no-explicit-any`) **plus a rule that rejects batch-ID literals (`B-\d{5}`) in `src/app/**` outside specs**, the STEP 19 "no `const batches`" guard;
   - Prettier;
   - Stylelint (standard-scss);
   - root npm scripts: `dev` (simulator, Api and web through **concurrently**), `lint`, `test:web`, `api:types`.
3. **API types**:
   - the Api writes its OpenAPI document at build time (`Microsoft.Extensions.ApiDescription.Server` → `src/WeighingMS.Api/WeighingMS.Api.json`);
   - `openapi-typescript` generates `web/src/app/core/api/generated/schema.ts` from it;
   - thin, hand-written API services sit on top (tech-stack).
4. **Core** (`src/app/core`):
   - `provideHttpClient(withFetch(), withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' }))`;
   - an app initializer that calls `GET /api/antiforgery`;
   - an error interceptor that turns ProblemDetails into a typed `AppError` (`code`, `traceId`);
   - `SessionService` (signals), with an **auth guard** that sends anyone without a session to `/sign-in`;
   - `HealthService` polls `/api/devices/health` every 5 s, configurable;
   - `PlantClockService` takes the offset from `/api/station` and ticks locally;
   - `ScannerService` polls `/api/scanner/scans?after=` with a cursor every 500 ms, configurable. **It starts at the current cursor, so scans made before the screen opened are never replayed**;
   - `KeyboardShortcutsService` maps F1–F4 to the actions the current screen registers. This bolt builds the routing; the Confirm, Re-tare, Next batch and Quit actions come with bolts 010 and 013;
   - `WeightPipe` and a `wms-banner` component show translated error messages with "Ref" plus the first 8 characters of the trace ID.
5. **Styles**:
   - `src/styles/_tokens.scss` holds the **prototype's CSS custom properties under their prototype names** (`--hmi-*`, `--ok`, `--warn`, `--over`, `--page-*`, `--mono`, `--sans`, ux-guide);
   - `_mixins.scss` holds `touch-target`, `mono-readout` and `focus-ring`;
   - `_base.scss` holds the base styles.

   The prototype's implementation is **not copied**.
6. **Features**:
   - **Shell** (`app-shell`):
     - top bar: `WeighingMS · STATION 01`, a BATCH / ORDER / MACHINE crumb once a batch is open, the operator chip with name and shift. Touching the chip offers **Sign out**;
     - status strip: both scales, the scanner, the printer and OrgaTEX as READY or OFFLINE, each with a dot **and a text label**, plus `EN` and the plant-time clock. An unsynced-count slot is left for bolt 011.
   - **Sign-in** (`/sign-in`): a typed badge (and optional shift) and an `OK` button. An unknown badge shows the translated `Auth.BadgeNotRecognised` message. Success opens `/batches`.
   - **Select batch** (`/batches`):
     - the scan prompt "Scan a batch card to begin / Or touch a row below";
     - the queue table: Batch, Order, Machine, Fabric / lot, Shade, Comp., and a Status tag (`QUEUED`, or `WEIGHING` in amber, with a text label);
     - touching a row or **scanning a batch ID** opens the batch;
     - an ID not in the queue shows "Batch not found" **without leaving the screen**;
     - OrgaTEX down shows an error with **Retry**;
     - an empty queue shows "No batches released".
   - **Batch opened** (`/batches/:batchId/weigh`): the batch header and the loaded recipe (the snapshot taken on first load). The full Weigh screen arrives in bolt 010 (see D1).
7. **Translations**: every operator string lives in `public/i18n/en.json` (Transloco, keys like `batchQueue.table.shade`). Error codes map to messages under `errors.<code>`.

### Files to be created / modified

- **Created**:
  - `web/` (Angular workspace): `angular.json`, `package.json`, `proxy.conf.json`, `eslint.config.js`, `.stylelintrc.json`, `.prettierrc`, `tsconfig*.json`
  - `web/src/app/{app.config.ts,app.routes.ts,app.ts}`
  - `web/src/app/core/{api,auth,devices,clock,scanner,errors,keyboard}/*`
  - `web/src/app/features/{shell,sign-in,batch-queue,batch-opened}/*`
  - `web/src/app/shared/ui/{banner,status-dot}/*`
  - `web/src/styles/{_tokens,_mixins,_base}.scss`
  - `web/public/i18n/en.json`
  - specs next to the sources
- **Modified**:
  - root `package.json` (workspaces `web`, scripts `dev`, `lint`, `test:web`, `api:types`; `concurrently` dev dependency)
  - `Directory.Packages.props` and `WeighingMS.Api.csproj` (`Microsoft.Extensions.ApiDescription.Server`)
  - `.gitignore` (`web/dist`, `.angular/`)
  - `CLAUDE.md` (the web commands now available)

### APIs involved (WeighingMS.Api, through the `/api` proxy)

| Call | Used by |
|---|---|
| `GET /api/antiforgery` | App start-up (XSRF cookie) |
| `POST /api/session`, `GET /api/session`, `DELETE /api/session` | Sign-in, auth guard, sign-out |
| `GET /api/station` | Station ID and plant clock |
| `GET /api/devices/health` | Status strip |
| `GET /api/batches` | Queue table (STEP 19) |
| `GET /api/batches/{id}` | Touch or scan selection; 404 → "Batch not found" |
| `GET /api/batches/{id}/recipe` | Opening the batch (snapshot) |
| `GET /api/scanner/scans?after=` | Scanned batch IDs on the queue screen |

No new Api endpoints are needed.

### Decisions at this checkpoint: resolved (APPROVE PLAN, 2026-11-05)

- **D1: option 1.** Opening a batch goes to `/batches/:id/weigh`, which shows the batch header and recipe; bolt 010 builds the Weigh screen on that route.
- **D2: option 1.** Touching the operator chip offers Sign out.

#### Options as presented


**D1. What does opening a batch show in this bolt?** The Weigh screen belongs to bolt 010.
1 - **(Recommended)** Open `/batches/:id/weigh`: it loads the batch and its recipe and shows the header plus the recipe list in sequence, with a clear "Weighing controls arrive with the Weigh screen" note. The STEP 19 three-call flow is then real and visible, and bolt 010 builds the full screen on the same route.
2 - Load the batch and recipe but stay on the queue screen until bolt 010.

**D2. Sign-out is not in the mockup.**
1 - **(Recommended)** Touching the operator chip in the top bar shows a small "Sign out" action. That's the minimum needed for a shared station (ADR-009), styled like the mockup.
2 - No sign-out UI in V1 (the session expires after 12 hours).

### Dependencies

- Bolts 003, 004 and 005 (complete): the session, XSRF, station, device health, scanner and batch endpoints.
- **Node 26.13.0 through nvm** for every web command (Homebrew's node 24 is too old for Angular CLI 22).
- New npm dev dependencies, pinned in `web/package.json`:
  - the Angular 22 packages, `@jsverse/transloco` 8.4.0, `@angular/cdk`;
  - `openapi-typescript` 7.13.0;
  - `angular-eslint`, `typescript-eslint`, `prettier`, `stylelint`, `stylelint-config-standard-scss`;
  - `@testing-library/angular`, `@testing-library/user-event`;
  - `concurrently` 10.0.6 (root).

  `@ngrx/signals` arrives with the weighing session store in bolt 010.
- New NuGet package: `Microsoft.Extensions.ApiDescription.Server`, for the OpenAPI document at build time.

### Technical approach

- **Zoneless, OnPush, signals everywhere**: services expose signals; components read them. RxJS is used only for polling (`timer` + `switchMap`), as tech-stack specifies.
- **No hard-coded scenario data**: every batch, scale or device value comes from the Api. The ESLint rule and a spec enforce this.
- **Scan handling**:
  - the queue screen subscribes to `ScannerService` and treats a code as a **batch ID**;
  - lot handling on the Weigh screen arrives in bolt 010;
  - scans never steal focus from a text field (ux-guide).
- **Errors**: `AppError(code, traceId)` from ProblemDetails is shown as a translated message plus Ref. Raw `detail` is never shown (FR-24).
- **Accessibility**:
  - touch targets of at least 44 px;
  - a visible focus ring;
  - the status strip and status tags carry text, not colour alone;
  - the table is a semantic `<table>`.

### Acceptance criteria (from the stories)

- [ ] Without a session, every route goes to `/sign-in` first.
- [ ] A seeded badge (`OP-1001`) opens the queue; an unknown badge shows the translated error and stays on sign-in.
- [ ] The top bar shows WeighingMS, the station, the operator chip (name and shift) and, once a batch is open, the batch / order / machine crumb.
- [ ] The status strip shows both scales, the scanner, the printer and OrgaTEX, each READY or OFFLINE with text, plus `EN` and the plant-time clock. A faulted device turns OFFLINE within one health poll.
- [ ] The queue shows the scan prompt and a table built from `GET /api/batches` (4 seed batches, every column, status tags with text). Removing a batch from the dummy removes it from the screen.
- [ ] Touching B-24187 or **scanning `B-24187`** (`POST /_sim/scanner/scan`) opens it. The calls go `GET /api/batches/B-24187`, then `GET /api/batches/B-24187/recipe`, both visible in DevTools.
- [ ] A scanned unknown ID shows "Batch not found" and the queue stays.
- [ ] OrgaTEX down → an error with Retry; Retry after recovery loads the queue.
- [ ] F1–F4 go to the current screen's registered actions (the routing is in place).
- [ ] All operator text comes from Transloco, and **no batch literals exist in `src/app`** (lint).
- [ ] `ng build`, lint and `stylelint` pass with 0 errors; the backend tests still pass.

### How this will be tested (Stage 3)

1. **Vitest + Testing Library** (components found by role and label; `HttpTestingController`):
   - the sign-in flow (success, unknown badge);
   - the auth guard;
   - the queue table rendering from a mocked `GET /api/batches`, with every column and the status tags;
   - row touch and **scanned-ID** selection, including "batch not found" and staying on the screen;
   - OrgaTEX down → Retry;
   - the empty queue;
   - status-strip rendering of READY and OFFLINE;
   - the error interceptor mapping;
   - `WeightPipe`;
   - the scanner cursor (no replay);
   - the key routing.
2. **Lint**: the batch-literal rule is proven by a fixture that should fail it.
3. **Live run** with `npm run dev`: simulator, Api and web together on http://localhost:4200. It gives the STEP 19 evidence:
   - `STEP-19-batch-screen.png`: the queue screen;
   - `STEP-19-network.png`: DevTools showing the three calls;
   - `STEP-19-test.png`: the Vitest run.

   I'll write the step-by-step for the screenshots, and I'll use spare ports if your instances are running.

### Out of scope

- The Weigh screen, lot and container entry, override dialog and weighing session store: bolt 010.
- The Confirm & print screen, ticket and review list: bolt 013.
- Full error and offline states on every screen, and Playwright E2E: bolt 014.
- The unsynced-count value: bolt 011.
- The OTel Web SDK: the observability bolt. A `traceparent` interceptor could come earlier if wanted.
