---
stage: implement
bolt: 010-assisted-weighing-station-ui
created: 2026-11-06T03:15:00Z
---

## Implementation Walkthrough: assisted-weighing-station-ui (bolt 010)

### Summary

**Screen 2 · Weigh** now runs on `/batches/:batchId/weigh` against the weighing Api of bolts 007–009, replacing bolt 006's placeholder. The screen renders what the server decides:
- class, can-confirm and what's missing;
- surplus and the override offer.

It sends the operator's commands:
- select, lot, container;
- tare / re-tare, confirm, quit;
- override, leave.

After the last component it opens the **interim summary** (INTERIM-SUMMARY). No backend change was needed.

### Structure Overview

- **`core/weighing/weighing-api.service.ts`**: typed calls for every session endpoint, plus `/api/scales` and the step-up.
- **`features/weigh/weighing-session.store.ts`** (NgRx SignalStore):
  - the session, recipe, scale names, poll interval, latest reading, errors;
  - **reading polling** at the server's `pollIntervalMs` while a component is active (`exhaustMap`);
  - **commands queued in order, never dropped**.
- **`features/weigh/weigh.component.*`**: the header with the scale badge, lot and container fields, readout, status line, missing hints, keys F1/F2/F4, the Quit confirmation, Back to queue.
- **`features/weigh/recipe-rail.component.ts`**, **`weigh-bar.component.ts`** (BAR-RANGE), **`override-dialog.component.ts`** (CDK Dialog).
- **`features/batch-summary/batch-summary.component.ts`**: the read-only interim summary.

### Completed Work

- [x] `web/src/app/core/weighing/weighing-api.service.ts`; `core/api/api.types.ts` (weighing types, `WeightClass`).
- [x] `web/src/app/features/weigh/weighing-session.store.ts`:
  - `open` loads `/api/scales`, `GET …/recipe` (the STEP 19 flow kept) and `POST …/session`; with nothing active it selects the suggested component (PD-07);
  - `select`, `captureLot`, `captureContainer`, `tare`, `confirm`, `quit`, `leave`, `override`;
  - `canConfirm` and `overrideOffered` straight from the reading.
- [x] `web/src/app/features/weigh/recipe-rail.component.ts`:
  - each component's swatch, name, code · scale class, target and state `✓ actual` [`OVR`] / `WEIGHING` / `PENDING`;
  - "n of N components weighed"; done rows are disabled.
- [x] `web/src/app/features/weigh/weigh-bar.component.ts`:
  - fill, tolerance band and target line over **0 → 1.25 × the upper bound** (BAR-RANGE);
  - caption "TOLERANCE ±0.5 g (93.5 – 94.5)";
  - `progressbar` role.
- [x] `web/src/app/features/weigh/weigh.component.{ts,html,scss}`:
  - the header (code, LOT, CONTAINER, TARE) and the badge "SCALE SELECTED · 3 kg precision / 32 kg floor · automatic";
  - lot and container fields: the container pre-filled, Enter or blur saves, a scan fills the lot without stealing focus;
  - readout "Weighed now / Still to weigh / Target" with an **UNSTABLE** tag;
  - a status line per class (DISPENSE … REMAINING, amber NEAR TARGET at ≥ 90%, WITHIN TOLERANCE, TARGET ACHIEVED, OVER TOLERANCE — SURPLUS +x (+y%) with the three recovery actions; NO TOLERANCE DEFINED; WAITING FOR THE SCALE);
  - **plain-language "missing" hints** from the server;
  - keys F1 (only when `canConfirm`), F2 Tare / Re-tare, F4 Quit (confirmation when something was captured), Supervisor override (only when offered), Back to queue (leave);
  - class changes announced politely.
- [x] `web/src/app/features/weigh/override-dialog.component.ts`: badge, masked PIN, reason (required, up to 500); step-up then override; errors with Ref; the PIN is cleared after a failure.
- [x] `web/src/app/features/batch-summary/batch-summary.component.ts`: n of N, YES or NO + overrides, total dispensed, elapsed, operators, completed time, OrgaTEX "—", per-chemical lines with OVR, Back to queue.
- [x] Modified:
  - `app.routes.ts` (the weigh route → `WeighComponent`; new `/batches/:batchId/summary`);
  - `core/format/weight.pipe.ts` (`withUnit` option, used for the range numbers);
  - `styles.scss` (CDK overlay);
  - `public/i18n/en.json` (`weigh`, `override`, `summary`, error codes `Weighing.*`, `Auth.*`, `Batch.Incomplete`, `Concurrency.Conflict`);
  - `web/package.json` (`@ngrx/signals` 22.0.1).
