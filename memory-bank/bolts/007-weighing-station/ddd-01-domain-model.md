---
unit: 005-weighing-station
bolt: 007-weighing-station
stage: model
status: complete
updated: 2026-11-05T17:35:00Z
---

# Static Model - Weighing Station

## Bounded Context

**Guided weighing.** This context owns everything that decides whether a weighing can be valid: the weighing session of one batch, which component is being weighed, which scale it goes on, which tolerance applies and how each reading is classified. Every rule runs on the server in the pure Domain project (no I/O); the UI only renders the state and classes it computes (unit brief).

Neighbouring contexts and what it takes from them:
- **Batch & recipe** (unit 003): the immutable `RecipeSnapshot` (components in sequence, target, optional recipe tolerance, suggested container) and the configured tolerance catalogue (chemical + scale class). Read-only here.
- **Device integration** (unit 004): scale readings (`net {value, unit}`, stable, validity) and the scale list (id, class, capacity, resolution). The domain receives readings as values; it never calls a device.
- **Station access** (unit 002): the signed-in operator and the plant clock (UTC).

This bolt (007) models the session, component selection, scale selection, tolerance resolution and classification. The same aggregate is extended by bolt 008 (lot, container, tare, confirm, quit, progression) and bolt 009 (overshoot, override, completion, records); those parts are listed below as **later** so the boundary is visible, but are not built here.

## Domain Entities

| Entity | Properties | Business Rules |
|--------|------------|----------------|
| **ComponentWeighing** (inside `WeighingSession`) | `sequence`, `chemicalId`, `code`, `name`, `target: Quantity`, `scale: ScaleSelection`, `tolerance: ToleranceResolution`, `state: Pending \| Active \| Done` (later: lot, container, tare, record) | One per recipe line, created from the snapshot when the session starts and never added or removed. Identity = `sequence` within the session. Scale and tolerance are resolved **once, at session start**, and fixed for the session (the recipe snapshot is immutable; a configuration change applies to sessions started afterwards). `Done` is final: a done component can't become active again (FR-4). A component whose tolerance is `Blocked` can be selected (so the operator sees why) but no reading of it is classifiable or confirmable (assumption A3). Scale and tolerance are fixed at session start (assumption A1). |

## Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| **Quantity** (exists, Domain/Common) | `value: decimal`, `unit: g \| kg` | Fixed-point decimals only (no float). Conversion is exact (1 kg = 1000 g). Reused, extended with: conversion to another unit, rounding to a resolution, comparison across units. |
| **ScaleClass** | `Precision \| HeavyDuty` | The two classes named by the PDF. Prototype names: "3 kg precision", "32 kg floor". |
| **ScaleSelectionPolicy** | `thresholdKg: decimal`, `assignments: ScaleClass → scaleId` | From configuration, never constants (FR-8). `thresholdKg > 0`; both classes assigned. Default 3 kg, inclusive. |
| **ScaleSelection** | `scaleClass`, `scaleId`, `thresholdKg` used | Rule: `target in kg ≤ thresholdKg → Precision`, else `HeavyDuty` (2.999 / **3.000** → Precision, 3.001 → HeavyDuty). Comparison after unit conversion; g vs kg never compared raw. "Automatic": never chosen by the operator in V1. |
| **ScaleProfile** | `scaleId`, `scaleClass`, `unit`, `resolution: Quantity` | The measuring characteristics the domain needs from the scale list (unit 004). `resolution > 0`. |
| **ToleranceBand** | `target: Quantity`, `plusMinus: Quantity`, `lower`, `upper` | `plusMinus > 0`; `lower = target − plusMinus`, `upper = target + plusMinus`, **both inclusive**. Shown as "TOLERANCE ±0.5 g (93.5 – 94.5)" (FR-10). |
| **ToleranceSource** | `Recipe \| Configured` | Snapshotted on the record (bolt 009). |
| **ToleranceResolution** | `Resolved(band, source)` or `Blocked("no tolerance defined")` | Order (decision TOL): recipe tolerance → configured tolerance for chemical + **selected scale class** → blocked. No universal default, ever. |
| **WeightClass** | `Underweight \| WithinRange \| TargetAchieved \| Overweight` | The four PDF classes. `WithinRange` and `TargetAchieved` are both confirmable; the others are not (overshoot/override: bolt 009). |
| **Classification** | `class`, `net` (as measured, at resolution), `remaining: Quantity`, `band` | `remaining = max(0, target − max(0, net))` (FR-11, with A5). Computed on the server with fixed-point decimals only. |

