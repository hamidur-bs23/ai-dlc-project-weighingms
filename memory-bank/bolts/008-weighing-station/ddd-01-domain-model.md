---
unit: 005-weighing-station
bolt: 008-weighing-station
stage: model
status: complete
updated: 2026-11-05T18:15:00Z
---

# Static Model - Weighing Station (bolt 008: the weighing attempt and the record)

## Bounded Context

Same context as bolt 007 (**guided weighing**), same aggregate. Bolt 007 built the session, component selection, scale selection, the tolerance source and reading classification. This bolt adds what happens **while a component is being weighed and when it is confirmed**:
- the **attempt**: lot, container and tare captured for the active component;
- **re-tare**;
- **confirmation**, re-checked on the server, writing exactly **one immutable weighing record**;
- **progression** to the next pending component;
- **quit** (discard the attempt) and **leave** (exit the batch, keep everything confirmed).

Inputs from other contexts are unchanged: the scale port (tare, reading with `stable`, `validity` and `onlineSince` from unit 004), the signed-in operator and plant clock (unit 002), the audit trail.

Later (bolt 009): overshoot handling (PD-05), supervisor override (OVR), batch completion (`BatchCompleted`), the review list.

## Domain Entities

| Entity | Properties | Business Rules |
|--------|------------|----------------|
| **ComponentWeighing** (from bolt 007, extended) | + `attempt: WeighingAttempt?` · + `record: WeighingRecord?` | `Active` ⇔ it has an attempt. `Done` ⇔ it has a record. A done component never gets an attempt again. Quit or switching away discards the attempt (no record) and returns it to `Pending`. |
| **WeighingAttempt** (inside the component) | `startedAt`, `startedBy`, `lot?`, `container?`, `tare?: TareTaken` | Starts when the component becomes active (selection or progression). Holds what the operator has captured so far. Never stored as a record; discarded by quit, leave or switching. |
| **WeighingRecord** (inside the component; immutable) | `id`, `batchId`, `sequence`, `chemicalId`, `chemicalName`, `required: Quantity`, `actual: Quantity`, `class`, `tolerance` (± and source), `scaleId`, `scaleClass`, `lot`, `container`, `tare: Quantity`, `operator`, `confirmedAtUtc` (later: override details) | Created **only** by a successful confirm. **Exactly one per component.** Never changed or deleted by the app (FR-19). `actual` is the server's own reading at the moment of confirming, rounded to the scale resolution (WEIGH-ROUNDING). It carries every STEP 25 field: actualWeight, operator, component, lot, container, tare, scale, timestamp. |

## Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| **Lot** | `value: string` | Trimmed; not empty; at most 32 characters. Scanned (PD-03) or typed. **No identity check against the recipe in V1.** |
| **ContainerId** | `value: string` | Trimmed; not empty; at most 32 characters. The OrgaTEX suggestion (`C-12`) is offered but only stored when the operator accepts it or types one (story 004). |
| **TareTaken** | `scaleId`, `tare: Quantity`, `taredAtUtc` | The value the scale reports for the tare (FR-7). Valid for confirming only if it was taken **on the component's scale** and **after the scale last came online** (`taredAt ≥ onlineSince`, unit 004). A scale that went offline and returned needs a new tare (story 005). |
| **ConfirmationCheck** | `missing: [lot, container, tare, stableReading, inTolerance, …]` | The single rule behind both the enabled/disabled Confirm button and the server's rejection: the screen and the server use the same list. |

Bolt 007's value objects (`Quantity`, `ToleranceBand`, `Classification`, `ScaleProfile`, …) are reused unchanged.

## Aggregates

| Aggregate Root | Members | Invariants |
|----------------|---------|------------|
| **WeighingSession** (extended) | components, each with an optional attempt and an optional record | Bolt 007's invariants 1–9, plus: **10.** Lot, container and tare can only be set on the **active** component's attempt. **11.** A tare is accepted only from the active component's scale. **12. Confirm** succeeds only if all hold: the given component is the active one; its tolerance is resolved (not blocked); lot, container and a valid tare are present; and the **server's fresh reading** is valid, stable and classed Within range or Target achieved. Otherwise the confirm is rejected with the missing conditions and nothing changes. **13.** A confirm for a component that is already done is rejected (`Weighing.AlreadyConfirmed`), so a double tap produces **one** record. **14.** After a confirm, the component is `Done` with its record and the **next pending component becomes active** with a fresh, empty attempt (decision D1). When no component is pending, nothing is active and the session is **ready to complete** (completion itself: bolt 009). **15. Quit** discards the active attempt: no record, the component returns to Pending, nothing is active. **16. Leave** discards any active attempt the same way and leaves the session in progress; every record is kept (decision D3). **17.** Records are never removed, and a done component never returns to Pending. |

