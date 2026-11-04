---
intent: 001-assisted-weighing-station
phase: inception
status: draft
created: 2026-11-04T14:00:00Z
updated: 2026-11-04T14:00:00Z
---

# UI Analysis: Operator Terminal Mockup (STEP 8)

> **Source**: `docs/customize-weighing-station.html` (870 lines). `L123` = line in that file.
> **Purpose**: extract what must be built. The prototype is **not redesigned**, and its implementation is **not copied** (CLAUDE.md).
> **Status of the mockup itself**: it says "Not a specification. Values, layout and terminology are our assumptions" (L419) and "Label content is invented" (L612).
> **Tags**: ✅ matches an approved requirement · ⚠️ **GAP**: the mockup differs from the approved requirements; the requirement wins · 🎭 **demo-only**: a mockup control that is not part of the product.

## 1. Screen inventory

| # | Screen | Element id | Lines | Purpose | Entered by (mockup) | Entered by (product) |
|---|---|---|---|---|---|---|
| 1 | **Select batch** (batch queue) | `v-queue` | L448–487 | Choose the batch to weigh, by scanning the batch card or touching a row | Demo segment button (L425) | Start of the shift; "Next batch" (FR-1, FR-2) |
| 2 | **Weigh** | `v-weigh` | L490–564 | Weigh one component at a time against the target and tolerance | Default active view (L490); demo button | Batch selected (FR-2, FR-3) |
| 3 | **Confirm & print** (completion) | `v-done` | L567–616 | Batch summary, OrgaTEX write-back status, ticket preview and print | `btn-ok` on the last pending component (L820) | All N components confirmed (FR-18) |
| — | Persistent **top bar** | `.topbar` | L440–445 | Station, batch / order / machine, operator | Always visible | Always visible |
| — | Persistent **status strip** | `.strip` | L618–627 | Health of each device and integration, language, clock | Always visible | Always visible (FR-23, FR-24) |
| 🎭 | Demo **controls** | `.controls` | L422–434 | Switch screens, simulate dispense or overshoot, reset, toggle question pins | — | **Not in the product.** Simulation moves to the scale simulator (`/_sim/*`) |
| 🎭 | **Meeting notes** and footer | `.notes` | L633–670 | Facilitation notes and the list of pinned questions | — | Not in the product |

Missing screens the product needs (no mockup): **sign-in** (badge, ADR-009), the **supervisor override dialog** (PIN + reason, FR-15), the **lot / container entry** step (FR-5, FR-6), the **completed-batch summary list** for later review (PDF §6 Step 9, FR-18), and **error / offline states** (FR-23, FR-24).

## 2. UI component inventory

