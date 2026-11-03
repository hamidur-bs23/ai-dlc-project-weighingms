---
intent: 001-assisted-weighing-station
phase: inception
status: draft
created: 2026-11-03T21:41:00Z
updated: 2026-11-03T21:41:00Z
---

# Requirements: Assisted Dyestuff Weighing Station (WeighingMS)

> **Stage: Intent (STEP 6).** This file records the intent: problem, users, outcome, scope, sources and unknowns.
> Functional and non-functional requirements are elaborated in **STEP 7** and are deliberately left empty here.
> Nothing sourced from `docs/brief.md` has been confirmed by FCI. Such items cite their brief ID / assumption ID (A-xx) and are marked *unconfirmed*.

## Intent Overview

**WeighingMS** is an assisted weighing station for the dyeing department of FCI, an RMG / textile dyeing facility.
It guides an operator through hand-weighing every chemical (dyestuffs and auxiliaries) in a dyeing job's recipe,
and it records what was actually weighed.

**Business problem.** Recipe weighing is assumed today to be manual and unrecorded: the recipe is on paper, the
operator weighs by eye, and the actual quantity dispensed is never captured (brief §2, §6.1, *A-13 unconfirmed*).
When a batch fails shade approval, nobody can tell whether the recipe or the weighing was wrong, so the error recurs.
The project requirement states the objective as "improve weighing accuracy, reduce manual calculation errors,
and provide a clear, controlled, and traceable chemical-weighing process" (PDF §1).

**What WeighingMS is:**
- A **precision-weighing management system**: it guides and monitors. Operators still dispense by hand (PDF §1).
- A station that sits **between the operator and the OrgaTEX MES**. OrgaTEX remains the system of record for
  jobs and recipes and the receiver of actual consumption. **WeighingMS does not replace or modify OrgaTEX**
  (brief §5.2).

**What WeighingMS is not:** an automated dispensing system, a recipe-authoring tool, or a lab or shade-approval tool.

### Users

| Role | Relationship | Source |
|------|--------------|--------|
| **Weighing operator** | **Primary user.** Selects a job, weighs each chemical, confirms, completes | PDF §1, §6; brief §4 |
| Dyehouse supervisor | Grants the authorised exception (override) and watches progress | PDF §6 Step 7; brief §4 (*unconfirmed detail*) |
| Lab / QA | Reviews weighed-vs-recipe records when a shade fails | PDF §10; brief §4 |
| Store / inventory | Uses actual consumption | brief §4, BO-04 (*unconfirmed*) |
| FCI IT | Network, deployment, support | brief §4 |

### Desired Outcome

A **complete, working weighing workflow end to end**, running on mock integrations (PDF §12):

> select a dyeing job → retrieve its recipe → for each chemical: the system selects the scale automatically → live
> measured weight → classify as Underweight / Within range / Target achieved / Overweight → confirm → complete the
> job → completed-job summary (and, per the workbook, consumption posted to OrgaTEX and a ticket printed).

**Top acceptance criterion:** the whole workflow works end to end. Resembling the mockup's screens is not enough (PDF §12).

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| G1: Core weighing workflow (PDF §11 Priority 1) | An operator completes seed batch `B-24187` (5 components) end to end through the UI against the dummy APIs, with no hard-coded scenario data in the UI | Must |
| G2: Operational visibility (PDF §11 Priority 2) | Progress (done / current / outstanding) and the Under / Within / Over state are visible at all times. A summary of each completed job can be reviewed afterwards | Must |
| G3: Integration readiness (PDF §11 Priority 3) | OrgaTEX, scale, scanner and printer mocks sit behind HTTP integration boundaries, so they can be replaced without changing business logic | Must |
| G4: Weighing accuracy (brief BO-01, *unconfirmed*) | No weighing outside tolerance is recorded as successful unless an explicit, authorised exception was granted (PDF §6 Step 7) | Must |
| G5: Component-level traceability (brief BO-02, *unconfirmed*) | 100% of confirmed weighings record job, chemical, required qty, actual qty, scale, result and timestamp (PDF §10), plus operator, lot, container and tare (workbook) | Must |
| G6: Defined behaviour under failure (workbook STEP 30) | Scale, OrgaTEX or printer unavailable, invalid / under / over weight, re-tare, quit and incomplete batch all have defined, tested behaviour | Must |