**Classification rule** (FR-12, PDF §7), with `t` target, `d` tolerance, `n` net, all in the **scale's unit**:
1. Round `n` to the scale resolution (rounding mode: decision D1).
2. `n < t − d` → **Underweight**.
3. `n > t + d` → **Overweight**.
4. `n = t` (target expressed at the same resolution) → **Target achieved**.
5. Otherwise → **Within range** (so `t − d ≤ n ≤ t + d` and `n ≠ t`).

Checked examples:
- 1.000 ± 0.010 kg: 0.980 → Underweight · 0.995 → Within · 1.000 → Target achieved · 1.008 → Within · 1.020 → Overweight.
- Yellow RGB 94.0 ± 0.5 g: 93.4 → Underweight · **93.5** → Within · 94.0 → Target achieved · **94.5** → Within · 94.6 → Overweight.
- A negative net (below zero after tare) → Underweight; remaining = target (assumption A5).

Units (assumption A4): the target and the tolerance are converted into the scale's unit before comparing (a 2.5 kg target on a g-reporting precision scale is classified as 2500.0 g). The result is the same in either unit because conversion is exact.

## Aggregates

| Aggregate Root | Members | Invariants |
|----------------|---------|------------|
| **WeighingSession** (one per batch) | `batchId`, `status: InProgress` (later: `Completed`), `startedAtUtc`, `startedBy`, `components: ComponentWeighing[]` (in recipe sequence), `activeSequence?`, `version` (concurrency) | 1. Created only from a valid `RecipeSnapshot`: one `ComponentWeighing` per line, in sequence, **all Pending**. 2. **At most one session per batch**; starting again resumes the existing one (no duplicate, nothing reset). 3. **At most one Active component.** 4. Only a `Pending` component can be selected; selecting a `Done` component changes nothing. 5. Selecting another component while one is Active **abandons the attempt** (the old one returns to Pending, no record — same as Quit, FR-4/FR-14). 6. The **suggested** component is the first Pending one in sequence (PD-07: free selection, sequence suggested). 7. A reading can only be classified for the Active component and **only from its selected scale** (FR-8: live weight is read only from that scale). 8. Done components never change (confirmed work survives interruption and restart). 9. Concurrent changes from two stations: the second is rejected (optimistic concurrency on `version`; assumption A2). |

Later in the same aggregate: lot / container / tare per attempt and confirm (bolt 008); overshoot, override, completion and records (bolt 009).

## Domain Events

| Event | Trigger | Payload |
|-------|---------|---------|
| **WeighingSessionStarted** | First open of a batch for weighing | batchId, operator, component count, startedAtUtc |
| **WeighingSessionResumed** | Opening a batch that already has a session | batchId, operator, done count, resumedAtUtc |
| **ComponentSelected** | A Pending component becomes Active | batchId, sequence, chemicalId, scaleId, toleranceSource or blocked |
| **WeighingAttemptAbandoned** | Switching away from the Active component before confirming | batchId, sequence, operator, atUtc |

Events are recorded on the aggregate and written to the audit trail (unit 002 pattern); no other context consumes them in this bolt. `BatchCompleted` (consumed by units 006 and 007) is raised in bolt 009.

## Domain Services

| Service | Operations | Dependencies |
|---------|------------|--------------|
| **ScaleSelector** | `Select(target: Quantity, policy) → ScaleSelection` | `ScaleSelectionPolicy` (configuration, passed in) |
| **ToleranceResolver** | `Resolve(component, scaleClass, catalogue) → ToleranceResolution` | Recipe tolerance on the snapshot line; `IToleranceCatalogue.Find(chemicalId, scaleClass)` (exists, unit 003) |
| **ReadingClassifier** | `Classify(band, net, scaleProfile) → Classification` | None (pure function) |

