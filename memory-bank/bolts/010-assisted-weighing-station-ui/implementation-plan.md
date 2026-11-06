---
stage: plan
bolt: 010-assisted-weighing-station-ui
created: 2026-11-05T19:15:00Z
---

## Implementation Plan: assisted-weighing-station-ui (bolt 010)

### Objective

Build **screen 2 · Weigh**, replacing bolt 006's opened-batch placeholder on `/batches/:batchId/weigh`, on top of the weighing Api from bolts 007–009. The screen holds **no rules of its own**: every class, "can confirm", surplus and override offer comes from the server (`…/session`, `…/session/reading`). The screen only renders them and sends the operator's commands.

Workbook: **STEP 20** (weigh screen), **STEP 21** (scale switch 3 kg precision → 32 kg floor on screen), **STEP 22** (tolerance UI), plus the UI parts of **STEP 23** (progression), **24** (overshoot UI) and **25** (confirmation).

### Stories in scope

1. `004-weigh-screen-rail-and-header`: FR-4, FR-17
2. `005-weigh-screen-readout-bar-status`: FR-9 to FR-12, FR-16
3. `006-weigh-actions-and-keys`: FR-13, FR-7, FR-14
4. `007-lot-and-container-entry`: FR-5, FR-6
5. `008-supervisor-override-dialog`: FR-15

### What will change

1. **The weighing session store** (`features/weigh/weighing-session.store.ts`, **NgRx SignalStore**, as tech-stack specifies for the weighing session):
   - state: session, recipe (shade, colours), scale names, the latest reading, busy and error;
   - methods `open`, `select`, `captureLot`, `captureContainer`, `tare`, `confirm`, `quit`, `leave`, `override`;
   - **reading polling** at the server's `pollIntervalMs` (250 ms, from `GET /api/scales`) while a component is active, stopped on leave and destroy;
   - one poll in flight at a time (`exhaustMap`), and a failed poll shows the device banner without stopping the polling.
2. **`WeighingApiService`** (`core/weighing/`): the typed calls for every session endpoint (generated types).
3. **Weigh screen** (`features/weigh/weigh.component.*`), laid out like the mockup:
   - **Recipe rail**:
     - header "Recipe · Teal 4C", the batch ID, "n of N components weighed";
     - one row per component: colour swatch, name, code · scale class, target + unit, and the state `✓ 94.2 g` (`OVR` when overridden) / `WEIGHING` / `PENDING`;
     - pending rows are touchable; done rows aren't.
   - **Header**:
     - name; `CODE · LOT … · CONTAINER … · TARE …`;
     - the scale badge "SCALE SELECTED · 3 kg precision · automatic", which switches to "32 kg floor" for Sodium Sulphate (STEP 21).
   - **Lot and container** (story 007): two labelled fields in the header area.
     - The container is pre-filled from the recipe's suggestion and saved on blur or Enter.
     - The lot is typed, **or filled by a scan** (`ScannerService`); a scan sends the lot command right away.
     - A scan never steals focus from a field the operator is typing in.
   - **Readout**: "Weighed now" (large, about 52 px), "Still to weigh", "Target", all with units (1 dp g, 3 dp kg); an **UNSTABLE** tag when the reading isn't stable.
   - **Bar**: fill, the tolerance band, the target line; caption "0 · TOLERANCE ±0.5 g (93.5 – 94.5) · max" (range: decision D1).
   - **Status line**, one distinct state per class, **text and colour**:
     - `DISPENSE — 12.4 g REMAINING` (underweight);
     - amber `DISPENSE — 3.0 g REMAINING · NEAR TARGET` (≥ 90% of target, Could, OQ-4);
     - `WITHIN TOLERANCE — PRESS OK TO CONFIRM`;
     - `TARGET ACHIEVED — PRESS OK TO CONFIRM` (distinct from Within);
     - `OVER TOLERANCE — SURPLUS +2.4 g (+2.6%)`, with the three recovery actions named.
     
     No recalculation table (PD-05 option 1).
   - **Confirm hints**: when Confirm is disabled, the server's `missing` list is shown in plain words ("Enter the lot", "Tare the empty container", "Wait for a stable reading"), so the operator knows why.
   - **Action keys**:
     - `OK · Confirm F1`, enabled **only** when the server says `canConfirm`;
     - `Tare / Re-tare F2` (the label depends on whether a tare was taken);
     - `Quit F4`, which asks for confirmation if lot, container or tare were captured (ux-guide: destructive actions);
     - `Supervisor override`, shown only when the server says `override.offered`;
     - `Back to queue`, which calls leave.
     
     F1, F2 and F4 go through bolt 006's `FunctionKeysService`.