| Component | Screen | Lines | Contents / behaviour | Requirement |
|---|---|---|---|---|
| Brand mark | top bar | L441 | "WeighingMS · STATION 01" | Station id is config |
| Batch breadcrumb | top bar | L442 | BATCH · ORDER · MACHINE of the active batch | FR-2 |
| Operator chip | top bar | L444 | Green dot, "Operator R. Karim · Shift A" (pin Q37) | FR-19, ADR-009 |
| Scan prompt | 1 | L450–455 | Animated beam: "Scan a batch card to begin / Or touch a row below. Queue received from OrgaTEX at 06:12" | FR-1, FR-2, FR-27 |
| Batch queue table | 1 | L458–484 | Columns: Batch, Order, Machine, Fabric / lot, Shade, Comp. (count), Status tag (`WEIGHING` amber / `QUEUED`) | FR-1 |
| Recipe rail header | 2 | L494–498 | "Recipe · Teal 4C", batch id, "*n* of 5 components weighed" | FR-3, FR-17 |
| Component list (rail) | 2 | L696–716 | One button per component: colour swatch, name, code, scale short name, target + unit, state text (`✓ actual` / `WEIGHING` / `PENDING`) | FR-4, FR-17 |
| Weigh header | 2 | L503–507 | Component name; meta "code · LOT · CONTAINER · TARE" (pins Q28/Q29) | FR-4–FR-7 |
| Scale badge | 2 | L508–512 | "Scale selected · 3 kg precision · automatic" (pin Q15) | FR-8 |
| Readout, 3 cells | 2 | L515–528 | **Weighed now** (colour by zone, pin Q16), **Still to weigh**, **Target**, each with a unit | FR-9, FR-10, FR-11 |
| Progress bar | 2 | L530–535 | Fill (colour by zone), tolerance **band**, **target line**; ARIA progressbar | FR-9, FR-12 |
| Bar caption | 2 | L536–540 | `0` · "TOLERANCE ±0.5 g (93.5 – 94.5)" (pin Q24) · bar max "125 g" | FR-12 |
| Status line | 2 | L541 | Text and colour by zone (see §8) | FR-12, FR-13 |
| Surplus panel | 2 | L544–554 | Shown when over: "Surplus +2.4 g (+2.6%)", recalculated remaining components (pin Q25) | FR-16, **PD-05** |
| Action keys | 2 | L556–561 | `OK · Confirm F1` (disabled unless ok), `Re-tare F2`, `Quit F4`, `Supervisor override` (pin Q33) | FR-13, FR-7, FR-14, FR-15 |
| Completion summary | 3 | L570–578 | Components weighed 5 of 5 · All within tolerance YES · Total dispensed · Elapsed · Operator · Written to OrgaTEX CONFIRMED hh:mm:ss | FR-18, FR-20 |
| Release note | 3 | L579–582 | "Actual consumption posted… Container released for transport to JET-04" (pin Q11/Q13) | FR-20 |
| Completion keys | 3 | L583–586 | `Print ticket F1`, `Next batch F3` | FR-21, FR-1 |
| Ticket preview | 3 | L590–610 | 80 mm-style receipt: header, BATCH, ORDER, MACHINE, SHADE, DATE, OPER., component TGT/ACT lines, "ALL WITHIN TOLERANCE", barcode, batch id (pin Q18) | FR-21, PD-04 |
| Status strip | all | L618–627 | `SCALE 3 kg · READY`, `SCALE 32 kg · READY`, `SCANNER`, `PRINTER` (green dots), `ORGATEX · LINKED 07:41` (amber dot, pin Q6), `EN` (pin Q35), clock | FR-23, FR-24, ADR-012 |
| Question pins | all | L385, L444… | Numbered badges; hover shows a brief §9 question | 🎭 discussion aid |

## 3. User actions

| Action | Where | Lines | Mockup effect | Product effect (requirement) |
|---|---|---|---|---|
| Scan batch card | 1 | L453 | None (text only) | Load the batch (FR-2, FR-27) |
| Touch a queue row | 1 | L463–482 | **None**: rows have no handler | Select the batch (FR-2) |
| Touch a component in the rail | 2 | L711, L718–724 | Make it active; **resets the live weight**; done rows ignored | FR-4, PD-07 free selection ✅ |
| Dispense powder (physical) | 2 | — | Simulated by `sim-ok` / `sim-over` 🎭 | Live weight from the scale (FR-9, FR-26) |
| `OK · Confirm` (F1) | 2 | L814–822 | Store `actual = now`, mark done, go to the first pending component, or to screen 3 | FR-13, FR-17, FR-18 |
| `Re-tare` (F2) | 2 | L811 | `reset()`: weight to 0 | Call the scale tare, record the tare (FR-7) |
| `Quit` (F4) | 2 | L812 | `reset()` **only**: stays on the screen | Safe exit, batch stays incomplete (FR-14) |
| `Supervisor override` | 2 | L824–826 | Status text "OVERRIDE REQUESTED — AWAITING SUPERVISOR" only | Badge + PIN + reason (FR-15) |
| `Print ticket` (F1) | 3 | L584 | **None** | Print / reprint (FR-21, FR-24) |
| `Next batch` (F3) | 3 | L585 | **None** | Back to the queue (FR-1) |
| 🎭 Simulate dispense / overshoot / reset | controls | L802–810 | Animate to target + 0.4·tol, or to target + 4.8·tol; reset | Scale scenarios via `/_sim/scales/{id}/scenario` (FR-26) |
| 🎭 Screen switch / pins toggle | controls | L829–845 | Show any view; show/hide pins | Not in the product |

