---
unit: 004-device-integration
intent: 001-assisted-weighing-station
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-11-04T15:00:00Z
updated: 2026-11-04T15:00:00Z
---

# Unit Brief: Device Integration

## Purpose

Turn the station devices into reliable ports: live scale readings (value, unit, stable flag), tare commands, scale and scanner status, and scan delivery to the Api.

## Scope

### In Scope
- `IScaleDevice` adapter: list, status, weight, tare; polled at ~250 ms (ADR-004, configurable)
- Invalid-reading detection: missing, non-numeric, below zero beyond the zero band, above capacity, error flag (FR-23)
- Scale offline: weight "—", Confirm and Tare disabled, re-tare on return (FR-23)
- `IScanner` adapter: scanner status; collecting scans from the dummy and handing them to the Api (delivery detail fixed in its bolt, within PD-03)
- Scanner offline: touch selection and typed lot still work (FR-24 criterion b)
- Device health for the status strip; resilience policies

### Out of Scope
- Deciding what a reading means for tolerance (005)
- Choosing the scale for a component (005, FR-8)
- Keyboard-wedge scanning (PD-03: not in V1)

---

## Assigned Requirements

| FR | Requirement | Priority |
|---|---|---|
| FR-23 | Scale unavailable or invalid weight | Must |
| FR-24 (b) | Scanner unavailable | Must |

Full acceptance criteria: `../../requirements.md`. Unit decomposition: `../../units.md`.

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|---|---|---|
| ScaleInfo | A station scale | scaleId, name, capacity, resolution, status |
| Reading | One scale reading | value, unit, stable, validity, atUtc |
| ScanEvent | One scan | code, atUtc |
| DeviceHealth | Status for the strip | device, status, since |

### Key Operations
| Operation | Description | Inputs | Outputs |
|---|---|---|---|
| Read weight | Get the current reading | scaleId | Reading or ScaleUnavailable |
| Tare | Tare a scale | scaleId | tare value or error |
| Get status | Device status | device | DeviceHealth |
| Receive scan | Deliver the next scan | — | ScanEvent |

---

## Story Summary

*To be filled at STEP 11 (stories).*

---

## Dependencies

### Depends On
| Unit | Reason |
|---|---|
| 001-station-simulators | Dummy scale and scanner contracts |

### Depended By
| Unit | Reason |
|---|---|
| 005-weighing-station | Scale port |
| 008-assisted-weighing-station-ui | Status strip, scans |

### External Dependencies
| System | Purpose | Risk |
|---|---|---|
| Scales, scanner (dummies in lab) | Readings, tare, scans | High: models and protocols unconfirmed (Q15, Q16) |

---

## Technical Context

### Suggested Technology
Typed HttpClient adapters with resilience (timeout, retry with jitter, circuit breaker); `IntegrationErrors.ScaleUnavailable` → 503 ProblemDetails.

### Integration Points
| Integration | Type | Protocol |
|---|---|---|
| Dummy Scale API | API | REST/JSON |
| Dummy Scanner | API | REST/JSON |

### Data Storage
| Data | Type | Volume | Retention |
|---|---|---|---|
| None persisted | — | — | Readings are not stored while streaming |

---

## Constraints

- Fixed-point decimals for readings
- Weighing domain never sees HTTP
- Poll interval configurable

---

## Success Criteria

### Functional
- [ ] A rising scenario reaches the UI at ≤ 250 ms intervals
- [ ] An offline scale returns ScaleUnavailable and nothing is recorded
- [ ] Over-capacity and negative readings are flagged invalid
- [ ] A scan emitted on the dummy reaches the Api

### Quality
- [ ] All acceptance criteria of the assigned FRs met
- [ ] Tests per tech-stack standards pass
- [ ] Code reviewed and approved

---

## Bolt Suggestions

Default bolt type: **Simple**. Final bolts are planned at STEP 12.

| Bolt | Type | Objective |
|---|---|---|
| bolt-device-integration-1 | Simple | Scale adapter + readings + tare + health |
| bolt-device-integration-2 | Simple | Scanner adapter + scan delivery |
