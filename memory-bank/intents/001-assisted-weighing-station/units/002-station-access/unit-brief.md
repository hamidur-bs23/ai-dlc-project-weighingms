---
unit: 002-station-access
intent: 001-assisted-weighing-station
phase: inception
status: stories-defined
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-11-04T15:00:00Z
updated: 2026-11-04T15:00:00Z
---

# Unit Brief: Station Access

## Purpose

Identify who is at the station and authorise privileged actions. Keep the immutable audit trail. Provide the plant clock and time zone.

## Scope

### In Scope
- Badge sign-in (typed badge ID) → cookie session; roles Operator and Supervisor (ADR-009)
- Supervisor badge + PIN step-up for each privileged action; PIN hashed; rate limiting and lockout
- Append-only audit records: overrides, quits, re-tares, reprints, sync failures
- `TimeProvider`: UTC storage; display and print in the configured IANA zone, default `Asia/Dhaka` (FR-22)
- Lab seed users (dev seed config only)

### Out of Scope
- Deciding *when* an override is allowed (005)
- Real identity provider integration (not confirmed)
- Badge scanning (PD-03: not in V1)

---

## Assigned Requirements

| FR | Requirement | Priority |
|---|---|---|
| FR-22 | Time zone (UTC stored, configured IANA zone displayed) | Must |

Full acceptance criteria: `../../requirements.md`. Unit decomposition: `../../units.md`.

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|---|---|---|
| StationUser | Operator or supervisor | badgeId, name, role, pinHash |
| Session | Signed-in operator at the station | userId, stationId, shift, startedAt |
| StepUpGrant | One-time supervisor authorisation | supervisorId, action, issuedAt |
| AuditEntry | Immutable audit record | type, actor, subject, reason, atUtc |

### Key Operations
| Operation | Description | Inputs | Outputs |
|---|---|---|---|
| Sign in / out | Start or end an operator session | badgeId | Session |
| Step up | Verify supervisor badge + PIN for one action | badgeId, PIN, action | StepUpGrant or error |
| Write audit | Append an audit entry | entry | — |
| Format plant time | Convert UTC to the plant zone | instant | local date/time |

---

## Story Summary

- **Total Stories**: 4
- **Must Have**: 3
- **Should Have**: 1
- **Could Have**: 0

### Stories

- [ ] **001-operator-badge-sign-in**: Operator badge sign-in and session - Must - Planned
- [ ] **002-supervisor-pin-step-up**: Supervisor badge + PIN step-up for one action - Must - Planned
- [ ] **003-immutable-audit-trail**: Immutable audit trail - Should - Planned
- [ ] **004-plant-time-zone**: Plant time zone for display and print - Must - Planned

---

## Dependencies

### Depends On
None.

### Depended By
| Unit | Reason |
|---|---|
| 005-weighing-station | Operator identity, step-up, audit, clock |
| 007-ticket-printing | Plant time zone, reprint audit |
| 008-assisted-weighing-station-ui | Sign-in, session |

### External Dependencies
None (training substitutes only).

---

## Technical Context

### Suggested Technology
ASP.NET Core cookie auth, policy-based authorization (`RequireOperator`, `RequireSupervisorStepUp`), antiforgery, rate limiter; EF Core / PostgreSQL (system-architecture Security Patterns).

### Integration Points
| Integration | Type | Protocol |
|---|---|---|
| 008 UI | API | REST/JSON |

### Data Storage
| Data | Type | Volume | Retention |
|---|---|---|---|
| Users, audit | SQL (PostgreSQL) | Small | Audit kept indefinitely |

---

## Constraints

- No credentials in documents or committed config other than dev seed data
- Audit is append-only
- Every timestamp stored as UTC

---

## Success Criteria

### Functional
- [ ] An operator signs in and the operator appears on records
- [ ] An override without a valid supervisor PIN is rejected
- [ ] 2026-11-03T21:48:22Z displays as 2026-11-04 03:48 in Asia/Dhaka
- [ ] Repeated wrong PINs trigger lockout

### Quality
- [ ] All acceptance criteria of the assigned FRs met
- [ ] Tests per tech-stack standards pass
- [ ] Code reviewed and approved

---

## Bolt Suggestions

Planned bolts (STEP 12) in `memory-bank/bolts/`:

| Bolt | Type | Stories | Workbook |
|---|---|---|---|
| 003-station-access | Simple | 001, 002, 003, 004 | STEP 25 preparation |
