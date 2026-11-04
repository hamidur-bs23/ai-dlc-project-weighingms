---
intent: 001-assisted-weighing-station
phase: inception
status: construction
created: '2026-11-03T21:41:00Z'
updated: '2026-11-04T07:30:00Z'
---

# Requirements: Assisted Dyestuff Weighing Station (WeighingMS)

> **Stage: Requirements (STEP 7). Awaiting CHECKPOINT 2 (`APPROVE REQUIREMENTS`).**
> The intent sections (Intent Overview to Dummy Integrations) were approved at CHECKPOINT 1 on 2026-11-03T21:57:00Z.
> The evidence for the STEP 7 decisions is in `decisions-brief.md`.

## How to read the requirements

Every requirement has a **Basis** tag. The tags keep four kinds of statement apart: confirmed requirements, assumptions, unresolved items and training-only dummy behaviour.

| Tag | Meaning |
|---|---|
| **[C] Confirmed** | Stated in the project-requirement PDF or `workbook.md`. These define mandatory scope (ADR-015) |
| **[D] Decided** | A product-owner decision made at STEP 7 (PD-xx, TOL, OVR in `standards/decision-index.md`). It is not an FCI confirmation |
| **[A] Assumption** | Taken from `docs/brief.md` and resting on an A-xx that **FCI has not confirmed** |
| **[P] Proposed** | Proposed by the agent where every source is silent. OQ-1 to OQ-4 were proposed this way and accepted at CHECKPOINT 2 (now [D]) |
| **[T] Training-only** | Behaviour of the dummy integrations. It is not a claim about the real OrgaTEX or devices |

The `FR-n` numbers belong to this intent. Requirements from the brief are cited as "brief FR-xx".

---

## Intent Overview

**WeighingMS** is an assisted weighing station for the dyeing department of FCI, an RMG / textile dyeing facility.
It guides an operator through hand-weighing every chemical (dyestuffs and auxiliaries) in a dyeing job's recipe,
and it records what was actually weighed.

**Business problem.** Recipe weighing is assumed to be manual and unrecorded today (brief §2, §6.1, *A-13 unconfirmed*).
When a batch fails shade approval, there is no way to tell whether the recipe or the weighing was at fault.
The PDF states the objective: "improve weighing accuracy, reduce manual calculation errors, and provide a clear,
controlled, and traceable chemical-weighing process" (PDF §1).

**What WeighingMS is:**
- A **precision-weighing management system**. It guides and monitors; operators dispense by hand (PDF §1).
- A station **between the operator and the OrgaTEX MES**. OrgaTEX stays the system of record for jobs and recipes,
  and it receives the actual consumption. **WeighingMS does not replace or modify OrgaTEX** (brief §5.2).

### Users

| Role | Relationship | Source |
|------|--------------|--------|
| **Weighing operator** | **Primary user.** Selects a job, weighs each chemical, confirms, completes | PDF §1, §6; brief §4 |
| Dyehouse supervisor | Grants the authorised exception (override) | PDF §6 Step 7; brief §4 |
| Lab / QA | Reviews weighed-vs-recipe records | PDF §10; brief §4 |
| Store / inventory | Uses actual consumption | brief §4, BO-04 (*unconfirmed*) |
| FCI IT | Network, deployment, support | brief §4 |

### Desired Outcome

A **complete, working weighing workflow end to end**, running against mock integrations (PDF §12):

> select a job → recipe → for each chemical: the system selects the scale → live weight → classify →
> confirm → complete → summary, consumption posted to OrgaTEX, ticket printed.

**Top acceptance criterion:** the whole workflow works end to end. Resembling the mockup is not enough (PDF §12).

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| G1 Core workflow (PDF §11 P1) | An operator completes seed batch `B-24187` (5 components) end to end against the dummy APIs. The UI holds no hard-coded scenario data | Must |
| G2 Operational visibility (PDF §11 P2) | Progress and the Under/Within/Over state are visible at all times. A completed-job summary can be reviewed | Must |
| G3 Integration readiness (PDF §11 P3) | All four dummies sit behind HTTP integration boundaries and can be replaced without changing business logic | Must |
| G4 Weighing accuracy (PDF §6 Step 7) | No out-of-tolerance weighing is recorded as successful without an authorised exception | Must |
| G5 Traceability (PDF §10; workbook STEP 25) | Every confirmed weighing carries the full record set (FR-19) | Must |
| G6 Failure behaviour (workbook STEP 30) | Every STEP 30 scenario, plus scanner offline, has defined and tested behaviour | Must |

