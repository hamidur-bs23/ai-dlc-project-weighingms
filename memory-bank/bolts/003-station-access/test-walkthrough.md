---
stage: test
bolt: 003-station-access
created: 2026-11-04T21:40:00Z
---

## Test Report: station-access (bolt 003)

### Summary

- **Tests**: 188/188 passed across the solution, 66 of them new, with the same result in two consecutive runs. The new tests are:

  | Project | Tests | Runs against |
  |---|---|---|
  | `WeighingMS.Domain.Tests` | 15 | — |
  | `WeighingMS.Application.Tests` | 22 | hand-written fakes |
  | `WeighingMS.Api.IntegrationTests` | 24 | real **PostgreSQL 17** (Testcontainers), HTTPS, cookies |
  | Architecture tests | 5 new rules | — |

- **Coverage** of hand-written lines (generated code and migrations excluded):

  | Project | Coverage |
  |---|---|
  | `WeighingMS.Domain` | **100%** (94/94) |
  | `WeighingMS.Application` | **100%** (118/118) |
  | `WeighingMS.Infrastructure` | 91.2% (114/125) |
  | `WeighingMS.Api` | 91.1% (236/259) |

  The coding standard asks for ≥ 90% on Domain and Application. The uncovered lines are development tooling only: the `dotnet ef` design-time factory, and the warning branches of development seeding.
- **Build**: 0 warnings, 0 errors. **Format**: clean.
- **Server time zone check**: the station-clock tests also pass with the process time zone set to `America/New_York`.

### Test Files

- [x] `tests/WeighingMS.Domain.Tests/StationUserTests.cs` - badges are normalised; operators have no PIN; a supervisor needs a hash; 4 failures don't lock; the 5th locks for exactly 5 minutes; a success clears the failures
- [x] `tests/WeighingMS.Domain.Tests/StepUpGrantTests.cs` - a grant carries the supervisor, action and expiry; only a supervisor can get one; it can be used once; it fails at and after expiry; the wrong action is refused and leaves the grant unused
- [x] `tests/WeighingMS.Domain.Tests/AuditEntryTests.cs` - entries are stored in UTC (a +06:00 input is converted); an actor is required; no public setters
- [x] `tests/WeighingMS.Application.Tests/StationAccessServiceTests.cs`, with fakes:
  - sign-in returns the session and audits; an unknown badge writes nothing; invalid badges → validation errors; a replaced session is audited; sign-out is audited;
  - step-up: correct PIN → grant; wrong PIN, operator badge or unknown badge → refused and audited;
  - lockout on the 5th wrong PIN, which also blocks the correct PIN, and is lifted after 5 minutes;
  - PIN and action validation; a grant consumed once, unknown or expired.
- [x] `tests/WeighingMS.Api.IntegrationTests/SessionTests.cs`:
  - sign-in returns 200 with a `__Host-` session cookie that is HttpOnly and Secure, plus `Cache-Control: no-store`;
  - an unknown badge → 401 `application/problem+json` with `type`, `errorCode` and `traceId`, and no cookie;
  - an invalid badge → 422 with field errors;
  - 401 without a session; the session lasts until sign-out;
  - a replaced session is audited with the right actor and subject;
  - no XSRF token → 400 `Request.AntiforgeryInvalid`; the XSRF token is renewed on sign-in;
  - sign-in and sign-out audited at the exact UTC time.
- [x] `tests/WeighingMS.Api.IntegrationTests/StepUpTests.cs`:
  - a grant is returned, and the database holds only a hash with no trace of the PIN;
  - wrong PIN → 403 and audited, with no grant; an operator badge → 403 `Auth.SupervisorRequired`;
  - the 5th wrong PIN → 429 `Auth.LockedOut`, **and the lock survives an Api restart**, then lifts after 5 minutes;
  - 422 field errors; 401 without a session;
  - a grant consumed once through the service is rejected the second time;
  - the rate limiter → 429 `Auth.TooManyAttempts`.
- [x] `tests/WeighingMS.Api.IntegrationTests/StationClockTests.cs` - `/api/station` returns UTC 21:48:22Z and plant time `2026-11-04T03:48:22+06:00`; the FR-22 example converts correctly for Asia/Dhaka, Europe/Berlin and UTC; an unknown zone fails at start-up
- [x] `tests/WeighingMS.Api.IntegrationTests/PersistenceTests.cs` - the migration applies with nothing pending; audit entries survive a restart; deleting an audit entry throws; `/health/ready` checks the database and `/health/live` answers; seeding is idempotent
- [x] `tests/WeighingMS.Api.IntegrationTests/ApiFixture.cs` - a Postgres 17 container per run, migrated once; Respawn and re-seeding before each test; a fresh `FakeTimeProvider` per test; an HTTPS client with a cookie jar that sends the XSRF header
- [x] `tests/WeighingMS.Architecture.Tests/LayerDependencyTests.cs` (extended) - Domain doesn't depend on FluentValidation or Npgsql; Application doesn't depend on EF Core, Npgsql or ASP.NET Core

### Acceptance Criteria Validation

- ✅ **A seeded badge signs in and the session shows name and shift; an unknown badge → 401 with no session** (`SessionTests`, `StationAccessServiceTests`).
- ✅ **401 without a session; sign-out → 204; a new sign-in replaces the session and is audited** (`SessionTests`).
- ✅ **Supervisor badge + PIN gives a single-use grant for the action; a wrong PIN or operator badge is refused, changes nothing and is audited** (`StepUpTests`, `StationAccessServiceTests`).
- ✅ **5 wrong PINs → 429 `Auth.LockedOut`; a grant can't be reused, used for another action or used after expiry** (`StepUpGrantTests`, `StepUpTests`).
- ✅ **Audit entries have type, actor, subject, reason and UTC time; they can't be modified or deleted** (`AuditEntryTests`, `PersistenceTests`).
- ✅ **2026-11-03T21:48:22Z shows as 2026-11-04 03:48 in Asia/Dhaka, whatever the server zone; timestamps are stored in UTC** (`StationClockTests`, run again with `TZ=America/New_York`; `SessionTests`).
- ✅ **State-changing requests without XSRF → 400** (`SessionTests`).
- ✅ **PINs are stored only as hashes and never appear in logs or responses** (`StepUpTests` checks the database; the Implement-stage log check found 0 occurrences of the PIN).
- ✅ **Errors are ProblemDetails with an `Area.Reason` type and a `traceId`** (`SessionTests`).
- ✅ **Build has 0 warnings; format clean; architecture rules hold.**

### Issues Found (all fixed in this stage)

1. **A badge with surrounding spaces was rejected.** The validator checked `" op-1001 "` before trimming, which contradicts story 001's edge case ("trimmed"). The format is now checked on the trimmed value (`BadgeFormat`).
2. **Error responses had `Content-Type: application/json`** instead of `application/problem+json`. The cause was `[Produces("application/json")]` on the base controller, which is now removed.
3. **A runtime version mismatch**: Infrastructure compiled against `Microsoft.EntityFrameworkCore.Relational` 10.0.14 (pulled in through the design package), but the test host loaded the older version Npgsql depends on. Relational is now pinned to 10.0.14.
4. **Test isolation** (test code only): the integration tests now run one at a time against the shared database, and each gets a fresh fake clock.

### Notes

- Docker (OrbStack) was running, so the Testcontainers tests ran for real against `postgres:17`. Without Docker, `WeighingMS.Api.IntegrationTests` cannot run.
- The integration suite takes about 6 s, plus the container start on the first run.
- The development database used for the Implement-stage smoke test is separate from the test containers.
