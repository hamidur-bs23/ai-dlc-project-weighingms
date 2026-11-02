# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

WeighingMS is an assisted dyestuff weighing station for the client FCI. It sits between the operator and the OrgaTEX MES. The repo is currently **pre-code**: it holds only source material. It is being built as a hands-on lab that follows `workbook.md` (the specs.md AI-DLC flow), so work moves through that file's steps in order.

There is no stack yet and no build, lint or test commands. The tech stack has to be proposed and approved before Inception (see "Define the Tech Stack Standard" in `workbook.md`). Once it is approved, add the real commands here, including how to run a single test and the Playwright suite.

## Source material (read before generating artifacts)

- `docs/brief.md`: despite the filename, this is a draft Business Requirements Document (v1.1). Requirements have IDs like `FR-xx` and `DR-xx` and cite assumptions `A-01`…`A-15` (the register is in §12). **FCI has confirmed none of it.** Keep the assumption citations when deriving requirements, and don't treat assumptions as facts.
- `docs/customize-weighing-station.html`: the UI/UX prototype ("Operator Terminal Mockup"). It has three views: 1 · Select batch, 2 · Weigh, 3 · Confirm & print. The built app must reproduce its functional experience. Read its embedded JS to find states and rules, but **do not copy its implementation**.
- `docs/project-requirement.pdf`: the project requirement ("Dyeing Chemical Precision Weighing System"). Its rules are summarised under "Project requirement rules" below.
- `workbook.md`: the step-by-step process (STEP 0–35) and the evidence each step expects.

How the sources rank (from the PDF):
1. The product brief and requirement define the business intent.
2. The HTML mockup defines the visual experience and interaction patterns.
3. The working app must connect the two through real business logic, mock integrations and a complete workflow.

**The top acceptance criterion is that the whole workflow works end to end**, not that the screens look like the mockup.

## AI-DLC process rules (from workbook.md)

- specs.md is installed with `npx specsmd@latest install` (AI-DLC flow). Agents: `/specsmd-master-agent`, `/specsmd-inception-agent`, `/specsmd-construction-agent`, `/specsmd-operations-agent`. Dashboard: `npx specsmd@latest dashboard`.
- Artifacts live in `memory-bank/`:
  - `standards/`
  - `intents/<intent>/`, containing `requirements.md`, `system-context.md` and `units/`
  - `bolts/`
  - `operations/`

  Project state is derived from these files.
- Order of work: Intent → Requirements → UI analysis → System Context → Units → Stories → Bolt plan → Construction, one Bolt at a time.
- **Stop at every human validation checkpoint** and wait for explicit approval (e.g. `APPROVE INTENT`, `APPROVE PLAN`). Never build the whole app at once. Only execute the next approved Bolt.
- Never invent requirements silently: ask, record the question, decide, then continue. Never invent real OrgaTEX API contracts.
- Bolt types:
  - **Simple** (Plan → Implement → Test) for UI, integration and utility work.
  - **DDD** (Model → Design → ADR → Implement → Test) for domain-heavy work.

## Architecture constraints

The real OrgaTEX, scales, scanner and printer are not available. **The dummy APIs are first-class product code.** The UI must consume them over HTTP and must never hold hard-coded scenario data (e.g. no `const batches = [...]` in components). They are training contracts, not claims about the real OrgaTEX interface.

Minimum dummy endpoints:
- OrgaTEX: `GET /api/batches`, `GET /api/batches/:batchId`, `GET /api/batches/:batchId/recipe`, `POST /api/batches/:batchId/consumption`
- Scale: `GET /api/scales`, `GET /api/scales/:scaleId/status`, `GET /api/scales/:scaleId/weight`, `POST /api/scales/:scaleId/tare`
- Printer: `POST /api/tickets`
- Scanner: also needs a dummy; its contract hasn't been defined yet

## Project requirement rules (docs/project-requirement.pdf)

- **Scope:** this is a precision-weighing *management* system, not automated dispensing. Operators dispense by hand; the app guides and monitors them.
- **Automatic scale selection:**
  - Required quantity ≤ 3 kg (exactly 3 kg included) → Precision Scale.
  - Required quantity > 3 kg → Heavy-Duty Scale.
  - The operator doesn't choose the scale in the normal flow.
  - The threshold and scale assignments must be configurable, not hard-coded.
- **Tolerance:** must be configurable per chemical and scale; there is no universal tolerance.
  - Classify readings as Underweight, Within range or Overweight. Treat "target achieved" as distinct from just being within range.
  - Example: 1.000 kg ± 0.010 kg gives 0.980 → Underweight, 0.995 → Within, 1.000 → Target, 1.008 → Within, 1.020 → Overweight.
- **Recording failures:** a failed weighing must never be recorded as successful unless an authorised exception is explicitly granted (the supervisor override in the mockup).
- **Weighing record:** each record holds the job/batch ref, chemical id and name, required quantity, actual quantity, scale, result and confirmation timestamp. The job → recipe → chemical → record relationship must be preserved, and completed jobs must stay reviewable through a summary.
- **Simulated scale:** the weighing simulator must support these scenarios:
  - Weight rising gradually toward the target
  - Weight staying below range
  - Weight entering range
  - Weight overshooting
  - Readings appropriate to the selected scale's precision
- **Replaceable mocks:** keep mock services behind clear integration boundaries, separate from business logic, so real APIs and devices can replace them later.
- **Priorities:**
  1. The core workflow
  2. Visibility: progress, status indicators, records, summary
  3. Integration readiness

## Training scenario and key business rules

- The seed batch is `B-24187`, with order `SO-9931`, machine `JET-04` and shade `Teal 4C`. The batch queue also shows B-24188, B-24190 and B-24191.
- The recipe has five components, weighed in this order: Remazol Brill. Blue RSPL → Remazol Red RB → Remazol Yellow RGB → Sodium Sulphate → Soda Ash.
  - Each component has its own unit and scale.
  - For example, Yellow RGB uses the 3 kg precision scale and Sodium Sulphate uses the 32 kg floor scale. These are the prototype's names for the PDF's Precision and Heavy-Duty scales.
- Tolerance is inclusive. Example: Yellow RGB has target 94.0 g ± 0.5 g.
  - 93.5, 94.0 and 94.5 can be confirmed.
  - 93.4 and 94.6 cannot.
  - The Confirm button is enabled only when the weight is within tolerance.
- A confirmed weighing records actualWeight, operator, component, lot, container, tare, scale and timestamp.
- A batch completes only when every component is confirmed. Completion does two things:
  - POSTs the consumption to OrgaTEX.
  - Generates a ticket with batch, order, machine, shade, date, operator, components, target and actual weights, and a barcode.
- Overshoot handling is an **open business decision**. Present 2–3 options and wait for a human to choose. Don't adopt the prototype's calculation.
- These failure scenarios must be handled and tested:
  - Scale, OrgaTEX or printer unavailable
  - Invalid, under-tolerance or over-tolerance weight
  - Re-tare
  - Quit
  - Incomplete batch

## Evidence

Each workbook step expects evidence files (`STEP-NN-*.png/.md/.txt`) under `evidence/`. `.claudeignore` excludes `evidence/` and media files, so don't expect to read them.