4. **Override dialog** (`features/weigh/override-dialog.component.*`, **CDK Dialog**, focus trapped; story 008):
   - supervisor badge, PIN (masked), reason (required, up to 500 characters);
   - submit → `POST /api/session/step-up` → `POST …/session/override`;
   - errors shown with a Ref: wrong PIN, locked out, four eyes, used grant, missing reason;
   - success → the rail shows `✓ 96.4 g OVR` and the next component is active.
5. **After the last component** (decision D2): navigate to `/batches/:batchId/summary`, a read-only interim summary from `GET …/session/summary` (n of N, within tolerance, override count, total, elapsed, operators). The full Confirm & print screen is bolt 013.
6. **Translations**: every new string in `public/i18n/en.json` (weigh, override, status lines, missing hints, error codes `Weighing.*`, `Auth.FourEyesRequired`, `Auth.GrantInvalid`, `Auth.StepUpFailed`, `Batch.Incomplete`).

### Files to be created / modified

- **Created**:
  - `web/src/app/core/weighing/weighing-api.service.ts`
  - `web/src/app/features/weigh/{weighing-session.store.ts,weigh.component.{ts,html,scss},recipe-rail.component.ts,weigh-readout.component.ts,weigh-bar.component.ts,override-dialog.component.ts}`
  - `web/src/app/features/batch-summary/batch-summary.component.ts` (interim, D2)
  - specs next to each
- **Modified**:
  - `web/src/app/app.routes.ts` (the weigh route uses the new screen; add `/batches/:batchId/summary`)
  - `web/src/app/core/api/api.types.ts` (weighing types)
  - `web/public/i18n/en.json`
  - `web/package.json` (`@ngrx/signals`)
