---
version: 1.0.0
recorded: 2026-11-12T20:10:00Z
status: deferred
decision: OPS-MONITORING-DEFERRED
---

# Monitoring: 1.0.0

**Status: deferred.** The observability stack planned in `standards/tech-stack.md` (OTel Collector, Prometheus, Tempo, Loki and Grafana in a Compose `observability` profile, plus the OTel Web SDK) was never added to the bolt plan and isn't built (OPS-MONITORING-DEFERRED). This file records what 1.0.0 provides today and what remains.

## What exists in 1.0.0

| Signal | Where | Notes |
|---|---|---|
| Liveness | Api `GET /health/live` | Process up, runs no checks |
| Readiness | Api `GET /health/ready` | **Postgres only** (`Program.cs:118`). It does not check the simulators, contrary to `system-architecture.md` ("Postgres + simulator"); see the follow-up list |
| Simulator health | `GET /api/scales` on :5090 | The simulator host has **no `/health/*` endpoints** (the standard says it has the same pair). Compose uses this contract endpoint as its probe |
| Container health | Compose healthchecks on all services; `restart: unless-stopped` in `deploy/docker-compose.prod.yml` | `docker compose ps` shows `healthy` or `unhealthy` |
| Device and integration state | `GET /api/devices/health` (signed in): scales, scanner, printer, OrgaTEX and `unsyncedWriteBacks` | The same data drives the operator's status strip |
| Logs | Serilog to the console (plain text, `WriteTo.Console()`); `docker compose logs api` | Default `Information`, framework noise at `Warning`. Not structured JSON |
| Traces and metrics | OpenTelemetry in the Api and the simulators: ASP.NET Core, HttpClient, Npgsql and runtime instrumentation; source and meter `WmsTelemetry`; custom histogram `wms.scale.read.duration` (s); `HealthyWeightPollFilter` drops healthy scale-poll spans | **Exported only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.** Not set in any environment yet, so no telemetry leaves the processes |

Because the apps only emit OTLP, adding a backend later is a configuration change for the apps (`OTEL_EXPORTER_OTLP_ENDPOINT`) plus new infrastructure.

## Suggested alert conditions (for when a backend exists)

| Alert | Signal | Why |
|---|---|---|
| Api not ready | `/health/ready` not 200 for more than 1 min | The station can't record weighings |
| OrgaTEX sync failed | write-back status `syncFailed` (summary, or `unsyncedWriteBacks` > 0 for more than 15 min) | Consumption isn't reaching OrgaTEX; someone must press Retry (PD-01) |
| Tickets piling up | batches in `printPending` for more than 30 min | Printer down, or operators skipping F1 (PD-02) |
| Device offline | a scale, scanner or printer not `ready` in `/api/devices/health` for more than 5 min | Weighing is blocked, or running on fallbacks |
| Scale read latency | p95 `wms.scale.read.duration` > 1 s | Matches the integration `AttemptTimeout` of 1 s |
| Disk | Postgres volume over 80 % | The station database is local (A-11) |

## Per-environment status

| Environment | Monitoring |
|---|---|
| dev | Console logs in the `npm run dev` terminal; health endpoints |
| staging (`weighingms-staging`) | Compose healthchecks; `docker compose logs`; no OTLP endpoint |
| prod (handoff) | Compose healthchecks, `restart: unless-stopped`, `docker compose logs`, and the runbook's verification and backup steps; no OTLP endpoint |

## Follow-up backlog (for Inception or Construction)

| ID | Item | Priority |
|---|---|---|
| OPS-PROD-USERS | Provision station users outside Development (configuration or an admin path). **Blocks any production deployment.** Then release 1.0.1 and run staging again in `Production` | **Blocker** |
| OPS-MONITORING-DEFERRED | Plan and build the observability bolt (Collector, Prometheus, Tempo, Loki, Grafana; OTel Web SDK; dashboards and the alerts above) | High |
| OPS-FORWARDED-HEADERS | Replace the `ASPNETCORE_FORWARDEDHEADERS_ENABLED` mitigation with code-level `ForwardedHeadersOptions` that names the known proxy | Medium |
| OPS-READY-SIMULATORS | Make `/health/ready` cover the simulator dependency, and add `/health/live` and `/health/ready` to the simulator host, as `system-architecture.md` specifies | Medium |
| OPS-LOG-JSON | Structured (JSON) console logs, so Loki or another aggregator can query fields | Low |
| E2E-CI-DEFERRED | There's no git remote and no CI. Add a remote, then a pipeline (build, format, lint, tests, E2E on the Compose stack, image build for amd64) | Medium |