F1/F2/F3/F4 are **labels only**: there is no `keydown` handler. Note that F1 means Confirm on screen 2 and Print on screen 3.

## 4. Application states

**Component state** (`state`, L678–682; styling L216–225): `pending` → `active` → `done`.
**Weight zone** (`zone()`, L729–735), which drives the colour, status text, surplus panel and OK button:

| Zone | Rule (mockup) | Colour | Approved classification (FR-12) |
|---|---|---|---|
| `under` | `v < 0.9 × target` | neutral ink | Underweight |
| `near` | `0.9 × target ≤ v < target − tol` | amber | Underweight; amber is a **visual hint only** (OQ-4 ✅) |
| `ok` | `target − tol ≤ v ≤ target + tol` | green | Within range **or** Target achieved ⚠️ **GAP**: the mockup has no separate "target achieved" |
| `over` | `v > target + tol` | red | Overweight |

**Screen state**: `queue` / `weigh` / `done` (L829–836).
**Device / integration state** (strip): `up` (green) / `warnd` (amber); there's no "down" state drawn (L323–324).
**Batch state** (queue tags): `WEIGHING`, `QUEUED` (L466, L471).
**Override state**: "requested / awaiting supervisor" (text only, L825).

## 5. Data displayed

| Data | Example (seed) | Lines | Format rule |
|---|---|---|---|
| Station, shift | STATION 01, Shift A | L441, L444 | — |
| Batch, order, machine, shade | B-24187, SO-9931, JET-04, Teal 4C | L442, L464–465 | Monospace ids |
| Fabric / lot, batch weight | 100% CTN knit · 480 kg | L465 | Free text |
| Component count, batch status | 5, WEIGHING | L465–466 | — |
| Queue received time | 06:12 | L454 | Local hh:mm |
| Component: name, code, colour, target, unit, tolerance, scale, lot, container, tare, actual | Remazol Yellow RGB, DYE-4471, 94.0 g ±0.5, 3 kg precision, 2026-0387, C-12, 214 g | L678–682 | **g → 1 decimal, kg → 3 decimals** (`dp()`, L692) |
| Live, remaining, target | 0.0 / 94.0 / 94.0 g | L518–526 | Remaining = `max(0, target − now)` (L747) |
| Tolerance band | ±0.5 g (93.5 – 94.5) | L763–765 | Same decimals as the unit |
| Progress % | `now / max` | L751 | `max` = **display range per component**, not scale capacity |
| Summary | 5 of 5, YES, 35.322 kg, 6 min 41 s, R. Karim, CONFIRMED 07:48:22 | L572–577 | Total = Σ actual in kg (1.2415 + 0.3854 + 0.0942 + 23.980 + 9.621 = 35.322) |
| Ticket | header, 6 header fields, 5 TGT/ACT lines, result, barcode, batch id | L591–609 | Date `YYYY-MM-DD hh:mm` |
| Device status, OrgaTEX link time, language, clock | READY, LINKED 07:41, EN, 07:47:03 | L619–626 | — |

## 6. Data required (to drive the screens; source → endpoint)

| Entity | Fields the UI needs | Provided by |
|---|---|---|
| Batch (queue item) | batchId, orderId, machine, fabric/lot text, batch kg, shade, componentCount, status, receivedAt | OrgaTEX dummy `GET /api/batches`, `/api/batches/:id` (FR-25) |
| Recipe component | sequence, chemicalId, code, name, displayColour, target, unit, tolerance (if in recipe) | `GET /api/batches/:id/recipe` (FR-3, FR-25) |
| Tolerance config | per chemical + scale (fallback) | WeighingMS config (FR-12, TOL) |
| Scale | id, name, capacity, resolution/decimals, status | `GET /api/scales`, `/status` (FR-8, FR-26) |
| Scale assignment | threshold 3 kg (inclusive), precision vs heavy-duty | Config (PDF §4.1, FR-8) |
| Live reading | value, unit, **stable flag**, timestamp | `GET /api/scales/:id/weight` (FR-9, OQ-3) |
| Tare | value | `POST /api/scales/:id/tare` (FR-7) |
| Lot, container | lotNo (scan / typed), containerId (typed, suggested) | Scanner dummy / operator (FR-5, FR-6, FR-27) |
| Operator, station, shift | operatorId, name, stationId, shift | Sign-in (ADR-009), config |
| Weighing record | all of the above + actual, class, overridden, reason, supervisor, confirmedAtUtc | WeighingMS DB (FR-19) |
| Write-back status | synced / pending / failed, syncedAt | Outbox (FR-20, PD-01) |
| Ticket | STEP 27 fields + barcode value | Printer dummy `POST /api/tickets` (FR-21, FR-28) |
| Display bar range (`max`) | per component | **Not in any source.** Derive from the tolerance band (see Q-UI-6) |

