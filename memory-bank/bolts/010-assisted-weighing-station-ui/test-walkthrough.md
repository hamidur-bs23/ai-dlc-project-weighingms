---
stage: test
bolt: 010-assisted-weighing-station-ui
created: 2026-11-06T03:25:00Z
---

## Test Report: assisted-weighing-station-ui (bolt 010)

### Summary

- **Web tests**: **131/131 passed** (Vitest 5 + Testing Library, jsdom), the same in 3 consecutive runs. **59 are new**, in 6 spec files:
  - `weighing-session.store` 13;
  - `weigh.component` 24;
  - `weigh-bar` 8;
  - `recipe-rail` 4;
  - `override-dialog` 6;
  - `batch-summary` 3.
- **Coverage** (web, v8): **97.9% lines** overall; `features/weigh` 97.5% lines; `features/batch-summary` 98.7%.
- **Lint**: `npm run lint` is clean (strict-type-checked ESLint, template accessibility, the batch-literal rule and its self-check, Stylelint); Prettier is clean; the production build has 0 warnings.
- **Backend**: unchanged, **582/582** pass.
- **Evidence**: `evidence/STEP-22-tolerance-ui-tests.txt` is the verbose run of the 59 Weigh / override / summary cases, including the five STEP 22 boundary readings and STEP 21. The screenshots and the STEP 23 recording are taken by you; see the guide below.

### Test Files

- [x] `features/weigh/weighing-session.store.spec.ts`:
  - open loads `/api/scales`, the recipe (STEP 19 flow) and the session; with nothing active, **the suggested component is selected** (PD-07);
  - **polling at the server interval, one request at a time** (`exhaustMap`);
  - a failed poll shows the device error and polling continues;
  - can-confirm and the override offer **only from the server**;
  - a reading of another component is ignored;
  - **commands are queued in order and never dropped** (container on blur + lot on Enter: the regression test for the defect found in the live run);
  - a command error goes to the banner and the next command still runs;
  - confirm names the active component, and the next active one is followed;
  - **polling stops when the batch completes**;
  - override errors return to the dialog;
  - a session that can't be opened; quit and leave.
- [x] `features/weigh/weigh.component.spec.ts`:
  - **STEP 22** with server readings: **93.4 g → DISPENSE — 0.6 g REMAINING, Confirm disabled · 93.5 g → WITHIN TOLERANCE, enabled · 94.0 g → TARGET ACHIEVED, enabled · 94.5 g → WITHIN, enabled · 94.6 g → OVER TOLERANCE — SURPLUS +0.6 g (+0.6%), disabled**;
  - the header (code, LOT, CONTAINER, TARE), **badge "3 kg precision · automatic"**, still to weigh, the caption "(93.5 – 94.5)";
  - **STEP 21: Sodium Sulphate → "32 kg floor"**;
  - the amber NEAR TARGET at 86 g;
  - **UNSTABLE** tag and "Wait for a stable reading";
  - the missing hints (lot, container, tare);
  - overshoot: the surplus status and recovery text, override offered, **no recalculation table**; no override within tolerance;
  - container pre-filled C-12; **lot trimmed on Enter**; container on Enter;
  - **a scan fills the lot**;
  - **F1 refused unless allowed**; F1 confirm → the next component;
  - F2 tare, then the Re-tare label;
  - **F4 asks first when something was captured**, then quits; quits at once when nothing was; "Keep weighing" cancels;
  - the override dialog opens, with the PIN masked;
  - **completion → `/batches/B-24187/summary`**;
  - Back to queue → leave → `/batches`;
  - a command error with Ref;
  - NO TOLERANCE DEFINED.
- [x] `features/weigh/weigh-bar.component.spec.ts` (**BAR-RANGE**):
  - the caption with range numbers only, and the maximum 118.1 g (1.25 × 94.5);
  - the fill at 0 / 59.1 / 94.5 / 118.1 / 500 g → 0 / 50 / 80 / 100 / 100% (pinned);
  - the band and target at about 80%;
  - the zone attribute.
- [x] `features/weigh/recipe-rail.component.spec.ts`: the header and "2 of 5 components weighed"; states `✓ 1241.5 g`, `OVR`, `WEIGHING`, `PENDING`; code · scale class; targets in g and kg; `aria-current`; touching a pending row selects it; **done rows can't be selected**.
- [x] `features/weigh/override-dialog.component.spec.ts`:
  - **badge, PIN and reason are required before anything is sent**; the PIN is masked;
  - **a wrong PIN → the error, the PIN cleared, nothing recorded**;
  - success → step-up, then the override with the grant and the trimmed reason, then close;
  - a refused override (four eyes) stays open with the error;
  - cancel.
- [x] `features/batch-summary/batch-summary.component.spec.ts`: 5 of 5, NO · 1 override, 35.322 kg, 6 min 41 s, operators, the OVR line; read-only; Retry after an error; Back to queue.

### Acceptance Criteria Validation