- **Removed**: `features/batch-opened/*` (bolt 006's placeholder is replaced; the STEP 19 flow still calls `GET /api/batches/{id}/recipe` when the screen opens).

### APIs involved (all exist; no backend change)

| Call | Used for |
|---|---|
| `GET /api/batches/{id}` (from the queue) · `GET /api/batches/{id}/recipe` | Header crumb · shade, colour swatches (STEP 19 flow kept) |
| `POST /api/batches/{id}/session` | Start or resume when the screen opens |
| `GET /api/scales` | Scale names for the badge ("3 kg precision", "32 kg floor") and `pollIntervalMs` |
| `GET …/session/reading` (polled) | Net, class, remaining, band, surplus, `confirmation`, `override.offered` |
| `POST …/session/selection`, `attempt/lot`, `attempt/container`, `tare`, `confirmation`, `quit`, `leave` | Operator actions |
| `POST /api/session/step-up` → `POST …/session/override` | Override dialog |
| `GET …/session/summary` | Interim summary after completion (D2) |
| `GET /api/scanner/scans` (bolt 006 `ScannerService`) | A scanned lot |

### Decisions at this checkpoint: resolved (APPROVE PLAN, 2026-11-06)

- **D1: option 1.** The bar runs from 0 to 1.25 × the upper tolerance bound (BAR-RANGE).
- **D2: option 1.** After completion, the interim read-only summary at `/batches/:id/summary` (INTERIM-SUMMARY).

#### Options as presented


**D1. What range does the progress bar show?** FR-9 says "against the scale display range", but the Api gives no display range, and the mockup uses hand-picked maxima (125 g for Yellow RGB). The scale's capacity (3200 g) would make a 94 g pour a sliver.
1 - **(Recommended)** **0 → 1.25 × the upper tolerance bound** (Yellow RGB: 0 – 118.1 g; Sodium Sulphate: 0 – 30.150 kg). The band and target always sit at about 80% of the bar, overshoot stays visible up to +25%, and a reading above the range pins the fill at 100% while the numbers keep the exact value. It's computed from the server's band, not invented per chemical.
2 - **0 → the scale capacity** (3200 g / 32 kg): true to the device, but small targets are barely visible.

**D2. What does the screen show after the last component completes the batch?** The full "3 · Confirm & print" screen (ticket, print, next batch) is bolt 013.
1 - **(Recommended)** Navigate to an **interim read-only summary** at `/batches/:id/summary`, built from the server's summary (5 of 5, all within tolerance YES or NO + overrides, total dispensed, elapsed, operators, per-chemical lines), with "Back to queue". The STEP 23 five-component recording then ends on a real completed state, and bolt 013 extends this screen.
2 - Stay on the Weigh screen with a "Batch complete" banner until bolt 013.

### Dependencies

- Bolts 006 (shell, keys, scanner, banner, tokens) and 009 (complete weighing Api): both complete.
- New npm dependency: **`@ngrx/signals` 22.0.1** (planned for this bolt in bolt 006). `@angular/cdk` (Dialog, A11y) is already installed.
- The Api types are regenerated in bolts 007–009 (`npm run api:types`), so there is no backend change.

### Technical approach

- **The server is the only judge**: the UI never compares weights. Class, `canConfirm`, `missing`, `surplus` and `override.offered` are rendered as received. Confirm stays disabled while a command is in flight (no double submit; the server rejects one anyway).
- **Polling**: `timer(0, pollIntervalMs)` with `exhaustMap`, running only while a component is active and the screen is open. The latest reading wins.
- **Zoneless + OnPush + signals**: the store exposes signals; components are presentational (rail, readout, bar) with `input()`.
- **Accessibility**:
  - the bar is a `progressbar` with `aria-valuenow`;
  - status changes are announced through the CDK `LiveAnnouncer`, on class changes only, not every reading;
  - keys are 62 px high;
  - the dialog traps focus and Escape cancels;
  - Enter in the dialog submits only when it's valid.
- **No scenario data in UI code**: the lint rule from bolt 006 stays enforced.

### Acceptance criteria (from the stories)

- [ ] The rail shows every component (swatch, name, code, scale, target, state `✓ actual` / `WEIGHING` / `PENDING`) and "n of N components weighed"; touching a pending one activates it; done rows can't be selected.
- [ ] The header shows name, code, LOT, CONTAINER, TARE and the scale badge "automatic"; **Yellow RGB shows "3 kg precision", Sodium Sulphate "32 kg floor"** (STEP 21).
- [ ] Weighed now / Still to weigh / Target with units (1 dp g, 3 dp kg); the bar with fill, band and target line; the caption "TOLERANCE ±0.5 g (93.5 – 94.5)".
- [ ] A distinct state with text per class; Target achieved is distinct from Within range; the amber near-target hint; overshoot shows only the surplus; unstable is labelled.
- [ ] **STEP 22**: at 93.4 g Confirm is disabled; at 93.5, 94.0 and 94.5 g it's enabled; at 94.6 g it's disabled, as the server decides.
- [ ] F1 confirms only when allowed; F2 tares or re-tares; F4 quits (with a confirmation when something was captured); override is offered only out of tolerance.
- [ ] Lot and container fields; the container pre-filled; a scan fills the lot; empty → Confirm disabled with the reason shown.
- [ ] The override dialog needs badge, masked PIN and a reason; a wrong PIN or empty reason → an error and nothing recorded; success → done with OVR.
- [ ] After the last component → the interim summary (D2).
- [ ] Lint (including no batch literals), Stylelint, build: 0 errors; backend and earlier web tests still pass.

### How this will be tested (Stage 3)

1. **Vitest + Testing Library** with `HttpTestingController`:
   - the store: open → session, polling on and off, every command, error handling;
   - the rail: states, selection, done not selectable;
   - the readout, bar and status for **each class from server readings at 93.4 / 93.5 / 94.0 / 94.5 / 94.6 g**, the near-target hint, overshoot surplus, unstable;
   - **Confirm enabled exactly when `canConfirm`**; the missing hints;
   - F1, F2, F4 and the Quit confirmation;
   - lot, container and scan;
   - the override dialog: validation, wrong PIN, four eyes, success → OVR;
   - completion → summary navigation.
2. **Live run** (`npm run dev`, spare ports if yours are running): the B-24187 five-component weighing, with an overshoot and an override. The step-by-step guide for **STEP-20-weigh-screen.png, STEP-21-scale-switch.png, STEP-22-tolerance-ui.png, STEP-24-overshoot-ui.png, STEP-25-confirmation.png** and the **STEP-23-five-component-demo.mp4** recording is written with exact simulator commands; for example, `rising` to 93.4 g settles exactly there.

### Out of scope

- The Confirm & print screen, ticket preview and print, the review-list screen: bolt 013 (after 011 and 012).
- Full error and offline states on every screen, and Playwright E2E: bolt 014.
- Any backend change.