All three are pure and deterministic; configuration and catalogue are inputs, so the boundary tests (STEP 22, FR-8) need no infrastructure.

## Repository Interfaces

| Repository | Entity | Methods |
|------------|--------|---------|
| **IWeighingSessionRepository** | WeighingSession | `GetByBatchAsync(batchId)` (with components) · `AddAsync(session)` · `SaveAsync(session)` with optimistic-concurrency failure reported as a conflict. "Add" of a second session for the same batch is reported as already-exists, so a race of two first opens ends with one session (same rule as the recipe snapshot). |

Read from other contexts (existing ports, not new repositories): `IRecipeSnapshotStore` (unit 003), `IToleranceCatalogue` (unit 003), scale list / reading via the device port (unit 004).

## Ubiquitous Language

| Term | Definition |
|------|------------|
| Weighing session | The weighing of one batch at the station, from first open to completion; survives quit and restart. |
| Component | One recipe line (chemical + target) inside a session. Pending → Active → Done. |
| Pending / Active / Done | Not yet weighed / being weighed now / confirmed (final). |
| Suggested component | The first Pending component in recipe sequence; the operator may pick any other Pending one (PD-07). |
| Attempt | One try at weighing the Active component; abandoned by switching away or quitting, ended by confirming. |
| Target | The required quantity from the recipe. |
| Tolerance (±) | The allowed deviation, from the recipe or configuration; never a universal default. |
| Tolerance band | `[target − tol, target + tol]`, bounds inclusive. |
| Blocked | No tolerance found in recipe or configuration: the component can't be weighed ("no tolerance defined"). |
| Scale class | Precision (≤ threshold) or Heavy-Duty (> threshold); prototype names "3 kg precision" and "32 kg floor". |
| Threshold | The configurable boundary for scale selection, default 3 kg, inclusive on the Precision side. |
| Resolution | The smallest step a scale reports (0.1 g; 0.001 kg). Readings are compared at this step. |
| Net | The weight on the scale minus the tare. |
| Remaining | `max(0, target − net)`, "still to weigh". |
| Underweight / Within range / Target achieved / Overweight | The four classes of a reading (PDF §7); Target achieved is distinct from Within range. |

## Story Coverage

- ✅ **001 start or resume**: `WeighingSession` invariants 1, 2, 8, 9; `WeighingSessionStarted` / `Resumed`; repository (restart durability, two stations).
- ✅ **002 select pending component**: invariants 3–6; `ComponentSelected`, `WeighingAttemptAbandoned`.
- ✅ **003 automatic scale selection**: `ScaleSelectionPolicy`, `ScaleSelection`, `ScaleSelector` (2.999 / 3.000 / 3.001 kg; Yellow RGB → 3 kg precision; Sodium Sulphate → 32 kg floor; configuration-driven).
- ✅ **006 classify reading**: `ToleranceBand`, `WeightClass`, `Classification`, `ReadingClassifier` (PDF §7 and STEP 22 values; remaining; resolution; band caption).
- ✅ **007 resolve tolerance source**: `ToleranceResolution`, `ToleranceSource`, `ToleranceResolver` (recipe → configured → blocked; source kept for the record).

## Decisions at This Checkpoint: resolved (APPROVE MODEL, 2026-11-05)

- **D1: option 1.** Readings are rounded to the scale resolution half away from zero (WEIGH-ROUNDING).
- **D2: option 1.** Any signed-in operator may resume an unfinished session; each record carries its own operator (SESSION-RESUME).
- **A1–A5: accepted** as recommended.

#### Options as presented

### D1 · Rounding a reading that has more digits than the scale shows

**Situation:** the 3 kg precision scale shows steps of 0.1 g, but a real indicator may still send `93.45 g`. The server must decide which 0.1 g value that is before classifying. Yellow RGB band: 93.5 – 94.5 g.

