---
version: 1.0.0
commit: 586bf5f
tag: v1.0.0
built: 2026-11-12T19:34:00Z
status: success
scope: whole-system
units:
  - 001-station-simulators
  - 002-station-access
  - 003-batch-recipe
  - 004-device-integration
  - 005-weighing-station
  - 006-consumption-writeback
  - 007-ticket-printing
  - 008-assisted-weighing-station-ui
---

# Build: 1.0.0

The first release after intent `001-assisted-weighing-station` completed (bolts 001–014, 56 stories, workbook STEPs 0–35).

**Scope (OPS-SCOPE-SYSTEM):** the eight units ship together as one modular monolith (the Api, with the web build inside) plus the separate simulator host. The release is built and deployed as one whole system, not unit by unit. This record covers all eight units.

## Artifacts

All three images were built from `docker-compose.yml` (profile `full`) with `WMS_VERSION=1.0.0`. Each also carries the tag `1.0.0-586bf5f`.

| Image | Dockerfile target | Image ID | Size | Platform |
|---|---|---|---|---|
| `weighingms/api:1.0.0` | `src/WeighingMS.Api/Dockerfile` → `api` (ASP.NET Core Api serving the Angular build from `wwwroot`) | `sha256:4ea6093b7d98…` | 284 MB | linux/arm64 |
| `weighingms/migrator:1.0.0` | `src/WeighingMS.Api/Dockerfile` → `migrator` (EF Core migrations bundle) | `sha256:73f4b56a11e2…` | 299 MB | linux/arm64 |
| `weighingms/simulators:1.0.0` | `src/WeighingMS.Simulators.Host/Dockerfile` (dummy OrgaTEX, scales, scanner, printer) | `sha256:725670276a82…` | 273 MB | linux/arm64 |
| `postgres:17` | stock image, not built | — | — | — |

**Registry:** there isn't one. The images are local to the build machine. For the production handoff they are exported with `docker save` to `dist/release/` (gitignored):

```text
4bfc8803a236fb2f78111f10d5406105a19b3c2d4fb5d1140d03af7abbf463b2  weighingms-api-1.0.0.tar.gz         (99 MB)
ba2808c11b0e2e544306f7e14cb47792cc97db92321c043a0ad8853eec37ecfd  weighingms-migrator-1.0.0.tar.gz    (103 MB)
5d59aab6f41b157dadcccaea72a471c97ba54c8d82ed010e7af8b3230aed5831  weighingms-simulators-1.0.0.tar.gz  (95 MB)
```

**The cache was reused but the images match the source.** The build took 61 s. The web stage re-ran (`npm ci`, `ng build`) because `package.json` changed. The app doesn't embed the version, so its output was byte-identical, and BuildKit kept the earlier image IDs, which still show a creation time of 2026-11-11. The last source change (`f61b38d`, 2026-11-11 00:39) predates those images.

## Pre-build verification (commit `b7c6a24`, before the version bump)

| Check | Command | Result |
|---|---|---|
| Backend build, warnings as errors | `dotnet build WeighingMS.sln` | ✅ |
| Format | `dotnet format WeighingMS.sln --verify-no-changes` | ✅ |
| Frontend lint | `npm run lint` | ✅ |
| Web unit tests | `npm run test:web` | ✅ 205 / 205 |
| Backend tests (incl. Testcontainers PostgreSQL) | `dotnet test --solution WeighingMS.sln` | ✅ 717 / 717 |
| E2E | `npm run e2e` | at the staging step, against these images |

The version-bump commit `586bf5f` changes only `version` fields in the package files and adds `image:` names in `docker-compose.yml`.

## Build environment

- Host: macOS 14 (Darwin 23.6.0), Apple Silicon
- Docker 27.5.0, Compose 2 (`083f676`), BuildKit
- Base images: `node:26.13.0-bookworm-slim` (web), `mcr.microsoft.com/dotnet/sdk:10.0` (build), `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime)
- .NET SDK 10.0.101, Node 26.13.0, npm 12.2.1

## Known issues carried forward

| ID | Issue | Affects |
|---|---|---|
| OPS-ARCH | The images are **linux/arm64 only**, because they were built on Apple Silicon. A typical x86-64 station server can't run them. | Production. They need an amd64 build (`docker buildx build --platform linux/amd64`, or a build on an amd64 host). |
| OPS-PROD-USERS | Station users are seeded only in `Development` and `Test` (`Program.cs:123`), and the app has no other way to provision users. In `Production`, nobody can sign in. | Staging and production configuration. |
| OPS-SECURE-COOKIES | Outside `Development`, the session cookies are `Secure` (`Program.cs:63`). Browsers accept them on `localhost`, but a station server reached by hostname needs HTTPS. | Production. |
| OPS-MONITORING-DEFERRED | The observability bolt (OTel Collector, Prometheus, Tempo, Loki, Grafana; `tech-stack.md`) was never planned, and there's no `observability/` folder. | Checkpoint 4. |
| E2E-CI-DEFERRED | There's no CI pipeline, and no git remote to run one. | Every environment. |