## Source Artifacts and Precedence

| Source | Role | Precedence (ADR-015) |
|--------|------|----------------------|
| `docs/project-requirement.pdf` (`docs/project-requirement.md`) | Business intent, workflow, rules | **Mandatory scope** |
| `workbook.md` | Lab process, dummy endpoints, seed scenario, failure scenarios | **Mandatory scope** |
| `docs/customize-weighing-station.html` | **UI/UX source** with three views. Read for its states and rules, **not copied** | Visual + interaction reference |
| `docs/brief.md` (draft BRD v1.1) | Fills gaps only | *Unconfirmed by FCI* |

## Scope

### In scope
The 20 functional areas from workbook STEP 7, specified as FR-1 to FR-28 below.

### Out of scope (this intent)
- Controlling a dispensing machine; automated transport; recipe authoring; replacing or modifying OrgaTEX; shade approval; machine-side dyeing control (brief §5.2).
- Integration with the **real** OrgaTEX or real devices.
- Liquid dosing (*A-12*).
- **Deviations from the unconfirmed brief, made deliberately:**
  - Keyboard-wedge scanner mode (IR-15) (PD-03).
  - Dyestuff identity verification by barcode (FR-09/BR-10) (PD-03).
  - Badge scan and container scan (PD-03).
  - Configurable ticket template (IR-16, NFR-14) (PD-04).
  - Scale calibration tracking (FR-12, BR-24).
  - Supervisor manual scale reselection (FR-08).
  - Multi-station (A-11).
  - Reporting beyond the completed-job summary (FR-29, BR-23).
- **Overshoot handling beyond FR-16** is pending PD-05 (STEP 24).

### Dummy Integrations (first-class product code)
The dummies run in a separate simulator process (ADR-003) and are reached **over HTTP only**. The UI calls only the WeighingMS API.
They are **training contracts, not claims about the real OrgaTEX interface**. No real OrgaTEX contract is invented. See FR-25 to FR-28.

---

## Functional Requirements

### A. Batch queue, selection and recipe

### FR-1: Batch queue *(workbook topic 1)*
- **Description**: Show the batches released for weighing, read from the OrgaTEX dummy (`GET /api/batches`) through the WeighingMS API.
- **Acceptance Criteria**:
  - Each row shows batch, order, machine, fabric/lot description, shade, component count and status (queued / weighing / complete). Completed rows also show "print pending" or "sync pending" when either applies.
  - With seed data, the queue shows B-24187, B-24188, B-24190 and B-24191.
  - No batch data is defined in UI code. Removing a batch from the dummy's seed removes it from the screen.
- **Priority**: Must
- **Basis**: [C] PDF §3, §4.1, §6 Step 1; workbook STEP 16. Columns from the mockup view 1.

### FR-2: Batch selection *(topic 2)*
- **Description**: The operator selects a batch by touching its row or by scanning its barcode (FR-27).
- **Acceptance Criteria**:
  - Touch and scan give the same result: the Weigh view opens for that batch.
  - A scanned ID that isn't in the queue shows "batch not found" and changes nothing.
  - A completed batch opens its read-only summary (FR-18) instead of the Weigh view.
  - The header shows batch, order, machine, shade and the signed-in operator.
- **Priority**: Must
- **Basis**: [C] PDF §6 Step 2. [D] PD-03. Mockup: "Scan a batch card to begin. Or touch a row below." [A] brief FR-02, FR-03.