---

## Source Artifacts and Precedence

| Source | Role | Precedence (ADR-015) |
|--------|------|----------------------|
| `docs/project-requirement.pdf` (verbatim copy: `docs/project-requirement.md`) | Project requirement: business intent, workflow, rules | **Mandatory scope** |
| `workbook.md` | Lab process, mandatory dummy endpoints, seed scenario, failure scenarios | **Mandatory scope** |
| `docs/customize-weighing-station.html` | **UI/UX source.** Defines the functional and visual experience across three views: 1 · Select batch, 2 · Weigh, 3 · Confirm & print. The app must reproduce this experience. Its JS is read for states and rules but **not copied** | Visual + interaction reference |
| `docs/brief.md` (draft BRD v1.1) | Fills gaps where the PDF and the workbook are silent | Gap-filler only; cite FR/BR/A-xx IDs; *unconfirmed by FCI* |

## Scope

### In scope

- Retrieve dyeing jobs (batch queue) and select one. Batch selection by scanner is also in scope (workbook §0; scope of scanning is open, see PD-03).
- Retrieve and display the job's recipe: chemicals, required qty, unit, selected scale, weighing status.
- **Automatic scale selection:** required qty ≤ 3 kg → Precision Scale; > 3 kg → Heavy-Duty Scale. The operator does not choose. The threshold and the scale assignments are configurable (PDF §5.3).
- Live weight display from a simulated scale, plus tare / re-tare.
- **Tolerance classification** against a configurable tolerance per chemical and scale (no universal tolerance). "Target achieved" is distinct from "Within range". Bounds are inclusive (PDF §7; workbook: 94.0 g ± 0.5 g → 93.5 and 94.5 accepted, 93.4 and 94.6 rejected).
- Confirm a weighing only when it is within tolerance. Otherwise confirmation requires an explicit authorised exception (supervisor override).
- Progress through all chemicals, then job completion.
- Weighing records that preserve job → recipe → chemical → record, and a reviewable completed-job summary.
- On completion: POST consumption to the OrgaTEX dummy and generate a ticket through the printer dummy (workbook STEP 26–27).
- Defined, tested failure handling (see G6).

### Out of scope (this intent)

- Any control of a dispensing machine or automated dyestuff kitchen; automated container transport (brief §5.2).
- Recipe authoring or modification, since recipes are owned by OrgaTEX (brief §5.2).
- Replacing or modifying any OrgaTEX functionality (brief §5.2).
- Laboratory colour matching or shade approval; machine-side dyeing control (brief §5.2).
- Integration with the **real** OrgaTEX, scales, scanner or printer. Only dummies are built.
- Liquid chemical dosing (brief §5.2, *A-12 unconfirmed*).

### Dummy Integrations (first-class product code)

The real systems are unavailable. The dummies run in a separate simulator process and are reached **over HTTP
only**. The UI calls only the WeighingMS API and holds no scenario data. These are **training contracts, not claims
about the real OrgaTEX interface**. No real OrgaTEX API contract will be invented.

| Dummy | Minimum endpoints (workbook) | Notes |
|-------|------------------------------|-------|
| **OrgaTEX** | `GET /api/batches`, `GET /api/batches/:batchId`, `GET /api/batches/:batchId/recipe`, `POST /api/batches/:batchId/consumption` | Seed: `B-24187` / `SO-9931` / `JET-04` / `Teal 4C`; queue also has B-24188, B-24190, B-24191 |
| **Weighing scales** | `GET /api/scales`, `GET /api/scales/:scaleId/status`, `GET /api/scales/:scaleId/weight`, `POST /api/scales/:scaleId/tare` | Precision (prototype "3 kg precision") and Heavy-Duty (prototype "32 kg floor"). Scenarios: rising to target, staying under, entering range, overshooting, precision suited to the scale (PDF §9) |
| **Scanner** | **Contract undefined.** See PD-03 | Required by workbook §0 |
| **Printer** | `POST /api/tickets` | Ticket content: batch, order, machine, shade, date, operator, components, target and actual, barcode. Layout is open (PD-04) |

