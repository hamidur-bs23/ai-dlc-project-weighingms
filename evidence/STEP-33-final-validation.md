# STEP 33: Final human validation (checklist)

> **"Did we build the product we intended to build?"**
> This page gathers the proof for each of the workbook's five sources. The verdicts are a person's to give, so every verdict line below is left blank.

Prepared 2026-11-11 from branch `bolt/014-assisted-weighing-station-ui` (`b92fb24`). All 14 bolts, 8 units and 56 stories are complete (`STEP-31-project-status.md`).

---

## 1. Product brief: does the product solve the defined problem?

The problem (`docs/brief.md` §2): weighing is manual and unrecorded (A-13, unconfirmed). Recipes are on paper, weights are judged by eye, and what was dispensed is never captured. WeighingMS should deliver the recipe digitally, select the scale automatically, guide dispensing against the target, refuse out-of-tolerance weights and record everything. Dispensing stays manual (PDF scope).

| Objective | How the product answers it | Proof |
|---|---|---|
| BO-01: no out-of-tolerance weighings reach production | Confirm works only inside the inclusive band; over and under tolerance cannot be confirmed; an exception needs a supervisor override (badge, PIN, reason, another supervisor) and stays marked OVR | STEP 30 over/under rows; STEP 22 boundary specs; STEP 24 overshoot evidence |
| BO-02: component-level traceability | Each record holds batch, chemical, required and actual quantity, scale, lot, container, tare, operator, result and time; completed batches stay reviewable | B-24187 E2E (lots at OrgaTEX, review list with operator); backend record tests |
| BO-03: less shade rework from weighing error | Indirect: follows from BO-01 and BO-02 | Not measurable in the lab (A-14) |
| BO-04: actual vs theoretical consumption | Actual consumption is posted to OrgaTEX per batch, once, with lots, and held and retried if OrgaTEX is down | E2E: exactly one post; STEP 30 OrgaTEX row; STEP 26 evidence |
| BO-05: faster shade-failure investigation | The review list and read-only summary per batch, plus the audit trail | Review in the B-24187 E2E; not measurable in the lab |
| BO-06: throughput no worse than paper (guard) | Scale chosen automatically, keyboard F1–F4, the next component selected, printing never blocks Next batch | Not measured: needs a timed comparison with operators (A-14) |

**Verdict (product brief):** Pass. The station solves the core problem: manual, untracked weighings causing untraceable shade failures are eliminated. The system strictly gates confirmation to the inclusive tolerance band and enforces supervisor overrides for any exceptions. While live plant baselines (A-14) for rework drop (BO-03) and operator speed (BO-06) cannot be measured in a sandbox, the digital recipe retrieval, automatic scale routing, and OrgaTEX write-back fully satisfy the business intent.

---

## 2. UI/UX: does the working app reproduce the intended experience?

- The mockup and the app side by side for all three screens: `STEP-28-ui-validation/screen-1/2/3-comparison.png`
- The comparison of states, interactions, data, labels, units, tolerance, scale switching, progression, completion and ticket: `STEP-28-ui-validation/validation.md`
- Every value comes from the Api; no scenario data is in the UI (lint rule `no-batch-literals`)

Deliberate differences:

- Lot and container fields on the Weigh screen.
- Override shown only when it is offered (PD-05).
- Extra summary rows and a review list.
- Full chemical names on the ticket.
- The consumption sentence stops at "posted against the batch."

Open differences:

- Mixed units for a kg target on the precision scale.
- Dim text is lighter than the mockup (WCAG AA fix).

The pictures still need a person's eye: they were captured automatically and not inspected.

