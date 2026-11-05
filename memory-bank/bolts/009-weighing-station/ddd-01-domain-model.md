---
unit: 005-weighing-station
bolt: 009-weighing-station
stage: model
status: complete
updated: 2026-11-05T18:45:00Z
---

# Static Model - Weighing Station (bolt 009: out of tolerance, override, completion, review)

## Bounded Context

Same context and aggregate as bolts 007–008 (**guided weighing**). This bolt finishes unit 005:
- **out-of-tolerance handling**: overshoot warns and blocks, showing the surplus (story 010);
- the **supervisor override**, the authorised exception (story 011, decision OVR);
- **batch completion** when all N components are confirmed, raising `BatchCompleted` (story 013);
- the **completed-batch summary** and the **review list** (story 014).

Inputs from other contexts:
- **Station access** (unit 002): the supervisor **step-up grant** (badge + PIN; single use; action `override`; short-lived, ADR-009) and the audit trail.
- **Write-back and ticket** (units 006, 007): consume `BatchCompleted` in bolts 011 and 012. Until then the summary shows their state as "not yet available".

**PD-05 (overshoot / surplus handling, workbook STEP 24) isn't decided in this model.** The model already includes what is known (warn, block, show the surplus, and offer re-tare / quit / override; no recalculation). The **ADR stage** presents 2–3 safe training options and stops for your decision. The marked "PD-05 slot" below is where the chosen rule plugs in.

## Domain Entities

| Entity | Properties | Business Rules |
|--------|------------|----------------|
| **WeighingRecord** (bolt 008, extended) | + `override: OverrideDetails?` | An override record keeps its **true class** (Underweight or Overweight) and is flagged with the override (OVR). A normal record has no override. Both are immutable. |
| **WeighingSession** (extended) | + `status: InProgress \| Completed` · + `completedAt?` · + `completedBy?` | A completed session is **read-only**: no select, capture, tare, confirm, override, quit or leave (409 `Weighing.BatchCompleted`). It can only be read and reviewed. |

## Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| **Surplus** | `amount: Quantity`, `percentOfTarget: decimal` | Only for **Overweight**: `amount = net − target`, `percent = amount ÷ target × 100`, rounded to 1 dp (A16). Example: Yellow RGB 96.4 g → **+2.4 g (+2.6%)**, as in the prototype's status line "OVER TOLERANCE — SURPLUS +2.4 g (+2.6%)". Shown, never used to change another component (story 010; the prototype's recalculation is not adopted). |
| **OverrideDetails** | `supervisorBadgeId`, `supervisorName`, `reason`, `grantId`, `overriddenAt` | The reason is trimmed and required (1–500 characters). It comes from a valid, unused, unexpired `override` step-up grant (ADR-009). |
| **BatchSummary** | `done`, `total`, `allWithinTolerance`, `overrideCount`, `totalDispensedKg`, `elapsed`, `operators[]`, `orgatexState`, `ticketState`, `lines[]` (sequence, chemical, required, actual, final status) | Computed from the session and its records (FR-18). Final status per line: `withinRange`, `targetAchieved`, `underweight (OVR)` or `overweight (OVR)`. |

## Aggregates

| Aggregate Root | Members | Invariants |
|----------------|---------|------------|
| **WeighingSession** (extended) | components → attempt / record (+ override) | Bolts 007–008's invariants 1–17, plus: **18. Overshoot** (net above the band) is never confirmable normally; the reading reports the surplus, and the actions are re-tare and re-weigh, quit, or override. *PD-05 slot: the rule chosen at STEP 24 is applied here.* **19. Override** is possible only for an **Underweight or Overweight** reading. For a reading in range it isn't offered: normal confirm applies (story 011, edge case). **20.** An override needs a **valid supervisor grant** (action `override`, unused, not expired, supervisor ≠ the signed-in operator, decision D2) and a **non-empty reason**. Otherwise nothing changes and the grant isn't used. **21.** An override still requires everything **except** the tolerance: lot, container, a valid tare, and a valid and stable reading (A11). **22.** The override record keeps the **true class**, the override details and the reading's surplus. The grant is consumed in the **same transaction** (single use). **23. Completion** only when **all N** components are done; otherwise 409 `Batch.Incomplete`. Completion raises **`BatchCompleted` in the same transaction** as the completed status (decision D1 decides when completion runs). **24.** After completion the session never changes again. |

