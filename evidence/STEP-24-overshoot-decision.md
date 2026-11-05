> Workbook STEP 24 evidence — overshoot decision (PD-05). Copy of `memory-bank/bolts/009-weighing-station/adr-001-overshoot.md`, accepted 2026-11-06. Flow: observed problem → business rule → implementation → test (sections 1, 5, 6).

# ADR-001 (bolt 009): Overshoot / surplus handling, training rule (PD-05, workbook STEP 24)

**Status: accepted** (human decision "APPROVE ADR option 1", 2026-11-06 00:42 +06). **Option 1 is the training rule.**

## 1. Observed problem

While pouring, the operator can go past the upper tolerance bound. Example: Remazol Yellow RGB, target **94.0 g ± 0.5 g** (band 93.5 – 94.5 g), and the scale settles at **96.4 g**. That is Overweight, with a surplus of **+2.4 g (+2.6%)** against the target. The chemical can't be "un-dosed" by software, so the station needs a defined, safe rule for what happens next.

## 2. What is known (sources that bind us)

| Source | What it says | Strength |
|---|---|---|
| PDF §6 Step 6 | Overweight: "the application **warns** the operator that the quantity is excessive." | Mandatory (ADR-015: PDF defines scope) |
| PDF §6 Step 7 | A result is recorded when the quantity is reached **within the acceptable tolerance**. | Mandatory |
| PDF §7 | 1.020 kg against 1.000 ± 0.010 kg is **Overweight** (a distinct class) | Mandatory |
| Workbook STEP 24 | "The prototype **deliberately represents overshoot as a business question**. **Do not simply copy its proposed calculation.**" Present 2–3 safe options; **stop for a human decision** | Mandatory |
| Workbook STEP 30 | Over tolerance → **cannot confirm** (a tested failure scenario) | Mandatory |
| Approved FR-16 (CP2) | Warn with "OVER TOLERANCE — SURPLUS +x (+y%)"; Confirm disabled; actions **re-tare and re-weigh, quit, or supervisor override**; no recalculation adopted | Approved requirement |
| Approved FR-15 / decision OVR | Any out-of-tolerance weighing (Under **or Over**) can be recorded **only** through a supervisor override: badge + PIN, a mandatory reason, true class kept, flagged "OVR" | Approved requirement |
| Built and tested (bolts 007–009 design) | Overweight is never confirmable normally (`Weighing.OutOfTolerance`); the surplus is computed against the target (A16); records are immutable; overrides are four-eyes (OVR-FOUR-EYES) | Implemented |

## 3. What is assumed (not confirmed by FCI)

| Item | Where it comes from | Status |
|---|---|---|
| "Surplus compensation" means **proportionally recalculating the remaining components** to keep the shade ratio | Brief A-08, FR-20–24; the prototype's surplus panel ("Proposal: recalculate the remaining components…", Sodium Sulphate 24.000 → 24.624 kg) | **Assumption only.** The prototype itself marks it as a question (pin §9.4 Q25: "what does FCI actually mean by surplus compensation?", Q26: "what happens when an overdose cannot be compensated?"). The brief rates A-08 as a **severe** assumption. |
| Which over-doses are "compensable" | Brief FR-21 | Unknown: no rule exists |
| Whether surplus material can be removed from the container (pour back, spatula) | Practice at FCI | Unknown |
| A maximum surplus beyond which a batch must be discarded | — | Unknown: no source gives a number |

**Therefore:** recalculating other components is **not** a safe training option. It would change approved recipe targets on an unconfirmed rule, and the workbook forbids copying it. It's listed under "rejected" below, not as an option.

## 4. Options (safe training implementations)

All three options keep the warning, the surplus display, the blocked Confirm and the recipe targets unchanged. They differ in **how an Overweight can end**.

### Option 1 · Block and recover, with supervisor override as the only exception (**recommended**)

The rule as written in the approved FR-15 and FR-16, with nothing added.

- **Behaviour.** Overweight stays unconfirmed. The operator can do one of three things:
  - (a) **bring it back into range**: remove material, the reading falls into the band, then confirm normally;
  - (b) **quit and re-weigh**: discard the container, re-tare, pour again;
  - (c) ask a **supervisor override**: a second person with badge and PIN, plus a reason. The record keeps **Overweight**, the surplus and "OVR".
- **Example.** Yellow RGB at 96.4 g:
  - (a) the operator spoons out about 2 g, the reading becomes 94.3 g Within range, and they confirm;
  - or (c) supervisor SV-2001 overrides with the reason "Shade lab accepts +2.4 g for this lot", which records 96.4 g Overweight, OVR, +2.4 g (+2.6%).
- **Impact.**
  - Shade risk: controlled by a person, and visible on the summary ("All within tolerance: NO, 1 override") and the ticket ("OVR").
  - Waste: only when the operator chooses (b).
  - Traceability: full.
  - Requirements: matches the approved FR-15 and FR-16 exactly, with no requirement change.
  - Effort: nothing beyond the design already approved.

