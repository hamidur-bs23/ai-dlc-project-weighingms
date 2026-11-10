---
stage: plan
bolt: 014-assisted-weighing-station-ui
created: 2026-11-06T09:30:00Z
---

## Implementation Plan: assisted-weighing-station-ui (bolt 014)

### Objective

Make every failure state visible and safe on every screen, and prove the whole workflow with a Playwright run of B-24187 against the full stack, plus the STEP 30 failure suite. This is the last bolt of the intent.

Workbook: **STEP 28** (UI validation), **STEP 29** (end-to-end test) and **STEP 30** (failure scenarios).

### Stories in scope

1. `011-error-and-offline-states`: FR-23, FR-24
2. `013-end-to-end-batch-flow`: FR-1, FR-13, FR-18, FR-20, FR-21

### What the code does today (checked while planning)

Criteria of story 011 that are **not met yet**:

- **Scale offline, stale display**: when a reading fails, the store keeps the last reading. The readout goes on showing the old weight and Confirm stays enabled. The server re-reads the scale on confirm, so no bad record can result, but the operator sees a wrong weight.
- **Scale offline, wording and keys**: there is a generic banner, no "Scale offline" text, and Tare (button and F2) stays enabled.
- **Over capacity**: the Api sends `validity`, and the web app never reads it. No message exists.
- **Translated errors**: six codes the backend can emit have no text: `Printer.IdempotencyKeyMissing`, `Quantity.NotPositive`, `Quantity.UnknownUnit`, `Scale.NotFound`, `Scanner.InvalidCursor`, `Weighing.InvalidScalePolicy`. Some may never reach the UI; each gets either text or a recorded reason.

Criteria already in place, which only need tests here: OrgaTEX down on load with Retry; printer down and print pending; no tolerance defined; SCANNER OFFLINE on the strip.

Carried over from bolts 011 and 013:

- A refused OrgaTEX Retry replaces the whole screen.
- Opening a summary or review by URL leaves the header without the batch line.
- Retry on the review list after a failed page change reloads the wrong page.
- The "Batch … weighed" title is white where the mockup's is green.

Infrastructure that **does not exist yet**:

- Compose `full` has only `postgres` and `simulators`. There is no `migrator`, no `api` service and no Api image.
- The Api does not serve the Angular build.
- There is no `playwright/` workspace.
- There is no CI workflow in the repo.

### What will change

1. **Error and offline states (web)**:
   - A failed reading clears the shown weight: "—", "Scale offline", Confirm and Tare disabled, F1 and F2 included. It recovers by itself when readings return, and a tare taken before the outage still needs redoing (existing `tareStale` hint).
   - Over capacity: a clear message from `validity`, and Confirm stays disabled.
   - Text for the six codes, or a recorded reason why one can't reach the UI.
   - A refused Retry shows in a banner and keeps the screen.
   - A direct URL fills the header's batch line.
   - The review-list Retry reloads the page that failed.
   - The title colour matches the mockup.
2. **Full stack (see D1)**:
   - A multi-stage Api image that also serves the Angular build, with same-origin `/api` and a route fallback.
   - A `migrator` service that runs the EF migrations once.
   - An `api` service, healthy only after the migrator has finished.
3. **Playwright workspace** (`playwright/`, tests as `*.e2e.ts`, per coding-standards):
   - **Happy path**: sign in, select B-24187, weigh all five components on the real Weigh screen using scale scenarios, including the switch between the 3 kg and 32 kg scales, then confirm completion. Check `GET /_sim/orgatex/consumption` shows one post and `GET /_sim/printer/tickets` shows the ticket.
   - **STEP 30 suite**: scale, OrgaTEX and printer unavailable; invalid weight; over and under tolerance; re-tare; quit; incomplete batch.
   - **Accessibility**: `@axe-core/playwright` checks on the three screens (ux-guide).
4. **Evidence**:
   - `STEP-28-ui-validation/`: screens 1 and 2 comparisons and `validation.md` (screen 3 exists; it will be retaken if the title changes).
   - `STEP-29-playwright-report/` and `STEP-29-e2e-video.mp4`.
   - `STEP-30-failure-tests.md`, filled in from real runs.

### Decisions at this checkpoint: resolved (APPROVE PLAN, 2026-11-06)

- **D1: option 1.** Build Compose `full` now (E2E-FULL-STACK).
- **D2: option 1.** CI is left to the Operations phase (E2E-CI-DEFERRED).
- **D3: option 1.** Touch for batches, typing for lot and container (SCANNER-FALLBACK-SCOPE).
- **D4: option 1.** No browser print button (TICKET-NO-BROWSER-PRINT).