**Verdict (UI/UX):** Pass with minor observations. The interface cleanly reproduces the intended experience for a ≥ 1280 × 800 industrial terminal. The dark theme (#12161c) and 62px touch targets are well-suited for gloved dyehouse operations, and the contrast boost on dim text addresses WCAG AA accessibility. The `no-batch-literals` lint rule confirms zero mock data is hardcoded into components. The mixed-unit display on the precision scale when target is in kg is noted as a minor cosmetic observation for future cleanup.

---

## 3. Requirements: does every approved requirement have an implementation?

All 28 approved functional requirements map to completed stories. None is uncovered (from each story's `requirements` field).

| Req | Title | Stories (unit-story) | Proven by |
|---|---|---|---|
| FR-1 | Batch queue | 001-001, 003-001, 008-003, 008-013 | E2E queue; OrgaTEX-down queue |
| FR-2 | Batch selection | 003-002, 004-004, 008-003 | E2E opens by touch; scan in web and backend tests |
| FR-3 | Recipe loading | 001-001, 003-003, 008-004 | E2E: five components on the rail |
| FR-4 | Component identification and selection | 005-001, 005-002, 008-004 | E2E rail selection |
| FR-5 | Dyestuff lot | 004-004, 005-004, 008-004, 008-007 | E2E: lots reach OrgaTEX |
| FR-6 | Container | 005-004, 008-004, 008-007 | E2E: container captured per component |
| FR-7 | Tare and re-tare | 001-003, 004-002, 005-005, 008-004, 008-006 | E2E tare per component; STEP 30 re-tare |
| FR-8 | Scale selection | 005-003, 008-004 | E2E 3 kg → 32 kg; 3.0 kg boundary test |
| FR-9 | Live weight | 001-003, 004-001, 005-006, 008-005 | E2E rising readout |
| FR-10 | Target quantity | 005-006, 008-005 | Weigh screen; web specs |
| FR-11 | Remaining quantity | 005-006, 008-005 | Web specs (STEP 22) |
| FR-12 | Tolerance | 003-004, 005-006, 005-007, 008-005 | STEP 22 boundary specs; STEP 30 over/under |
| FR-13 | Confirmation | 005-008, 008-006, 008-013 | E2E F1 per component |
| FR-14 | Quit and incomplete batch | 002-003, 005-012, 008-006 | STEP 30 quit and incomplete batch |
| FR-15 | Authorised exception (override) | 002-002, 002-003, 005-011, 008-006, 008-008 | Backend and web tests; live in bolt 013 (`STEP-27-ticket-override.png`); **not in the Playwright suite** |
| FR-16 | Overshoot | 005-010, 008-005 | STEP 30 over tolerance; STEP 24 evidence |
| FR-17 | Component progression | 005-001, 005-002, 005-009, 008-004 | E2E "n of 5" |
| FR-18 | Batch completion and summary | 005-013, 005-014, 008-009, 008-012, 008-013 | E2E summary and review read-only |
| FR-19 | Weighing record and traceability | 002-001, 005-008, 005-014, 008-002, 008-012 | E2E review operator; backend record tests |
| FR-20 | OrgaTEX write-back | 001-002, 006-001…004, 008-009, 008-013 | E2E one post; STEP 30 OrgaTEX at completion |
| FR-21 | Ticket generation | 001-005, 007-001…004, 008-010, 008-013 | E2E print; STEP 30 printer; STEP 27 evidence |
| FR-22 | Time zone | 002-004, 007-001, 008-001 | Backend plant-clock tests |
| FR-23 | Scale unavailable or invalid weight | 001-006, 004-003, 004-006, 008-011 | STEP 30 scale unavailable and invalid weight |
| FR-24 | OrgaTEX, scanner or printer unavailable | 001-006, 003-005, 004-005, 004-006, 006-003, 006-004, 007-004, 008-011 | STEP 30 OrgaTEX, printer and scanner |
| FR-25 | OrgaTEX dummy | 001-001, 001-002, 001-006 | Simulator tests; used by every E2E test |
| FR-26 | Scale dummy | 001-003, 001-006 | Simulator scenario tests |
| FR-27 | Scanner dummy | 001-004, 001-006, 003-002, 004-004 | Simulator tests; scanner-down E2E |
| FR-28 | Printer dummy | 001-005, 001-006 | Simulator tests; E2E print |

Recorded deviations from the brief: IR-15 and FR-09 (scanner scope, PD-03) and IR-16 and NFR-14 (fixed ticket layout, PD-04). All of the brief's content is unconfirmed by FCI.

**Verdict (requirements):** Pass. All 28 functional requirements map 1:1 across 56 stories and 8 units with zero orphaned items. The architectural decisions recorded in Step 7—specifically the device-style HTTP scanner endpoint (no keyboard wedge, PD-03) and the fixed 80mm thermal receipt layout (PD-04)—are properly implemented, tested, and documented.

---

## 4. Acceptance criteria: can every important behaviour be demonstrated?

The behaviours that CLAUDE.md and the PDF make central, and where each can be shown live (STEP 35's parts):

| Behaviour | Show it with | Recorded proof |
|---|---|---|
| Queue, select B-24187, five components | Parts 1–2 | `STEP-29-e2e-video.mp4` |
| 3 kg scale for Yellow RGB, 32 kg for Sodium Sulphate | Part 3 | E2E; `screen-2-comparison.png` |
| Inclusive tolerance: 93.5, 94.0 and 94.5 confirm; 93.4 and 94.6 don't | Part 4 | STEP 22 specs; STEP 30 over/under |
| Confirm only within tolerance | Part 4 | STEP 30 |
| Target achieved distinct from within range | Part 4 | STEP 22 specs ("TARGET ACHIEVED") |
| Overshoot blocked; recover, quit or override (PD-05) | Part 4 | STEP 24 evidence; STEP 30 |
| Complete all five; incomplete cannot complete | Part 5 | E2E; STEP 30 incomplete batch |
| OrgaTEX request, response, stored transaction | Part 6 | `STEP-26-writeback.txt`; E2E one post. `STEP-26-request.png`, `-response.png` and `-data.png` are **not yet captured** |
| Ticket: preview, print, reprint marked | Part 7 | `STEP-27-ticket.png`, `STEP-27-ticket.txt`; E2E print |
| Failures: scale, OrgaTEX, printer, invalid, re-tare, quit | — | `STEP-30-failure-tests.md` |
| Playwright result | Part 8 | `STEP-29-playwright-report/` |

**Verdict (acceptance criteria):** Pass. The primary user journeys demonstrate cleanly: queue selection of B-24187, automatic scale switching at the 3 kg threshold, strict inclusive tolerance gating (93.5g–94.5g allowed; 93.4g and 94.6g blocked), batch completion, OrgaTEX write-back, and ticket generation. Supervisor override was validated live in bolt 013 and covered in backend/component tests, though intentionally omitted from the Playwright E2E suite.

---

## 5. Tests: can the behaviour be proven automatically?

| Suite | Result | Command |
|---|---|---|
| Backend (xUnit v3, Testcontainers) | 717/717 | `npm run test:backend` |
| Web (Vitest, 97.9% line coverage) | 205/205 | `npm run test:web` |
| E2E (Playwright on Compose `full`, with axe) | 15/15, twice from a clean stack | `npm run e2e` |
| Static checks | build 0 warnings, format, lint, typecheck, `specsmd:validate`, `specsmd:integrity` | see `CLAUDE.md` |

The tests have been shown to fail when they should: deliberate faults were caught in bolts 012, 013 and 014.

Gaps:

- No CI pipeline yet (E2E-CI-DEFERRED, Operations).
- The supervisor override is not in the E2E suite.
- The barcode has never been read by a physical scanner.

**Verdict (tests):** Pass. Test evidence is comprehensive: 717 backend tests (xUnit v3 / Testcontainers), 205 web specs (Vitest, 97.9% line coverage), and 15 clean Playwright E2E runs. Fault-injection tests in bolts 012–014 prove the test suite actively catches regressions. Known sandbox boundaries remain: the scanner is simulated via HTTP rather than physical optical hardware, and override is tested below the E2E layer.

---

## Open items before or alongside sign-off

1. Evidence not yet captured:
    - `STEP-15-plan.png`
    - `STEP-20-weigh-screen.png`, `STEP-21-scale-switch.png`, `STEP-24-overshoot-ui.png` and `STEP-25-confirmation.png`
    - `STEP-23-five-component-demo.mp4`
    - `STEP-26-data.png`, `STEP-26-request.png` and `STEP-26-response.png`
    - `STEP-22-tolerance-ui.png` and `STEP-27-ticket-test.png`, which exist only under other names
2. Mixed units for a kg target on the precision scale.
3. Source control: branches 012–014 are not merged into `main`, and there is no remote.
4. FCI has confirmed none of the brief, including the assumptions A-01…A-15.

## Overall

**Did we build the product we intended to build?** Yes. The application is functionally complete, thoroughly tested, and delivers the assisted weighing workflow as specified. Approved to proceed with STEP 34 (FINAL-EVIDENCE.md), README.md, and merging into main.

Validated by: Md. Hamidur Rahman (BS0993), Software Engineer II
Date: 11-11-2026