---

## Functional Requirements

*To be elaborated in STEP 7 (Requirements). Not part of the intent.*

## Non-Functional Requirements

*To be elaborated in STEP 7. Project-wide technical standards are already recorded in `memory-bank/standards/`.*

---

## Constraints

### Technical Constraints

**Project-wide standards:** loaded from `memory-bank/standards/` (tech-stack, data-stack, coding-standards,
system-architecture, api-conventions, ux-guide; decisions in `decision-index.md`).

**Intent-specific constraints:**
- The UI must not hard-code batches, recipes or weights. All scenario data comes through the dummy APIs.
- Mocks must stay separate from business logic, behind replaceable integration boundaries (PDF §9, §11 P3).
- The scale threshold, scale assignments and tolerances must be configuration, not code (PDF §5.3, §6 Step 6).

### Business Constraints
- This is a hands-on AI-DLC lab: the work follows `workbook.md` step by step and stops at every human checkpoint.
  Bolts are built one at a time, and only after approval.
- Business decisions that are open (PD-01…PD-07) must not be implemented or stated as decided until they are resolved in `decision-index.md`.

---

## Assumptions

All of these come from `docs/brief.md` §12 and are **unconfirmed by FCI**. The intent relies on them only as far as stated.

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| A-01: OrgaTEX exposes jobs and recipes through an API (*severe*) | The integration approach changes | Only dummies are built. The adapter boundary absorbs the real protocol later |
| A-02: OrgaTEX accepts consumption postings (*severe*) | Write-back is not possible | Write-back targets the dummy only; behaviour on outage is in PD-01 |
| A-05: Scales stream weight continuously with a stability flag (*severe*) | Live guided weighing needs a redesign | The scale is simulated and polled (ADR-004) behind a device boundary |
| A-08: Overshoot means compensation or escalation (*high*) | Wrong overshoot behaviour | Not adopted. Decided at STEP 24 (PD-05) |
| A-13: Today's process is paper-based and unrecorded (*high*) | The business case changes | Affects justification only, not the lab build |
| A-10: Dyestuffs carry barcodes; UI is in English | Scanner scope changes | Scanner scope is open (PD-03); text is externalised (ADR-012) |
| A-11: One station, one site in V1 | Multi-station needs | Architecture does not preclude several stations |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| PD-01: Behaviour when OrgaTEX is unavailable (consumption write-back, sync, locally held data) | Product owner (user) | STEP 7 | Pending |
| PD-02: Behaviour when the printer is unavailable at completion | Product owner (user) | STEP 7 | Pending |
| PD-03: Scanner mode (keyboard wedge / device API) and scope (badge, batch, dyestuff lot, container); dummy scanner contract | Product owner (user) | STEP 7 | Pending |
| PD-04: Ticket paper size, layout, configurability, barcode content, override marking | Product owner (user) | STEP 7 | Pending |
| PD-06: Time zone for displayed and printed timestamps | Product owner (user) | STEP 7 | Pending |
| PD-07: Chemical weighing order (free selection vs strict recipe sequence vs sequence with supervisor skip) | Product owner (user) | STEP 7 | Pending |
| PD-05: Overshoot / surplus handling (2–3 options; prototype calculation not adopted) | Product owner (user) | STEP 24 | Pending |
| Tolerance source precedence: recipe-provided tolerance (PDF §4.2 "if provided") vs per chemical + scale configuration (PDF §6–7) | Product owner (user) | STEP 7 | Pending |
| What exactly the "authorised exception" is: who may grant it, and what reason is recorded (PDF §6 Step 7; mockup supervisor override; ADR-009 PIN step-up) | Product owner (user) | STEP 7 | Pending |
| Which brief assumptions A-01…A-15 FCI confirms (A-01, A-02, A-05, A-08, A-13 are severe or high) | FCI via product owner | Outside the lab | Pending; carried as unconfirmed |