### FR-3: Recipe loading *(topic 3)*
- **Description**: On selection, load the batch and recipe (`GET /api/batches/:batchId`, `GET /api/batches/:batchId/recipe`). Keep job → recipe → chemical associated for the whole session.
- **Acceptance Criteria**:
  - The rail lists every chemical in recipe sequence. For each: name, code, required quantity, unit, selected scale, status (pending / weighing / ✓ actual).
  - The rail shows the counter "n of N components weighed".
  - The loaded recipe is held locally, so the batch can still be weighed and completed if OrgaTEX later goes offline (FR-24).
  - If the recipe can't be loaded, an error with Retry is shown and no weighing starts.
- **Priority**: Must
- **Basis**: [C] PDF §4.2, §6 Steps 2–3. [D] PD-01. [A] brief FR-30.

### B. Component preparation

### FR-4: Component identification and selection *(topic 4)*
- **Description**: The operator selects the chemical to weigh. The selected chemical's identity is shown prominently.
- **Acceptance Criteria**:
  - Any chemical not yet done can be selected, in any order. A chemical that is done can't be selected again.
  - After a confirmation, the next pending chemical in recipe sequence is pre-selected.
  - The header shows the selected chemical's name, code, lot, container and tare.
  - Switching to another chemical mid-attempt abandons the current attempt, the same as Quit (FR-14).
- **Priority**: Must
- **Basis**: [C] PDF §6 Steps 4, 8. [D] PD-07. Mockup `select()` (l.718).

### FR-5: Dyestuff lot *(topic 5)*
- **Description**: Capture the lot of the dyestuff being consumed for each weighing.
- **Acceptance Criteria**:
  - The lot is scanned (FR-27) or typed. Confirm stays disabled while the lot is empty.
  - The lot is stored on the record and shown in the header.
  - Identity verification against the recipe is **not** performed in V1.
- **Priority**: Must
- **Basis**: [C] workbook STEP 25 (lot recorded). [D] PD-03 (lot capture only). [A] brief FR-10, A-09.

### FR-6: Container *(topic 6)*
- **Description**: Record the container that each chemical is dispensed into.
- **Acceptance Criteria**:
  - The container ID is entered per chemical. The OrgaTEX dummy may supply a suggested value (seed: C-09, C-11, C-12, D-03, D-05).
  - Confirm stays disabled while the container ID is empty. The container ID is stored on the record.
- **Priority**: Must
- **Basis**: [C] workbook STEP 25 (container recorded). [D] PD-03 (no container scan). [D] per-chemical entry with a suggested value (OQ-1, CP2).

### FR-7: Tare and re-tare *(topic 7)*
- **Description**: Tare the empty container on the selected scale (`POST /api/scales/:scaleId/tare`). The operator can re-tare at any time before confirming.
- **Acceptance Criteria**:
  - Tare zeroes the net reading and stores the tare weight the scale reports.
  - Re-tare (F2) zeroes the net reading again and replaces the stored tare. It creates no weighing record.
  - Confirm requires a tare taken on the current scale during this attempt.
  - The tare is stored on the record and shown in the header.
- **Priority**: Must
- **Basis**: [C] workbook STEP 16 (tare endpoint), STEP 25 (tare recorded), STEP 30 (re-tare → weight reset). [D] tare required before confirming (OQ-2, CP2).

### FR-8: Scale selection *(topic 8)*
- **Description**: The system picks the scale for each chemical from its required quantity. The operator doesn't choose.
- **Acceptance Criteria**:
  - Quantities are compared in kg after unit conversion: required ≤ threshold → Precision scale; required > threshold → Heavy-Duty scale.
  - The threshold (default **3 kg, inclusive**) and the scale assignments come from configuration.
  - Tests:
    - 2.999 kg → Precision; 3.000 kg → Precision; 3.001 kg → Heavy-Duty.
    - Yellow RGB 94.0 g → "3 kg precision"; Sodium Sulphate 24.000 kg → "32 kg floor".
  - The selected scale is displayed with an "automatic" marker. Live weight is read only from that scale.
- **Priority**: Must
- **Basis**: [C] PDF §5.1–5.3; workbook.

### C. Guided weighing