#### Options as presented

**D1. What does Playwright run against?** The story and the tech-stack standard say Compose `full`, which doesn't exist yet in that form.

1 - **(Recommended)** **Build Compose `full` now** as described above. It meets the story as written and also gives the one-command demo the final validation needs. It is the largest single item in this bolt.
2 - **Run against native hosts** started by Playwright. Much less work, but it departs from the story and the standard, and it never proves the packaged app works.

**D2. CI.** The story expects the suite "in CI", the standards name GitHub Actions, and the repo has no workflow at all.

1 - **(Recommended)** **Leave CI to the Operations phase and record the gap.** A whole pipeline is more than this story's suite, and the repo's remote setup is still unsettled.
2 - **Add a GitHub Actions workflow in this bolt.** It meets the wording, but it can't be proven to run until it is pushed.

**D3. Scanner down: is "touch/typed fallback" met for batches?** The queue has touch rows only, and typing exists for lot and container. PD-03 says "touch or typed fallback".

1 - **(Recommended)** **Touch for batches, typing for lot and container, no new field.** Every batch an operator can open is already a row in the queue.
2 - **Add a typed batch ID field to the queue.** Closest to the literal wording, but it adds a control the mockup doesn't have.

**D4. Is a browser print button needed?** STEP 27 ends in "Print / Print Preview". Today F1 goes to the printer API, and a browser print is reached only through the browser's menu.

1 - **(Recommended)** **No button.** The on-screen preview is the print preview, and F1 is the print.
2 - **Add a "Print preview" button** that opens the browser's dialog. A small addition, but it gives a second route to paper that isn't counted or marked REPRINT.

### Assumptions (say if any is wrong)

- **The E2E stack is isolated**: it runs as its own Compose project with its own volume and ports, so a test run can reset its database without touching the development data in `pgdata`.
- **It runs in the Development environment**, because the lab users and `/_sim/*` exist only there. The image is not a production configuration.
- **Tests share a limited seed**: there are four batches, so scenarios that only reach a fault can share them, and the happy path uses B-24187. Tests run in a fixed order rather than in parallel.
- **"Invalid weight"** in STEP 30 is tested as over capacity, which the simulator can produce. A malformed reading stays covered by the backend tests unless the simulator needs a new scenario.
- **"No tolerance defined"** stays a component-test case unless the seed already has such a chemical, because adding one would change the training scenario.

### Dependencies

- Bolts 001 to 013 (all complete).
- **New packages**: `@playwright/test` and `@axe-core/playwright`, both named in the tech-stack standard and pinned exactly. Versions are chosen at Implement, because the registry lookup timed out while planning.
- A new npm workspace, `playwright/`, in the root `package.json`.

### Acceptance criteria (from the stories)

- [ ] Scale offline: the readout shows "—" and "Scale offline", and Confirm and Tare are disabled.
- [ ] OrgaTEX down on load: an error with Retry.
- [ ] Scanner down: SCANNER OFFLINE with the fallback from D3.
- [ ] Printer down: print pending.
- [ ] "No tolerance defined" or "Over capacity": the message is shown and Confirm stays disabled.
- [ ] Any integration error: a translated message by code, never raw details.
- [ ] On the full stack, the test signs in, selects B-24187 and weighs all 5 components, and the batch completes.
- [ ] `GET /_sim/orgatex/consumption` shows one post, and `GET /_sim/printer/tickets` shows the ticket.
- [ ] Each STEP 30 fault scenario's defined behaviour is verified.

### How this will be tested (Stage 3)

1. **Vitest** for each error state, including the stale-reading fix.
2. **Backend** tests stay green, with a test for the Api serving the web build and its route fallback.
3. **Playwright** from a clean stack, run twice to catch order and timing problems.
4. **Deliberate faults** to prove the new tests can fail, for example leaving Tare enabled offline.
5. **Comparison against the mockup** for STEP 28, screen by screen.

### Limits to expect

- The real OrgaTEX, scales, scanner and printer stay simulated, so this proves the workflow against training contracts only.
- `STEP-29-e2e-video.mp4` needs converting from Playwright's recording, which needs ffmpeg or an equivalent. If neither works, the recording is submitted as is and flagged.
- Evidence the workbook expects you to capture by hand (the STEP 20 to 25 screenshots, the STEP 23 recording) is not part of this bolt.

### Out of scope

- Device repair flows and load testing (stories 011 and 013).
- A production image or deployment (Operations phase).
- STEP 31 onwards, which come after this bolt.