### Option 2 · Option 1, plus a configurable override ceiling

- **Behaviour.** As option 1, but an Overweight whose surplus is above a **configured limit** (per chemical + scale class, for example `MaxOverrideSurplus = 2 × tolerance`) **can't be overridden** and must be quit and re-weighed. Below the limit, override is allowed.
- **Example.** Yellow RGB ±0.5 g with a limit of 1.0 g:
  - 94.9 g (surplus +0.9 g) → override possible;
  - 96.4 g (+2.4 g) → refused (`Weighing.SurplusTooLarge`), so the operator must re-weigh.
- **Impact.**
  - Shade risk: lower, because gross over-doses can't be accepted.
  - Waste: higher. Large over-doses are always discarded.
  - Requirements: adds a rule that no source gives a number for, so the limit values would be **training placeholders** that are explicitly unconfirmed.
  - Effort: one more configuration section, validation on start, and tests.
  - Extends FR-16 (additive); FR-15 is narrowed for large surpluses.

### Option 3 · Overweight is never overridable: always recover or re-weigh

- **Behaviour.** Override is kept **only for Underweight**. An Overweight must be brought back into range or quit and re-weighed. A supervisor can't accept an over-dose.
- **Example.** Yellow RGB at 96.4 g → override isn't offered. The operator removes material or starts again. Red RB at 383.9 g (Underweight, a supplier short-fill) can still be overridden.
- **Impact.**
  - Shade risk: lowest.
  - Waste and operator effort: highest.
  - Requirements: **contradicts the approved FR-15 and OVR**, which allow overriding Under *or* Over. Choosing it means changing those requirements (a recorded deviation).
  - Effort: small (a class check in `OverrideRules`), plus requirement and test changes.

### Rejected · Automatic or proposed recalculation of the remaining components

The prototype's surplus panel and brief A-08 / FR-22. This is rejected for the training build:
- the workbook forbids copying it;
- the meaning of "compensation" is an open FCI question (Q25 / Q26, A-08 severe);
- it would change approved recipe targets of components not yet weighed.

It can be revisited when FCI defines the rule.

## 5. Decision

**Selected: Option 1 · Block and recover, with supervisor override as the only exception.** Decided by the product owner (lab user) on 2026-11-06 at 00:42 +06, after options 1–3 were presented.

**Recorded training rule:**

> For this training implementation, an overweight component remains unconfirmed. The operator must bring it back into range, quit and re-weigh it, or obtain a supervisor override (badge, PIN and reason), which records its true class (Overweight), the surplus and OVR. No recalculation of other components is performed.

Why option 1:
- it is exactly the approved FR-15 and FR-16, with no requirement change;
- every exception is a deliberate, four-eyes, recorded act;
- no invented limits.

Options 2 and 3 are kept below for the record, together with the rejected recalculation.

Training rule text, as presented for each option:
- **Option 1:** "For this training implementation, an overweight component remains unconfirmed. The operator must bring it back into range, quit and re-weigh it, or obtain a supervisor override (badge, PIN and reason), which records its true class (Overweight), the surplus and OVR. No recalculation of other components is performed."
- **Option 2:** as option 1, plus: "An overweight whose surplus exceeds the configured limit for that chemical and scale cannot be overridden and must be re-weighed. The limits are training values, not FCI data."
- **Option 3:** "For this training implementation, an overweight component can never be accepted. The operator must bring it back into range or quit and re-weigh it. Supervisor override applies to underweight results only. This deviates from FR-15 / OVR."

## 6. Consequences

- **Implementation** (bolt 009 Implement):
  - Overweight stays `Weighing.OutOfTolerance` on confirm.
  - The reading reports the surplus (+x, +y% of target) and the offered actions: re-tare / re-weigh, quit, override.
  - `OverrideRules` accepts Overweight and Underweight equally; there is **no surplus limit and no configuration**.
  - Bringing the reading back into range simply makes the normal confirm possible again (no special state).
  - No code anywhere changes another component's target.
- **Tests:**
  - an Overweight blocks confirm and reports the surplus;
  - removing material back into the band → normal confirm;
  - quit and re-weigh → a fresh attempt;
  - an override of an Overweight records Overweight + surplus + OVR;
  - an override of a large surplus (e.g. +50%) is still allowed (no limit, by decision);
  - **the other components' targets are unchanged after an overshoot**.
  - The test run is the evidence for `STEP-24-overshoot-test.png`.
- **UI** (bolt 010): the "OVER TOLERANCE — SURPLUS +2.4 g (+2.6%)" state with the three actions (`STEP-24-overshoot-ui.png`).
- **Records:** `evidence/STEP-24-overshoot-decision.md`; PD-05 is resolved in `decision-index.md`; FR-16 is updated.