### FR-9: Live weight *(topic 9)*
- **Description**: Show the net weight from the selected scale, updating continuously.
- **Acceptance Criteria**:
  - Polls `GET /api/scales/:scaleId/weight` through the WeighingMS API. The interval is configurable, default 250 ms (ADR-004).
  - Shown in the chemical's unit: 1 decimal place for g, 3 for kg. Values respect the scale's resolution.
  - A progress bar shows the weight against the scale display range, with the tolerance band and the target marked.
  - Unstable readings are labelled as unstable.
- **Priority**: Must
- **Basis**: [C] PDF §3, §6 Step 5, §9. [A] A-05 (stability flag). Display from the mockup.

### FR-10: Target quantity *(topic 10)*
- **Description**: While weighing, always show the target, the tolerance (±t) and the acceptable range (min – max). The target is shown separately from the range.
- **Acceptance Criteria**: For Yellow RGB the screen shows "Target 94.0 g" and "TOLERANCE ±0.5 g (93.5 – 94.5)".
- **Priority**: Must
- **Basis**: [C] PDF §7, §8.

### FR-11: Remaining quantity *(topic 11)*
- **Description**: Show how much is still to be weighed.
- **Acceptance Criteria**:
  - Still to weigh = max(0, target − net).
  - While below range, the status line reads "DISPENSE — x REMAINING".
- **Priority**: Must
- **Basis**: [C] workbook topic 11. Mockup. [A] brief FR-14.

### FR-12: Tolerance *(topic 12)*
- **Description**: Classify each reading against the chemical's tolerance. Bounds are inclusive.
- **Acceptance Criteria**:
  - Classes:
    - **Underweight**: net < target − tol.
    - **Within range**: target − tol ≤ net ≤ target + tol, and net ≠ target.
    - **Target achieved**: net = target at the scale's resolution.
    - **Overweight**: net > target + tol.
  - PDF example with 1.000 ± 0.010 kg: 0.980 Underweight, 0.995 Within, 1.000 Target achieved, 1.008 Within, 1.020 Overweight.
  - Workbook example (STEP 22, Yellow RGB 94.0 ± 0.5 g): 93.5, 94.0 and 94.5 can be confirmed; 93.4 and 94.6 cannot.
  - **Tolerance source:**
    - the recipe's tolerance if provided;
    - otherwise the configured tolerance for chemical + scale;
    - otherwise weighing is blocked with "no tolerance defined".
  - There is **no universal default**.
  - The tolerance applied and its source are snapshotted on the record.
  - All quantity arithmetic uses fixed-point decimals.
  - Each class has a distinct visual state, with text as well as colour.
- **Priority**: Must
- **Basis**: [C] PDF §4.2, §6 Step 6, §7; workbook STEP 22. [D] TOL. [A] brief NFR-16.

### FR-13: Confirmation *(topic 13)*
- **Description**: The operator confirms the weighing (OK / F1). The system records it and marks the chemical done.
- **Acceptance Criteria**:
  - Confirm is enabled only when all of these hold:
    - the class is Within range or Target achieved;
    - the reading is stable;
    - lot, container and tare are present.
  - The server re-checks the same rule. An out-of-tolerance confirmation without an override (FR-15) is rejected.
  - Each confirmation creates exactly one record (FR-19). The rail then shows "✓ actual".
- **Priority**: Must
- **Basis**: [C] PDF §6 Step 7; workbook STEP 25. [D] stable reading required (OQ-3, CP2; A-05).

### FR-14: Quit and incomplete batch
- **Description**: The operator can abandon the current chemical (Quit, F4) and can leave a batch unfinished.
- **Acceptance Criteria**:
  - Quit discards the live attempt and creates no record. The chemical returns to pending.
  - Leaving the batch keeps every confirmed record. The batch stays "weighing" and resumes at the chemicals not yet done.
  - Quit and leave events are written to the audit log.
- **Priority**: Must
- **Basis**: [C] workbook STEP 30 (Quit → safe exit; incomplete batch → cannot complete). [A] brief FR-19, NFR-06. [P] auditing quits.

