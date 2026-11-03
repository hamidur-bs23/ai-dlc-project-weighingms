# Data Stack

## Overview
WeighingMS stores its own operational data in **PostgreSQL 17**: weighing sessions, confirmed weighing records, overrides, consumption write-back state (outbox if confirmed by PD-01), tickets and audit. It accesses that data with **EF Core (Npgsql)** and code-first migrations. The dummy external systems own their own data inside `WeighingMS.Simulators.Host`. WeighingMS never reads the simulators' storage directly; it only uses their HTTP contracts.

## Database
**PostgreSQL 17**, run from the `postgres:17` image via Docker Compose (named volume `pgdata`).

- Development uses `docker compose up -d postgres`. The demo, E2E and CI use `--profile full`. Integration tests start a throwaway instance with **Testcontainers**, so each test run is isolated.
- Rationale: production-like, the same engine in development, tests and CI, and strong transactional guarantees for the weighing records (a failed weighing must never be recorded as successful). It also works with the `postgres-inspector` MCP for inspection.
- Target production (assumption **A-11**): a local Postgres on the station or site server, so weighing continues during OrgaTEX or network outages. Backup and retention policy is deferred to Operations (the brief lists backup as an open item, l.120).

### Data ownership
| Data | Owner | Storage |
|---|---|---|
| Batch queue, recipe, components, tolerances, dyestuff master | **OrgaTEX** (Dummy OrgaTEX in the simulator) | Simulator: JSON seed file loaded into memory, with a `POST /_sim/reset` endpoint *(proposed)* |
| Received consumption write-backs, printed tickets | Dummy OrgaTEX / Dummy Printer | Simulator in memory, viewable via `/_sim/*` inspection endpoints for evidence (STEP-26/27) *(proposed)* |
| Weighing sessions, confirmed records (actual, operator, component, lot, container, tare, scale, timestamp), overrides with reason, write-back state (outbox pending PD-01), ticket references, audit log | **WeighingMS** | PostgreSQL |
| Operators (badge, role, PIN hash), configuration (scale threshold, scale assignments, tolerances where locally configurable) | **WeighingMS** | PostgreSQL (seeded) |

## ORM / Database Client
**EF Core 10** with **Npgsql.EntityFrameworkCore.PostgreSQL**.

| Concern | Convention |
|---|---|
| Modelling | Code-first. `IEntityTypeConfiguration<T>` per aggregate in `WeighingMS.Infrastructure/Persistence/Configurations`. Domain entities stay persistence-ignorant (no attributes) |
| Naming | **snake_case** tables and columns via `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention()`) |
| Keys | `uuid` primary keys generated with **`Guid.CreateVersion7()`** (time-ordered). External references (`B-24187`, `SO-9931`) are stored as text natural keys with unique indexes |
| Quantities | `numeric(12,4)`, mapped to `decimal`, **never float/double**. The unit is stored explicitly (`g`/`kg`) next to the quantity. The canonical unit is decided in Requirements |
| Timestamps | `timestamptz`, always UTC, produced by `TimeProvider`. Display/print time zone *pending PD-06* |
| Enums | Stored as text (`HasConversion<string>()`) for readability and safe reordering |
| Concurrency | Optimistic concurrency with Postgres `xmin` on aggregates that change during weighing |
| Migrations | `dotnet ef migrations add <Name>` in Infrastructure. Applied by a **migrator** (EF migrations bundle) in the Compose `full` profile and by the integration-test fixture; **not** at API startup |
| Seeding | Reference data (operators, configuration) via an idempotent seeder. Scenario data (B-24187 …) lives in the **simulator seed**, not the WeighingMS database |
| Reads vs writes | EF Core tracking for commands; `AsNoTracking()` projections for queries and summaries (no Dapper) |
| Write-back | *Proposed default, pending PD-01 (decide in STEP 7):* **transactional outbox** table: the consumption POST to OrgaTEX is recorded in the same transaction as batch completion and delivered with retry, so it survives outages; backlog exposed as `wms.outbox.pending`. If PD-01 selects "stop the station", the outbox is not built |

## Decision Relationships
- **EF Core + Npgsql** fits the .NET 10 / Clean architecture stack. The configuration classes keep the Domain project free of EF Core.
- **Testcontainers + Respawn** run the integration tests against real Postgres 17 and reset it quickly between tests.
- **Data owned by the simulator** keeps the boundary honest: WeighingMS sees OrgaTEX only through HTTP, exactly as it would see the real system.
- **`TimeProvider` + `timestamptz`** give an auditable, ordered weighing history; the outbox (if confirmed by PD-01) adds outage tolerance.
- **`numeric` quantities with explicit units** avoid floating-point errors at tolerance boundaries (e.g. 93.5 g / 94.5 g must compare exactly).
