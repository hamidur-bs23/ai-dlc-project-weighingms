# WeighingMS 1.0.0: production runbook

The handoff package for one station or site server running Docker. The on-premise target is brief.md **A-11, unconfirmed by FCI**. Built from git tag `v1.0.0` (commit `586bf5f`).

> ⚠️ **Release 1.0.0 is not production-ready.** It installs and starts cleanly, but **nobody can sign in** in the `Production` environment (OPS-PROD-USERS, below). Use it to rehearse the installation; the first deployable release needs a user-provisioning change.

## Contents

| File | Purpose |
|---|---|
| `weighingms-api-1.0.0-linux-amd64.tar` | Api with the operator terminal (Angular build), for x86-64 servers |
| `weighingms-migrator-1.0.0-linux-amd64.tar` | Database migrations (EF Core bundle, runs once at every start) |
| `weighingms-simulators-1.0.0-linux-amd64.tar` | Dummy OrgaTEX, scales, scanner and printer (see "Dummy integrations") |
| `SHA256SUMS` | Checksums of the tarballs |
| `docker-compose.prod.yml` | The production stack. It only references images; it builds nothing |
| `production.env.template` | Settings to copy to `production.env` |

The arm64 tarballs in `dist/release/` (no suffix) are for Apple Silicon or ARM hosts. Use the `-linux-amd64` set on an ordinary x86-64 server (OPS-ARCH).

## Requirements

- Linux x86-64 with Docker Engine 24+ and the Compose plugin v2
- A TLS reverse proxy on the same host (for example Caddy or nginx) with a certificate for the station's hostname
- Postgres runs in the stack, on a named volume. The disk must be local to the station, so weighing continues during an OrgaTEX or network outage (A-11)

## Install or upgrade

```bash
sha256sum -c SHA256SUMS                       # every line must say OK
for f in weighingms-*-1.0.0-linux-amd64.tar; do docker load -i "$f"; done
docker image ls 'weighingms/*'                # weighingms/{api,migrator,simulators}:1.0.0

cp production.env.template production.env && chmod 600 production.env
# edit production.env: set POSTGRES_PASSWORD (openssl rand -hex 24), and check STATION_ID and STATION_TIME_ZONE

docker compose --env-file production.env -f docker-compose.prod.yml up -d --wait
docker compose --env-file production.env -f docker-compose.prod.yml ps
# expected: postgres, simulators and api healthy; migrator "Exited (0)"
```

On an upgrade, the migrator applies new migrations before the Api starts (`service_completed_successfully`). **Back up the database first** (see below).

## TLS reverse proxy (required)

The Api listens only on `127.0.0.1:5080`. The browser has to reach it over **HTTPS through a proxy on the same host**, because:

- its session and antiforgery cookies are `Secure` with the `__Host-` prefix, and
- antiforgery **rejects (500) any request it doesn't see as HTTPS**. The stack sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, so the Api trusts the proxy's `X-Forwarded-Proto: https` (OPS-FORWARDED-HEADERS). Never publish port 5080 on a public interface while that setting is on.

Example `Caddyfile`:

```caddy
station01.fci.local {
    tls /etc/ssl/station01.crt /etc/ssl/station01.key
    reverse_proxy 127.0.0.1:5080     # Caddy sends X-Forwarded-Proto and X-Forwarded-For itself
}
```

With nginx, set `proxy_set_header X-Forwarded-Proto $scheme;` and `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;`.

## Verify

```bash
curl -fsS http://127.0.0.1:5080/health/ready                       # 200 on the host
curl -s -o /dev/null -w '%{http_code}\n' https://station01.fci.local/api/antiforgery   # 204 through the proxy
```

Then open `https://station01.fci.local` in the station browser. The sign-in screen should appear. **In 1.0.0 every badge returns "Badge not recognised" (OPS-PROD-USERS).**

## Configuration

Set these as environment variables on the `api` service (`Section__Key`). The defaults come from `appsettings.json`.

| Setting | Default | Notes |
|---|---|---|
| `Station__StationId`, `Station__TimeZone` | `STATION 01`, `Asia/Dhaka` | From `production.env`. The time zone is PD-06 |
| `Weighing__ScaleSelection__ThresholdKg` | `3.0` | ≤ threshold → precision scale (inclusive) |
| `Weighing__ScaleSelection__Assignments__Precision` / `__HeavyDuty` | `scale-3kg` / `scale-32kg` | Scale IDs |
| `Tolerances__Entries__N__ChemicalId`, `__ScaleClass`, `__PlusMinus`, `__Unit` | *(none)* | **Fallback tolerances** for chemicals whose recipe has none. Without one, that component is **blocked** from weighing (decision TOL). The lab values exist only in Development; FCI must supply real ones |
| `WriteBack__MaxAttempts`, `__BaseDelay`, `__MaxDelay` | `5`, `5 s`, `5 min` | OrgaTEX retry; then "sync failed" with a manual Retry (PD-01) |
| `RateLimits__AuthPermitLimitPerMinute` | `10` | Sign-in attempts per minute |
| `Serilog__MinimumLevel__Default` | `Information` | Logs go to the container's stdout (`docker compose logs api`) |

## Dummy integrations

There is no real OrgaTEX, scale, scanner or printer integration yet. The `simulators` service provides training contracts, not the real OrgaTEX interface (ADR-015). In `Production`, its `/_sim/*` fault and scenario controls are off and it isn't reachable from outside the Compose network. Connecting the real devices and OrgaTEX is future work. Replacing `Integrations__Simulators__BaseUrl` by itself is not enough.

## Operations

| Task | Command |
|---|---|
| Logs | `docker compose -f docker-compose.prod.yml --env-file production.env logs -f api` |
| Back up | `docker compose … exec -T postgres sh -c 'pg_dump -U "$POSTGRES_USER" -Fc "$POSTGRES_DB"' > weighingms-$(date +%F).dump` |
| Restore | `docker compose … exec -T postgres sh -c 'pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists' < weighingms-YYYY-MM-DD.dump` |
| Unsynced OrgaTEX write-backs | `unsyncedWriteBacks` in `GET /api/devices/health` (signed in), also shown on the station status strip |
| Stop / start | `docker compose … stop` / `docker compose … up -d --wait` |

## Rollback

1. `docker compose … down`. **Do not use `--volumes`; that deletes the database.**
2. Restore the backup taken before the upgrade (migrations aren't reversed automatically).
3. Set `WMS_VERSION` to the previous version (its images must already be loaded) and run `up -d --wait`.

1.0.0 is the first release, so there is no earlier version to roll back to.

## Known blockers and limits (1.0.0)

| ID | Issue | Effect | Fix |
|---|---|---|---|
| **OPS-PROD-USERS** | Station users are seeded only in Development and Test; there is no other way to provision them | **Nobody can sign in.** Confirmed in the production rehearsal: `401 Auth.BadgeNotRecognised` | A Construction bolt: users from configuration or an admin path, then 1.0.1 |
| OPS-FORWARDED-HEADERS | The Api has no forwarded-headers middleware of its own | Behind TLS it would return 500 on every state-changing call | Mitigated by `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` in this stack; a code-level `ForwardedHeadersOptions` with known proxies would be stricter |
| OPS-MONITORING-DEFERRED | No metrics or tracing backend (observability bolt not built) | Only health endpoints and stdout logs | Plan the observability bolt |
| — | `/scalar` and other unknown non-API paths return the operator terminal (SPA fallback) | None: the Scalar UI is off outside Development | — |
| A-11 and the other `A-xx` items | The target server, the network and FCI's requirements are unconfirmed | — | Confirm with FCI |