### FR-15: Authorised exception (supervisor override)
- **Description**: An underweight or overweight weighing can be confirmed only through a supervisor override.
- **Acceptance Criteria**:
  - The override requires a supervisor badge + PIN step-up (ADR-009) and a **mandatory reason**.
  - A wrong PIN or a missing reason creates no record and changes nothing.
  - The record keeps its true class (Underweight or Overweight). It is flagged with the supervisor, reason and time, and shown as an override in the summary and on the ticket ("OVR").
  - Without the override, the operator alone cannot confirm (workbook STEP 30: over and under tolerance → cannot confirm).
- **Priority**: Must
- **Basis**: [C] PDF §6 Step 7. [D] OVR. [A] brief BR-07, BR-13, FR-18, FR-35.

### FR-16: Overshoot *(topic 14)*
- **Description**: Above the upper tolerance bound, the station warns the operator and blocks normal confirmation.
- **Acceptance Criteria**:
  - The status shows "OVER TOLERANCE — SURPLUS +x (+y%)", with a distinct visual state.
  - Confirm is disabled. The available actions are re-tare and re-weigh (FR-7), quit (FR-14), or supervisor override (FR-15).
  - **Anything further (compensation, recalculation, discard rules) is decided at STEP 24 (PD-05).** The prototype's recalculation is not adopted.
- **Priority**: Must
- **Basis**: [C] PDF §6 Step 6; workbook STEP 24. [D] OVR. **PD-05 pending.**

### D. Progression, completion and outputs

### FR-17: Component progression *(topic 15)*
- **Description**: Progress through the recipe is always visible and is persisted.
- **Acceptance Criteria**:
  - Each chemical shows as done (with the actual weight), weighing, or pending. The counter shows "n of N".
  - When no pending chemicals remain, the batch moves to completion (FR-18).
  - Reloading or reopening the batch restores its progress exactly.
- **Priority**: Must
- **Basis**: [C] PDF §6 Step 8; workbook STEP 23. [D] PD-07. [A] brief NFR-06.

### FR-18: Batch completion and summary *(topic 16)*
- **Description**: A batch completes only when every chemical has a confirmed record. Completion produces a summary that can be reviewed later.
- **Acceptance Criteria**:
  - Completion while any chemical is pending is rejected, and the server enforces this.
  - The summary shows:
    - components weighed (n of N);
    - all within tolerance (YES, or NO with the override count);
    - total dispensed, elapsed time and operator;
    - the OrgaTEX state (FR-20) and the ticket state (FR-21);
    - for each chemical: required, actual and final status.
  - The summary of a completed batch can be reopened later, read-only.
  - Actions: Print ticket (F1), Next batch (F3).
- **Priority**: Must
- **Basis**: [C] PDF §6 Step 9, §10; workbook STEP 23. Mockup view 3.

### FR-19: Weighing record and operator traceability *(topic 19)*
- **Description**: Every confirmed weighing produces an immutable record linked job → recipe → chemical.
- **Acceptance Criteria**:
  - Each record holds:
    - batch, chemical ID and name;
    - required quantity and unit, actual quantity;
    - the tolerance applied and its source, and the result class;
    - scale, lot, container, tare;
    - operator and confirmation timestamp (UTC);
    - override details, if any.
  - Operators sign in at session start with their badge number, typed in (ADR-009). Every record carries the signed-in operator.
  - Records survive a restart of the API. They can't be edited or deleted through the app.
- **Priority**: Must
- **Basis**: [C] PDF §10; workbook STEP 25. [A] brief BR-11, NFR-05, NFR-13, A-09. [D] PD-03.

### FR-20: OrgaTEX write-back *(topic 17)*
- **Description**: On completion, post the actual consumption to the OrgaTEX dummy (`POST /api/batches/:batchId/consumption`) through a store-and-forward outbox.
- **Acceptance Criteria**:
  - Completion and the outbox entry are saved in one transaction. The POST never blocks completion.
  - Payload: the batch, plus the actual quantity, unit and lot for each chemical.
  - Every POST carries an idempotency key, so a retry never posts twice.
  - When OrgaTEX is reachable, the summary shows "Written to OrgaTEX hh:mm:ss" (STEP 26: 200 OK).
  - On failure:
    - automatic retry with back-off;
    - the status strip shows OrgaTEX offline and the unsynced count, and the summary shows "Pending sync";
    - after a configurable limit, the entry becomes "sync failed" for manual retry.
