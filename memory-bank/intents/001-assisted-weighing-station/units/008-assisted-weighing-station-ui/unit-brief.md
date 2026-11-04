---
unit: 008-assisted-weighing-station-ui
intent: 001-assisted-weighing-station
phase: inception
status: draft
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-11-04T15:00:00Z
updated: 2026-11-04T15:00:00Z
---

# Unit Brief: Assisted Weighing Station UI

## Purpose

The operator terminal. It reproduces the mockup's functional experience (Select batch, Weigh, Confirm & print) **using only the Api**, with no scenario data in components. It is one unit for all screens because they share one session store and one status strip.

## Scope

### In Scope
- Sign-in screen
- Queue view with scan prompt
- Weigh view: recipe rail, readout, progress bar with tolerance band and target line, status line, distinct Target-achieved display
- Lot / container entry; F1–F4 key handlers (Confirm/Print, Re-tare, Next batch, Quit)
- Override dialog (supervisor badge + PIN + reason)
- Completion summary and review list; ticket preview, print, reprint
- Status strip: scales, scanner, printer, OrgaTEX, unsynced count
- Error and offline states
- Transloco (EN); `WeighingSessionStore` (NgRx SignalStore)
- Playwright end-to-end test of B-24187 (STEP 29); closing the ui-analysis gaps

### Out of Scope
- Any business rule (computed by 005 and the others)
- Hard-coded scenario data
- The mockup's surplus calculation (PD-05)

---

## Assigned Requirements

| FR | Requirement | Priority |
|---|---|---|
| — (no FR owned) | Renders all user-facing FRs of 002 to 007; owns the Usability NFRs (F-keys, text labels, Transloco, touch targets ≥ 15 mm) | Must |

Full acceptance criteria: `../../requirements.md`. Unit decomposition: `../../units.md`.

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|---|---|---|
| WeighingSessionStore | Client state for the active session | status, currentComponent, reading, tare, lot, container |
| View models | Queue, rail, summary, ticket preview | from the Api |

### Key Operations
| Operation | Description | Inputs | Outputs |
|---|---|---|---|
| Render | Show Api state | Api responses | Screens |
| Send command | Call the Api | user action | result / error |

---

## Story Summary

*To be filled at STEP 11 (stories).*

---

## Dependencies

### Depends On
| Unit | Reason |
|---|---|
| 002-station-access | Sign-in |
| 003-batch-recipe | Queue, recipe |
| 004-device-integration | Status strip, scans |
| 005-weighing-station | Weighing API |
| 006-consumption-writeback | Sync status |
| 007-ticket-printing | Ticket |

### Depended By
None.

### External Dependencies
None (training substitutes only).

---

## Technical Context

### Suggested Technology
Angular (standalone, signals, zoneless), NgRx SignalStore, SCSS with the prototype's CSS tokens, Angular CDK, Transloco; Vitest + Testing Library; Playwright (TS).

### Integration Points
| Integration | Type | Protocol |
|---|---|---|
| WeighingMS.Api | API | REST/JSON over same-origin /api |

### Data Storage
| Data | Type | Volume | Retention |
|---|---|---|---|
| None persisted in the browser | — | — | State rebuilt from the Api on reload |

---

## Constraints

- Calls only WeighingMS.Api (B0)
- Status never shown by colour alone
- Do not copy the mockup's implementation

---

## Success Criteria

### Functional
- [ ] An operator completes B-24187 end to end against the dummies (Playwright)
- [ ] Every failure state from FR-23/FR-24 is visible and safe
- [ ] All operator text goes through Transloco

### Quality
- [ ] All acceptance criteria of the assigned FRs met
- [ ] Tests per tech-stack standards pass
- [ ] Code reviewed and approved

---

## Bolt Suggestions

Default bolt type: **Simple**. Final bolts are planned at STEP 12.

| Bolt | Type | Objective |
|---|---|---|
| bolt-assisted-weighing-station-ui-1 | Simple | Shell, sign-in, queue view |
| bolt-assisted-weighing-station-ui-2 | Simple | Weigh view + override dialog |
| bolt-assisted-weighing-station-ui-3 | Simple | Completion, ticket, status strip, E2E |