## Domain Events

| Event | Trigger | Payload |
|-------|---------|---------|
| **LotCaptured** / **ContainerCaptured** | Lot or container set on the attempt | batchId, sequence, value, operator, at |
| **ScaleTared** | First tare in the attempt | batchId, sequence, scaleId, tare, operator, at |
| **ScaleReTared** | A tare replacing an earlier one in the same attempt (F2) | batchId, sequence, scaleId, old and new tare, operator, at |
| **WeighingConfirmed** | A successful confirm | the record (all STEP 25 fields) |
| **WeighingAttemptQuit** | Quit (F4) | batchId, sequence, operator, at |
| **BatchLeft** | Leaving the batch with components still pending | batchId, done count, total, operator, at |
| (bolt 007) Started, Resumed, ComponentSelected, AttemptAbandoned | — | — |

Audit trail: Quit, BatchLeft and ReTare already exist as audit types (unit 002); `WeighingConfirmed` is added. Lot, container and first-tare events are kept on the attempt and in the record, not audited separately (the record carries them).

## Domain Services

| Service | Operations | Dependencies |
|---------|------------|--------------|
| **ConfirmationRules** | `Check(component, reading?) → ConfirmationCheck` | `ReadingClassifier` (bolt 007). Pure: the one place the confirm rule lives. The session's `Confirm` and the "can I confirm?" query for the screen both call it. |
| **NextComponentRule** | `After(session, confirmedSequence) → sequence?` | Pure (decision D1) |
| (bolt 007) ScaleSelector, ToleranceResolver, ReadingClassifier | — | — |

## Repository Interfaces

| Repository | Entity | Methods |
|------------|--------|---------|
| **IWeighingSessionRepository** (extended) | WeighingSession | Loads components **with their attempt and record**. Saving a confirm writes the record and the session change in **one transaction**, under the session's optimistic concurrency, so a double submit can't write two records. A records store guard rejects any update or delete of a record (like the audit trail). |

## Ubiquitous Language

| Term | Definition |
|------|------------|
| Attempt | One try at weighing the active component: lot, container and tare captured so far. Ends in a record (confirm) or nothing (quit, leave, switch). |
| Lot | The dyestuff lot being consumed, scanned or typed. |
| Container | The container the chemical is dispensed into; OrgaTEX may suggest one. |
| Tare / re-tare | Zeroing the empty container on the scale; re-tare (F2) repeats it and replaces the stored tare. |
| Valid tare | A tare taken in this attempt on the component's scale, after that scale last came online. |
| Confirm (F1) | Recording a valid weighing. Allowed only when in tolerance, stable, tared, with lot and container. |
| Weighing record | The immutable result of a confirm: required, actual, class, tolerance and source, scale, lot, container, tare, operator, time. |
| Quit (F4) | Abandoning the current attempt; the chemical goes back to pending. |
| Leave | Exiting the batch unfinished; confirmed records stay, and the batch resumes later. |
| Ready to complete | Every component is done; completion follows (bolt 009). |

## Story Coverage

- ✅ **004 lot and container**: `Lot`, `ContainerId`, invariant 10, `ConfirmationCheck` (empty → Confirm disabled), no identity check.
- ✅ **005 tare and re-tare**: `TareTaken` (scale value, after the scale came online), invariant 11, `ScaleReTared` (no record).
- ✅ **008 confirm**: `ConfirmationRules`, invariants 12–13, `WeighingRecord`, `WeighingConfirmed`.
- ✅ **009 progression**: invariant 14, `NextComponentRule`, done count "n of N", ready to complete.
- ✅ **012 quit and leave**: invariants 15–17, `WeighingAttemptQuit`, `BatchLeft`.

## Decisions at This Checkpoint: resolved (APPROVE MODEL, 2026-11-06)