| Raw reading | 1 · Half away from zero | 2 · Half to even | 3 · Truncate |
|---|---|---|---|
| 93.45 g | 93.5 → **Within** (confirmable) | 93.4 → **Underweight** | 93.4 → **Underweight** |
| 93.55 g | 93.6 → Within | 93.6 → Within | 93.5 → Within |
| 94.55 g | 94.6 → **Overweight** | 94.6 → **Overweight** | 94.5 → **Within** (confirmable) |
| 94.04 g | 94.0 → Target achieved | 94.0 → Target achieved | 94.0 → Target achieved |

1 - **(Recommended)** Half away from zero. The operator reads `93.5` on the indicator and the server agrees.
2 - Half to even. The server can disagree with the indicator (display 93.5, class Underweight).
3 - Truncate. 94.55 g, which is really over the band, would be accepted.

In the lab the simulator already sends values at 0.1 g / 0.001 kg, so all three behave the same; the choice matters for real scales.

### D2 · Who may resume an unfinished batch

**Example:** R. Karim (OP-1001, shift A) confirms 3 of 5 components of B-24187; shift A ends. OP-1002 (shift B) signs in and opens B-24187.

1 - **(Recommended)** Any signed-in operator. OP-1002 finishes components 4 and 5. Records 1–3 say R. Karim and records 4–5 say OP-1002, so traceability is per weighing (FR-19) and shift handover works.
2 - Only the operator who started it. OP-1002 is refused until a supervisor (SV-2001) approves, so the batch waits at every shift change.

## Assumptions (unconfirmed by FCI; each needs accept or change)

### A1 · Scale and tolerance are fixed when the session starts

**Example:** B-24187 starts; Soda Ash uses a configured ±0.048 kg (the recipe gives none). Mid-batch, an engineer changes it to ±0.100 kg, or moves the threshold from 3 kg to 2 kg.

- **Recommended:** the open batch keeps ±0.048 kg and its scales; the change applies from the next batch. All records of one batch are judged by the same rules, the recorded "tolerance + source" matches what the operator saw, and the scale never switches mid-pour.
- Alternative: apply changes at once. Components of one batch could be judged by different tolerances, and a pour in progress could change class without the reading changing.

### A2 · Two stations on the same batch is a conflict, not a feature

**Example:** station 01 has B-24187 open; someone opens it on a second terminal and selects a component at the same moment.

- **Recommended:** the first change wins; the second gets "this batch was changed elsewhere, reload" (409). Nothing is lost or duplicated. V1 has one station (`STATION 01`); multi-station hand-off is not in any source.
- Two first opens at the same moment still end with one session (the same race rule as the recipe snapshot).

### A3 · A component with no tolerance can be selected but not weighed

**Example:** recipe line "Leveller X" has no recipe tolerance, and the configuration has no entry for Leveller X on the precision scale.

- **Recommended:** Leveller X is listed and selectable. Selecting it shows "No tolerance defined: ask a supervisor", with no class and Confirm never possible. The other components can still be weighed. The batch can't complete until the configuration is fixed (then the batch is quit and reopened). No universal default (decision TOL).
- Alternative: refuse to start the session at all. Safer, but one missing entry stops the whole batch, including chemicals that are fine.

### A4 · Classification is done in the scale's unit

**Example:** a 2.500 kg target goes to the precision scale (≤ 3 kg), which reports in g.

- **Recommended:** convert the target and tolerance into the scale's unit and compare there: 2500.0 g ± 10.0 g against the reading in g, at 0.1 g resolution. Conversion is exact (1 kg = 1000 g), so the class is the same in either unit; the scale's real resolution decides "Target achieved", and the screen shows what the scale shows.
- Sodium Sulphate 24.000 kg on the 32 kg floor scale (kg, 0.001) needs no conversion.

### A5 · Below-zero readings

**Example:** after tare the container is lifted off; the reading is −0.3 g (Yellow RGB, target 94.0 g).

- **Recommended:** a negative net is **Underweight** (never confirmable), and **remaining shows the full target, 94.0 g**, by counting a negative net as 0. FR-11's formula `max(0, target − net)` would show 94.3 g, which is more than the recipe asks for. This is a small refinement of FR-11, for approval.
- Alternative: keep FR-11 literally (94.3 g).
- The unit 004 validities (below zero, unstable, malformed) are kept for the confirm rules in bolt 008: Confirm needs a stable, valid and tared reading.