- [x] Removed: `features/batch-opened/*` (bolt 006's placeholder; its 5 specs went with it, so 72 web specs remain until Stage 3).

### Key Decisions

- **The approved decisions**: BAR-RANGE, INTERIM-SUMMARY.
- **The server is the only judge**: the UI never compares weights. The one UI-side rule is the amber "near target" hint (≥ 90% of target, display only, OQ-4), which the story assigns to the UI.
- **Field drafts use `linkedSignal`**: they reset when the active component or its saved value changes, and otherwise keep what the operator types.
- **Commands are queued, not dropped.** Leaving the container field saves it while the operator is already typing the lot, and both must reach the server in order. Double confirms are still impossible: F1 is enabled only when nothing is pending, and the server rejects a repeat.
- **A fresh or resumed session with nothing active starts on the suggested component**, so the operator lands on a weighable component.

### Deviations from Plan

- **None in scope.** The interim summary shows the OrgaTEX state; the ticket state (also "—") is left for bolt 013's screen.

### Dependencies Added

- `@ngrx/signals` 22.0.1 (web).

### Developer Notes

- **Verification**: `npm run lint` is clean (ESLint, the batch-literal rule and its self-check, Stylelint); Prettier is clean; the production build has 0 warnings; **72 web specs pass**; there is no backend change.
- **Live run** in a real browser, `ng serve` on 4300 → Api 5180 → simulator 5190 (5080 and 5090 untouched), batch B-24188, operator S. Akter:
  - the queue → Weigh: the rail (5 components, swatches, "0 of 5"), Blue RSPL active, badge **3 kg precision · automatic**, container pre-filled C-14, hints "lot, container, tare";
  - lot + container + **F2 tare** (214.0 g) + pour → **WITHIN TOLERANCE**, Confirm enabled, green → **F1** → "1 of 5", Red RB active;
  - Red RB overshoot → **OVER TOLERANCE — SURPLUS +10.6 g (+2.5%)** with the recovery text, Confirm disabled, **Supervisor override** offered;
  - the dialog with a wrong PIN → "Supervisor badge or PIN not accepted. Ref …" and nothing recorded; with the right PIN (SV-2001) → rail "✓ 428.8 g OVR", Yellow RGB active;
  - Yellow RGB poured to exactly 101.8 g → **TARGET ACHIEVED** → F1;
  - **Sodium Sulphate → badge "32 kg floor"** (STEP 21), kg to 3 dp, range "(25.870 – 26.130)";
  - Soda Ash: Within tolerance while **UNSTABLE** → F1 refused with "Wait for a stable reading"; once stable → F1 → **the last record completed the batch → the summary**: 5 of 5, NO · 1 override, **38.298 kg**, S. Akter, Red RB "Overweight OVR".
  - 0 error lines in the Api log.
- **Defects found and fixed during the live run**:
  1. **A lot typed right after another field's save was lost.** Two causes: (a) an effect reset the field drafts late; (b) the store **dropped** a command while another was in flight (blurring the container field saved it while the lot was being entered). Fixed with `linkedSignal` drafts and a command queue, and re-verified: the container and lot both arrive in order within the same second.
  2. The range caption showed units inside the brackets; it now reads "(93.5 – 94.5)" as in FR-10 and the mockup.
- **Stage 3**:
  - Vitest specs for the store (open, polling, queued commands), the rail, the bar (BAR-RANGE), every status per class with the 93.4 / 93.5 / 94.0 / 94.5 / 94.6 g boundary readings, Confirm enabled exactly when `canConfirm`, the hints, F1/F2/F4 and the Quit confirmation, lot / container / scan, and the override dialog;
  - the summary component;
  - the screenshot and recording guide for STEP 20–25.