## 7. State model and transitions

```text
SCREEN FLOW
 [Sign-in]* ──► [1 Select batch] ──scan/touch batch──► [2 Weigh] ──last component confirmed──► [3 Confirm & print]
                     ▲                                   │  │                                       │
                     │                                   │  └─Quit (FR-14): batch stays incomplete──┤
                     └───────────────────── Next batch ◄─┴──────────────────────────────────────────┘
 (* not in mockup)

COMPONENT LIFECYCLE (per recipe line)
 PENDING ──select (L718)──► ACTIVE ──OK in "ok" zone (L814)──► DONE (no re-select, L719)
    ▲                          │
    └──select another (L720)───┘   (active → pending, live weight reset)
                               └──Override approved (FR-15; mockup: text only)──► DONE [OVR]

WEIGHT ZONE (inside ACTIVE, re-evaluated on each reading, L729)
 UNDER ──≥90% target──► NEAR ──≥ target−tol──► OK ──> target+tol──► OVER
   ▲                                            │                      │
   └──────────── Re-tare / Quit / select → 0 ◄──┴──────────────────────┘
 OK  → OK button enabled (L779)            OVER → surplus panel shown (L778), PD-05
```

| From | Event | To | Guard | Lines |
|---|---|---|---|---|
| queue | batch scanned or touched | weigh | batch loads (FR-2) | ⚠️ not wired in the mockup |
| weigh | OK on the last pending component | done | all N components done | L818–820 |
| done | Next batch | queue | — | ⚠️ not wired |
| pending | touched | active | state ≠ done | L718–724 |
| active | another component touched | pending | — | L720 |
| active | OK | done | zone = ok (+ stable, OQ-3) | L779, L814–817 |
| done (component) | — | — | terminal; there's no undo | L719 |
| any zone | Re-tare / Quit / select | under (0) | — | L722, L785, L811–812 |

## 8. Validation behaviour

| Rule | Mockup | Lines | Approved requirement |
|---|---|---|---|
| Confirm only when within tolerance | `btn-ok.disabled = zone ≠ ok` | L779 | ✅ FR-13 |
| Bounds inclusive | `v > target+tol` is over; `v ≥ target−tol` is ok, so 93.5 and 94.5 are ok, while 93.4 and 94.6 are not | L731–732 | ✅ FR-12 (STEP 22 values) |
| Number type | JS floating point | L731–733 | ⚠️ **GAP**: the product uses fixed-point decimals (NFR); the boundary tests must pass exactly |
| Target achieved distinct | absent | — | ⚠️ **GAP**: PDF §4.2 requires a 4th class |
| Stable reading before confirm | absent | — | ⚠️ **GAP**: OQ-3 approved (stable required) |
| Tare before confirm | absent (tare is static data) | L741–742 | ⚠️ **GAP**: OQ-2 approved |
| Lot and container captured | static data, no entry | L678–682 | ⚠️ **GAP**: FR-5, FR-6 |
| Done component not re-weighable | `if done return` | L719 | ✅ |
| Over tolerance | red status, surplus panel, OK disabled | L770–771, L778 | ✅ block; the calculation is **PD-05** (not adopted) |
| Override | text only; it doesn't enable confirm or record anything | L824–826 | ⚠️ **GAP**: FR-15 (PIN + reason, true class kept, OVR) |
| Completion | automatic when no pending component is left | L818–820 | ✅ FR-18 (all N) |
| Invalid / negative / offline reading | absent | — | ⚠️ **GAP**: FR-23 |