## Domain Events

| Event | Trigger | Payload |
|-------|---------|---------|
| **WeighingOverridden** | A successful override | the record (true class, override details) |
| **BatchCompleted** | The session completes | batchId, order, machine, shade, completedAt, completedBy, the N records (for write-back, unit 006, and the ticket, unit 007) |
| (bolts 007–008) Started … BatchLeft | — | — |

`BatchCompleted` is published **in-process inside the completing transaction**. Bolt 011 adds the outbox entry (PD-01, store-and-forward) and bolt 012 the print-pending ticket, as handlers that join that transaction. Audit: the existing `Override` type, plus a new `BatchCompleted`.

## Domain Services

| Service | Operations | Dependencies |
|---------|------------|--------------|
| **OverrideRules** | `Check(component, reading, grant, reason) → gaps` | `ConfirmationRules` (bolt 008) without the tolerance gap, plus the class must be Under or Over |
| **SurplusCalculator** | `Of(classification) → Surplus?` | Pure |
| **BatchSummaryBuilder** | `Build(session) → BatchSummary` | Pure; the OrgaTEX and ticket states come from units 006 and 007 later |
| (bolts 007–008) ScaleSelector, ToleranceResolver, ReadingClassifier, ConfirmationRules, NextComponentRule | — | — |

## Repository Interfaces

| Repository | Entity | Methods |
|------------|--------|---------|
| **IWeighingSessionRepository** (extended) | WeighingSession | + `ListCompletedAsync(page)` (newest first) for the review list. The completion transaction also saves what the `BatchCompleted` handlers add. |
| **IStepUpGrantStore** (unit 002, exists) | StepUpGrant | Find and consume the supervisor grant in the override transaction |

## Ubiquitous Language

| Term | Definition |
|------|------------|
| Overshoot | A reading above the tolerance band (Overweight). |
| Surplus | How much an overshoot exceeds the target: amount and % of target. |
| Supervisor override (OVR) | An authorised exception recording an Underweight or Overweight weighing with its true class, the supervisor, the reason and the time. |
| Step-up grant | The supervisor's single-use, short-lived permission (badge + PIN) for one override. |
| Completion | The batch is finished: every component has a record. Raises `BatchCompleted`. |
| Summary | The completed batch's account: n of N, within tolerance (YES / NO + overrides), total dispensed, elapsed, operators, OrgaTEX and ticket state, per-chemical lines. |
| Review list | The completed batches, which can be reopened read-only. |

## Story Coverage

- ✅ **010 overshoot**: invariant 18, `Surplus`, the reading's surplus and offered actions, no recalculation. *The STEP 24 rule (PD-05) is in the ADR stage.*
- ✅ **011 override**: invariants 19–22, `OverrideDetails`, `OverrideRules`, `WeighingOverridden`, audit `Override`.
- ✅ **013 completion**: invariants 23–24, `BatchCompleted`, `BatchSummary`.
- ✅ **014 records and review**: `WeighingRecord` + override details (survives restarts, immutable since bolt 008), `ListCompletedAsync`, a read-only summary.

## Decisions at This Checkpoint: resolved (APPROVE MODEL, 2026-11-06)

- **D1: option 1.** The batch completes automatically with the last confirm or override, in the same transaction; an explicit completion call stays for the server rule (AUTO-COMPLETE).
- **D2: option 1.** Four eyes: the authorising supervisor must differ from the signed-in operator (OVR-FOUR-EYES).
- **A11–A18: accepted** as recommended.

#### Options as presented

