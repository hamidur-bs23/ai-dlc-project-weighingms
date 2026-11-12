# WeighingMS

An assisted dyestuff weighing station for the client FCI. It sits between the operator and the OrgaTEX MES. Operators dispense chemicals by hand, and WeighingMS guides and checks them through the whole job:

1. Pick a batch from the queue.
2. Weigh each recipe component on the scale chosen automatically (precision ≤ 3 kg, heavy-duty above).
3. Confirm only within tolerance.
4. Record lot, container, tare, operator and time.
5. Post the actual consumption to OrgaTEX and print a ticket.

It was built as a hands-on lab following [`workbook.md`](workbook.md) with the specs.md AI-DLC flow (Inception → Construction → Operations). [`FINAL-EVIDENCE.md`](FINAL-EVIDENCE.md) maps every requirement to the UI, API, code, tests and evidence.

> The real OrgaTEX, scales, scanner and printer are not available. They are replaced by **dummy APIs** in a separate process (`WeighingMS.Simulators.Host`), and the app talks to them over HTTP. These are training contracts, not claims about the real OrgaTEX interface.

## Architecture

```text
Browser (Angular operator terminal)
   │  /api  (cookie auth, antiforgery)
   ▼
WeighingMS.Api  ──HTTP──▶  WeighingMS.Simulators.Host   (dummy OrgaTEX, scales, scanner, printer)
   │
   ▼
PostgreSQL 17
```

| Part | Stack |
|---|---|
| Backend | ASP.NET Core on .NET 10 (Controllers), a Clean modular monolith: `WeighingMS.Domain`, `.Application`, `.Infrastructure`, `.Api`. ErrorOr + ProblemDetails, FluentValidation, Mapster, Serilog, OpenTelemetry, EF Core |
| Frontend | Angular (standalone, signals, zoneless), NgRx SignalStore, SCSS with the prototype's tokens, Angular CDK, Transloco (`web/`) |
| Dummy integrations | `src/WeighingMS.Simulators.Host`, seeded from `Seed/seed.json` (batch B-24187 · SO-9931 · JET-04 · Teal 4C) |
| Tests | xUnit v3 + Shouldly + Testcontainers, Vitest + Testing Library, Playwright |

Standards and every architecture decision are in [`memory-bank/standards/`](memory-bank/standards/) (index: `decision-index.md`).

## Quick start: the whole app in containers

Needs Docker with Compose v2.

```bash
cp .env.example .env            # set POSTGRES_PASSWORD
docker compose --profile full up --wait --build
```

Open <http://localhost:5080> and sign in with badge **`OP-1001`** (operator R. Karim). The supervisor `SV-2001` and its PIN are in `src/WeighingMS.Api/appsettings.Development.json`; this stack runs in the Development environment, where the lab users are seeded.

## Local development

Needs Node **26.13.0** (`.nvmrc`; use nvm) and the .NET SDK **10.0.101** (`global.json`).

```bash
npm ci
docker compose up -d postgres     # host port 5433, needs .env
npm run db:update                 # apply migrations
npm run dev                       # simulators :5090, Api :5080, web :4200 (proxies /api)
```

| Task | Command |
|---|---|
| Backend build (warnings are errors) | `npm run build:backend` |
| Backend tests (unit, integration with Testcontainers, architecture) | `npm run test:backend` |
| Format check | `npm run format:backend` |
| Frontend lint | `npm run lint` |
| Frontend unit tests | `npm run test:web` |
| E2E (builds and starts its own isolated stack on :5280/:5290/:5434, then removes it) | `npm run e2e` |
| Regenerate the OpenAPI document and Angular API types | `npm run api:types` |
| Add a migration | `npm run db:migrations:add -- <Name>` |
| Validate the AI-DLC memory bank | `npm run specsmd:validate` · `npm run specsmd:integrity` |

The simulators' Scalar UI is at <http://localhost:5090/scalar> in Development. Faults and scenarios are driven with `/_sim/*`, for example `POST /_sim/faults {"target":"orgatex","mode":"unavailable"}` or `POST /_sim/scales/{scaleId}/scenario`. The full list is in [`CLAUDE.md`](CLAUDE.md).

## Business rules at a glance

- **Scale selection:** required quantity ≤ 3 kg (inclusive) → precision scale, otherwise the heavy-duty scale. Configurable in `Weighing:ScaleSelection`.
- **Tolerance:** inclusive, per chemical and scale. For example, Yellow RGB is 94.0 g ± 0.5 g, so 93.5–94.5 g can be confirmed and 93.4 g or 94.6 g cannot. "Target achieved" is distinct from "within range".
- **Overshoot (PD-05):** an overweight stays unconfirmed until it is back in range, quit and re-weighed, or overridden by a second supervisor. There is no recipe recalculation.
- **Completion:** only when every component is confirmed. Then the consumption is written back to OrgaTEX (store-and-forward with retry, PD-01) and the ticket is composed. Printing is always manual (F1).
- **Failures:** scale, OrgaTEX, printer or scanner unavailable, invalid, under- or over-tolerance weights, re-tare, quit and incomplete batch are all handled and tested (`evidence/STEP-30-failure-tests.md`).

## Repository map

| Path | Contents |
|---|---|
| `docs/` | Sources: product brief (`brief.md`, a draft BRD), UI/UX prototype (`customize-weighing-station.html`), project requirement (`project-requirement.pdf` / `.md`) |
| `memory-bank/` | AI-DLC artifacts: `standards/`, `intents/` (requirements, system context, units, stories), `bolts/`, `operations/` |
| `src/`, `tests/` | Backend and its tests |
| `web/` | Angular operator terminal |
| `playwright/` | E2E suite (`tests/`) and the workbook evidence scripts (`evidence/`) |
| `evidence/` | Workbook evidence `STEP-00` … `STEP-35` |
| `deploy/` | Production handoff: image-only Compose file, env template, runbook |
| `FINAL-EVIDENCE.md` | Final evidence matrix (workbook STEP 34) and the 34-section package map; `npm run evidence:package` builds the zip |

## Status

- **Release 1.0.0** (`v1.0.0`): intent 001 is complete (8 units, 56 stories, 14 bolts), and workbook STEPs 0–35 are done.
- **Tests:** backend 717, web 205 and E2E 15, all passing.
- **Operations:** dev and staging are deployed and verified. Production is a handoff package (`deploy/README.md`) and is **not production-ready**: in the `Production` environment no station users exist (OPS-PROD-USERS). Intent `002-production-readiness` addresses this and is in Inception.
- **Unconfirmed sources:** FCI has confirmed none of the source material. Requirements derived from `brief.md` cite its assumptions (`A-01` … `A-15`) and stay unconfirmed.
