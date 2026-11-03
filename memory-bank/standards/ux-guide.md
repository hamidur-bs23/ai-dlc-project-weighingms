# UX Guide

## Overview
UI/UX standards for the WeighingMS operator terminal. **The supplied prototype `docs/customize-weighing-station.html` defines the visual experience and interaction patterns.** The app reproduces its functional experience in three views (1 · Select batch, 2 · Weigh, 3 · Confirm & print) using its design tokens, and is written from the approved requirements, never by copying the prototype's implementation. The detailed UI analysis (screens, components, states) happens in Inception (STEP 8).

## Design System / Component Library
**Our own component set on Angular CDK**, styled with the prototype's design tokens.

- **Tokens** live in `web/src/styles/_tokens.scss` as CSS custom properties with the **prototype's names**:
  - HMI surface: `--hmi-bezel`, `--hmi-bg`, `--hmi-panel`, `--hmi-panel-2`, `--hmi-line`, `--hmi-ink`, `--hmi-muted`, `--hmi-dim`
  - Status: `--ok` (#2fbf6b), `--warn` (#f0a726), `--over` (#e8503a), `--steel` (#5b8cc4)
  - Page chrome: `--page-bg`, `--page-panel`, `--page-ink`, `--page-muted`, `--page-line`, `--page-accent`, `--page-accent-soft` (light + dark set)
  - Type: `--mono`, `--sans`
- The prototype's **annotation pins** (`--pin`, `--pin-ink`, "hover a pin to read its question") are design-review chrome. They are **not** product UI and are not reproduced
- Core components (`web/src/app/shared/ui/`, selector prefix `wms-`): `wms-panel`, `wms-button` (primary/secondary/danger), `wms-status-pill`, `wms-weight-readout` (large mono digits), `wms-tolerance-gauge` (target band + live marker), `wms-progress` (completed / current / outstanding, PDF Step 8), `wms-component-list`, `wms-batch-table`, `wms-banner` (error/info with "Ref"), `wms-dialog` (CDK Dialog: supervisor override, quit confirmation), `wms-ticket` (print layout + barcode)
- CDK usage: `Dialog`, `A11yModule` (focus trap, live announcer), `Overlay`; no Angular Material, no Tailwind

## Styling Approach
- **SCSS + CSS custom properties**, component-scoped (`:host`, emulated encapsulation); global styles only in `src/styles/` (`_tokens`, `_mixins`, `_base`)
- Class naming: BEM-lite inside components (`weigh-panel__readout--over`); state via attributes or modifier classes driven by signals (`[class.is-over]="status() === 'over'"`)
- Mixins: `touch-target($min: 44px)`, `mono-readout($size)`, `focus-ring`, `status-colour($state)`
- No inline styles and no hard-coded colours or sizes in components. Use tokens and a spacing scale (`--space-1…6`, 4 px base)
- Typography: `--sans` for UI text, `--mono` for weights, codes, lots and batch IDs. The live weight readout is large (prototype ≈ 52 px) and must be readable from ~1 m

## Status & Feedback Semantics
| State | Colour token | Meaning | Confirm |
|---|---|---|---|
| Underweight (below lower bound) | `--warn` | Keep dispensing | Disabled |
| Within tolerance | `--ok` | Acceptable; may confirm | **Enabled** |
| Target achieved (≈ target) | `--ok` + emphasis | Distinct from "within" (PDF rule) | **Enabled** |
| Overweight (above upper bound) | `--over` | Warn that the quantity is excessive (PDF §6 Step 6); recovery policy *pending PD-05 (STEP 24)* | Disabled; success only via an explicit, authorised exception (PDF §6 Step 7) |
| Scale unstable / offline | `--hmi-dim` / `--over` banner | Reading not usable | Disabled |
| Confirmed | `--ok` check | Component recorded | — |

- Colour is **never the only signal**: each state also has a text label and an icon/shape (colour-blind safe)
- Tolerance boundaries are inclusive (e.g. 93.5 and 94.5 g are within). The UI shows the band `min–max` and the remaining quantity
- Errors use a `wms-banner` with an operator-readable, translated message plus "Ref <traceId8>"; no raw technical text

## Accessibility
- Target **WCAG 2.2 AA**: contrast ≥ 4.5:1 for text (verify `--hmi-muted`/`--hmi-dim` on dark panels), focus visible on all interactive elements
- **Touch targets ≥ 44 × 44 px** (gloved operators); primary actions (OK/Confirm, Re-tare, Quit) are larger and spaced to avoid mis-taps
- Fully operable by **keyboard and scanner**: scanner input never steals focus from dialogs or text/PIN fields (scanner mode *pending PD-03*); Enter confirms only when Confirm is enabled
- Live updates (weight, status) are announced politely via CDK `LiveAnnouncer` at status changes, not every reading
- Destructive actions (Quit with an unfinished batch) require confirmation
- Enforced by angular-eslint template a11y rules and `@axe-core/playwright` checks in E2E

## Responsive Strategy
- **Station terminal first**: landscape, desktop-class screen; layout designed for ≥ 1280 × 800, content max-width ~1180 px (prototype). It scales up to 1920 × 1080 without stretching readouts
- Below 1024 px wide: supported for supervisors viewing progress (stacked panels), not optimised for weighing
- No hover-only interactions (touch screens)
- Print: the ticket view has dedicated `@media print` styles (paper size, layout and configurability *pending PD-04*), black-on-white, barcode at scannable size

## Content, Numbers & Language
- All operator text via **Transloco** keys (see coding-standards → i18n); English in V1; concise imperative labels as in the prototype ("OK", "Re-tare", "Quit", "Supervisor override")
- Weights are shown with the **unit and the decimals of the active scale's resolution** (`WeightPipe`); never mix g and kg in one value
- Identifiers (batch, order, machine, shade, lot, container) are shown in mono and are copyable
- Dates and times are stored in UTC; the display/print time zone is *pending PD-06 (decide in STEP 7)*

## Decision Relationships
- Reusing the prototype's token names makes the visual comparison (STEP 28) direct, while the component implementation stays our own
- Status semantics map 1:1 to the domain's tolerance classification (Underweight / Within / Target / Overweight), so UI and tests share one vocabulary
- CDK provides accessible dialogs and focus management without a visual library that would fight the HMI look