- **D1: option 1.** After a confirm, the next pending component after the confirmed one becomes active, wrapping to the start (NEXT-COMPONENT).
- **D2: option 1.** The server reads the scale itself at confirm time and records that value (CONFIRM-SERVER-READ).
- **D3: option 1.** Leaving the batch discards the unfinished attempt; a crash or restart keeps it (LEAVE-DISCARDS).
- **A6–A10: accepted** as recommended.

#### Options as presented

### D1 · Which component becomes active after a confirm?

FR-4 says "the next pending chemical in recipe sequence is pre-selected", but doesn't say where "next" starts.

**Example:** B-24187 has components 1–5. Blue RSPL (1) is temporarily unavailable, so the operator picks Yellow RGB (3) first and confirms it. Pending: 1, 2, 4, 5.

1 - **(Recommended)** The next pending one **after** the confirmed component, wrapping to the start. After 3, that's **4** (Sodium Sulphate), then 5, then back to 1 and 2. The operator's choice to skip ahead is respected, and the recipe order still decides what comes next.
2 - The **first** pending one in the whole sequence (the same rule as "suggested"). After 3, that's **1** again, the chemical the operator had just skipped, so they have to switch away from it once more.

### D2 · Which weight is recorded when the operator confirms?

**Example:** the screen shows 94.2 g Within range and the operator presses F1.

1 - **(Recommended)** The server **reads the scale itself** at the moment of confirming, checks that reading (stable, valid, in tolerance) and records that value. If the weight changed in the meantime (for example 94.6 g), the confirm is rejected with the new class, and the screen already shows it on the next poll. The UI can never record a number the scale didn't report (FR-13: "the server re-checks the same rule").
2 - The UI sends the weight it displayed, and the server re-reads the scale to compare. That's two values to reconcile, and a tampered request could still try to send a number.

### D3 · What happens to an unfinished attempt when the operator leaves the batch?

**Example:** on Yellow RGB the operator has typed lot `2026-0387`, accepted container C-12 and tared (214.0 g), then touches **Back to queue**.

1 - **(Recommended)** Leaving **discards the attempt**, like Quit: Yellow RGB returns to Pending, and coming back means a fresh tare and lot. A tare can't be trusted after the operator has walked away (the container may have been moved). Confirmed components are untouched. Note: a **crash or restart** is not "leaving": the attempt is kept, and the tare rule (after the scale came online) still protects it.
2 - Keep the attempt (lot, container, tare) and continue where it stopped. Faster to resume, but it can reuse a tare that no longer matches what's on the scale.

## Assumptions (unconfirmed by FCI; each needs accept or change)

### A6 · Lot and container are free text, checked only for presence

**Example:** lot ` 2026-0387 ` is stored as `2026-0387`; a 40-character lot is rejected (422); `ABC` is accepted even if it isn't a real lot.

- **Recommended:** trimmed, not empty, at most 32 characters, no format or recipe check in V1 (FR-5; PD-03: lot capture only).

### A7 · Any tare the scale reports is accepted, including 0

**Example:** a container weighing 214.0 g gives tare 214.0 g; weighing straight onto the pan gives tare 0.0 g.

- **Recommended:** store the value the scale reports, whatever it is (FR-7: "stores the tare weight the scale reports"). The record shows it, so a 0.0 g tare is visible in review.
- Alternative: require tare > 0 (a container is mandatory). Not in any source.

### A8 · A double tap is rejected by naming the component being confirmed

**Example:** the operator double-taps F1 on Yellow RGB (3). The first request records 94.2 g and makes Sodium Sulphate (4) active. The second request still says "confirm 3".

- **Recommended:** the confirm command names the component. The second request finds 3 already done → **409 `Weighing.AlreadyConfirmed`**, one record, and component 4 is untouched. Without the name, a fast second tap could be applied to the newly active component 4 (it would still fail its own guards, but it shouldn't even be tried).

### A9 · Records are immutable at every layer

**Example:** a record for Yellow RGB says 94.2 g. Nobody, including a supervisor, can change it to 94.0 g through the app.

- **Recommended:** no update or delete endpoint, and the persistence layer refuses any change to a stored record (the same guard as the audit trail). A correction would be a new, separate record type in a later release, not an edit.

### A10 · An unstable or invalid reading is never confirmable, even if it is in tolerance

**Example:** while still pouring, the scale shows 94.0 g but `stable: false`.

- **Recommended:** Confirm stays disabled and the server rejects it (FR-13, OQ-3). Over-capacity, malformed and below-zero readings are not confirmable either.