- ✅ **004**: the rail with swatch, name, code, scale, target and state (✓ actual / WEIGHING / PENDING) and "n of N"; touching a pending component activates it; done ones can't be selected; the header shows name, code, LOT, CONTAINER, TARE and the "automatic" scale badge.
- ✅ **005**: Weighed now / Still to weigh / Target with units (1 dp g, 3 dp kg); the bar with fill, band and target line, and the caption "TOLERANCE ±0.5 g (93.5 – 94.5)"; a distinct state with text per class (Target achieved distinct); the amber near-target hint; overshoot shows only the surplus (no recalculation); unstable is labelled.
- ✅ **006**: OK · Confirm (F1) enabled exactly when the server says `canConfirm`; Re-tare (F2) and Quit (F4) send their commands; Supervisor override is offered only out of tolerance and opens the dialog.
- ✅ **007**: lot and container fields; a scan fills the lot; the suggested container is pre-filled and editable; an empty field → Confirm disabled with the reason shown.
- ✅ **008**: badge, masked PIN and a mandatory reason; a wrong PIN or no reason → an error and nothing recorded; success → done with OVR.
- ✅ **D2 / INTERIM-SUMMARY**: the last record → the read-only summary.
- ✅ Lint, Stylelint and build have 0 errors; backend and earlier web tests pass.

### Issues Found

- **Two defects were found in the Implement-stage live run and fixed before this stage**: the dropped lot command (now a queue) and the units in the range caption. The store spec now has a regression test for the queue.
- **In this stage**: no product defects. The spec fixes were harness-only: a queued command starts one microtask later, so specs await it; `expectOne` consumes a request; strict lint on spec casts.

### Evidence guide (STEP 20–25 screenshots and the STEP 23 recording)

**Before you start**:
1. **Restart your Api and simulator.** The instances running on 5080/5090 were started before bolts 007–010 and don't have the weighing endpoints. Stop them, then run `npm run dev` from the repo root (simulator :5090, Api :5080, web :4200).
2. **Recommended: a clean training batch.** The development database still holds my test runs (B-24187 already has Yellow RGB confirmed). For screenshots that start from B-24187 "0 of 5", reset the development database. This deletes development data only:
   - `docker compose down`
   - `docker volume rm weighingms_pgdata`
   - `docker compose up -d postgres`
   - `npm run db:update`
   - then `npm run dev` again (the lab users are re-seeded).
3. Open http://localhost:4200, sign in **OP-1001**, shift **A**, and touch **B-24187**. The Weigh screen opens on Remazol Brill. Blue RSPL.

**Simulator commands** (a second terminal, repo root). The pour settles after about 8 s:
- reset the scales: `curl -X POST localhost:5090/_sim/reset`
- pour to an exact weight on the precision scale: `curl -X POST localhost:5090/_sim/scales/scale-3kg/scenario -H 'content-type: application/json' -d '{"scenario":"rising","target":{"value":94.0,"unit":"g"}}'`
- floor scale: the same with `scale-32kg` and `{"value":24.0,"unit":"kg"}`
- overshoot: `-d '{"scenario":"overshoot","target":{"value":94.0,"unit":"g"},"tolerance":0.5}'` (lands at 96.4 g)

**For each component**: type the lot and press Enter, confirm the container (click it, Enter), **reset the scales**, press **F2** (tare), then send the pour.

1. **`STEP-20-weigh-screen.png`**: on Blue RSPL, after lot, container and F2, pour `rising` to 900.0 g. Take a screenshot of the whole station screen (rail, header, badge, readout, bar, status, keys).
2. **`STEP-21-scale-switch.png`**:
   - weigh 1–3 (pour exactly to the target: 1240.0, 386.0, 94.0 g; then F1);
   - when **Sodium Sulphate** becomes active, the badge reads **32 kg floor** and the rail shows the three precision components done;
   - take the screenshot (optionally also one of Yellow RGB showing **3 kg precision**, side by side).
3. **`STEP-22-tolerance-ui.png`**: on **Yellow RGB**, for each of **93.4, 93.5, 94.0, 94.5, 94.6 g**, reset the scales, press F2, pour `rising` to that value and take a screenshot of the status line and keys. Confirm should be disabled at 93.4 and 94.6, and enabled at the other three. Combine the five, or submit them as `STEP-22-tolerance-ui-93.4.png` and so on. (Don't press F1 while doing this.)
4. **`STEP-24-overshoot-ui.png`**: on Yellow RGB, reset, F2, `overshoot` (96.4 g) → **OVER TOLERANCE — SURPLUS +2.4 g (+2.6%)**, the recovery text and **Supervisor override**. Optionally open the dialog (SV-2001, PIN 2468, reason) for a second screenshot.
5. **`STEP-25-confirmation.png`**: on Yellow RGB, reset, F2, pour `rising` to **94.2** → WITHIN TOLERANCE → press **F1**. Take the screenshot after the confirm: the rail shows **✓ 94.2 g**, and Sodium Sulphate is active. (DevTools → Network → `confirmation` → Response shows the same STEP 25 record as `evidence/STEP-25-transaction.json`.)
6. **`STEP-23-five-component-demo.mp4`** (macOS: ⇧⌘5 → "Record Selected Portion"): with a fresh B-24187, weigh all five in order (1240.0 g, 386.0 g, 94.0 g on `scale-3kg`; 24.000 kg, 9.600 kg on `scale-32kg`). Each moves to the next automatically, and the last opens the summary "Batch B-24187 weighed · 5 of 5". Before the last one, show that it can't complete early: the summary only appears after all five are done.