- **Priority**: Must
- **Basis**: [C] workbook STEP 26, STEP 30. [D] PD-01. [A] brief BR-16–18, FR-31–33, IR-08, A-02, A-03.

### FR-21: Ticket generation *(topic 18)*
- **Description**: On completion, generate a ticket and send it to the printer dummy (`POST /api/tickets`).
- **Acceptance Criteria**:
  - Fixed V1 layout on 80 mm paper, from top to bottom:
    - batch, order, machine, shade, date/time (FR-22), operator;
    - each component's target and actual, with unit; overridden lines are marked "OVR";
    - the line "ALL WITHIN TOLERANCE" or "OVERRIDES: n";
    - a Code 128 barcode of the batch ID, with the batch ID printed beneath it.
  - The summary shows an on-screen preview of the same ticket.
  - If the printer is unavailable, the batch still completes and the ticket is held as "print pending" (FR-24).
  - Every print after the first is marked REPRINT.
- **Priority**: Must
- **Basis**: [C] workbook STEP 27. [D] PD-02, PD-04. [A] brief FR-27, FR-28. Accepted risk: BR-15.

### FR-22: Time zone
- **Description**: Timestamps are stored in UTC, then displayed and printed in a configured IANA zone (default `Asia/Dhaka`).
- **Acceptance Criteria**:
  - The OS time zone of the browser or server has no effect.
  - Example: with the default zone, a record at 2026-11-03T21:48:22Z shows as 2026-11-04 03:48.
- **Priority**: Must
- **Basis**: [D] PD-06.

### E. Failure handling *(topic 20; workbook STEP 30)*

### FR-23: Scale unavailable or invalid weight
- **Description**: Behave safely when the selected scale is offline or returns an invalid reading.
- **Acceptance Criteria**:
  - The status strip shows each scale as READY or OFFLINE (`GET /api/scales/:scaleId/status`).
  - When the selected scale is offline:
    - the weight shows "—" and the message reads "Scale offline";
    - Confirm and Tare are disabled, and nothing is recorded;
    - when the scale is back, a new tare is required.
  - An invalid reading never enables Confirm. Invalid means missing, non-numeric, below zero beyond the scale's zero band, above capacity, or flagged as an error. Above capacity also shows "Over capacity".
- **Priority**: Must
- **Basis**: [C] workbook STEP 30 (scale unavailable → safe failure; invalid weight). [A] brief IR-13. [P] the exact list of invalid readings.

### FR-24: OrgaTEX, scanner or printer unavailable
- **Description**: Defined behaviour when each integration is down.
- **Acceptance Criteria**:
  - **OrgaTEX** while loading the queue or a recipe: show an error with Retry. A batch whose recipe is already loaded can still be weighed and completed, and its write-back follows FR-20.
  - **Scanner**: the status strip shows SCANNER OFFLINE. Touch selection and typed lot entry still work.
  - **Printer**: the batch still completes. The ticket is held as "print pending", the status strip shows PRINTER OFFLINE, and Print (F1) sends it later. There is no auto-print.
  - Integration errors appear as translated messages keyed by error code. Raw `detail` text is never shown.
- **Priority**: Must
- **Basis**: [C] workbook STEP 30. [D] PD-01, PD-02, PD-03.

### F. Dummy integrations: training-only behaviour

### FR-25: OrgaTEX dummy [T]
- **Description**: Serves the four workbook STEP 16 endpoints from seed data.
- **Acceptance Criteria**:
  - The seed contains:
    - B-24187 / SO-9931 / JET-04 / Teal 4C, with a 5-component recipe in this order: Blue RSPL, Red RB, Yellow RGB, Sodium Sulphate, Soda Ash;
    - B-24188, B-24190 and B-24191.
  - Tolerances follow the mockup values, including Yellow RGB 94.0 g ± 0.5 g. This is training data only.
  - Received consumption can be inspected (`GET /_sim/orgatex/consumption`).
  - Faults can be injected (`/_sim/faults`).