## 9. Integration points

| Integration | Mockup evidence | Lines | Product contract |
|---|---|---|---|
| OrgaTEX: queue in | "Queue received from OrgaTEX at 06:12" | L454 | `GET /api/batches` (FR-25) |
| OrgaTEX: recipe in | rail populated from the `components` array | L677–683 | `GET /api/batches/:id/recipe` |
| OrgaTEX: consumption out | "Written to OrgaTEX CONFIRMED 07:48:22", "Actual consumption posted" | L577, L580 | `POST /api/batches/:id/consumption` via outbox (FR-20, PD-01) |
| OrgaTEX: link health | `ORGATEX · LINKED 07:41`, amber | L623 | Health + unsynced count (FR-24) |
| Scale: live weight | `run()` timer, 34 ms steps | L787–800 | `GET /api/scales/:id/weight`, polled ~250 ms (ADR-004) |
| Scale: tare | `TARE 214 g` text; Re-tare resets | L506, L811 | `POST /api/scales/:id/tare` |
| Scale: selection / status | badge "automatic"; strip READY | L511, L619–620 | Threshold config; `GET /api/scales/:id/status` |
| Scanner | "Scan a batch card"; strip SCANNER | L453, L621 | `POST /_sim/scanner/scan` → app (FR-27, PD-03) |
| Printer | Print ticket key; strip PRINTER | L584, L622 | `POST /api/tickets` (FR-28, PD-02) |
| Simulation control 🎭 | Simulate dispense / overshoot / reset | L430–432 | `/_sim/scales/{id}/scenario`, `/_sim/faults` |

## 10. Business rules embedded in the JavaScript

| # | Rule | Code | Lines | Adopt? |
|---|---|---|---|---|
| R1 | Zones: over if `> target+tol`; ok if `≥ target−tol`; near if `≥ 0.9·target`; else under | `zone()` | L729–735 | ✅ bounds; near = hint only (OQ-4) |
| R2 | OK enabled only in zone ok | `paint()` | L779 | ✅ (+ stable, tare: OQ-2/3) |
| R3 | Remaining = `max(0, target − now)`; never negative | `paint()` | L747, L775 | ✅ FR-11 |
| R4 | Over shows surplus = `now − target` | `paint()` | L771 | ✅ display; compensation = PD-05 |
| R5 | Decimals: g → 1, kg → 3 | `dp()` | L692 | ✅ per scale resolution (FR-9, config) |
| R6 | Done components can't be re-selected | `select()` | L719 | ✅ |
| R7 | Free selection of any non-done component; switching resets the reading | `select()` | L718–724 | ✅ PD-07 |
| R8 | After confirm, go to the **first** pending component in recipe order | `btn-ok` | L818 | ✅ "sequence suggested" (PD-07) |
| R9 | Batch completes when no pending component is left | `btn-ok` | L820 | ✅ FR-18 |
| R10 | Scale is a fixed property of each component | data `scale` | L678–682 | ⚠️ the product **computes** it from the target vs a configurable 3 kg threshold (FR-8) |
| R11 | Tolerance is a per-component value in the recipe data | data `tol` | L678–682 | ✅ recipe-first (TOL) |
| R12 | Re-tare = Quit = reset to 0 | handlers | L811–812 | ⚠️ different in the product (FR-7 vs FR-14) |
| R13 | Bar range `max` per component | data `max` | L751 | Display only (Q-UI-6) |
| R14 | Simulated pour: step = `max(0.4% target, 5.5% remaining)` every 34 ms; lands at target + 0.4·tol (ok) or target + 4.8·tol (over) | `run()` | L787–809 | 🎭 informs the scale-simulator "rising" scenario (FR-26), not app logic |
| R15 | Surplus compensation: remaining components scaled up by the overshoot % (24.000 → 24.624 kg = +2.6%) | static panel | L545–549 | ❌ **Not adopted**: PD-05, STEP 24; the mockup itself says "deliberately a guess" (L638) |
| R16 | Reduced motion: jump straight to the landing value | `run()` | L690, L791 | ✅ accessibility (ux-guide) |

