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

### linux/amd64 set (added at Checkpoint 3, resolves OPS-ARCH)

Built from tag `v1.0.0` with `docker buildx build --builder wms-release` (driver `docker-container`, so it can export) `--platform linux/amd64`, exported straight to tarballs. Each image carries the tags `:1.0.0` and `:1.0.0-586bf5f` and the OCI labels `org.opencontainers.image.version=1.0.0` and `org.opencontainers.image.revision=586bf5f…`. On this machine they are tagged `:1.0.0-linux-amd64`, so the arm64 `:1.0.0` tags stay unchanged.

| Image | Image ID | Build time | Tarball |
|---|---|---|---|
| `weighingms/api` | `sha256:6023569278f9…` | 969 s (mostly downloading the amd64 base images) | `weighingms-api-1.0.0-linux-amd64.tar.gz` (105 MB) |
| `weighingms/migrator` | `sha256:810719e4194b…` | 3 s (cache) | `weighingms-migrator-1.0.0-linux-amd64.tar.gz` (110 MB) |
| `weighingms/simulators` | `sha256:b7c558b8d730…` | 18 s | `weighingms-simulators-1.0.0-linux-amd64.tar.gz` (102 MB) |

```text
4b69d21b28a47a3bba788bf07055ace79b7bcb6a6b0a36e00c45633d06c417bd  weighingms-api-1.0.0-linux-amd64.tar.gz
a2a4ca8daaa078a925bc6a423f2d73bd6556f495cc0b0ccc85568e85c8b22fb1  weighingms-migrator-1.0.0-linux-amd64.tar.gz
7bdb6314c5ef44bcb9ff55bd4319c316c67fdbf36c3ebd29c1c56b0ec17f479d  weighingms-simulators-1.0.0-linux-amd64.tar.gz
```

**Checks:** each tarball's own config reads `linux/amd64` with the expected tags and labels. A boot test followed the runbook (`docker load`, then `deploy/docker-compose.prod.yml` in `Production`, under emulation): containers report `x86_64`, healthy in 24 s, migrator exited 0, 7 migrations applied, `health/ready` 200, antiforgery through the proxy 204, web served.

**The full package** in `dist/release/` (gitignored) holds both image sets, `SHA256SUMS` covering all six tarballs, and copies of `deploy/docker-compose.prod.yml`, `production.env.template` and `README.md`.

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
| OPS-ARCH | ~~The images are linux/arm64 only~~ **Resolved:** a linux/amd64 set was built and boot-tested (above). | — |
| OPS-PROD-USERS | Station users are seeded only in `Development` and `Test` (`Program.cs:123`), and the app has no other way to provision users. In `Production`, nobody can sign in. | Staging and production configuration. |
| OPS-SECURE-COOKIES | Outside `Development`, the session cookies are `Secure` (`Program.cs:63`). Browsers accept them on `localhost`, but a station server reached by hostname needs HTTPS. | Production. |
| OPS-MONITORING-DEFERRED | The observability bolt (OTel Collector, Prometheus, Tempo, Loki, Grafana; `tech-stack.md`) was never planned, and there's no `observability/` folder. | Checkpoint 4. |
| E2E-CI-DEFERRED | There's no CI pipeline, and no git remote to run one. | Every environment. |