- **Priority**: Must
- **Basis**: [C] workbook STEP 16, 17. [T].

### FR-26: Scale dummy [T]
- **Description**: Two scales, Precision and Heavy-Duty, with capacities 3 kg and 32 kg, each with its own resolution. Each exposes status, weight and tare.
- **Acceptance Criteria**:
  - Scenarios are set with `POST /_sim/scales/{scaleId}/scenario`: rising towards the target, staying below range, entering range, overshooting, stable.
  - Readings respect each scale's resolution.
  - Each scale can be set offline, unstable or invalid.
- **Priority**: Must
- **Basis**: [C] PDF §9; workbook STEP 16. [T].

### FR-27: Scanner dummy [T]
- **Description**: A device-style HTTP scanner in the simulator.
- **Acceptance Criteria**:
  - `POST /_sim/scanner/scan {"code": "…"}` emits a scan. WeighingMS receives it through its own API and routes it by view:
    - on the queue view, the code is a batch ID;
    - on the weigh view, the code is a lot.
  - The scanner reports READY or OFFLINE and can be faulted.
  - There is no keyboard-wedge input.
- **Priority**: Must
- **Basis**: [C] workbook STEP 16 ("Dummy Scanner"). [D] PD-03. [T] contract defined here.

### FR-28: Printer dummy [T]
- **Description**: `POST /api/tickets` accepts a ticket and stores it as printed.
- **Acceptance Criteria**:
  - Printed tickets can be inspected (`GET /_sim/printer/tickets`).
  - When faulted, the POST fails with a defined error code, which drives FR-24.
- **Priority**: Must
- **Basis**: [C] workbook STEP 16, 27. [T].

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target | Basis |
|-------------|--------|--------|-------|
| Live weight freshness | Screen update interval while weighing | ≤ 250 ms (configurable) | ADR-004 |
| Recipe load | Selection → recipe displayed (dummy online) | < 3 s | [A] brief NFR-02 |
| Confirm round-trip | OK pressed → rail updated | < 1 s p95 locally | [P] |

### Reliability
| Requirement | Metric | Target | Basis |
|-------------|--------|--------|-------|
| Durability | A confirmed record survives an API restart | 100% | [A] brief NFR-05 |
| Outage tolerance | Weighing and completion while OrgaTEX is offline | Yes | [D] PD-01 |
| Resume | An interrupted batch resumes at the chemicals not yet done | Yes | [A] brief NFR-06 |

### Security
| Requirement | Standard | Notes | Basis |
|-------------|----------|-------|-------|
| Operator identification | Badge sign-in (typed), cookie auth | No badge scan in V1 | ADR-009, [D] PD-03 |
| Privileged actions | Supervisor badge + PIN step-up for each action | Override (FR-15) | ADR-009 |
| Audit | Immutable audit of overrides, quits, re-tares, reprints and sync failures | Not editable | [A] brief FR-36, NFR-13 |
| Credentials | No credentials in documents or committed config, other than dev seed data | — | [P] |

### Usability
| Requirement | Target | Basis |
|-------------|--------|-------|
| Function keys | F1 Confirm/Print, F2 Re-tare, F3 Next batch, F4 Quit | Mockup |
| Status not shown by colour alone | Every class has a text label | ux-guide |
| Externalised text | All operator text goes through Transloco | ADR-012, [A] NFR-11 |
| Touch targets | ≥ 15 mm | [A] brief NFR-07 |

### Configurability
| Item | Basis |
|------|-------|
| Scale threshold and scale assignments | [C] PDF §5.3 |
| Per-chemical + scale tolerances | [C] PDF §6; [D] TOL |
| Polling interval, plant time zone, outbox retry policy, integration base URLs | [D] PD-01, PD-06; ADR-004 |