## 11. Assumptions represented by the prototype

| # | Assumption | Evidence | Status |
|---|---|---|---|
| UA-1 | Weight can be read **continuously** | L517 pin Q16, L646 | Accepted for the dummy (FR-9, ADR-004); real scale unconfirmed (A-05) |
| UA-2 | "Surplus compensation" = recalculating the remainder | L551, L646 | **Open**: PD-05 |
| UA-3 | One operator weighs **one component at a time** against an OrgaTEX-owned queue | L647 | ✅ matches the requirements |
| UA-4 | Each operator is identified (named chip) | L444 pin Q37 | ✅ badge sign-in (ADR-009) |
| UA-5 | One container per component, with a known tare | L506 pin Q29, data `cont` | ✅ OQ-1 approved; tare measured, not assumed |
| UA-6 | Two scales: 3 kg precision and 32 kg floor | L619–620 | ✅ = PDF Precision / Heavy-Duty |
| UA-7 | Tolerance is absolute, per component (±6.0, ±2.0, ±0.5 g, ±0.120, ±0.048 kg) | data `tol` | Example values; source = recipe or config (TOL) |
| UA-8 | 15" touchscreen, F-keys available | L415, key labels | ux-guide; keys are labels only |
| UA-9 | English UI | L625 pin Q35 | Transloco, EN first (ADR-012) |
| UA-10 | The ticket is an 80 mm receipt with a barcode | L590–610, L612 | ✅ PD-04 (content was "invented"; now decided) |
| UA-11 | The OrgaTEX write-back is synchronous and confirmed at completion | L577 | Outbox + retry + "sync failed" (PD-01) |
| UA-12 | Lot numbers are known in advance per component | data `lot` | ⚠️ captured at weighing instead (FR-5) |

## 12. Questions the prototype cannot answer

| # | Question | Pin / lines | Where it's answered |
|---|---|---|---|
| Q-UI-1 | What does surplus compensation mean, and what if it can't be compensated? | Q25/Q26, L552 | **PD-05, STEP 24** (open) |
| Q-UI-2 | How does the app show **Target achieved** separately from Within range? | — | Design in Bolt UI (FR-12); visual not in the mockup |
| Q-UI-3 | What do sign-in, override PIN + reason, and lot / container entry look like? | Q33, Q28/29 | No mockup; design in construction per ux-guide |
| Q-UI-4 | What does each failure look like (scale offline, OrgaTEX down, printer down, scanner down)? The strip has no "down" state | L618–623 | FR-23/24 define the behaviour; visuals per ux-guide |
| Q-UI-5 | Where is the completed-batch summary list for later review? | — | PDF §6 Step 9 / FR-18; no mockup |
| Q-UI-6 | What does the bar range `max` come from? | L751 | **Proposed**: derive it from the band (e.g. target + tol × k) rather than store it; confirm in Bolt UI |
| Q-UI-7 | What happens to an in-progress reading when the operator switches component? | L722 | Mockup resets it; product: the reading is not recorded, re-tare required (FR-7) |
| Q-UI-8 | Does F1 mean Confirm on screen 2 and Print on screen 3 (the same key)? | L557, L584 | Keep the mockup's mapping; add keyboard handlers |
| Q-UI-9 | What happens to the next batch's state (QUEUED → WEIGHING) and to a quit batch in the queue? | L466–471 | FR-1, FR-14 (state shown); tag names from the mockup |
| Q-UI-10 | Data inconsistency: Blue RSPL has id `DYE-4471-B` but code `DYE-1180` | L678 | Seed data uses consistent ids; not a product question |
| Q-UI-11 | Real OrgaTEX fields, scale protocol, printer language | Q6, Q15, Q18 | Outside the lab; the dummies are training contracts |

## Gap summary (the mockup vs the approved requirements; the requirements win)
Target achieved class · fixed-point arithmetic · stable-reading guard · tare required · lot / container capture · real override (PIN + reason + OVR) · Quit ≠ Re-tare · computed scale selection · wired queue / print / next-batch actions · failure states · sign-in · keyboard handlers · summary review list. The surplus calculation is **not adopted** (PD-05).