### D1 · When does a batch complete?

**Example:** the operator confirms Soda Ash, the 5th and last component of B-24187.

1 - **(Recommended)** **Automatically, with the last confirm or override**, in the same transaction: the session becomes Completed and `BatchCompleted` is raised. The screen goes straight to view 3 "Batch B-24187 weighed" (the mockup flow). The batch can't be left forgotten in "5 of 5, not completed". An explicit `complete` call still exists for the server rule "completion while any chemical is pending is rejected" (story 013), and is idempotent once complete.
2 - An **explicit operator action** ("Complete batch") after the last confirm. One more step for the operator, and a batch can sit at 5 of 5 without being completed (no write-back, no ticket).

### D2 · May the supervisor who authorises an override be the signed-in operator?

**Example:** supervisor M. Rahman (SV-2001) is signed in and weighing B-24190 themselves. Black B reads Overweight. They want to override it with their own badge and PIN.

1 - **(Recommended)** **No: four eyes.** The supervisor badge must differ from the signed-in operator (409 `Auth.SupervisorRequired` style refusal: "another supervisor must authorise"). An override is an exception to a quality rule, so one person shouldn't be able to approve their own.
2 - Yes: any supervisor badge + PIN, including the signed-in one. Faster on a night shift with one supervisor, but it removes the independent check.

## Assumptions (unconfirmed by FCI; each needs accept or change)

### A11 · An override bypasses only the tolerance

**Example:** Red RB reads 389.1 g (Overweight, band 384.0 – 388.0 g), but no lot has been entered yet.

- **Recommended:** the override is refused until lot, container, a valid tare and a **stable, valid** reading are present. Only "in tolerance" is waived. Otherwise an override could record an unstable or untared number.

### A12 · The summary lists every operator who confirmed

**Example:** R. Karim confirmed components 1–3 and S. Akter (OP-1002) confirmed 4–5 after a shift change.

- **Recommended:** "Operator: R. Karim, S. Akter" (in order of first confirmation); each record keeps its own operator.

### A13 · "Total dispensed" is the sum of the actual weights, in kg

**Example:** B-24187: 1241.5 g + 385.4 g + 94.2 g + 23.980 kg + 9.621 kg = **35.322 kg** (the mockup's view 3 value).

- **Recommended:** sum every record's actual (overrides included), converted exactly to kg and shown to 3 decimals. Tares are excluded.

### A14 · "Elapsed" is from session start to completion

**Example:** started 07:41:41, completed 07:48:22 → **6 min 41 s**, including any pause or shift change in between.

- **Recommended:** completedAt − startedAt (the mockup's "Elapsed 6 min 41 s"). Pauses are not subtracted.

### A15 · OrgaTEX and ticket states read "not yet available" until bolts 011 / 012

**Example:** completing B-24187 in this bolt shows "Written to OrgaTEX: —" and "Ticket: —".

- **Recommended:** the summary carries both fields now, with a neutral state, so the screen and contract don't change when bolts 011 and 012 fill them in (pending / confirmed / sync failed; print pending / printed).

### A16 · Surplus is measured against the target, as a % of the target

**Example:** Yellow RGB target 94.0 g, reading 96.4 g → surplus **+2.4 g (+2.6%)**. It is not +1.9 g measured from the upper bound 94.5 g.

- **Recommended:** relative to the target, rounded to 1 dp, as in the prototype's status line and FR-16. Only shown for Overweight. (Underweight already shows "remaining".)

### A17 · A completed session is frozen

**Example:** after completion someone calls select or confirm on B-24187 again.

- **Recommended:** 409 `Weighing.BatchCompleted`, and nothing changes. Records were already immutable (A9); now the session is too.

### A18 · The review list shows completed batches newest first, 20 per page

**Example:** the review list shows B-24187 (completed 07:48), then older ones. Touching a batch opens its summary read-only.

- **Recommended:** all completed sessions at the station, newest first, paged by 20. No edit actions.