### Testability
| Requirement | Basis |
|-------------|-------|
| Unit tests cover every PDF §7 and workbook STEP 22 boundary value | [C] |
| Every STEP 30 scenario, plus scanner offline, has an automated test | [C] |
| A Playwright test completes B-24187 end to end against the dummies (STEP 29) | [C] |

---

## Constraints

### Technical Constraints
**Project-wide standards:** these live in `memory-bank/standards/` and are not repeated here.

**Constraints specific to this intent:**
- The UI must not hard-code batches, recipes or weights.
- Mocks stay behind replaceable integration boundaries.
- Quantities use fixed-point decimals throughout.

### Business Constraints
- Work proceeds one checkpoint at a time, and one approved Bolt at a time.
- PD-05 must not be implemented beyond FR-16 until STEP 24.

---

## Assumptions

All of these come from `docs/brief.md` §12 and **FCI has not confirmed any of them**.

| Assumption | Used by | Risk if Invalid | Mitigation |
|------------|---------|-----------------|------------|
| A-01 OrgaTEX API for jobs and recipes | FR-1, FR-3 | Integration approach changes | Only dummies are built; an adapter boundary isolates them |
| A-02 OrgaTEX accepts consumption | FR-20 | No write-back possible | Write-back goes to the dummy only |
| A-03 Weighing continues through an outage | FR-20, FR-24 | The outbox isn't needed | Dropping it would be a simplification |
| A-04 A ticket printer is present | FR-21 | No physical ticket | Preview and summary still exist |
| A-05 Continuous readings with a stability flag | FR-9, FR-13 | Guided weighing has to be redesigned | Simulated scale behind a device boundary |
| A-09 Per-operator records and lot capture | FR-5, FR-19 | Some fields aren't needed | Fields can be dropped without structural change |
| A-11 One station, one site | Scope | Multiple stations needed | Architecture doesn't preclude it |

---

## Open Questions

| ID | Question | Owner | Due | Resolution |
|----|----------|-------|-----|------------|
| PD-01 | OrgaTEX unavailable | Product owner | STEP 7 | **Resolved**: outbox with bounded automatic retry (FR-20) |
| PD-02 | Printer unavailable | Product owner | STEP 7 | **Resolved**: print pending, then manual print (FR-21, FR-24) |
| PD-03 | Scanner mode and scope | Product owner | STEP 7 | **Resolved**: device API, batch + lot, manual fallback (FR-27) |
| PD-04 | Ticket | Product owner | STEP 7 | **Resolved**: fixed 80 mm layout, Code 128 batch ID (FR-21) |
| PD-06 | Time zone | Product owner | STEP 7 | **Resolved**: UTC stored, configured IANA zone displayed (FR-22) |
| PD-07 | Weighing order | Product owner | STEP 7 | **Resolved**: free selection with suggested sequence (FR-4) |
| TOL | Tolerance source | Product owner | STEP 7 | **Resolved**: recipe → chemical + scale config → blocked (FR-12) |
| OVR | Override scope | Product owner | STEP 7 | **Resolved**: any out-of-tolerance weighing, with PIN + reason (FR-15) |
| PD-05 | Overshoot handling beyond warning, blocking and override | Product owner | **STEP 24** | Pending |
| OQ-1 | Container: one per chemical, entered by hand with a suggested value? (FR-6) | Product owner | CHECKPOINT 2 | **Resolved**: yes, one per chemical, typed with a suggested value (FR-6) |
| OQ-2 | Must a tare be taken before every confirmation? (FR-7) | Product owner | CHECKPOINT 2 | **Resolved**: yes, re-tare allowed (FR-7) |
| OQ-3 | Must the reading be stable before confirming? (FR-13) | Product owner | CHECKPOINT 2 | **Resolved**: yes (FR-13) |
| OQ-4 | Mockup's "near target" (≥ 90%) amber state: a visual hint only, not a class? | Product owner | CHECKPOINT 2 | **Resolved**: visual hint only, not a class; priority Could |
| A-xx | FCI confirmation of A-01…A-15 | FCI | Outside the lab | Carried as unconfirmed |
